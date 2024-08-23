```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.22621.4037/22H2/2022Update/SunValley2)
12th Gen Intel Core i7-1280P, 1 CPU, 20 logical and 14 physical cores
.NET SDK 9.0.100-preview.7.24407.12
  [Host]    : .NET 8.0.7 (8.0.724.31311), X64 RyuJIT AVX2
  MediumRun : .NET 8.0.7 (8.0.724.31311), X64 RyuJIT AVX2

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method        | Mean        | Error     | StdDev     | Gen0   | Allocated |
|-------------- |------------:|----------:|-----------:|-------:|----------:|
| CC_OpenSend   |    41.91 ns |  0.650 ns |   0.972 ns | 0.0038 |      48 B |
| CC_OpenSend8  |    54.13 ns |  0.909 ns |   1.274 ns | 0.0038 |      48 B |
| CC_OpenSend88 |   365.16 ns |  4.738 ns |   7.091 ns | 0.0305 |     384 B |
| MP_OpenSend   |    89.58 ns |  0.938 ns |   1.404 ns | 0.0044 |      56 B |
| MP_OpenSend8  |    98.55 ns |  1.161 ns |   1.702 ns | 0.0044 |      56 B |
| MP_OpenSend88 |   805.17 ns | 12.591 ns |  18.845 ns | 0.0353 |     448 B |
| PS_OpenSend   |   267.89 ns | 13.514 ns |  20.228 ns | 0.0381 |     480 B |
| PS_OpenSend8  |   672.32 ns | 65.482 ns |  95.982 ns | 0.1268 |    1600 B |
| PS_OpenSend88 | 2,921.00 ns | 99.322 ns | 145.584 ns | 0.3586 |    4544 B |
