```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8246)
Unknown processor
.NET SDK 10.0.201
  [Host]   : .NET 9.0.15 (9.0.1526.17522), X64 RyuJIT AVX2
  ShortRun : .NET 9.0.15 (9.0.1526.17522), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                   | Mean      | Error       | StdDev     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------:|------------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| NLog_Single_Info         |  93.05 ns |    72.83 ns |   3.992 ns |  0.37 |    0.04 |    1 | 0.0280 |     176 B |        0.46 |
| NLog_Multi_Info          |  97.38 ns |    30.77 ns |   1.687 ns |  0.39 |    0.04 |    1 | 0.0280 |     176 B |        0.46 |
| NLog_Multi_Error         | 131.33 ns |   157.23 ns |   8.618 ns |  0.52 |    0.06 |    2 | 0.0279 |     176 B |        0.46 |
| Serilog_Single_Info      | 254.92 ns |   538.75 ns |  29.531 ns |  1.01 |    0.14 |    3 | 0.0610 |     384 B |        1.00 |
| Serilog_Multi_Error      | 377.65 ns |   169.35 ns |   9.283 ns |  1.49 |    0.14 |    4 | 0.1554 |     976 B |        2.54 |
| Serilog_Multi_Info       | 384.53 ns |   292.39 ns |  16.027 ns |  1.52 |    0.15 |    4 | 0.1554 |     976 B |        2.54 |
| LoggerHelper_Single_Info | 806.30 ns |   709.44 ns |  38.887 ns |  3.19 |    0.33 |    5 | 0.2651 |    1664 B |        4.33 |
| LoggerHelper_Multi_Info  | 957.08 ns | 1,226.17 ns |  67.211 ns |  3.79 |    0.43 |    5 | 0.3300 |    2072 B |        5.40 |
| LoggerHelper_Multi_Error | 977.78 ns | 1,956.55 ns | 107.245 ns |  3.87 |    0.52 |    5 | 0.3300 |    2072 B |        5.40 |
