```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8246)
AMD Ryzen 5 5600G with Radeon Graphics, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 9.0.14 (9.0.1426.11910), X64 RyuJIT AVX2
  ShortRun : .NET 9.0.14 (9.0.1426.11910), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error        | StdDev       | Ratio  | RatioSD | Rank | Gen0   | Gen1   | Gen2   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-------------:|-------------:|-------:|--------:|-----:|-------:|-------:|-------:|----------:|------------:|
| Serilog_Startup      |     805.9 ns |   1,762.3 ns |     96.60 ns |   1.01 |    0.14 |    1 | 0.3681 | 0.0019 |      - |   3.01 KB |        1.00 |
| NLog_Startup         | 165,818.3 ns |  81,931.0 ns |  4,490.92 ns | 207.63 |   20.80 |    2 | 6.8359 | 6.5918 | 0.2441 |  55.42 KB |       18.42 |
| LoggerHelper_Startup | 257,630.9 ns | 317,731.0 ns | 17,415.91 ns | 322.59 |   36.69 |    3 | 7.8125 |      - |      - |  67.36 KB |       22.39 |
