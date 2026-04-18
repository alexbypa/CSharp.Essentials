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
| Serilog_BelowMinLevel          |   4.516 ns |   3.940 ns |  0.2159 ns |  0.02 |    0.00 |    1 |      - |         - |        0.00 |
| NLog_BelowMinLevel             |  18.468 ns |  13.409 ns |  0.7350 ns |  0.09 |    0.01 |    2 |      - |         - |        0.00 |
| LoggerHelper_BelowMinLevel     |  31.410 ns |  26.099 ns |  1.4306 ns |  0.15 |    0.02 |    3 | 0.0067 |      56 B |        0.15 |
| NLog_SingleMessage             |  64.807 ns |  57.397 ns |  3.1461 ns |  0.31 |    0.03 |    4 | 0.0210 |     176 B |        0.46 |
| NLog_StructuredPayload         |  81.285 ns |  60.891 ns |  3.3376 ns |  0.39 |    0.04 |    5 | 0.0267 |     224 B |        0.58 |
| Serilog_SingleMessage          | 212.358 ns | 481.996 ns | 26.4198 ns |  1.01 |    0.15 |    6 | 0.0458 |     384 B |        1.00 |
| Serilog_StructuredPayload      | 279.578 ns | 451.558 ns | 24.7514 ns |  1.33 |    0.17 |    7 | 0.0591 |     496 B |        1.29 |
| LoggerHelper_SingleMessage     | 508.708 ns | 561.837 ns | 30.7962 ns |  2.42 |    0.28 |    8 | 0.1373 |    1152 B |        3.00 |
| LoggerHelper_StructuredPayload | 691.742 ns | 266.260 ns | 14.5946 ns |  3.29 |    0.35 |    9 | 0.1545 |    1296 B |        3.38 |
