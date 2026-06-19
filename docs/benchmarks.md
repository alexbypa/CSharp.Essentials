# LoggerHelper v5 — Benchmark Results

> Generated: 2026-06-19 | Runtime: .NET 9 | OS: ubuntu-latest

Comparison: **LoggerHelper v5** vs **Serilog** (baseline) vs **NLog**.
All frameworks use a no-op sink/target — measures framework overhead, not I/O.

---

## Throughput

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.301
  [Host]   : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2
  ShortRun : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=5  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean       | Error      | StdDev     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |-----------:|-----------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Serilog_BelowMinLevel          |   5.570 ns |  0.0604 ns |  0.0093 ns |  0.03 |    0.00 |    1 |      - |         - |        0.00 |
| NLog_BelowMinLevel             |  30.020 ns |  0.1194 ns |  0.0185 ns |  0.16 |    0.00 |    2 |      - |         - |        0.00 |
| LoggerHelper_BelowMinLevel     |  40.833 ns |  0.6735 ns |  0.1749 ns |  0.22 |    0.00 |    3 | 0.0033 |      56 B |        0.15 |
| NLog_SingleMessage             |  94.280 ns |  3.0367 ns |  0.7886 ns |  0.52 |    0.01 |    4 | 0.0105 |     176 B |        0.46 |
| NLog_StructuredPayload         | 114.399 ns |  2.1839 ns |  0.3380 ns |  0.63 |    0.01 |    4 | 0.0134 |     224 B |        0.58 |
| Serilog_SingleMessage          | 182.141 ns |  7.9891 ns |  2.0747 ns |  1.00 |    0.01 |    5 | 0.0229 |     384 B |        1.00 |
| Serilog_StructuredPayload      | 263.832 ns | 11.1575 ns |  2.8976 ns |  1.45 |    0.02 |    6 | 0.0296 |     496 B |        1.29 |
| LoggerHelper_SingleMessage     | 591.741 ns |  9.2771 ns |  2.4092 ns |  3.25 |    0.04 |    7 | 0.0687 |    1152 B |        3.00 |
| LoggerHelper_StructuredPayload | 721.144 ns | 57.3915 ns | 14.9044 ns |  3.96 |    0.09 |    7 | 0.0772 |    1296 B |        3.38 |

---

## Routing Overhead

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.301
  [Host]   : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2
  ShortRun : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=5  LaunchCount=1  
WarmupCount=3  

```
| Method                   | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| NLog_Single_Info         |  94.26 ns |  3.514 ns |  0.913 ns |  0.54 |    0.01 |    1 | 0.0105 |     176 B |        0.46 |
| NLog_Multi_Info          | 102.00 ns | 17.097 ns |  4.440 ns |  0.58 |    0.02 |    1 | 0.0105 |     176 B |        0.46 |
| NLog_Multi_Error         | 114.56 ns |  2.736 ns |  0.710 ns |  0.65 |    0.01 |    1 | 0.0105 |     176 B |        0.46 |
| Serilog_Single_Info      | 175.73 ns |  9.535 ns |  2.476 ns |  1.00 |    0.02 |    2 | 0.0229 |     384 B |        1.00 |
| Serilog_Multi_Error      | 339.22 ns | 35.853 ns |  9.311 ns |  1.93 |    0.05 |    3 | 0.0582 |     976 B |        2.54 |
| Serilog_Multi_Info       | 365.03 ns | 65.901 ns | 17.114 ns |  2.08 |    0.09 |    3 | 0.0582 |     976 B |        2.54 |
| LoggerHelper_Single_Info | 575.59 ns |  5.607 ns |  0.868 ns |  3.28 |    0.04 |    4 | 0.0687 |    1152 B |        3.00 |
| LoggerHelper_Multi_Info  | 685.79 ns | 48.017 ns | 12.470 ns |  3.90 |    0.08 |    4 | 0.0925 |    1560 B |        4.06 |
| LoggerHelper_Multi_Error | 693.17 ns | 46.150 ns |  7.142 ns |  3.95 |    0.06 |    4 | 0.0925 |    1560 B |        4.06 |

---

## Startup Time

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.301
  [Host]   : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2
  ShortRun : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=5  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|----------:|-------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Serilog_Startup      |   1.177 μs |  0.0153 μs | 0.0040 μs |   1.00 |    0.00 |    1 | 0.1831 |      - |   3.01 KB |        1.00 |
| NLog_Startup         | 121.144 μs | 11.3606 μs | 2.9503 μs | 102.91 |    2.31 |    2 | 3.1738 | 2.9297 |  55.41 KB |       18.42 |
| LoggerHelper_Startup | 215.546 μs |  4.1646 μs | 1.0815 μs | 183.11 |    1.01 |    3 | 4.3945 |      - |  77.69 KB |       25.83 |

---

## Emit Overhead

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.301
  [Host]   : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2
  ShortRun : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=5  LaunchCount=1  
WarmupCount=3  

```
| Method                                                                  | Mean          | Error       | StdDev     | Ratio     | RatioSD | Rank | Gen0   | Gen1   | Gen2   | Allocated | Alloc Ratio |
|------------------------------------------------------------------------ |--------------:|------------:|-----------:|----------:|--------:|-----:|-------:|-------:|-------:|----------:|------------:|
| &#39;Telegram legacy — GetAwaiter().GetResult() blocca il chiamante&#39;        |     0.3131 ns |   0.0108 ns |  0.0017 ns |      1.00 |    0.01 |    1 |      - |      - |      - |         - |          NA |
| &#39;Throttle modern — fast path (CAS, interval=0)&#39;                         |     0.9884 ns |   0.0012 ns |  0.0003 ns |      3.16 |    0.02 |    2 |      - |      - |      - |         - |          NA |
| &#39;Throttle legacy — fast path (interval=0, ritorna true)&#39;                |     1.3271 ns |   0.0051 ns |  0.0008 ns |      4.24 |    0.02 |    3 |      - |      - |      - |         - |          NA |
| &#39;Throttle modern — throttled path (GetOrAdd + CAS, ritorna false)&#39;      |    34.9245 ns |   0.2585 ns |  0.0671 ns |    111.54 |    0.57 |    4 |      - |      - |      - |         - |          NA |
| &#39;Throttle legacy — throttled path (GetOrAdd + check, ritorna false)&#39;    |    35.2096 ns |   0.2065 ns |  0.0536 ns |    112.45 |    0.56 |    4 |      - |      - |      - |         - |          NA |
| &#39;Telegram modern — Task.Run fire-and-forget, Emit() torna in ~2µs&#39;      |   109.4890 ns |  10.9203 ns |  2.8360 ns |    349.67 |    8.48 |    5 | 0.0041 | 0.0029 | 0.0005 |      64 B |          NA |
| &#39;Email modern — template cached al costruttore, solo Replace&#39;           |   345.9646 ns |  25.9748 ns |  6.7456 ns |  1,104.89 |   20.45 |    6 | 0.1225 |      - |      - |    2056 B |          NA |
| &#39;Email legacy — LoadDefaultTemplate() + Replace ad ogni Emit (no file)&#39; |   355.4433 ns |  17.2856 ns |  4.4890 ns |  1,135.16 |   14.22 |    6 | 0.1225 |      - |      - |    2056 B |          NA |
| &#39;Email legacy — File.ReadAllText() + Replace ad ogni Emit (produzione)&#39; | 7,776.5526 ns | 157.7320 ns | 40.9625 ns | 24,835.51 |  168.31 |    7 | 0.6409 | 0.0153 |      - |   10800 B |          NA |

---

## Sink Routing Match

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.301
  [Host]   : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2
  ShortRun : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=5  LaunchCount=1  
WarmupCount=3  

```
| Method                           | LevelCount | Mean      | Error     | StdDev    | Ratio | Rank | Gen0   | Allocated | Alloc Ratio |
|--------------------------------- |----------- |----------:|----------:|----------:|------:|-----:|-------:|----------:|------------:|
| &#39;Modern — miss (HashSet O(1))&#39;   | 1          |  3.021 ns | 0.0104 ns | 0.0027 ns |  0.16 |    1 |      - |         - |        0.00 |
| &#39;Modern — hit (HashSet O(1))&#39;    | 1          |  3.684 ns | 0.0122 ns | 0.0032 ns |  0.19 |    1 |      - |         - |        0.00 |
| &#39;Legacy — miss (List full scan)&#39; | 1          | 17.371 ns | 0.2870 ns | 0.0745 ns |  0.91 |    2 | 0.0014 |      24 B |        1.00 |
| &#39;Legacy — hit (List scan)&#39;       | 1          | 19.033 ns | 0.3986 ns | 0.1035 ns |  1.00 |    2 | 0.0014 |      24 B |        1.00 |
|                                  |            |           |           |           |       |      |        |           |             |
| &#39;Modern — miss (HashSet O(1))&#39;   | 3          |  3.875 ns | 0.0226 ns | 0.0035 ns |  0.17 |    1 |      - |         - |        0.00 |
| &#39;Modern — hit (HashSet O(1))&#39;    | 3          |  4.101 ns | 0.0182 ns | 0.0028 ns |  0.18 |    1 |      - |         - |        0.00 |
| &#39;Legacy — hit (List scan)&#39;       | 3          | 23.075 ns | 0.6038 ns | 0.1568 ns |  1.00 |    2 | 0.0014 |      24 B |        1.00 |
| &#39;Legacy — miss (List full scan)&#39; | 3          | 25.972 ns | 0.6584 ns | 0.1019 ns |  1.13 |    2 | 0.0014 |      24 B |        1.00 |
|                                  |            |           |           |           |       |      |        |           |             |
| &#39;Modern — miss (HashSet O(1))&#39;   | 5          |  3.039 ns | 0.0189 ns | 0.0029 ns |  0.13 |    1 |      - |         - |        0.00 |
| &#39;Modern — hit (HashSet O(1))&#39;    | 5          |  4.122 ns | 0.0074 ns | 0.0019 ns |  0.18 |    2 |      - |         - |        0.00 |
| &#39;Legacy — hit (List scan)&#39;       | 5          | 23.010 ns | 0.2636 ns | 0.0684 ns |  1.00 |    3 | 0.0014 |      24 B |        1.00 |
| &#39;Legacy — miss (List full scan)&#39; | 5          | 29.570 ns | 0.5744 ns | 0.1492 ns |  1.29 |    4 | 0.0014 |      24 B |        1.00 |

---

## Sensitive Data Masking

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.301
  [Host]   : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2
  ShortRun : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=5  LaunchCount=1  
WarmupCount=3  

```
| Method                                                                                             | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------------------------------------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;Masking disabled — structured payload, no PII&#39;                                                    |   770.2 ns | 12.83 ns |  1.98 ns |  1.00 |    0.00 |    1 | 0.0772 |   1.27 KB |        1.00 |
| &#39;1 preset (Email) enabled — payload contains no email (no match)&#39;                                  | 1,335.0 ns | 15.38 ns |  3.99 ns |  1.73 |    0.01 |    2 | 0.0820 |   1.36 KB |        1.07 |
| &#39;5 presets + custom rule + SensitiveProperties — payload contains email, card, order id, password&#39; | 4,371.0 ns | 75.51 ns | 19.61 ns |  5.68 |    0.03 |    3 | 0.1526 |   2.51 KB |        1.98 |

---

## MCP Tools

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.301
  [Host]   : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2
  ShortRun : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=5  LaunchCount=1  
WarmupCount=3  

```
| Method                                | ErrorCount | Mean        | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |------------:|-----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| &#39;MCP: loggerhelper_get_sinks&#39;         | 0          |    27.82 ns |   0.745 ns |  0.115 ns |  0.45 |    0.01 |    1 | 0.0019 |      32 B |        0.33 |
| &#39;Direct API: ILogErrorStore.GetAll()&#39; | 0          |    61.43 ns |   3.307 ns |  0.859 ns |  1.00 |    0.02 |    2 | 0.0057 |      96 B |        1.00 |
| &#39;MCP: loggerhelper_get_errors&#39;        | 0          |    63.04 ns |   2.596 ns |  0.674 ns |  1.03 |    0.02 |    2 | 0.0057 |      96 B |        1.00 |
| &#39;MCP: loggerhelper_get_health&#39;        | 0          |    99.95 ns |   0.934 ns |  0.243 ns |  1.63 |    0.02 |    3 | 0.0143 |     240 B |        2.50 |
| &#39;MCP: loggerhelper_get_config&#39;        | 0          |   306.07 ns |   9.065 ns |  1.403 ns |  4.98 |    0.07 |    4 | 0.0792 |    1328 B |       13.83 |
|                                       |            |             |            |           |       |         |      |        |           |             |
| &#39;MCP: loggerhelper_get_sinks&#39;         | 10         |    28.56 ns |   1.014 ns |  0.263 ns |  0.14 |    0.00 |    1 | 0.0019 |      32 B |        0.16 |
| &#39;MCP: loggerhelper_get_health&#39;        | 10         |   108.24 ns |   5.386 ns |  0.833 ns |  0.51 |    0.00 |    2 | 0.0153 |     256 B |        1.28 |
| &#39;Direct API: ILogErrorStore.GetAll()&#39; | 10         |   211.29 ns |   5.795 ns |  1.505 ns |  1.00 |    0.01 |    3 | 0.0119 |     200 B |        1.00 |
| &#39;MCP: loggerhelper_get_config&#39;        | 10         |   303.37 ns |  12.802 ns |  3.325 ns |  1.44 |    0.02 |    4 | 0.0792 |    1328 B |        6.64 |
| &#39;MCP: loggerhelper_get_errors&#39;        | 10         | 1,638.94 ns |  54.503 ns | 14.154 ns |  7.76 |    0.08 |    5 | 0.1678 |    2808 B |       14.04 |
|                                       |            |             |            |           |       |         |      |        |           |             |
| &#39;MCP: loggerhelper_get_sinks&#39;         | 100        |    29.09 ns |   1.289 ns |  0.335 ns |  0.02 |    0.00 |    1 | 0.0019 |      32 B |        0.03 |
| &#39;MCP: loggerhelper_get_health&#39;        | 100        |   112.71 ns |   7.508 ns |  1.950 ns |  0.08 |    0.00 |    2 | 0.0153 |     256 B |        0.28 |
| &#39;MCP: loggerhelper_get_config&#39;        | 100        |   307.72 ns |  21.474 ns |  5.577 ns |  0.23 |    0.00 |    3 | 0.0792 |    1328 B |        1.44 |
| &#39;Direct API: ILogErrorStore.GetAll()&#39; | 100        | 1,343.69 ns |  22.971 ns |  3.555 ns |  1.00 |    0.00 |    4 | 0.0534 |     920 B |        1.00 |
| &#39;MCP: loggerhelper_get_errors&#39;        | 100        | 4,016.28 ns | 171.202 ns | 44.461 ns |  2.99 |    0.03 |    5 | 0.3281 |    5600 B |        6.09 |

---

## Sampling

```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.301
  [Host]   : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2
  ShortRun : .NET 9.0.17 (9.0.1726.26416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=5  LaunchCount=1  
WarmupCount=3  

```
| Method                                 | Mean      | Error     | StdDev    | Ratio | Rank | Allocated | Alloc Ratio |
|--------------------------------------- |----------:|----------:|----------:|------:|-----:|----------:|------------:|
| &#39;Matches() — level check only&#39;         |  4.305 ns | 0.0284 ns | 0.0074 ns |  1.00 |    1 |         - |          NA |
| &#39;ShouldEmit(null) — sampling disabled&#39; |  5.240 ns | 0.0467 ns | 0.0121 ns |  1.22 |    1 |         - |          NA |
| &#39;ShouldEmit(0.5) — 50% sampling&#39;       | 10.171 ns | 0.0901 ns | 0.0234 ns |  2.36 |    2 |         - |          NA |

---

_Benchmarks run automatically on each release via [GitHub Actions](../.github/workflows/benchmarks.yml)._
