```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8246)
Unknown processor
.NET SDK 10.0.201
  [Host]   : .NET 9.0.15 (9.0.1526.17522), X64 RyuJIT AVX2
  ShortRun : .NET 9.0.15 (9.0.1526.17522), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error        | StdDev       | Ratio  | RatioSD | Rank | Gen0    | Gen1   | Gen2   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-------------:|-------------:|-------:|--------:|-----:|--------:|-------:|-------:|----------:|------------:|
| Serilog_Startup      |     848.5 ns |     529.0 ns |     29.00 ns |   1.00 |    0.04 |    1 |  0.4902 | 0.0019 |      - |   3.01 KB |        1.00 |
| NLog_Startup         | 156,034.4 ns | 986,148.7 ns | 54,054.14 ns | 184.05 |   55.52 |    2 |  9.2773 | 4.6387 | 0.2441 |  55.42 KB |       18.43 |
| LoggerHelper_Startup | 320,958.4 ns | 470,080.4 ns | 25,766.69 ns | 378.58 |   28.69 |    3 | 10.7422 |      - |      - |  68.05 KB |       22.62 |
