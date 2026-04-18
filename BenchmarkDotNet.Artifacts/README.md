# Benchmark Results

Performance tracking for **CSharpEssentials.LoggerHelper** compared against Serilog (baseline) and NLog.

All benchmarks use **no-op sinks** to measure pure framework overhead, not I/O.

## Optimization History

### v3 — TraceSync/TraceAsync API (2026-04-18)

**Changes:**
1. **Added TraceSync/TraceAsync extension methods** on `ILogger` — port of the original `loggerExtension<T>` API with IdTransaction, Action, SpanName enrichment
2. **Replaced BeginScope with template appending** — `BeginScope` uses `AsyncLocal<T>` which copies `ExecutionContext` on every push/pop, causing ~500x overhead in hot paths. Switched to appending `{IdTransaction} {Action}` directly to the message template (same approach as the original)
3. **Removed unnecessary ArrayPool** from `LoggerExtensions` — for small arrays (4-5 elements) direct allocation is faster than Rent/Return overhead

**Results (TraceApiBenchmark):**

| Method | Mean | Allocated | Ratio vs Serilog |
|---|---|---|---|
| Serilog_Raw | 235 ns | 456 B | 1.00x |
| NLog_Raw | 74 ns | 216 B | 0.32x |
| LoggerHelper_ILogger | 659 ns | 1,240 B | 2.82x |
| LoggerHelper_TraceAsync | 1,038 ns | 1,671 B | 4.44x |
| LoggerHelper_TraceSync | 1,479 ns | 2,688 B | 6.33x |

**Bug caught:** First implementation used `BeginScope` + `LogContext.PushProperty` (AsyncLocal) — TraceSync was **494,000 ns** (0.5ms per log!). After switching to template appending: **1,479 ns** — a **334x speedup**.

---

### v2 — Memory Optimization (2026-04-18)

**Changes:**
1. **Eliminated LINQ in hot path** — `ToList()` + `Where().Select().ToArray()` replaced with `for` loop + `IReadOnlyList<T>` index access
2. **ArrayPool for values array** — `new object[]` per-log replaced with `ArrayPool<object?>.Shared.Rent/Return` in `LoggerHelperLogger`
3. **RenderedMessageEnricher now opt-in** — was rendering a string on every log event even when no sink needed it

**Results (RoutingBenchmark — Single Info):**

| Metric | v1 | v2 | Delta |
|---|---|---|---|
| Mean time | 716 ns | 507 ns | **-29%** |
| Allocated | 1,664 B | 1,152 B | **-31%** |
| Alloc Ratio vs Serilog | 4.33x | 3.00x | |
| Gen0 / 1000 ops | 0.1984 | 0.1373 | **-31%** |

**Results (ThroughputBenchmark — Structured Payload):**

| Metric | v1 | v2 | Delta |
|---|---|---|---|
| Mean time | 1,283 ns | 691 ns | **-46%** |
| Allocated | 1,920 B | 1,296 B | **-32%** |
| Alloc Ratio vs Serilog | 5.00x | 3.38x | |

---

### v1 — Baseline (2026-04-16)

Initial implementation with `ILogger<T>` provider bridge.
Known issues: LINQ allocations on every log call, RenderedMessageEnricher always active.

---

## Full Results by Version

Each subdirectory contains the complete BenchmarkDotNet reports (Markdown, CSV, HTML):

| Directory | Description |
|---|---|
| [`v1-baseline/`](v1-baseline/) | Original implementation |
| [`v2-memory-optimization/`](v2-memory-optimization/) | ArrayPool + IReadOnlyList fast-path |
| [`v3-trace-api/`](v3-trace-api/) | TraceSync/TraceAsync with template appending |
| [`results/`](results/) | Latest run (working directory for BenchmarkDotNet) |

## Benchmark Classes

- **RoutingBenchmark** — Single-sink vs multi-sink routing overhead per log message
- **ThroughputBenchmark** — Per-message throughput: single message, structured payload (3 props), below-min-level filtering
- **StartupBenchmark** — Cold-start initialization cost (DI container + Serilog pipeline creation)
- **TraceApiBenchmark** — TraceSync/TraceAsync vs ILogger direct vs Serilog/NLog

## How to Run

```bash
# Light run (~2 min, low CPU/RAM):
dotnet run -c Release --project src/CSharpEssentials.LoggerHelper.Benchmarks --framework net9.0 -- --filter * --job short --exporters github

# Full run (accurate, for publishing):
dotnet run -c Release --project src/CSharpEssentials.LoggerHelper.Benchmarks --framework net9.0 -- --filter * --exporters github

# Single benchmark class only:
dotnet run -c Release --project src/CSharpEssentials.LoggerHelper.Benchmarks --framework net9.0 -- --filter *TraceApi* --job short --exporters github
```

## Parameter Reference

| Flag | Effect | Example |
|---|---|---|
| `--job short` | 1 launch, 3 warmup, 3 iterations (~2 min) | Low CPU usage |
| `--job default` | 1 launch, auto warmup, ~15-100 iterations | Accurate results |
| `--filter *Name*` | Run only matching benchmarks | `--filter *Routing*` |
| `--exporters github` | Generate GitHub-flavored Markdown report | For README/docs |

## Environment

- **CPU:** AMD Ryzen 5 5600G, 6 cores / 12 threads
- **OS:** Windows 11
- **Runtime:** .NET 9.0 (RyuJIT AVX2)
- **BenchmarkDotNet:** v0.14.0
