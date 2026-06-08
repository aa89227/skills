---
name: csharp-performance
description: |
  Advanced C#/.NET hot-path performance tuning: allocation reduction (Span<T>, Memory<T>,
  stackalloc, ArrayPool, ObjectPool, ref struct, boxing/closure elimination), measurement
  with BenchmarkDotNet and [MemoryDiagnoser], GC tuning (server/workstation/concurrent GC,
  LatencyMode, LOH), hot-path async (ValueTask, pooled state machines), high-performance
  collections (FrozenDictionary, SearchValues, CollectionsMarshal, capacity presizing),
  SIMD/vectorization, and JIT/Native AOT tuning — including what .NET 10 optimizes for free.

  GATED SKILL — do NOT trigger for ordinary C# development. Performance optimization trades
  away readability, simplicity, and maintainability, so it is only worth it when the user has
  EXPLICITLY signalled a real performance need. Trigger ONLY when the user explicitly asks to
  optimize performance, reduce allocations / GC pressure, speed up a measured hot path, hit a
  throughput/latency/SLA target, or interpret a benchmark/profiler result. Trigger phrases:
  "optimize performance", "make this faster", "this is too slow", "reduce allocations",
  "zero-alloc", "hot path", "high throughput", "low latency", "GC pressure", "benchmark",
  "BenchmarkDotNet", "profiler shows", "memory churn". For general "write C# code" or
  "review C#" requests with no stated performance need, do NOT trigger — use
  csharp-best-practices instead.
license: MIT
metadata:
  author: aa89227
  version: "1.0"
  dotnet-version: "10 (LTS, Nov 2025)"
  tags: ["csharp", "dotnet", "performance", "optimization", "allocation", "gc", "benchmark"]
  trigger_keywords: ["performance", "optimize", "allocation", "GC pressure", "hot path", "throughput", "latency", "BenchmarkDotNet", "Span", "zero-alloc", "stackalloc", "ArrayPool"]
---

# C# / .NET Performance Tuning

**Target:** .NET 10 (LTS, Nov 2025), C# 14

## When this skill applies

This skill is **deliberately gated**. Performance work is an *engineering trade-off*: nearly
every technique here costs readability, increases the surface for subtle bugs, or constrains
future change. That cost is only justified when there is a concrete, stated performance need.

Before optimizing anything, confirm at least one of these is true. If none are, stop and tell
the user that the code is likely fine as-is — premature optimization is the more expensive
mistake:

- The user explicitly asked to make something faster, leaner, or higher-throughput.
- There is a measured problem: a profiler trace, a benchmark, a latency/throughput SLA, or
  observed GC pressure.
- The code is a genuine hot path — runs millions of times, on a tight loop, per-request in a
  high-RPS service, or in a real-time/low-latency context.

If the user just wants working, idiomatic code, defer to **csharp-best-practices** instead.

## The optimization workflow

Follow this order. Skipping straight to techniques is how people make code uglier *and* slower.

### 1. Measure first — never guess

The single most important rule. Intuition about C# performance is wrong more often than not,
and .NET's runtime already optimizes aggressively (see `references/dotnet10-runtime.md` — much
of what people hand-optimize is now free). Establish a baseline before touching anything.

- Micro-benchmarks → **BenchmarkDotNet** with `[MemoryDiagnoser]`. This is the standard; do not
  hand-roll `Stopwatch` loops, they produce misleading numbers (no warmup, JIT noise, dead-code
  elimination).
- Whole-app / production → `dotnet-trace`, `dotnet-counters`, the GC metrics, or a sampling
  profiler.

See `references/measure-and-diagnose.md` for setup and how to read the results.

### 2. Find the actual bottleneck

Optimize the hot 5%, not the cold 95%. A profiler tells you where time and allocations actually
go — it is almost never where you guessed. Allocation count and Gen0 rate matter as much as CPU
time, because GC pressure is the most common hidden cost in managed code.

### 3. Know what the runtime already does

.NET 10's JIT and GC eliminate many classic hand-optimizations: small arrays and non-escaping
delegates are stack-allocated automatically, struct args are kept in registers, array iteration
is de-virtualized, Arm64 GC pauses dropped 8–20%. Read `references/dotnet10-runtime.md` before
writing manual workarounds — you may be fighting the runtime for nothing.

### 4. Apply techniques in increasing order of cost

Reach for the cheapest fix that solves the measured problem. Roughly:

1. **Free / low-risk:** presize collections, `sealed` classes, avoid LINQ in tight loops,
   pick the right collection type, reuse buffers you already own.
2. **Moderate:** `Span<T>`/`ReadOnlySpan<T>` slicing, `ArrayPool<T>`, `StringBuilder`,
   interpolated-string handlers, `FrozenDictionary`/`SearchValues` for read-heavy lookups.
3. **Higher cost (readability / lifetime constraints):** `stackalloc`, `ref struct`,
   `ObjectPool`, custom struct enumerators, `CollectionsMarshal`, manual `ValueTask` pooling.
4. **Specialist (only with strong evidence):** SIMD/`Vector<T>`, `[SkipLocalsInit]`,
   `MethodImpl(AggressiveInlining)`, GC mode/latency tuning, Native AOT.

The technique catalog lives in the reference files below.

### 5. Re-measure and check the trade-off

Confirm the optimization actually helped with the same benchmark. Then ask whether the
readability/maintainability cost is worth the measured win. If a change makes code materially
harder to understand for a single-digit-percent gain on a non-critical path, revert it. Document
*why* any non-obvious hot-path code exists, so the next reader doesn't "simplify" it back.

## Reference files

Read the one relevant to the task — don't load all of them.

- **`references/measure-and-diagnose.md`** — BenchmarkDotNet setup, `[MemoryDiagnoser]`,
  reading results, `dotnet-counters`/`dotnet-trace`, GC and allocation diagnostics.
- **`references/reduce-allocations.md`** — `Span<T>`/`Memory<T>`, `stackalloc`, `ArrayPool`,
  `ObjectPool`, `ref struct`, struct vs class, boxing & closure elimination, string handling,
  high-performance collections (`FrozenDictionary`, `SearchValues`, `CollectionsMarshal`).
- **`references/async-and-gc.md`** — `ValueTask`, hot-path async, `IAsyncEnumerable`, GC modes
  (server/workstation/concurrent), `GCSettings.LatencyMode`, LOH, tiered PGO, R2R, Native AOT.
- **`references/dotnet10-runtime.md`** — what .NET 10's runtime optimizes for free (stack
  allocation, escape analysis, devirtualization, write barriers) and new perf-relevant APIs.

## Cheat sheet

| Symptom (from profiler) | First thing to reach for | Reference |
|---|---|---|
| High Gen0 rate, short-lived garbage | `Span`/`stackalloc`, `ArrayPool`, avoid LINQ/closures | reduce-allocations |
| Lots of `string` churn | `StringBuilder`, interpolated-string handlers, `string.Create` | reduce-allocations |
| Boxing (value types on heap) | generics, avoid `object`/non-generic collections | reduce-allocations |
| Slow read-heavy lookups | `FrozenDictionary`/`FrozenSet`, `SearchValues<T>` | reduce-allocations |
| `Task` alloc on sync-completing async | `ValueTask` / `ValueTask<T>` | async-and-gc |
| Long GC pauses, server workload | Server GC, concurrent GC, `LatencyMode` | async-and-gc |
| LOH fragmentation (>85 KB objects) | pool large buffers, `LOHCompactionMode` | async-and-gc |
| Cold-start latency | ReadyToRun, Native AOT, tiered PGO | async-and-gc |
| Numeric loops over large arrays | SIMD `Vector<T>` / `TensorPrimitives` | reduce-allocations |
| "It's slow" with no data | **Stop. Benchmark first.** | measure-and-diagnose |

Golden rule, repeated because it matters most: **measure, optimize the proven bottleneck,
re-measure, and weigh the readability cost every time.**
