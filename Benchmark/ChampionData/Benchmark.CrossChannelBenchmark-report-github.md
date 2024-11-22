```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.2314)
13th Gen Intel Core i9-13900K, 1 CPU, 32 logical and 24 physical cores
.NET SDK 9.0.100-rc.2.24474.11
  [Host]    : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2
  MediumRun : .NET 9.0.0 (9.0.24.47305), X64 RyuJIT AVX2

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method               | Mean       | Error     | StdDev    | Median     | Gen0   | Allocated |
|--------------------- |-----------:|----------:|----------:|-----------:|-------:|----------:|
| Send                 |   1.173 ns | 0.0233 ns | 0.0349 ns |   1.178 ns |      - |         - |
| OpenSend             |  28.623 ns | 0.6869 ns | 0.9630 ns |  29.125 ns | 0.0025 |      48 B |
| OpenSend8            |  32.368 ns | 0.2039 ns | 0.2989 ns |  32.368 ns | 0.0025 |      48 B |
| OpenSend_Weak        |  93.232 ns | 0.9040 ns | 1.3530 ns |  93.206 ns | 0.0038 |      72 B |
| OpenSend8_Weak       | 100.826 ns | 0.7967 ns | 1.1925 ns | 100.890 ns | 0.0038 |      72 B |
| SendKey              |   6.053 ns | 0.0826 ns | 0.1236 ns |   6.075 ns |      - |         - |
| OpenSend_Key         |  80.442 ns | 0.6470 ns | 0.9484 ns |  80.127 ns | 0.0161 |     304 B |
| OpenSend8_Key        | 181.158 ns | 0.7905 ns | 1.1831 ns | 181.168 ns | 0.0160 |     304 B |
| Class_Send           |   4.316 ns | 0.0777 ns | 0.1163 ns |   4.251 ns |      - |         - |
| Class_OpenSend       |  28.373 ns | 0.1863 ns | 0.2788 ns |  28.325 ns | 0.0025 |      48 B |
| Class_OpenSend8      |  54.114 ns | 0.4175 ns | 0.6119 ns |  54.123 ns | 0.0025 |      48 B |
| Class_OpenSend_Weak  |  95.118 ns | 1.0575 ns | 1.5167 ns |  95.435 ns | 0.0038 |      72 B |
| Class_OpenSend8_Weak | 126.428 ns | 0.5649 ns | 0.8455 ns | 126.384 ns | 0.0038 |      72 B |
| Class_SendKey        |   5.054 ns | 0.0258 ns | 0.0387 ns |   5.046 ns |      - |         - |
| Class_OpenSend_Key   |  80.126 ns | 0.3265 ns | 0.4887 ns |  80.173 ns | 0.0161 |     304 B |
| Class_OpenSend8_Key  | 180.394 ns | 0.9119 ns | 1.3649 ns | 180.089 ns | 0.0160 |     304 B |
