# LoggerHelper v5 — Benchmark Results

> Generated: 2026-10-07 | Runtime: .NET 10 | OS: ubuntu-latest

Comparison: **LoggerHelper v5** vs **Serilog** (baseline) vs **NLog**.
All frameworks use a no-op sink/target — measures framework overhead, not I/O.

---

## LoggerHelper vs Serilog vs NLog (via ILogger)

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean      | Error     | StdDev   | Ratio | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------:|----------:|---------:|------:|-----:|-------:|----------:|------------:|
| NLog_BelowMinLevel             |  23.10 ns |  0.595 ns | 0.033 ns |  0.13 |    1 | 0.0007 |      56 B |        0.14 |
| Serilog_BelowMinLevel          |  24.07 ns |  1.803 ns | 0.099 ns |  0.14 |    1 | 0.0007 |      56 B |        0.14 |
| LoggerHelper_BelowMinLevel     |  24.96 ns |  1.824 ns | 0.100 ns |  0.14 |    1 | 0.0007 |      56 B |        0.14 |
| NLog_SingleMessage             | 168.64 ns |  8.312 ns | 0.456 ns |  0.96 |    2 | 0.0048 |     408 B |        1.00 |
| Serilog_SingleMessage          | 175.14 ns |  7.728 ns | 0.424 ns |  1.00 |    2 | 0.0048 |     408 B |        1.00 |
| LoggerHelper_SingleMessage     | 287.04 ns | 22.906 ns | 1.256 ns |  1.64 |    3 | 0.0086 |     744 B |        1.82 |
| NLog_StructuredPayload         | 301.00 ns | 28.947 ns | 1.587 ns |  1.72 |    3 | 0.0062 |     552 B |        1.35 |
| Serilog_StructuredPayload      | 304.35 ns |  2.917 ns | 0.160 ns |  1.74 |    3 | 0.0091 |     784 B |        1.92 |
| LoggerHelper_StructuredPayload | 378.76 ns | 12.829 ns | 0.703 ns |  2.16 |    4 | 0.0105 |     888 B |        2.18 |

---

## Masking cost vs number of sinks

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method      | Sinks | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |------ |-----------:|------------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| **Masking_Off** | **1**     |   **347.1 ns** |     **2.13 ns** |  **0.12 ns** |  **1.00** |    **0.00** |    **1** | **0.0095** |     **832 B** |        **1.00** |
| Masking_On  | 1     | 2,366.4 ns |    91.58 ns |  5.02 ns |  6.82 |    0.01 |    2 | 0.0191 |    1816 B |        2.18 |
|             |       |            |             |          |       |         |      |        |           |             |
| **Masking_Off** | **3**     |   **340.0 ns** |    **18.60 ns** |  **1.02 ns** |  **1.00** |    **0.00** |    **1** | **0.0095** |     **832 B** |        **1.00** |
| Masking_On  | 3     | 2,339.6 ns |   397.49 ns | 21.79 ns |  6.88 |    0.06 |    2 | 0.0191 |    1816 B |        2.18 |
|             |       |            |             |          |       |         |      |        |           |             |
| **Masking_Off** | **5**     |   **345.6 ns** |     **9.27 ns** |  **0.51 ns** |  **1.00** |    **0.00** |    **1** | **0.0095** |     **832 B** |        **1.00** |
| Masking_On  | 5     | 2,382.9 ns | 1,227.29 ns | 67.27 ns |  6.89 |    0.17 |    2 | 0.0191 |    1816 B |        2.18 |

---

_Benchmarks run automatically on each release via [GitHub Actions](../.github/workflows/benchmarks.yml)._
