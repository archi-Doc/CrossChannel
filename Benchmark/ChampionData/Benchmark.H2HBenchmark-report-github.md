```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.2314)
13th Gen Intel Core i9-13900K, 1 CPU, 32 logical and 24 physical cores
.NET SDK 9.0.100-rc.2.24474.11
  [Host]    : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2
  MediumRun : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method        | Mean        | Error     | StdDev    | Median      | Gen0   | Allocated |
|-------------- |------------:|----------:|----------:|------------:|-------:|----------:|
| CC_OpenSend   |    29.12 ns |  0.353 ns |  0.518 ns |    29.00 ns | 0.0025 |      48 B |
| CC_OpenSend8  |    34.44 ns |  0.828 ns |  1.239 ns |    34.35 ns | 0.0025 |      48 B |
| CC_OpenSend88 |   282.60 ns | 13.417 ns | 19.666 ns |   298.49 ns | 0.0200 |     384 B |
| MP_OpenSend   |    69.54 ns |  0.427 ns |  0.640 ns |    69.50 ns | 0.0029 |      56 B |
| MP_OpenSend8  |    73.48 ns |  0.350 ns |  0.523 ns |    73.50 ns | 0.0029 |      56 B |
| MP_OpenSend88 |   612.95 ns |  3.361 ns |  5.030 ns |   613.16 ns | 0.0229 |     448 B |
| PS_OpenSend   |   150.37 ns |  0.739 ns |  1.106 ns |   150.18 ns | 0.0229 |     432 B |
| PS_OpenSend8  |   397.44 ns |  3.054 ns |  4.571 ns |   398.00 ns | 0.0734 |    1384 B |
| PS_OpenSend88 | 2,914.53 ns | 44.459 ns | 66.544 ns | 2,902.36 ns | 0.2060 |    3904 B |
