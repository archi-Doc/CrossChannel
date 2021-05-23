``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19043.985 (21H1/May2021Update)
Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK=5.0.203
  [Host]    : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT
  MediumRun : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|              Method |       Mean |     Error |    StdDev |     Median |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------- |-----------:|----------:|----------:|-----------:|-------:|------:|------:|----------:|
|          Radio_Open |  50.784 ns | 0.5998 ns | 0.8978 ns |  50.767 ns | 0.0114 |     - |     - |      48 B |
|       Radio_OpenKey |  57.632 ns | 0.1087 ns | 0.1627 ns |  57.634 ns | 0.0135 |     - |     - |      56 B |
|    Radio_OpenTwoWay |  50.183 ns | 0.2385 ns | 0.3420 ns |  50.077 ns | 0.0114 |     - |     - |      48 B |
| Radio_OpenTwoWayKey |  57.773 ns | 0.1298 ns | 0.1862 ns |  57.792 ns | 0.0135 |     - |     - |      56 B |
|          Class_Open |  70.498 ns | 1.7349 ns | 2.3747 ns |  72.347 ns | 0.0114 |     - |     - |      48 B |
|       Class_OpenKey |  92.001 ns | 0.8801 ns | 1.2900 ns |  91.433 ns | 0.0211 |     - |     - |      88 B |
|    Class_OpenTwoWay |  83.422 ns | 0.4797 ns | 0.6879 ns |  83.063 ns | 0.0191 |     - |     - |      80 B |
| Class_OpenTwoWayKey |  99.484 ns | 0.3034 ns | 0.4254 ns |  99.368 ns | 0.0230 |     - |     - |      96 B |
|          Radio_Send |   5.037 ns | 0.1905 ns | 0.2793 ns |   4.867 ns |      - |     - |     - |         - |
|       Radio_SendKey |  12.656 ns | 0.1736 ns | 0.2545 ns |  12.447 ns |      - |     - |     - |         - |
|    Radio_SendTwoWay |  18.325 ns | 0.2839 ns | 0.4161 ns |  18.139 ns | 0.0172 |     - |     - |      72 B |
| Radio_SendTwoWayKey |  26.264 ns | 0.0385 ns | 0.0539 ns |  26.253 ns | 0.0172 |     - |     - |      72 B |
|          Class_Send |  19.102 ns | 0.2788 ns | 0.4173 ns |  19.332 ns |      - |     - |     - |         - |
|       Class_SendKey |  41.382 ns | 0.4741 ns | 0.6799 ns |  41.231 ns | 0.0076 |     - |     - |      32 B |
|    Class_SendTwoWay |  46.961 ns | 0.0795 ns | 0.1140 ns |  46.956 ns | 0.0249 |     - |     - |     104 B |
| Class_SendTwoWayKey |  61.940 ns | 0.5976 ns | 0.7977 ns |  62.584 ns | 0.0267 |     - |     - |     112 B |
|     Radio_Weak_Open | 405.360 ns | 1.6483 ns | 2.3107 ns | 406.124 ns | 0.0458 |     - |     - |     192 B |
|     Radio_Weak_Send |  15.726 ns | 0.2326 ns | 0.3410 ns |  15.480 ns |      - |     - |     - |         - |
