```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.22621.4037/22H2/2022Update/SunValley2)
12th Gen Intel Core i7-1280P, 1 CPU, 20 logical and 14 physical cores
.NET SDK 9.0.100-preview.7.24407.12
  [Host]    : .NET 8.0.7 (8.0.724.31311), X64 RyuJIT AVX2
  MediumRun : .NET 8.0.7 (8.0.724.31311), X64 RyuJIT AVX2

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method               | Mean       | Error      | StdDev     | Gen0   | Allocated |
|--------------------- |-----------:|-----------:|-----------:|-------:|----------:|
| Send                 |   1.916 ns |  0.0200 ns |  0.0287 ns |      - |         - |
| OpenSend             |  39.654 ns |  0.3066 ns |  0.4494 ns | 0.0038 |      48 B |
| OpenSend8            |  54.575 ns |  0.3954 ns |  0.5796 ns | 0.0038 |      48 B |
| OpenSend_Weak        | 134.302 ns |  7.7571 ns | 11.3703 ns | 0.0057 |      72 B |
| OpenSend8_Weak       | 139.289 ns |  3.1632 ns |  4.5366 ns | 0.0057 |      72 B |
| SendKey              |   8.722 ns |  0.1016 ns |  0.1520 ns |      - |         - |
| OpenSend_Key         | 124.375 ns |  4.7073 ns |  6.5990 ns | 0.0241 |     304 B |
| OpenSend8_Key        | 287.545 ns |  9.2775 ns | 13.8862 ns | 0.0238 |     304 B |
| Class_Send           |   8.061 ns |  0.4541 ns |  0.6656 ns |      - |         - |
| Class_OpenSend       |  47.849 ns |  2.0198 ns |  2.9606 ns | 0.0038 |      48 B |
| Class_OpenSend8      |  82.368 ns |  0.6213 ns |  0.8911 ns | 0.0038 |      48 B |
| Class_OpenSend_Weak  | 156.877 ns |  8.0446 ns | 11.5373 ns | 0.0057 |      72 B |
| Class_OpenSend8_Weak | 217.078 ns | 17.0128 ns | 23.8496 ns | 0.0057 |      72 B |
| Class_SendKey        |   9.470 ns |  0.2608 ns |  0.3823 ns |      - |         - |
| Class_OpenSend_Key   | 126.246 ns |  2.0165 ns |  2.8920 ns | 0.0241 |     304 B |
| Class_OpenSend8_Key  | 285.156 ns |  8.0497 ns | 11.5447 ns | 0.0238 |     304 B |
