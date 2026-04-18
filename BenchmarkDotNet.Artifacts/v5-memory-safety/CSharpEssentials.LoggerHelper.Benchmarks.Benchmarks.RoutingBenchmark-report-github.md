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
| NLog_Multi_Info          |  65.05 ns |  19.25 ns |  1.055 ns |  0.36 |    0.02 |    1 | 0.0210 |     176 B |        0.46 |
| NLog_Single_Info         |  65.77 ns |  48.59 ns |  2.663 ns |  0.36 |    0.02 |    1 | 0.0210 |     176 B |        0.46 |
| NLog_Multi_Error         |  78.70 ns |  58.36 ns |  3.199 ns |  0.43 |    0.03 |    1 | 0.0210 |     176 B |        0.46 |
| Serilog_Single_Info      | 181.92 ns | 206.68 ns | 11.329 ns |  1.00 |    0.08 |    2 | 0.0458 |     384 B |        1.00 |
| Serilog_Multi_Info       | 312.22 ns | 461.04 ns | 25.271 ns |  1.72 |    0.15 |    3 | 0.1163 |     976 B |        2.54 |
| Serilog_Multi_Error      | 316.20 ns | 452.63 ns | 24.810 ns |  1.74 |    0.15 |    3 | 0.1163 |     976 B |        2.54 |
| LoggerHelper_Single_Info | 488.82 ns | 229.77 ns | 12.595 ns |  2.69 |    0.15 |    4 | 0.1373 |    1152 B |        3.00 |
| LoggerHelper_Multi_Error | 557.52 ns | 437.03 ns | 23.955 ns |  3.07 |    0.20 |    4 | 0.1860 |    1560 B |        4.06 |
| LoggerHelper_Multi_Info  | 601.70 ns | 388.81 ns | 21.312 ns |  3.32 |    0.20 |    4 | 0.1860 |    1560 B |        4.06 |
