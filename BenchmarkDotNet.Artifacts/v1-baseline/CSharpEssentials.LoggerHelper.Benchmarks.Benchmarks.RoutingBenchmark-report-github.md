```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8246)
AMD Ryzen 5 5600G with Radeon Graphics, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 9.0.14 (9.0.1426.11910), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.14 (9.0.1426.11910), X64 RyuJIT AVX2


```
| Method                   | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| NLog_Single_Info         |  61.09 ns |  0.709 ns |  0.553 ns |  0.35 |    0.01 |    1 | 0.0210 |     176 B |        0.46 |
| NLog_Multi_Info          |  62.12 ns |  1.272 ns |  3.546 ns |  0.35 |    0.02 |    1 | 0.0210 |     176 B |        0.46 |
| NLog_Multi_Error         |  80.95 ns |  1.640 ns |  3.833 ns |  0.46 |    0.03 |    2 | 0.0210 |     176 B |        0.46 |
| Serilog_Single_Info      | 177.19 ns |  3.536 ns |  7.381 ns |  1.00 |    0.06 |    3 | 0.0458 |     384 B |        1.00 |
| Serilog_Multi_Info       | 286.12 ns |  5.731 ns | 13.949 ns |  1.62 |    0.10 |    4 | 0.1163 |     976 B |        2.54 |
| Serilog_Multi_Error      | 306.22 ns |  6.177 ns | 16.164 ns |  1.73 |    0.12 |    5 | 0.1163 |     976 B |        2.54 |
| LoggerHelper_Single_Info | 716.65 ns | 14.259 ns | 33.046 ns |  4.05 |    0.25 |    6 | 0.1984 |    1664 B |        4.33 |
| LoggerHelper_Multi_Info  | 842.03 ns | 16.828 ns | 39.665 ns |  4.76 |    0.30 |    7 | 0.2470 |    2072 B |        5.40 |
| LoggerHelper_Multi_Error | 860.86 ns | 20.341 ns | 59.012 ns |  4.87 |    0.39 |    7 | 0.2460 |    2072 B |        5.40 |
