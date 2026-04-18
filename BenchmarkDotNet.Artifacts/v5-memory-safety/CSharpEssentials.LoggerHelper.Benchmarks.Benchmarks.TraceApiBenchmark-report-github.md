```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8246)
AMD Ryzen 5 5600G with Radeon Graphics, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 9.0.14 (9.0.1426.11910), X64 RyuJIT AVX2
  ShortRun : .NET 9.0.14 (9.0.1426.11910), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                             | Mean        | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------------- |------------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| NLog_Raw                           |    83.27 ns | 159.77 ns |  8.758 ns |  0.35 |    0.04 |    1 | 0.0257 |      - |     216 B |        0.47 |
| Serilog_Raw                        |   238.63 ns | 348.38 ns | 19.096 ns |  1.00 |    0.10 |    2 | 0.0544 |      - |     456 B |        1.00 |
| LoggerHelper_ILogger_WithException |   485.41 ns | 378.93 ns | 20.771 ns |  2.04 |    0.16 |    3 | 0.1373 |      - |    1152 B |        2.53 |
| LoggerHelper_ILogger               |   557.96 ns | 768.22 ns | 42.109 ns |  2.35 |    0.22 |    3 | 0.1478 |      - |    1240 B |        2.72 |
| LoggerHelper_BeginTrace_5Logs      |   635.78 ns | 507.01 ns | 27.791 ns |  2.68 |    0.21 |    3 | 0.1579 |      - |    1325 B |        2.91 |
| LoggerHelper_Trace_WithException   | 1,274.40 ns | 947.51 ns | 51.936 ns |  5.36 |    0.41 |    4 | 0.2918 | 0.1736 |    2456 B |        5.39 |
| LoggerHelper_Trace                 | 1,470.43 ns | 880.47 ns | 48.262 ns |  6.19 |    0.45 |    4 | 0.3204 | 0.2155 |    2688 B |        5.89 |
