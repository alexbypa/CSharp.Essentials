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
| Method                         | Mean       | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |-----------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Serilog_BelowMinLevel          |   5.541 ns |   0.0886 ns | 0.0049 ns |  0.03 |    0.00 |    1 |      - |         - |        0.00 |
| NLog_BelowMinLevel             |  26.007 ns |   1.4031 ns | 0.0769 ns |  0.13 |    0.00 |    2 |      - |         - |        0.00 |
| LoggerHelper_BelowMinLevel     |  42.002 ns |   9.9326 ns | 0.5444 ns |  0.21 |    0.00 |    3 | 0.0033 |      56 B |        0.15 |
| NLog_SingleMessage             |  98.695 ns |  10.1676 ns | 0.5573 ns |  0.50 |    0.01 |    4 | 0.0105 |     176 B |        0.46 |
| NLog_StructuredPayload         | 121.172 ns |  15.7724 ns | 0.8645 ns |  0.61 |    0.01 |    4 | 0.0134 |     224 B |        0.58 |
| Serilog_SingleMessage          | 198.383 ns |  38.5246 ns | 2.1117 ns |  1.00 |    0.01 |    5 | 0.0229 |     384 B |        1.00 |
| Serilog_StructuredPayload      | 283.532 ns |  65.0827 ns | 3.5674 ns |  1.43 |    0.02 |    6 | 0.0296 |     496 B |        1.29 |
| LoggerHelper_SingleMessage     | 618.287 ns |  90.3602 ns | 4.9529 ns |  3.12 |    0.04 |    7 | 0.0687 |    1152 B |        3.00 |
| LoggerHelper_StructuredPayload | 763.493 ns | 158.3894 ns | 8.6819 ns |  3.85 |    0.05 |    7 | 0.0772 |    1296 B |        3.38 |

---

_Benchmarks run automatically on each release via [GitHub Actions](../.github/workflows/benchmarks.yml)._
