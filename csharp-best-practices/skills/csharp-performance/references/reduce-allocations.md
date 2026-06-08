# Reducing Allocations

Most managed-code performance problems are allocation problems: short-lived garbage drives Gen0
collections, which cause pauses and cache churn. Driving allocations toward zero on a hot path is
usually the highest-leverage optimization. Every technique here has a cost — use it only on a
measured hot path (see the gating rules in SKILL.md).

## Contents
- [Span<T> and ReadOnlySpan<T>](#span)
- [stackalloc](#stackalloc)
- [Memory<T>](#memory)
- [ArrayPool<T>](#arraypool)
- [ObjectPool<T>](#objectpool)
- [struct vs class, and ref struct](#struct)
- [Boxing](#boxing)
- [Closures and lambda allocations](#closures)
- [LINQ on hot paths](#linq)
- [String handling](#strings)
- [High-performance collections](#collections)
- [SIMD / vectorization](#simd)

<a name="span"></a>
## Span<T> and ReadOnlySpan<T>

A `Span<T>` is a stack-only view over contiguous memory (array, `stackalloc`, string, native
memory). Slicing allocates nothing — it's just a pointer + length — so it replaces substring/
sub-array copies on hot paths.

```csharp
// Parse "key=value" with zero allocations
static (ReadOnlySpan<char> Key, ReadOnlySpan<char> Value) SplitPair(ReadOnlySpan<char> input)
{
    int eq = input.IndexOf('=');
    return (input[..eq], input[(eq + 1)..]);   // slices, no string allocated
}
```

- Prefer `ReadOnlySpan<char>` parameters over `string` for parsing helpers — callers can pass a
  slice of a larger string without copying.
- Many BCL APIs have span overloads: `int.Parse(ReadOnlySpan<char>)`, `Utf8Parser`,
  `span.IndexOf/Contains/Split`, `Encoding.GetBytes(span, span)`.
- **Constraint:** `Span<T>` is a `ref struct` — it lives on the stack only. It cannot be a field
  of a class, captured in a lambda/closure, used across an `await`, or stored on the heap. When
  you need heap-storable memory, use `Memory<T>`.

<a name="stackalloc"></a>
## stackalloc

Allocate a small buffer on the stack — zero heap allocation, zero GC:

```csharp
Span<char> buffer = stackalloc char[64];
bool ok = value.TryFormat(buffer, out int written);
return new string(buffer[..written]);
```

- **Keep it small and bounded** — the stack is ~1 MB. Never `stackalloc` an attacker- or input-
  controlled size; guard with a threshold and fall back to `ArrayPool` for larger sizes:
  ```csharp
  Span<byte> buf = len <= 256 ? stackalloc byte[256] : new byte[len];
  ```
- Pair with `[SkipLocalsInit]` (method or assembly level) to skip zero-initialization of locals
  when you immediately overwrite the buffer — measurable on very hot paths, but only safe when
  you never read before writing.

<a name="memory"></a>
## Memory<T>

`Memory<T>`/`ReadOnlyMemory<T>` is the heap-storable cousin of `Span<T>`: it *can* be a field,
captured, and held across `await`. Get a `Span<T>` from it (`memory.Span`) only at the point of
synchronous use. Use it for async pipelines (`Stream.ReadAsync(Memory<byte>)`,
`PipeReader`/`PipeWriter`) where spans can't cross the await boundary.

<a name="arraypool"></a>
## ArrayPool<T>

Rent reusable buffers instead of allocating throwaway arrays. Ideal for transient buffers in
serialization, I/O, and parsing.

```csharp
byte[] buffer = ArrayPool<byte>.Shared.Rent(size);   // may return a LARGER array than requested
try
{
    var span = buffer.AsSpan(0, size);               // slice to the size you asked for
    // ... use span ...
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);           // always return, even on exception
}
```

- The rented array length is **≥** requested — always slice to your actual size.
- Use `Return(buffer, clearArray: true)` if it held sensitive data.
- Never use a buffer after returning it; never return the same buffer twice. These are
  use-after-free-class bugs in managed code and corrupt the pool for everyone.

<a name="objectpool"></a>
## ObjectPool<T>

For pooling expensive-to-construct reference objects (e.g. `StringBuilder`, parser state).
`Microsoft.Extensions.ObjectPool`:

```csharp
var pool = new DefaultObjectPoolProvider().CreateStringBuilderPool();
var sb = pool.Get();
try { /* use sb */ }
finally { pool.Return(sb); }   // Return resets the StringBuilder
```

Only pool when construction cost or allocation rate is *proven* significant — pooling adds
lifetime-management complexity and its own synchronization cost.

<a name="struct"></a>
## struct vs class, and ref struct

- A `struct` lives inline (on the stack or embedded in its container) — no heap allocation, no GC
  tracking. Use for small, short-lived, immutable values. Keep them small (rule of thumb ≤ 3-4
  fields / 16-24 bytes); large structs are expensive to copy and can be *slower* than a class.
- Prefer `readonly struct` so the compiler can skip defensive copies, and `readonly record struct`
  for value-equality data holders.
- Pass large structs by `in`/`ref` to avoid copies; return by `ref`/`ref readonly` where it makes
  sense.
- `ref struct` (like `Span<T>`) is guaranteed stack-only — use for types that must never escape to
  the heap. In C# 13+ a `ref struct` can implement interfaces and be used as a generic type
  argument constrained with `allows ref struct`.

The wrong call here regresses performance — measure. .NET 10's escape analysis also stack-allocates
many non-escaping reference objects automatically (see dotnet10-runtime.md), so a `class` is often
fine.

<a name="boxing"></a>
## Boxing

Boxing copies a value type to the heap when it's treated as `object` or a non-generic interface —
a silent per-call allocation. Common sources:

- Value types in non-generic collections (`ArrayList`, `Hashtable`) — use `List<T>`/`Dictionary<,>`.
- `object`-typed APIs and `string.Format`/old logging — prefer generic or interpolated-string APIs.
- Calling an interface method on a value type via a non-generic constraint.
- `enum` to `Enum`/`object`; `struct` implementing an interface accessed through the interface.

Use generics with constraints (`where T : ...`) so the JIT specializes for value types and avoids
boxing entirely. `[MemoryDiagnoser]` allocations that you "didn't write" are usually boxing.

<a name="closures"></a>
## Closures and lambda allocations

A lambda that captures local variables allocates a closure object (and a delegate) each time it's
reached. On a hot path:

- **Hoist** the lambda to a `static` method or `static` lambda (`static () => ...`, C# 9+) so it
  can't capture and is cached.
- Use overloads that take a **state argument** to avoid capture:
  `dict.GetOrAdd(key, static (k, arg) => Create(k, arg), state)`.
- `Where`/`Select` with a capturing predicate inside a per-request loop is a frequent, invisible
  allocation source — see LINQ below.

<a name="linq"></a>
## LINQ on hot paths

LINQ is excellent for readability and fine for cold code. On a hot path it allocates: enumerators,
closures for predicates, and intermediate sequences. A plain `for`/`foreach` loop allocates
nothing and the JIT optimizes it well.

- Replace `list.Where(...).Select(...).ToList()` in a tight loop with a single explicit loop.
- `.Count()`, `.Any()`, `.First()` on an already-materialized `List<T>`/array still create an
  enumerator — use `.Count`/indexer/`Length` directly.
- Keep LINQ in setup/cold code where clarity wins. Only de-LINQ a path the profiler flagged.

<a name="strings"></a>
## String handling

Strings are immutable, so naive concatenation allocates repeatedly.

- **Loop concatenation** (`s += x`) is O(n²) allocations — use `StringBuilder` (presize with
  capacity if known).
- **Interpolated-string handlers** (C# 10+) mean `$"{a}{b}"` no longer always builds a temporary
  string — `StringBuilder.Append($"...")` and `string.Create` use handlers to format directly
  into a buffer. Logging frameworks use this so disabled log levels cost ~nothing.
- **`string.Create`** formats directly into a pre-sized buffer with no intermediate string:
  ```csharp
  string id = string.Create(len, state, static (span, s) => { /* write into span */ });
  ```
- **`TryFormat`** on numbers/dates writes into a `Span<char>`/UTF-8 `Span<byte>` with no string.
- For UTF-8 work, stay in `ReadOnlySpan<byte>` and use UTF-8 literals (`"abc"u8`) instead of
  encoding strings.

<a name="collections"></a>
## High-performance collections

- **Presize** when the count is known: `new List<T>(capacity)`, `new Dictionary<,>(capacity)`.
  Growth reallocates and copies; presizing removes that entirely.
- **`FrozenDictionary<TKey,TValue>` / `FrozenSet<T>`** (`System.Collections.Frozen`) — build once
  (slightly more expensive), then read with the fastest possible lookups. Ideal for lookup tables
  built at startup and never mutated.
- **`SearchValues<T>`** — precomputed set for `span.IndexOfAny(searchValues)`; far faster than
  repeated `IndexOfAny(char[])` or multiple comparisons. In .NET 9+ it also supports substring
  sets (`SearchValues<string>`).
- **`CollectionsMarshal`** — advanced, escape-hatch access:
  - `GetValueRefOrAddDefault(dict, key, out exists)` — single lookup to get-or-add, avoids the
    double hashing of `ContainsKey` + indexer.
  - `AsSpan(list)` — iterate a `List<T>` as a `Span<T>` with no bounds-check overhead per access.
  - These bypass safety checks; don't mutate the collection while holding the ref/span.
- **`Dictionary` alternatives:** `ConcurrentDictionary` for thread-safe access;
  `FrozenDictionary` for read-only; a sorted array + binary search for tiny, cache-friendly sets.

<a name="simd"></a>
## SIMD / vectorization

For numeric work over large arrays, single-instruction-multiple-data can give multiples of
throughput — specialist territory, justify with a benchmark.

- **`TensorPrimitives`** (`System.Numerics.Tensors`) — ready-made vectorized elementwise math
  (`Add`, `Multiply`, `Dot`, `CosineSimilarity`, …). Prefer this over hand-written intrinsics; it
  picks the best instruction set at runtime.
- **`Vector<T>`** — portable SIMD; loop in `Vector<T>.Count` strides with a scalar remainder tail.
- **`Vector128/256/512<T>`** and `System.Runtime.Intrinsics.*` — explicit instruction sets when you
  need precise control; gate on `Vector256.IsHardwareAccelerated`. .NET 10 adds AVX10.2 intrinsics.
