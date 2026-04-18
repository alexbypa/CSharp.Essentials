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
| NLog_Single_Info         |  63.93 ns |  87.58 ns |  4.800 ns |  0.35 |    0.03 |    1 | 0.0210 |     176 B |        0.46 |
| NLog_Multi_Info          |  64.47 ns |  80.99 ns |  4.439 ns |  0.35 |    0.03 |    1 | 0.0210 |     176 B |        0.46 |
| NLog_Multi_Error         |  77.80 ns |  67.08 ns |  3.677 ns |  0.43 |    0.02 |    1 | 0.0210 |     176 B |        0.46 |
| Serilog_Single_Info      | 182.74 ns | 146.03 ns |  8.005 ns |  1.00 |    0.05 |    2 | 0.0458 |     384 B |        1.00 |
| Serilog_Multi_Error      | 295.99 ns | 256.10 ns | 14.038 ns |  1.62 |    0.09 |    3 | 0.1163 |     976 B |        2.54 |
| Serilog_Multi_Info       | 306.43 ns | 255.72 ns | 14.017 ns |  1.68 |    0.09 |    3 | 0.1163 |     976 B |        2.54 |
| LoggerHelper_Single_Info | 507.24 ns | 114.94 ns |  6.300 ns |  2.78 |    0.11 |    4 | 0.1373 |    1152 B |        3.00 |
| LoggerHelper_Multi_Info  | 602.62 ns | 333.30 ns | 18.269 ns |  3.30 |    0.15 |    4 | 0.1860 |    1560 B |        4.06 |
| LoggerHelper_Multi_Error | 639.53 ns | 189.48 ns | 10.386 ns |  3.50 |    0.14 |    4 | 0.1860 |    1560 B |        4.06 |
