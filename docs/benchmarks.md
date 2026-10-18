# LoggerHelper v5 — Benchmark Results

> Generated: 2026-10-18 | Runtime: .NET 10 | OS: ubuntu-latest

Comparison: **LoggerHelper v5** vs **Serilog** (baseline) vs **NLog**.
All frameworks use a no-op sink/target — measures framework overhead, not I/O.

---

## LoggerHelper vs Serilog vs NLog (via ILogger)

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.32GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean      | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------:|-----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| NLog_BelowMinLevel             |  16.40 ns |   3.578 ns |  0.196 ns |  0.14 |    0.01 |    1 | 0.0033 |      56 B |        0.14 |
| LoggerHelper_BelowMinLevel     |  17.48 ns |  15.173 ns |  0.832 ns |  0.15 |    0.01 |    1 | 0.0033 |      56 B |        0.14 |
| Serilog_BelowMinLevel          |  17.61 ns |   5.087 ns |  0.279 ns |  0.15 |    0.01 |    1 | 0.0033 |      56 B |        0.14 |
| NLog_SingleMessage             | 113.61 ns |  65.739 ns |  3.603 ns |  0.95 |    0.04 |    2 | 0.0243 |     408 B |        1.00 |
| Serilog_SingleMessage          | 119.71 ns |  95.625 ns |  5.242 ns |  1.00 |    0.05 |    2 | 0.0243 |     408 B |        1.00 |
| LoggerHelper_SingleMessage     | 184.62 ns |   9.213 ns |  0.505 ns |  1.54 |    0.06 |    3 | 0.0443 |     744 B |        1.82 |
| Serilog_StructuredPayload      | 212.11 ns | 213.914 ns | 11.725 ns |  1.77 |    0.11 |    3 | 0.0467 |     784 B |        1.92 |
| NLog_StructuredPayload         | 218.19 ns | 159.587 ns |  8.747 ns |  1.82 |    0.09 |    3 | 0.0329 |     552 B |        1.35 |
| LoggerHelper_StructuredPayload | 269.24 ns |  65.703 ns |  3.601 ns |  2.25 |    0.09 |    4 | 0.0529 |     888 B |        2.18 |

---

## Masking cost vs number of sinks

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.32GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                          | Sinks | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------------- |------ |-----------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| **Masking_Off**                     | **1**     |   **234.0 ns** |  **26.53 ns** |  **1.45 ns** |  **1.00** |    **0.01** |    **1** | **0.0496** |     **832 B** |        **1.00** |
| Masking_On                      | 1     | 1,953.3 ns | 221.90 ns | 12.16 ns |  8.35 |    0.06 |    3 | 0.1068 |    1816 B |        2.18 |
| Masking_SensitivePropertiesOnly | 1     |   394.4 ns |  77.13 ns |  4.23 ns |  1.69 |    0.02 |    2 | 0.0653 |    1096 B |        1.32 |
|                                 |       |            |           |          |       |         |      |        |           |             |
| **Masking_Off**                     | **3**     |   **240.2 ns** |  **60.15 ns** |  **3.30 ns** |  **1.00** |    **0.02** |    **1** | **0.0496** |     **832 B** |        **1.00** |
| Masking_On                      | 3     | 1,970.6 ns | 147.49 ns |  8.08 ns |  8.21 |    0.10 |    3 | 0.1068 |    1816 B |        2.18 |
| Masking_SensitivePropertiesOnly | 3     |   427.1 ns |  96.29 ns |  5.28 ns |  1.78 |    0.03 |    2 | 0.0653 |    1096 B |        1.32 |
|                                 |       |            |           |          |       |         |      |        |           |             |
| **Masking_Off**                     | **5**     |   **241.7 ns** |   **7.97 ns** |  **0.44 ns** |  **1.00** |    **0.00** |    **1** | **0.0496** |     **832 B** |        **1.00** |
| Masking_On                      | 5     | 2,040.5 ns | 359.93 ns | 19.73 ns |  8.44 |    0.07 |    3 | 0.1068 |    1816 B |        2.18 |
| Masking_SensitivePropertiesOnly | 5     |   440.3 ns | 124.74 ns |  6.84 ns |  1.82 |    0.02 |    2 | 0.0653 |    1096 B |        1.32 |

---

## Routing Overhead

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.32GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                   | Mean      | Error      | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------:|-----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| NLog_Multi_Info          |  49.45 ns |  23.930 ns | 1.312 ns |  0.61 |    0.01 |    1 | 0.0105 |     176 B |        0.46 |
| NLog_Single_Info         |  50.05 ns |  11.587 ns | 0.635 ns |  0.61 |    0.01 |    1 | 0.0105 |     176 B |        0.46 |
| NLog_Multi_Error         |  65.12 ns |  20.881 ns | 1.145 ns |  0.80 |    0.01 |    2 | 0.0105 |     176 B |        0.46 |
| Serilog_Single_Info      |  81.57 ns |   6.266 ns | 0.343 ns |  1.00 |    0.01 |    3 | 0.0229 |     384 B |        1.00 |
| Serilog_Multi_Info       | 149.22 ns |  59.241 ns | 3.247 ns |  1.83 |    0.04 |    4 | 0.0582 |     976 B |        2.54 |
| Serilog_Multi_Error      | 153.00 ns |  54.310 ns | 2.977 ns |  1.88 |    0.03 |    4 | 0.0582 |     976 B |        2.54 |
| LoggerHelper_Multi_Error | 195.48 ns | 125.486 ns | 6.878 ns |  2.40 |    0.07 |    5 | 0.0443 |     744 B |        1.94 |
| LoggerHelper_Multi_Info  | 196.93 ns |  48.337 ns | 2.649 ns |  2.41 |    0.03 |    5 | 0.0443 |     744 B |        1.94 |
| LoggerHelper_Single_Info | 199.81 ns | 107.661 ns | 5.901 ns |  2.45 |    0.06 |    5 | 0.0443 |     744 B |        1.94 |

---

## Startup Time

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.32GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean        | Error        | StdDev      | Ratio  | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |------------:|-------------:|------------:|-------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Serilog_Startup      |    491.4 ns |     40.93 ns |     2.24 ns |   1.00 |    0.01 |    1 | 0.1841 |      - |   3.01 KB |        1.00 |
| NLog_Startup         | 49,183.4 ns | 18,410.80 ns | 1,009.16 ns | 100.10 |    1.82 |    2 | 3.2959 | 3.1738 |  55.34 KB |       18.40 |
| LoggerHelper_Startup | 93,409.0 ns | 70,220.52 ns | 3,849.02 ns | 190.11 |    6.83 |    3 | 4.1504 |      - |  68.88 KB |       22.90 |

---

## Trace API

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.32GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                             | Mean      | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------- |----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| NLog_Raw                           |  51.11 ns |   5.922 ns |  0.325 ns |  0.46 |    0.00 |    1 | 0.0129 |      - |     216 B |        0.47 |
| Serilog_Raw                        | 110.20 ns |  17.595 ns |  0.964 ns |  1.00 |    0.01 |    2 | 0.0272 |      - |     456 B |        1.00 |
| LoggerHelper_ILogger_WithException | 188.61 ns | 155.203 ns |  8.507 ns |  1.71 |    0.07 |    3 | 0.0443 |      - |     744 B |        1.63 |
| LoggerHelper_ILogger               | 228.22 ns | 147.974 ns |  8.111 ns |  2.07 |    0.07 |    4 | 0.0496 |      - |     832 B |        1.82 |
| LoggerHelper_BeginTrace_5Logs      | 264.48 ns |  26.919 ns |  1.476 ns |  2.40 |    0.02 |    4 | 0.0546 |      - |     917 B |        2.01 |
| LoggerHelper_Trace_WithException   | 644.25 ns |  64.385 ns |  3.529 ns |  5.85 |    0.05 |    5 | 0.1221 | 0.0944 |    2048 B |        4.49 |
| LoggerHelper_Trace                 | 754.28 ns | 185.726 ns | 10.180 ns |  6.84 |    0.10 |    5 | 0.1354 | 0.0582 |    2280 B |        5.00 |

---

_Benchmarks run automatically on each release via [GitHub Actions](../.github/workflows/benchmarks.yml)._
