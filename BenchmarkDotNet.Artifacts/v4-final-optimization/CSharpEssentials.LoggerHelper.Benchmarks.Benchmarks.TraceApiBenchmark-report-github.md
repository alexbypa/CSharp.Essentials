```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8246)
AMD Ryzen 5 5600G with Radeon Graphics, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 9.0.14 (9.0.1426.11910), X64 RyuJIT AVX2
  ShortRun : .NET 9.0.14 (9.0.1426.11910), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                             | Mean       | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------- |-----------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| NLog_Raw                           |   102.2 ns |   367.7 ns |  20.15 ns |  0.38 |    0.07 |    1 | 0.0257 |      - |     216 B |        0.47 |
| Serilog_Raw                        |   269.7 ns |   297.9 ns |  16.33 ns |  1.00 |    0.07 |    2 | 0.0544 |      - |     456 B |        1.00 |
| LoggerHelper_ILogger_WithException |   490.1 ns |   118.7 ns |   6.51 ns |  1.82 |    0.09 |    3 | 0.1373 |      - |    1152 B |        2.53 |
| LoggerHelper_ILogger               |   568.6 ns |   558.4 ns |  30.61 ns |  2.11 |    0.15 |    3 | 0.1478 |      - |    1240 B |        2.72 |
| LoggerHelper_BeginTrace_5Logs      |   657.0 ns |   368.1 ns |  20.18 ns |  2.44 |    0.14 |    3 | 0.1640 |      - |    1378 B |        3.02 |
| LoggerHelper_Trace_WithException   | 1,313.4 ns | 1,912.1 ns | 104.81 ns |  4.88 |    0.42 |    4 | 0.2918 | 0.1736 |    2456 B |        5.39 |
| LoggerHelper_Trace                 | 1,481.8 ns |   964.6 ns |  52.87 ns |  5.51 |    0.33 |    4 | 0.3204 | 0.2155 |    2688 B |        5.89 |
