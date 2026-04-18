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
| Serilog_BelowMinLevel          |   4.671 ns |   2.673 ns |  0.1465 ns |  0.03 |    0.00 |    1 |      - |         - |        0.00 |
| NLog_BelowMinLevel             |  18.875 ns |  19.500 ns |  1.0689 ns |  0.10 |    0.01 |    2 |      - |         - |        0.00 |
| LoggerHelper_BelowMinLevel     |  30.641 ns |  41.215 ns |  2.2591 ns |  0.16 |    0.01 |    3 | 0.0067 |      56 B |        0.15 |
| NLog_SingleMessage             |  64.965 ns |  35.774 ns |  1.9609 ns |  0.35 |    0.02 |    4 | 0.0210 |     176 B |        0.46 |
| NLog_StructuredPayload         |  78.823 ns |  54.968 ns |  3.0130 ns |  0.42 |    0.03 |    4 | 0.0267 |     224 B |        0.58 |
| Serilog_SingleMessage          | 186.838 ns | 256.420 ns | 14.0552 ns |  1.00 |    0.09 |    5 | 0.0458 |     384 B |        1.00 |
| Serilog_StructuredPayload      | 264.015 ns | 275.035 ns | 15.0756 ns |  1.42 |    0.11 |    6 | 0.0591 |     496 B |        1.29 |
| LoggerHelper_SingleMessage     | 488.634 ns | 300.009 ns | 16.4445 ns |  2.62 |    0.18 |    7 | 0.1373 |    1152 B |        3.00 |
| LoggerHelper_StructuredPayload | 614.003 ns | 501.038 ns | 27.4636 ns |  3.30 |    0.25 |    8 | 0.1545 |    1296 B |        3.38 |
