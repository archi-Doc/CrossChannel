``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.203
  [Host]    : .NET Core 5.0.6 (CoreCLR 5.0.621.22011, CoreFX 5.0.621.22011), X64 RyuJIT
  MediumRun : .NET Core 5.0.6 (CoreCLR 5.0.621.22011, CoreFX 5.0.621.22011), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|           Method |        Mean |     Error |    StdDev |      Median |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------- |------------:|----------:|----------:|------------:|-------:|------:|------:|----------:|
|      CC_OpenSend |    53.68 ns |  0.110 ns |  0.165 ns |    53.67 ns | 0.0114 |     - |     - |      48 B |
|     CC_OpenSend8 |    89.12 ns |  0.177 ns |  0.264 ns |    89.11 ns | 0.0114 |     - |     - |      48 B |
|      MP_OpenSend |   185.20 ns |  1.825 ns |  2.618 ns |   185.52 ns | 0.0134 |     - |     - |      56 B |
|     MP_OpenSend8 |   237.80 ns |  1.989 ns |  2.977 ns |   237.21 ns | 0.0134 |     - |     - |      56 B |
|      PS_OpenSend |   409.70 ns |  6.022 ns |  8.827 ns |   406.14 ns | 0.1144 |     - |     - |     480 B |
|     PS_OpenSend8 | 1,190.13 ns | 16.479 ns | 22.000 ns | 1,185.50 ns | 0.3815 |     - |     - |    1600 B |
|  CC_OpenSend_Key |    88.92 ns |  0.665 ns |  0.975 ns |    88.93 ns | 0.0138 |     - |     - |      58 B |
| CC_OpenSend8_Key |   187.39 ns |  0.618 ns |  0.924 ns |   187.27 ns | 0.0138 |     - |     - |      58 B |
|  MP_OpenSend_Key |   283.73 ns |  0.631 ns |  0.924 ns |   283.66 ns | 0.0725 |     - |     - |     304 B |
| MP_OpenSend8_Key |   471.66 ns |  0.983 ns |  1.441 ns |   470.84 ns | 0.0725 |     - |     - |     304 B |
