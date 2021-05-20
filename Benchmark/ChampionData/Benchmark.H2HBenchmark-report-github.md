``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19043.985 (21H1/May2021Update)
Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK=5.0.203
  [Host]    : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT
  MediumRun : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|           Method |        Mean |     Error |     StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------- |------------:|----------:|-----------:|-------:|------:|------:|----------:|
|      CC_OpenSend |    55.48 ns |  0.152 ns |   0.213 ns | 0.0114 |     - |     - |      48 B |
|     CC_OpenSend8 |    88.95 ns |  0.128 ns |   0.192 ns | 0.0114 |     - |     - |      48 B |
|      MP_OpenSend |   179.64 ns |  0.631 ns |   0.884 ns | 0.0134 |     - |     - |      56 B |
|     MP_OpenSend8 |   234.70 ns |  0.394 ns |   0.590 ns | 0.0134 |     - |     - |      56 B |
|      PS_OpenSend |   402.67 ns |  0.783 ns |   1.148 ns | 0.1144 |     - |     - |     480 B |
|     PS_OpenSend8 | 1,253.93 ns | 77.629 ns | 116.191 ns | 0.3815 |     - |     - |   1,600 B |
|  CC_OpenSend_Key |    71.84 ns |  0.929 ns |   1.390 ns | 0.0135 |     - |     - |      56 B |
| CC_OpenSend8_Key |   168.34 ns |  2.873 ns |   4.300 ns | 0.0134 |     - |     - |      56 B |
|  MP_OpenSend_Key |   287.17 ns |  4.814 ns |   6.904 ns | 0.0725 |     - |     - |     304 B |
| MP_OpenSend8_Key |   467.52 ns |  0.884 ns |   1.295 ns | 0.0725 |     - |     - |     304 B |
