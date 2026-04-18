```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8246)
AMD Ryzen 5 5600G with Radeon Graphics, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 9.0.14 (9.0.1426.11910), X64 RyuJIT AVX2
  ShortRun : .NET 9.0.14 (9.0.1426.11910), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                         | Mean       | Error      | StdDev     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |-----------:|-----------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Serilog_BelowMinLevel          |   4.774 ns |   3.534 ns |  0.1937 ns |  0.02 |    0.00 |    1 |      - |         - |        0.00 |
| NLog_BelowMinLevel             |  19.070 ns |  17.400 ns |  0.9538 ns |  0.10 |    0.01 |    2 |      - |         - |        0.00 |
| LoggerHelper_BelowMinLevel     |  31.918 ns |  53.837 ns |  2.9510 ns |  0.17 |    0.02 |    3 | 0.0067 |      56 B |        0.15 |
| NLog_SingleMessage             |  68.391 ns |  84.782 ns |  4.6472 ns |  0.35 |    0.03 |    4 | 0.0210 |     176 B |        0.46 |
| NLog_StructuredPayload         |  83.308 ns |  43.265 ns |  2.3715 ns |  0.43 |    0.03 |    4 | 0.0267 |     224 B |        0.58 |
| Serilog_SingleMessage          | 193.680 ns | 240.700 ns | 13.1936 ns |  1.00 |    0.08 |    5 | 0.0458 |     384 B |        1.00 |
| Serilog_StructuredPayload      | 278.721 ns | 185.328 ns | 10.1585 ns |  1.44 |    0.10 |    6 | 0.0591 |     496 B |        1.29 |
| LoggerHelper_SingleMessage     | 493.452 ns | 135.683 ns |  7.4373 ns |  2.56 |    0.16 |    7 | 0.1373 |    1152 B |        3.00 |
| LoggerHelper_StructuredPayload | 610.049 ns | 596.635 ns | 32.7036 ns |  3.16 |    0.24 |    7 | 0.1545 |    1296 B |        3.38 |
