# LoggerHelper v5 — Benchmark Results

> Generated: 2026-10-07 | Runtime: .NET 9 | OS: ubuntu-latest

Comparison: **LoggerHelper v5** vs **Serilog** (baseline) vs **NLog**.
All frameworks use a no-op sink/target — measures framework overhead, not I/O.

---

## Throughput

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 9.0.20 (9.0.2026.41315), X64 RyuJIT AVX2
  ShortRun : .NET 9.0.20 (9.0.2026.41315), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean       | Error       | StdDev     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |-----------:|------------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Serilog_BelowMinLevel          |   5.109 ns |   0.0491 ns |  0.0027 ns |  0.03 |    0.00 |    1 |      - |         - |        0.00 |
| NLog_BelowMinLevel             |  27.903 ns |   5.3986 ns |  0.2959 ns |  0.16 |    0.00 |    2 |      - |         - |        0.00 |
| LoggerHelper_BelowMinLevel     |  41.438 ns |   6.5389 ns |  0.3584 ns |  0.23 |    0.00 |    3 | 0.0033 |      56 B |        0.15 |
| NLog_SingleMessage             |  97.531 ns |  29.4764 ns |  1.6157 ns |  0.55 |    0.01 |    4 | 0.0105 |     176 B |        0.46 |
| NLog_StructuredPayload         | 121.711 ns |   6.9762 ns |  0.3824 ns |  0.69 |    0.01 |    5 | 0.0134 |     224 B |        0.58 |
| Serilog_SingleMessage          | 176.547 ns |  27.9100 ns |  1.5298 ns |  1.00 |    0.01 |    6 | 0.0229 |     384 B |        1.00 |
| Serilog_StructuredPayload      | 268.379 ns |  19.5960 ns |  1.0741 ns |  1.52 |    0.01 |    7 | 0.0296 |     496 B |        1.29 |
| LoggerHelper_SingleMessage     | 613.557 ns | 448.2069 ns | 24.5677 ns |  3.48 |    0.12 |    8 | 0.0687 |    1152 B |        3.00 |
| LoggerHelper_StructuredPayload | 750.733 ns |  38.6638 ns |  2.1193 ns |  4.25 |    0.03 |    8 | 0.0772 |    1296 B |        3.38 |

---

## Routing Overhead

_Results not available._

---

## Startup Time

_Results not available._

---

## Emit Overhead

_Results not available._

---

## Sink Routing Match

_Results not available._

---

## Sensitive Data Masking

_Results not available._

---

## MCP Tools

_Results not available._

---

## Sampling

_Results not available._

---

_Benchmarks run automatically on each release via [GitHub Actions](../.github/workflows/benchmarks.yml)._
