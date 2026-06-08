# What .NET 10 Optimizes For Free

Read this **before writing manual optimizations**. .NET 10's JIT and GC eliminate many classic
hand-tunings — if you hand-roll a workaround the runtime already does, you add complexity for no
gain and sometimes *block* the runtime's own optimization. Re-benchmark on .NET 10 before
concluding you need a manual fix.

Source: *What's new in the .NET 10 runtime* and *.NET 10 libraries* (Microsoft Learn).

## Contents
- [Stack allocation & escape analysis](#stackalloc)
- [JIT improvements](#jit)
- [GC improvements](#gc)
- [SIMD: AVX10.2](#avx)
- [Perf-relevant new library APIs](#libraries)
- [Practical implications](#implications)

<a name="stackalloc"></a>
## Stack allocation & escape analysis (the big one)

The JIT now stack-allocates far more objects when *escape analysis* proves they don't outlive the
method. This reduces GC-tracked objects and unlocks further optimizations (e.g. replacing an
object with its scalar fields in registers).

New in .NET 10:
- **Small fixed-size arrays of value types** that don't escape — e.g. `int[] numbers = {1,2,3}`
  inside a method is now on the stack, no heap allocation.
- **Small fixed-size arrays of reference types** — e.g. `string[] words = {"Hello","World!"}`
  scoped to a method is now stack-allocated (extends .NET 9's box stack-allocation).
- **Local struct fields** — an array referenced by a non-escaping struct's field no longer
  escapes, so it can be stack-allocated.
- **Delegates / closures** — a `Func` whose closure doesn't escape the method is stack-allocated.
  (The closure object itself still heap-allocates for now; full closure stack-allocation is
  planned for a later release.)

**Implication:** the reflexive "avoid small temporary arrays / local lambdas because they
allocate" advice is weaker on .NET 10. Write the clear version, benchmark with `[MemoryDiagnoser]`,
and only intervene if allocations actually show up. Keeping objects from *escaping* (don't store
to fields, don't pass to non-inlined methods) is what enables these wins.

<a name="jit"></a>
## JIT improvements

- **Array interface devirtualization** — iterating an array via `IEnumerable<T>`/`IList<T>` can now
  be devirtualized and inlined, so `foreach` over an array exposed as an interface is much closer
  to a direct loop. Reduces the old penalty for typing a field as `IReadOnlyList<T>`.
- **Array enumeration de-abstraction** — lower overhead enumerating arrays via enumerators;
  conditional escape analysis can stack-allocate the enumerator.
- **Improved code layout** — block ordering modeled as an asymmetric TSP with a 3-opt heuristic:
  denser hot paths, shorter branches.
- **Improved loop inversion** — graph-based natural-loop recognition turns `while` into hoisted
  `do-while` more precisely, enabling more loop cloning/unrolling.
- **Inlining improvements** — can inline methods that become devirtualizable *after* an earlier
  inline; can inline some `try-finally` methods; uses profile data to relax size limits for hot
  callees; no longer marks unprofitable callees `NoInlining` (which previously pessimized hot
  callers). Heuristics also favor methods returning small fixed-size arrays (to enable stack
  allocation).
- **Struct args in registers (physical promotion)** — struct members are passed directly in
  shared registers without a store-then-load round-trip through memory. Passing small structs by
  value is cheaper; the old "wrap in a class to avoid struct copies" reflex is less often needed.

**Implication:** re-measure before adding `[MethodImpl(AggressiveInlining)]` or restructuring code
to avoid interfaces/struct-copies — the JIT likely already handles it.

<a name="gc"></a>
## GC improvements

- **Arm64 write-barrier improvements** — the new default write-barrier handles GC regions more
  precisely; benchmarks show **GC pause improvements of 8% to over 20%** on Arm64 with the new
  defaults (a small write-barrier throughput cost). Relevant for Arm64 servers (Graviton, Ampere,
  Apple Silicon dev machines).
- DATAS (dynamic heap-count adaptation for Server GC, default on since .NET 9) continues to reduce
  memory footprint under variable load.

<a name="avx"></a>
## SIMD: AVX10.2

New intrinsics in `System.Runtime.Intrinsics.X86.Avx10v2`. Support is **disabled by default**
because AVX10.2-capable hardware isn't broadly available yet — don't depend on it in production
paths; prefer `TensorPrimitives`/`Vector<T>` which pick the best available instruction set
automatically.

<a name="libraries"></a>
## Perf-relevant new library APIs

- **JSON (`System.Text.Json`):**
  - `PipeReader` support for reading — more efficient streaming deserialization.
  - Strict serialization options and duplicate-property rejection (correctness + avoids surprise
    re-parsing).
- **Numerics / Tensors** — continued `TensorPrimitives` expansion for vectorized elementwise math
  (use for embeddings, similarity, signal processing).
- **Collections / serialization / diagnostics** — assorted new APIs; check *What's new in the
  .NET 10 libraries* when a specific type is in your hot path.
- **Diagnostics metrics** — `dotnet.gc.pause.time` and the modern `System.Runtime` meters give a
  truer GC-overhead picture than the legacy `% Time in GC` counter (see measure-and-diagnose.md).

<a name="implications"></a>
## Practical implications — how this changes your approach

1. **Benchmark on .NET 10 specifically.** Numbers and allocation counts from .NET 6/8 may no
   longer hold; an allocation you optimized away years ago may now be free.
2. **Write the clear version first, then measure.** The runtime rewards non-escaping, inlinable,
   straightforward code with automatic stack allocation and devirtualization. Convoluted manual
   workarounds can *prevent* these optimizations.
3. **Help escape analysis:** keep locals from escaping (don't stash into fields, don't pass to
   methods the JIT won't inline) and prefer `static` lambdas — that's what unlocks the free wins.
4. **Reserve manual techniques** (stackalloc, pooling, aggressive inlining, struct gymnastics) for
   paths where `[MemoryDiagnoser]` on .NET 10 *still* shows a problem after the runtime's help.
