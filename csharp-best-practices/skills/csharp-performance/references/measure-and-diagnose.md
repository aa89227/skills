# Measuring & Diagnosing Performance

You cannot optimize what you haven't measured. This file covers how to get trustworthy numbers
*before* changing code, and how to read them.

## Contents
- [BenchmarkDotNet — the standard for micro-benchmarks](#benchmarkdotnet)
- [Reading BenchmarkDotNet output](#reading-output)
- [Why not Stopwatch](#why-not-stopwatch)
- [Whole-app diagnostics: counters & traces](#whole-app)
- [GC & allocation diagnostics](#gc-diagnostics)

<a name="benchmarkdotnet"></a>
## BenchmarkDotNet — the standard for micro-benchmarks

Never hand-roll timing loops for micro-benchmarks. BenchmarkDotNet handles warmup, multiple
iterations, statistical analysis, and protects against dead-code elimination — all of which
hand-rolled `Stopwatch` code gets wrong.

```csharp
// dotnet add package BenchmarkDotNet
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

[MemoryDiagnoser]                 // reports allocations + Gen0/1/2 — usually the real story
public class StringJoinBench
{
    private readonly string[] _items = Enumerable.Range(0, 100).Select(i => i.ToString()).ToArray();

    [Benchmark(Baseline = true)]
    public string Concat()
    {
        var s = "";
        foreach (var i in _items) s += i;   // O(n^2) allocations
        return s;
    }

    [Benchmark]
    public string StringBuilder()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var i in _items) sb.Append(i);
        return sb.ToString();
    }
}

// Program.cs — run in Release, outside the debugger
public static class Program
{
    public static void Main() => BenchmarkRunner.Run<StringJoinBench>();
}
```

**Run it correctly** — Release config, no debugger attached:
```bash
dotnet run -c Release
```

Useful attributes:
- `[MemoryDiagnoser]` — allocations and GC collections per op. Reach for this almost always.
- `[Params(10, 100, 1000)]` on a field — sweep input sizes; perf characteristics change with N.
- `[SimpleJob(RuntimeMoniker.Net90)]` + `[SimpleJob(RuntimeMoniker.Net10_0)]` — compare runtimes.
- `[DisassemblyDiagnoser]` — emits the generated asm when you need to see what the JIT did.
- `[ThreadingDiagnoser]` — lock contention and completed work items.

<a name="reading-output"></a>
## Reading BenchmarkDotNet output

```
| Method        | Mean       | Ratio | Gen0     | Allocated |
|-------------- |-----------:|------:|---------:|----------:|
| Concat        | 4,812.0 ns |  1.00 |  10.1471 |   63.7 KB |
| StringBuilder |   612.4 ns |  0.13 |   0.3128 |    1.9 KB |
```

- **Mean** — average time per operation. **Ratio** — relative to the `Baseline = true` method.
- **Gen0/1/2** — GC collections per 1,000 ops. High Gen0 = lots of short-lived garbage; this is
  the most common hidden cost and the thing `[MemoryDiagnoser]` exists to surface.
- **Allocated** — managed bytes per op. **Often the number that matters most** — driving this to
  zero on a hot path frequently does more for tail latency than shaving nanoseconds, because it
  removes GC pauses entirely.
- Watch the **StdDev / Error** columns. High variance means the benchmark is noisy or the
  workload isn't isolated — don't trust a 5% difference if the error bars are 10%.

<a name="why-not-stopwatch"></a>
## Why not Stopwatch

A naive `Stopwatch` loop is misleading because:
- No warmup — you measure JIT compilation and tiered-compilation transitions, not steady state.
- The JIT can **eliminate** code whose result is unused, so you "measure" nothing.
- No statistical treatment — one outlier (a GC, a context switch) skews the result.
- Resolution and overhead of the timer itself contaminate sub-microsecond measurements.

If you only need a rough whole-operation timing (not a micro-benchmark), `Stopwatch.GetTimestamp()`
with `Stopwatch.GetElapsedTime(start)` is fine — but consume the result (log it, return it).

<a name="whole-app"></a>
## Whole-app diagnostics: counters & traces

For production or whole-application analysis, micro-benchmarks don't help. Use the dotnet
diagnostic tools (`dotnet tool install -g dotnet-counters dotnet-trace`):

```bash
# Live counters — GC, heap, threadpool, exceptions, lock contention
dotnet-counters monitor -p <pid> --counters System.Runtime

# Capture a trace for offline analysis (open in PerfView / Speedscope / VS)
dotnet-trace collect -p <pid> --profile cpu-sampling
dotnet-trace collect -p <pid> --providers Microsoft-DotNETCore-SampleProfiler
```

Key `System.Runtime` counters to watch:
- `dotnet.gc.heap.total_allocated` / allocation rate — the allocation firehose.
- `dotnet.gc.pause.time` — total time app threads were paused by GC (the honest GC-overhead
  metric; `% Time in GC` is easy to misread).
- `dotnet.thread_pool.queue.length` — rising queue = thread-pool starvation (often from sync-
  over-async blocking).
- `dotnet.monitor.lock_contentions` — lock contention rate.

<a name="gc-diagnostics"></a>
## GC & allocation diagnostics

- **Find allocation sources:** a `dotnet-trace` with the GC provider, opened in PerfView, gives
  an allocation-by-call-stack view — the fastest way to find *what* is allocating.
- **GC stats in a benchmark:** `[MemoryDiagnoser]` is enough for micro level.
- **Quick in-process check:** `GC.GetTotalAllocatedBytes()` before/after a workload gives a
  cheap allocation delta when you can't attach tooling.
- **GC collection counts:** `GC.CollectionCount(0/1/2)` to see generation pressure.

Always profile a **Release** build. Debug builds disable optimizations and inlining, so their
numbers are meaningless for performance decisions.
