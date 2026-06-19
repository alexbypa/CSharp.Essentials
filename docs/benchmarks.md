# LoggerHelper v5 — Benchmark Results

> Generated: 2026-06-19 | Runtime: .NET 9 | OS: ubuntu-latest

Comparison: **LoggerHelper v5** vs **Serilog** (baseline) vs **NLog**.
All frameworks use a no-op sink/target — measures framework overhead, not I/O.

---

## Throughput

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 2 logical cores and 1 physical core
.NET SDK 10.0.301
  [Host]   : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2
  ShortRun : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=5  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean       | Error      | StdDev     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |-----------:|-----------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Serilog_BelowMinLevel          |   5.558 ns |  0.3096 ns |  0.0804 ns |  0.02 |    0.00 |    1 |      - |         - |        0.00 |
| NLog_BelowMinLevel             |  27.324 ns |  0.1657 ns |  0.0256 ns |  0.09 |    0.00 |    2 |      - |         - |        0.00 |
| LoggerHelper_BelowMinLevel     |  41.616 ns |  1.1130 ns |  0.2891 ns |  0.13 |    0.00 |    3 | 0.0033 |      56 B |        0.15 |
| NLog_StructuredPayload         | 118.480 ns |  2.3310 ns |  0.3607 ns |  0.38 |    0.01 |    4 | 0.0134 |     224 B |        0.58 |
| NLog_SingleMessage             | 183.496 ns |  9.1676 ns |  1.4187 ns |  0.59 |    0.02 |    5 | 0.0105 |     176 B |        0.46 |
| Serilog_StructuredPayload      | 261.313 ns |  3.1820 ns |  0.4924 ns |  0.84 |    0.03 |    6 | 0.0296 |     496 B |        1.29 |
| Serilog_SingleMessage          | 312.512 ns | 41.8678 ns | 10.8729 ns |  1.00 |    0.04 |    6 | 0.0229 |     384 B |        1.00 |
| LoggerHelper_SingleMessage     | 514.003 ns | 15.0529 ns |  2.3294 ns |  1.65 |    0.05 |    7 | 0.0687 |    1152 B |        3.00 |
| LoggerHelper_StructuredPayload | 686.922 ns |  9.0638 ns |  1.4026 ns |  2.20 |    0.07 |    8 | 0.0772 |    1296 B |        3.38 |

---

## Routing Overhead

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 2 logical cores and 1 physical core
.NET SDK 10.0.301
  [Host]   : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2
  ShortRun : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=5  LaunchCount=1  
WarmupCount=3  

```
| Method                   | Mean      | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| NLog_Multi_Info          |  93.67 ns |  3.917 ns | 1.017 ns |  0.57 |    0.01 |    1 | 0.0105 |     176 B |        0.46 |
| NLog_Single_Info         |  97.43 ns |  2.430 ns | 0.631 ns |  0.59 |    0.00 |    1 | 0.0105 |     176 B |        0.46 |
| NLog_Multi_Error         | 116.00 ns |  2.909 ns | 0.755 ns |  0.71 |    0.01 |    1 | 0.0105 |     176 B |        0.46 |
| Serilog_Single_Info      | 164.32 ns |  3.043 ns | 0.790 ns |  1.00 |    0.01 |    2 | 0.0229 |     384 B |        1.00 |
| Serilog_Multi_Info       | 332.20 ns |  2.333 ns | 0.361 ns |  2.02 |    0.01 |    3 | 0.0582 |     976 B |        2.54 |
| Serilog_Multi_Error      | 338.75 ns |  7.939 ns | 1.229 ns |  2.06 |    0.01 |    3 | 0.0582 |     976 B |        2.54 |
| LoggerHelper_Single_Info | 545.73 ns |  3.060 ns | 0.473 ns |  3.32 |    0.01 |    4 | 0.0687 |    1152 B |        3.00 |
| LoggerHelper_Multi_Info  | 640.21 ns |  7.572 ns | 1.966 ns |  3.90 |    0.02 |    4 | 0.0925 |    1560 B |        4.06 |
| LoggerHelper_Multi_Error | 654.96 ns | 21.400 ns | 3.312 ns |  3.99 |    0.03 |    4 | 0.0925 |    1560 B |        4.06 |

---

## Startup Time

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 2 logical cores and 1 physical core
.NET SDK 10.0.301
  [Host]   : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2
  ShortRun : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=5  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error       | StdDev      | Ratio  | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |-----------:|------------:|------------:|-------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Serilog_Startup      |   1.127 μs |   0.0288 μs |   0.0045 μs |   1.00 |    0.01 |    1 | 0.1831 |      - |   3.01 KB |        1.00 |
| NLog_Startup         | 101.699 μs |   2.6586 μs |   0.6904 μs |  90.24 |    0.65 |    2 | 3.1738 | 2.9297 |  55.41 KB |       18.42 |
| LoggerHelper_Startup | 448.581 μs | 507.8421 μs | 131.8850 μs | 398.03 |  107.40 |    3 | 3.9063 |      - |  78.03 KB |       25.94 |

---

## Emit Overhead

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 2 logical cores and 1 physical core
.NET SDK 10.0.301
  [Host]   : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2
  ShortRun : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=5  LaunchCount=1  
WarmupCount=3  

```
| Method                                                                  | Mean          | Error       | StdDev     | Ratio     | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------------------------ |--------------:|------------:|-----------:|----------:|--------:|-----:|-------:|-------:|----------:|------------:|
| &#39;Telegram legacy — GetAwaiter().GetResult() blocca il chiamante&#39;        |     0.3530 ns |   0.0164 ns |  0.0025 ns |      1.00 |    0.01 |    1 |      - |      - |         - |          NA |
| &#39;Throttle modern — fast path (CAS, interval=0)&#39;                         |     0.7091 ns |   0.0189 ns |  0.0049 ns |      2.01 |    0.02 |    2 |      - |      - |         - |          NA |
| &#39;Throttle legacy — fast path (interval=0, ritorna true)&#39;                |     1.3487 ns |   0.1172 ns |  0.0304 ns |      3.82 |    0.08 |    3 |      - |      - |         - |          NA |
| &#39;Throttle modern — throttled path (GetOrAdd + CAS, ritorna false)&#39;      |    37.1566 ns |   0.1328 ns |  0.0206 ns |    105.28 |    0.67 |    4 |      - |      - |         - |          NA |
| &#39;Throttle legacy — throttled path (GetOrAdd + check, ritorna false)&#39;    |    37.5598 ns |   0.2590 ns |  0.0401 ns |    106.42 |    0.69 |    4 |      - |      - |         - |          NA |
| &#39;Telegram modern — Task.Run fire-and-forget, Emit() torna in ~2µs&#39;      |    94.1367 ns |  18.0395 ns |  4.6848 ns |    266.72 |   12.30 |    5 | 0.0038 | 0.0023 |      64 B |          NA |
| &#39;Email modern — template cached al costruttore, solo Replace&#39;           |   316.1608 ns |   3.7357 ns |  0.5781 ns |    895.79 |    5.91 |    6 | 0.1225 |      - |    2056 B |          NA |
| &#39;Email legacy — LoadDefaultTemplate() + Replace ad ogni Emit (no file)&#39; |   323.4780 ns |   5.1041 ns |  1.3255 ns |    916.52 |    6.76 |    6 | 0.1225 |      - |    2056 B |          NA |
| &#39;Email legacy — File.ReadAllText() + Replace ad ogni Emit (produzione)&#39; | 9,296.7619 ns | 141.2105 ns | 36.6719 ns | 26,340.89 |  192.45 |    7 | 0.6409 | 0.0153 |   10800 B |          NA |

---

## Sink Routing Match

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 2 logical cores and 1 physical core
.NET SDK 10.0.301
  [Host]   : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2
  ShortRun : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=5  LaunchCount=1  
WarmupCount=3  

```
| Method                           | LevelCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|--------------------------------- |----------- |----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;Modern — hit (HashSet O(1))&#39;    | 1          |  3.074 ns | 0.0135 ns | 0.0035 ns |  0.17 |    0.00 |    1 |      - |         - |        0.00 |
| &#39;Modern — miss (HashSet O(1))&#39;   | 1          |  3.086 ns | 0.1902 ns | 0.0294 ns |  0.17 |    0.00 |    1 |      - |         - |        0.00 |
| &#39;Legacy — hit (List scan)&#39;       | 1          | 17.820 ns | 0.7176 ns | 0.1110 ns |  1.00 |    0.01 |    2 | 0.0014 |      24 B |        1.00 |
| &#39;Legacy — miss (List full scan)&#39; | 1          | 18.171 ns | 0.6737 ns | 0.1750 ns |  1.02 |    0.01 |    2 | 0.0014 |      24 B |        1.00 |
|                                  |            |           |           |           |       |         |      |        |           |             |
| &#39;Modern — miss (HashSet O(1))&#39;   | 3          |  4.315 ns | 0.0654 ns | 0.0101 ns |  0.19 |    0.00 |    1 |      - |         - |        0.00 |
| &#39;Modern — hit (HashSet O(1))&#39;    | 3          |  4.371 ns | 0.0265 ns | 0.0041 ns |  0.19 |    0.00 |    1 |      - |         - |        0.00 |
| &#39;Legacy — hit (List scan)&#39;       | 3          | 22.704 ns | 0.6275 ns | 0.1630 ns |  1.00 |    0.01 |    2 | 0.0014 |      24 B |        1.00 |
| &#39;Legacy — miss (List full scan)&#39; | 3          | 27.144 ns | 0.4540 ns | 0.1179 ns |  1.20 |    0.01 |    2 | 0.0014 |      24 B |        1.00 |
|                                  |            |           |           |           |       |         |      |        |           |             |
| &#39;Modern — miss (HashSet O(1))&#39;   | 5          |  3.097 ns | 0.0121 ns | 0.0019 ns |  0.13 |    0.00 |    1 |      - |         - |        0.00 |
| &#39;Modern — hit (HashSet O(1))&#39;    | 5          |  4.373 ns | 0.0184 ns | 0.0048 ns |  0.19 |    0.00 |    2 |      - |         - |        0.00 |
| &#39;Legacy — hit (List scan)&#39;       | 5          | 23.012 ns | 1.0516 ns | 0.2731 ns |  1.00 |    0.02 |    3 | 0.0014 |      24 B |        1.00 |
| &#39;Legacy — miss (List full scan)&#39; | 5          | 32.469 ns | 1.2012 ns | 0.3119 ns |  1.41 |    0.02 |    4 | 0.0014 |      24 B |        1.00 |

---

## Sensitive Data Masking

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 2 logical cores and 1 physical core
.NET SDK 10.0.301
  [Host]   : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2
  ShortRun : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=5  LaunchCount=1  
WarmupCount=3  

```
| Method                                                                                             | Mean       | Error    | StdDev  | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------------------------------------------------------- |-----------:|---------:|--------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;Masking disabled — structured payload, no PII&#39;                                                    |   703.5 ns | 18.90 ns | 2.92 ns |  1.00 |    0.01 |    1 | 0.0772 |   1.27 KB |        1.00 |
| &#39;1 preset (Email) enabled — payload contains no email (no match)&#39;                                  | 1,261.7 ns |  4.15 ns | 0.64 ns |  1.79 |    0.01 |    2 | 0.0820 |   1.36 KB |        1.07 |
| &#39;5 presets + custom rule + SensitiveProperties — payload contains email, card, order id, password&#39; | 4,342.6 ns | 44.41 ns | 6.87 ns |  6.17 |    0.02 |    3 | 0.1526 |   2.51 KB |        1.98 |

---

## MCP Tools

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 2 logical cores and 1 physical core
.NET SDK 10.0.301
  [Host]   : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2
  ShortRun : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=5  LaunchCount=1  
WarmupCount=3  

```
| Method                                | ErrorCount | Mean        | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |------------:|-----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;MCP: loggerhelper_get_sinks&#39;         | 0          |    29.99 ns |   2.996 ns |  0.778 ns |  0.46 |    0.01 |    1 | 0.0019 |      32 B |        0.33 |
| &#39;MCP: loggerhelper_get_errors&#39;        | 0          |    64.22 ns |   0.312 ns |  0.048 ns |  0.99 |    0.00 |    2 | 0.0057 |      96 B |        1.00 |
| &#39;Direct API: ILogErrorStore.GetAll()&#39; | 0          |    64.86 ns |   1.075 ns |  0.279 ns |  1.00 |    0.01 |    2 | 0.0057 |      96 B |        1.00 |
| &#39;MCP: loggerhelper_get_health&#39;        | 0          |    86.82 ns |   0.901 ns |  0.139 ns |  1.34 |    0.01 |    3 | 0.0143 |     240 B |        2.50 |
| &#39;MCP: loggerhelper_get_config&#39;        | 0          |   308.47 ns |   3.603 ns |  0.558 ns |  4.76 |    0.02 |    4 | 0.0792 |    1328 B |       13.83 |
|                                       |            |             |            |           |       |         |      |        |           |             |
| &#39;MCP: loggerhelper_get_sinks&#39;         | 10         |    29.67 ns |   0.615 ns |  0.160 ns |  0.14 |    0.00 |    1 | 0.0019 |      32 B |        0.16 |
| &#39;MCP: loggerhelper_get_health&#39;        | 10         |    89.17 ns |   2.081 ns |  0.540 ns |  0.43 |    0.00 |    2 | 0.0153 |     256 B |        1.28 |
| &#39;Direct API: ILogErrorStore.GetAll()&#39; | 10         |   205.99 ns |   8.290 ns |  2.153 ns |  1.00 |    0.01 |    3 | 0.0119 |     200 B |        1.00 |
| &#39;MCP: loggerhelper_get_config&#39;        | 10         |   305.06 ns |   3.297 ns |  0.510 ns |  1.48 |    0.01 |    4 | 0.0792 |    1328 B |        6.64 |
| &#39;MCP: loggerhelper_get_errors&#39;        | 10         | 1,588.49 ns | 117.362 ns | 30.479 ns |  7.71 |    0.15 |    5 | 0.1678 |    2808 B |       14.04 |
|                                       |            |             |            |           |       |         |      |        |           |             |
| &#39;MCP: loggerhelper_get_sinks&#39;         | 100        |    34.94 ns |   0.884 ns |  0.230 ns |  0.02 |    0.00 |    1 | 0.0019 |      32 B |        0.03 |
| &#39;MCP: loggerhelper_get_health&#39;        | 100        |   102.22 ns |   0.522 ns |  0.136 ns |  0.07 |    0.00 |    2 | 0.0153 |     256 B |        0.28 |
| &#39;MCP: loggerhelper_get_config&#39;        | 100        |   312.63 ns |   9.076 ns |  2.357 ns |  0.22 |    0.00 |    3 | 0.0792 |    1328 B |        1.44 |
| &#39;Direct API: ILogErrorStore.GetAll()&#39; | 100        | 1,399.72 ns |  95.288 ns | 14.746 ns |  1.00 |    0.01 |    4 | 0.0534 |     920 B |        1.00 |
| &#39;MCP: loggerhelper_get_errors&#39;        | 100        | 4,053.28 ns |  94.165 ns | 24.454 ns |  2.90 |    0.03 |    5 | 0.3281 |    5600 B |        6.09 |

---

## Sampling

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 2 logical cores and 1 physical core
.NET SDK 10.0.301
  [Host]   : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2
  ShortRun : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=5  LaunchCount=1  
WarmupCount=3  

```
| Method                                 | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------------- |----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| &#39;Matches() — level check only&#39;         |  4.402 ns | 0.2313 ns | 0.0601 ns |  1.00 |    0.02 |    1 |         - |          NA |
| &#39;ShouldEmit(null) — sampling disabled&#39; |  5.643 ns | 0.0932 ns | 0.0144 ns |  1.28 |    0.02 |    2 |         - |          NA |
| &#39;ShouldEmit(0.5) — 50% sampling&#39;       | 11.053 ns | 0.1964 ns | 0.0510 ns |  2.51 |    0.03 |    3 |         - |          NA |

---

_Benchmarks run automatically on each release via [GitHub Actions](../.github/workflows/benchmarks.yml)._
