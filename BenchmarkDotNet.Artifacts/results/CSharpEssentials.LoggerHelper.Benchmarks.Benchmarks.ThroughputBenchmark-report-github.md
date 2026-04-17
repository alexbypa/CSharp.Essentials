```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8246)
Unknown processor
.NET SDK 10.0.201
  [Host]   : .NET 9.0.15 (9.0.1526.17522), X64 RyuJIT AVX2
  ShortRun : .NET 9.0.15 (9.0.1526.17522), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |-------------:|-------------:|------------:|------:|--------:|-----:|-------:|----------:|------------:|
| Serilog_BelowMinLevel          |     6.065 ns |     7.696 ns |   0.4218 ns |  0.02 |    0.00 |    1 |      - |         - |        0.00 |
| NLog_BelowMinLevel             |    28.034 ns |    11.623 ns |   0.6371 ns |  0.11 |    0.00 |    2 |      - |         - |        0.00 |
| LoggerHelper_BelowMinLevel     |    42.215 ns |    73.162 ns |   4.0103 ns |  0.17 |    0.01 |    3 | 0.0089 |      56 B |        0.15 |
| NLog_SingleMessage             |   106.491 ns |    88.902 ns |   4.8730 ns |  0.42 |    0.02 |    4 | 0.0280 |     176 B |        0.46 |
| NLog_StructuredPayload         |   113.169 ns |    14.631 ns |   0.8020 ns |  0.44 |    0.01 |    4 | 0.0356 |     224 B |        0.58 |
| Serilog_SingleMessage          |   254.948 ns |   162.015 ns |   8.8806 ns |  1.00 |    0.04 |    5 | 0.0610 |     384 B |        1.00 |
| Serilog_StructuredPayload      |   332.962 ns |   696.481 ns |  38.1765 ns |  1.31 |    0.14 |    5 | 0.0787 |     496 B |        1.29 |
| LoggerHelper_SingleMessage     |   979.814 ns | 1,224.528 ns |  67.1205 ns |  3.85 |    0.26 |    6 | 0.2651 |    1672 B |        4.35 |
| LoggerHelper_StructuredPayload | 1,283.596 ns | 2,035.680 ns | 111.5825 ns |  5.04 |    0.41 |    7 | 0.3052 |    1920 B |        5.00 |
