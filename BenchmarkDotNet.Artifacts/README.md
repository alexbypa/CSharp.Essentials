# Benchmark Results

Performance tracking for **CSharpEssentials.LoggerHelper** compared against Serilog (baseline) and NLog.

All benchmarks use **no-op sinks** to measure pure framework overhead, not I/O.

## Optimization History

### v4 — API Simplification + ArrayPool Removal (2026-04-18)

**Changes:**
1. **Dropped TraceSync/TraceAsync** — ILogger already supports levels, exceptions, structured properties. TraceAsync was slower than ILogger (1,038 ns vs 659 ns) for no benefit
2. **New API: `BeginTrace` scope** — sets IdTransaction/Action ONCE per operation. All ILogger calls inside inherit enrichment. Cost amortized across N logs
3. **Kept `Trace` single-shot** — for isolated one-off logs (template appending, no scope)
4. **Removed ArrayPool from LoggerHelperLogger** — `Rent(2)` returns size 16 → always forced a copy. Direct `new object?[N]` is faster for small arrays
5. **Fixed CompositeDisposable LIFO bug** — `BeginScope` was disposing in FIFO order, but Serilog's LogContext uses a stack. Properties leaked after scope disposal

**Results (TraceApiBenchmark):**

| Method | Mean | Allocated | Ratio vs Serilog |
|---|---|---|---|
| Serilog_Raw | 269 ns | 456 B | 1.00x |
| NLog_Raw | 102 ns | 216 B | 0.38x |
| LoggerHelper_ILogger | 568 ns | 1,240 B | 2.11x |
| LoggerHelper_ILogger_WithException | 490 ns | 1,152 B | 1.82x |
| **LoggerHelper_BeginTrace_5Logs** | **657 ns/log** | **1,378 B/log** | **2.44x** |
| LoggerHelper_Trace (single-shot) | 1,481 ns | 2,688 B | 5.51x |

**Key insight:** `BeginTrace` + standard ILogger (657 ns/log) is nearly as fast as bare ILogger (568 ns) because the scope cost is amortized. This is the recommended API.

**Results (RoutingBenchmark):**

| Method | v1 (baseline) | v4 | Delta |
|---|---|---|---|
| Single_Info time | 716 ns | 500 ns | **-30%** |
| Single_Info alloc | 1,664 B | 1,152 B | **-31%** |
| Multi_Info time | 842 ns | 570 ns | **-32%** |

**Results (ThroughputBenchmark):**

| Method | v1 (baseline) | v4 | Delta |
|---|---|---|---|
| SingleMessage time | 979 ns | 488 ns | **-50%** |
| SingleMessage alloc | 1,672 B | 1,152 B | **-31%** |
| StructuredPayload time | 1,283 ns | 614 ns | **-52%** |
| StructuredPayload alloc | 1,920 B | 1,296 B | **-32%** |

---

### v3 — TraceSync/TraceAsync API (2026-04-18)

**Changes:**
1. Added TraceSync/TraceAsync extension methods on ILogger
2. Replaced BeginScope with template appending (BeginScope used AsyncLocal → 494,000 ns per log!)
3. Removed unnecessary ArrayPool from LoggerExtensions

**Bug caught:** First implementation used `BeginScope` + `LogContext.PushProperty` (AsyncLocal) — TraceSync was **494,000 ns** (0.5ms per log!). After switching to template appending: **1,479 ns** — a **334x speedup**.

---

### v2 — Memory Optimization (2026-04-18)

**Changes:**
1. Eliminated LINQ in hot path (`ToList()` → `for` loop + `IReadOnlyList<T>`)
2. ArrayPool for values array in LoggerHelperLogger
3. RenderedMessageEnricher now opt-in

---

### v1 — Baseline (2026-04-16)

Initial implementation with `ILogger<T>` provider bridge.
Known issues: LINQ allocations on every log call, RenderedMessageEnricher always active.

---

## Full Results by Version

| Directory | Description |
|---|---|
| [`v1-baseline/`](v1-baseline/) | Original implementation |
| [`v2-memory-optimization/`](v2-memory-optimization/) | IReadOnlyList fast-path + RenderedMessage opt-in |
| [`v3-trace-api/`](v3-trace-api/) | TraceSync/TraceAsync with template appending |
| [`v4-final-optimization/`](v4-final-optimization/) | API simplification + ArrayPool removal + LIFO bugfix |
| [`results/`](results/) | Latest run (working directory for BenchmarkDotNet) |

## Benchmark Classes

- **RoutingBenchmark** — Single-sink vs multi-sink routing overhead per log message
- **ThroughputBenchmark** — Per-message throughput: single message, structured payload, below-min-level filtering
- **StartupBenchmark** — Cold-start initialization cost (DI container + Serilog pipeline creation)
- **TraceApiBenchmark** — BeginTrace scope vs Trace single-shot vs ILogger direct vs Serilog/NLog

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
