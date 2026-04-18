```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8246)
AMD Ryzen 5 5600G with Radeon Graphics, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 9.0.14 (9.0.1426.11910), X64 RyuJIT AVX2
  ShortRun : .NET 9.0.14 (9.0.1426.11910), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                | Mean        | Error       | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |------------:|------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| NLog_Raw                              |    74.12 ns |   117.91 ns |   6.463 ns |  0.32 |    0.04 |    1 | 0.0257 |      - |     216 B |        0.47 |
| Serilog_Raw                           |   235.41 ns |   429.44 ns |  23.539 ns |  1.01 |    0.12 |    2 | 0.0544 |      - |     456 B |        1.00 |
| LoggerHelper_ILogger                  |   659.77 ns |   661.82 ns |  36.277 ns |  2.82 |    0.27 |    3 | 0.1478 |      - |    1240 B |        2.72 |
| LoggerHelper_TraceAsync_WithException |   975.91 ns | 1,327.33 ns |  72.756 ns |  4.17 |    0.44 |    4 | 0.1812 | 0.0553 |    1552 B |        3.40 |
| LoggerHelper_TraceAsync               | 1,038.29 ns |   231.82 ns |  12.707 ns |  4.44 |    0.37 |    4 | 0.1945 | 0.0648 |    1671 B |        3.66 |
| LoggerHelper_TraceSync                | 1,479.79 ns |   947.13 ns |  51.916 ns |  6.33 |    0.56 |    5 | 0.3204 | 0.2155 |    2688 B |        5.89 |
| LoggerHelper_TraceSync_WithException  | 1,561.46 ns | 5,497.91 ns | 301.359 ns |  6.68 |    1.25 |    5 | 0.2918 | 0.1736 |    2456 B |        5.39 |
