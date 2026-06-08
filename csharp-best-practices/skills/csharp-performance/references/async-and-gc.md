# Hot-Path Async & GC Tuning

Covers async patterns that matter for throughput/latency, and the GC/JIT knobs worth turning —
all of which are specialist, evidence-required changes. Defer to the general async guidance in
the `csharp-best-practices` skill for everyday correctness (CancellationToken propagation,
`ConfigureAwait`, avoiding `async void`); this file is only about the *performance* dimension.

## Contents
- [ValueTask and ValueTask<T>](#valuetask)
- [Async hot-path allocation](#async-alloc)
- [IAsyncEnumerable](#iasyncenumerable)
- [GC modes](#gc-modes)
- [GC latency modes](#latency)
- [Large Object Heap](#loh)
- [JIT & startup: tiered PGO, R2R, Native AOT](#jit)

<a name="valuetask"></a>
## ValueTask and ValueTask<T>

Every `async Task<T>` that completes **synchronously** still allocates a `Task<T>` object.
`ValueTask<T>` avoids that allocation when the result is already available — valuable for hot
paths that hit a cache most of the time:

```csharp
public ValueTask<User> GetUserAsync(int id)
{
    if (_cache.TryGetValue(id, out var user))
        return new ValueTask<User>(user);          // no allocation on the hot (cache-hit) path
    return new ValueTask<User>(LoadFromDbAsync(id)); // wraps the real Task on the slow path
}
```

Rules that keep `ValueTask` safe (it is more fragile than `Task`):
- **Await it exactly once.** Do not await twice, do not store it, do not call `.Result`/`.GetAwaiter().GetResult()` before completion.
- If you must consume it more than once or fan it out, convert with `.AsTask()` first.
- Use `ValueTask` (non-generic) for hot async methods that usually complete synchronously and
  return no value.
- For high-frequency producers, `IValueTaskSource`/`ManualResetValueTaskSourceCore` enables a
  poolable, zero-alloc backing — this is what `Socket`/`Channel` use internally. Specialist; only
  with a measured need.

Don't blanket-replace `Task` with `ValueTask` — for methods that almost always complete
asynchronously, `Task` is simpler and the allocation is unavoidable anyway.

<a name="async-alloc"></a>
## Async hot-path allocation

- An `async` method that actually suspends allocates a state machine (boxed). Methods that
  frequently complete synchronously benefit most from `ValueTask`; ones that always suspend can't
  avoid it.
- Avoid `async`/`await` purely to re-wrap a single returned task — return the task directly when
  no work follows the await (but keep `await` if you need the try/finally or using semantics).
- In libraries, `ConfigureAwait(false)` avoids capturing/restoring the synchronization context —
  primarily a correctness/deadlock concern, but it also trims continuation overhead.
- Reuse `CancellationTokenSource` carefully; creating one with a timeout per call allocates a timer.

<a name="iasyncenumerable"></a>
## IAsyncEnumerable

Streaming `await foreach` avoids buffering an entire result set in memory — important for large or
unbounded sequences. Performance notes:

- Flow cancellation with `.WithCancellation(token)` and disable context capture with
  `.ConfigureAwait(false)` on the stream.
- Each `MoveNextAsync` that suspends has the usual async cost; for very chatty sources, batch.

<a name="gc-modes"></a>
## GC modes

The GC flavour is a deployment-level decision with large throughput/latency impact. Configure in
the `.csproj` or `runtimeconfig.json`:

```xml
<PropertyGroup>
  <ServerGarbageCollection>true</ServerGarbageCollection>   <!-- Server GC -->
  <ConcurrentGarbageCollection>true</ConcurrentGarbageCollection>
</PropertyGroup>
```

- **Workstation GC** (default for client apps): single heap, tuned for low latency on one core and
  small memory footprint. Right for desktop/CLI and low-core containers.
- **Server GC** (default for ASP.NET Core): a heap + dedicated GC thread **per core**, far higher
  allocation throughput. Right for multi-core server workloads — but uses more memory, and in a
  CPU-constrained container it can hurt. In small containers, consider Workstation GC or limit
  heap count with `DOTNET_GCHeapCount`.
- **Concurrent / background GC** (on by default): performs Gen2 collection concurrently with the
  app to cut pause times. Keep on for latency-sensitive services.
- **`DOTNET_GCDynamicAdaptationMode` / DATAS** (.NET 8+, default on in .NET 9/10): dynamically
  adapts heap count to load, reducing memory on Server GC. Usually leave on.

Always validate a GC change against your real workload and container limits — the "right" mode is
workload-specific.

<a name="latency"></a>
## GC latency modes

For short, latency-critical regions, hint the GC to avoid disruptive collections:

```csharp
var old = GCSettings.LatencyMode;
GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency; // suppress most Gen2 collections
try { /* latency-critical region */ }
finally { GCSettings.LatencyMode = old; }
```

- `SustainedLowLatency` — avoid full blocking Gen2 GCs while keeping the app healthy.
- `GCLatencyMode.NoGCRegion` via `GC.TryStartNoGCRegion(totalBytes)` — guarantees no GC for a
  bounded allocation budget (e.g. per trading tick); ends when you exit or exceed the budget.
- Never call `GC.Collect()` to "optimize" — it almost always hurts. Reserve it for benchmark
  setup/teardown.

<a name="loh"></a>
## Large Object Heap

Objects ≥ 85,000 bytes go on the LOH, which is collected only on Gen2 and (by default) not
compacted — leading to fragmentation under churn of large buffers.

- **Pool large buffers** (`ArrayPool<T>.Shared` handles this) instead of allocating big arrays
  repeatedly.
- Avoid crossing the 85 KB threshold for transient buffers; chunk large work.
- Compact on demand when fragmentation is proven:
  ```csharp
  GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
  GC.Collect();   // the one legitimate manual collect — triggers the compaction
  ```

<a name="jit"></a>
## JIT & startup: tiered PGO, R2R, Native AOT

Trade-offs between startup latency, steady-state throughput, and deployment constraints:

- **Tiered compilation** (on by default): methods start at quick Tier-0, hot ones recompile to
  optimized Tier-1. **Tiered PGO** (on by default in .NET 8+) adds dynamic profile-guided
  optimization for better Tier-1 code — leave it on; it usually wins.
- **ReadyToRun (R2R)** — AOT-precompile to native at publish to cut JIT work at startup:
  `<PublishReadyToRun>true</PublishReadyToRun>`. Bigger binary, faster cold start, slightly lower
  peak (Tier-1 still kicks in). Good for serverless/short-lived processes.
- **Native AOT** — fully ahead-of-time, no JIT, no runtime: fastest startup, smallest memory, but
  no runtime codegen (limits reflection/`Emit`), larger to build, and trimming-sensitive.
  `<PublishAot>true</PublishAot>`. Best for CLIs, serverless, and tight-footprint containers; not
  for plugin/reflection-heavy apps.
- `[MethodImpl(MethodImplOptions.AggressiveInlining)]` — force-inline a tiny, very-hot method when
  the profiler shows call overhead dominates. Use sparingly; the JIT usually decides well, and
  over-inlining bloats code and hurts the instruction cache.

.NET 10's JIT improved inlining, devirtualization, code layout, and stack allocation
substantially — re-measure on .NET 10 before adding manual JIT hints (see dotnet10-runtime.md).
