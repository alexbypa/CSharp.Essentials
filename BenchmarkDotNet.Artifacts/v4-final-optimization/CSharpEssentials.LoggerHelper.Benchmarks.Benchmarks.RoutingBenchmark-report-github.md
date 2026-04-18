```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8246)
AMD Ryzen 5 5600G with Radeon Graphics, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 9.0.14 (9.0.1426.11910), X64 RyuJIT AVX2
  ShortRun : .NET 9.0.14 (9.0.1426.11910), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                   | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| NLog_Single_Info         |  63.42 ns |  44.54 ns |  2.441 ns |  0.33 |    0.03 |    1 | 0.0210 |     176 B |        0.46 |
| NLog_Multi_Info          |  69.12 ns |  34.87 ns |  1.912 ns |  0.36 |    0.03 |    1 | 0.0210 |     176 B |        0.46 |
| NLog_Multi_Error         |  81.89 ns |  68.16 ns |  3.736 ns |  0.43 |    0.04 |    1 | 0.0210 |     176 B |        0.46 |
| Serilog_Single_Info      | 190.74 ns | 300.49 ns | 16.471 ns |  1.00 |    0.10 |    2 | 0.0458 |     384 B |        1.00 |
| Serilog_Multi_Info       | 298.04 ns | 327.12 ns | 17.930 ns |  1.57 |    0.14 |    3 | 0.1163 |     976 B |        2.54 |
| Serilog_Multi_Error      | 309.56 ns | 328.70 ns | 18.017 ns |  1.63 |    0.14 |    3 | 0.1163 |     976 B |        2.54 |
| LoggerHelper_Single_Info | 500.10 ns | 462.76 ns | 25.365 ns |  2.63 |    0.22 |    4 | 0.1373 |    1152 B |        3.00 |
| LoggerHelper_Multi_Info  | 570.29 ns | 641.03 ns | 35.137 ns |  3.00 |    0.27 |    4 | 0.1860 |    1560 B |        4.06 |
| LoggerHelper_Multi_Error | 580.16 ns | 264.32 ns | 14.488 ns |  3.06 |    0.23 |    4 | 0.1860 |    1560 B |        4.06 |
