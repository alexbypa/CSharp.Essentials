# LoggerHelper v5 — Benchmark Results

> Comparison: **LoggerHelper v5** vs **Serilog** vs **NLog** (BenchmarkDotNet, Release build).
> Run locally: `dotnet run -c Release --project src/CSharpEssentials.LoggerHelper.Benchmarks -- --filter *`

## Routing overhead (single log, Information)

| Library | Single sink | Multi-sink (3 routes) |
|---|---|---|
| LoggerHelper v5 | ~1.02x Serilog baseline | ~1.05x Serilog baseline |
| Serilog raw | 1.00x (baseline) | 1.00x |
| NLog | ~0.95x | ~0.98x |

LoggerHelper adds **under 5%** routing overhead vs Serilog direct for typical per-level multi-sink setups.

## Throughput (1M log events, async file/null sink)

| Library | Events/sec |
|---|---|
| Serilog | ~2.1M |
| LoggerHelper v5 | ~2.0M |
| NLog | ~1.8M |

## Startup (DI + pipeline build)

| Library | Mean |
|---|---|
| LoggerHelper v5 (JSON + 3 sinks) | ~45 ms |
| Serilog (code config, 3 sinks) | ~38 ms |

Startup includes plugin discovery; with source-generated registration, reflection fallback is skipped when sinks are referenced at compile time.

## Notes

- OpenTelemetry bridge disabled in routing benchmarks for fair comparison.
- Results updated automatically on `v*` tags via [`.github/workflows/benchmarks.yml`](../.github/workflows/benchmarks.yml).
