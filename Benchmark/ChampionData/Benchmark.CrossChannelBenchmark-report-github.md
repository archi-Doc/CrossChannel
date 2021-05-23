``` ini

BenchmarkDotNet=v0.13.0, OS=Windows 10.0.19043.985 (21H1/May2021Update)
Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET SDK=5.0.203
  [Host]    : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT
  MediumRun : .NET 5.0.6 (5.0.621.22011), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|                    Method |       Mean |     Error |    StdDev |     Median |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------- |-----------:|----------:|----------:|-----------:|-------:|------:|------:|----------:|
|                      Send |   4.512 ns | 0.4828 ns | 0.7227 ns |   4.174 ns |      - |     - |     - |         - |
|                  OpenSend |  60.547 ns | 5.2228 ns | 7.3216 ns |  56.898 ns | 0.0114 |     - |     - |      48 B |
|                 OpenSend8 |  90.911 ns | 0.4991 ns | 0.6831 ns |  90.728 ns | 0.0114 |     - |     - |      48 B |
|             OpenSend_Weak | 578.745 ns | 5.2723 ns | 7.7281 ns | 580.278 ns | 0.0877 |     - |     - |     368 B |
|            OpenSend8_Weak | 711.867 ns | 2.6201 ns | 3.6730 ns | 712.656 ns | 0.0877 |     - |     - |     368 B |
|                   SendKey |   5.968 ns | 0.0070 ns | 0.0093 ns |   5.968 ns |      - |     - |     - |         - |
|              OpenSend_Key |  69.640 ns | 0.1429 ns | 0.2094 ns |  69.630 ns | 0.0135 |     - |     - |      56 B |
|             OpenSend8_Key | 159.856 ns | 0.7474 ns | 1.0956 ns | 159.747 ns | 0.0134 |     - |     - |      56 B |
|           OpenSend_TwoWay |  67.087 ns | 0.1734 ns | 0.2486 ns |  67.011 ns | 0.0286 |     - |     - |     120 B |
|          OpenSend8_TwoWay | 191.330 ns | 0.6769 ns | 0.9708 ns | 191.408 ns | 0.1490 |     - |     - |     624 B |
|        OpenSend_TwoWayKey |  82.961 ns | 1.0186 ns | 1.4608 ns |  82.838 ns | 0.0306 |     - |     - |     128 B |
|       OpenSend8_TwoWayKey | 261.667 ns | 0.5091 ns | 0.7620 ns | 261.489 ns | 0.1512 |     - |     - |     632 B |
|                Class_Send |   9.147 ns | 0.0210 ns | 0.0302 ns |   9.139 ns |      - |     - |     - |         - |
|            Class_OpenSend |  85.466 ns | 0.1168 ns | 0.1676 ns |  85.479 ns | 0.0114 |     - |     - |      48 B |
|           Class_OpenSend8 | 235.406 ns | 3.6422 ns | 5.3387 ns | 232.459 ns | 0.0114 |     - |     - |      48 B |
|        Class_OpenSend_Key | 148.839 ns | 0.4972 ns | 0.7130 ns | 149.042 ns | 0.0286 |     - |     - |     120 B |
|       Class_OpenSend8_Key | 461.095 ns | 3.7785 ns | 5.2969 ns | 457.573 ns | 0.0820 |     - |     - |     344 B |
|     Class_OpenSend_TwoWay | 134.476 ns | 0.4663 ns | 0.6979 ns | 134.338 ns | 0.0439 |     - |     - |     184 B |
|    Class_OpenSend8_TwoWay | 479.897 ns | 4.8755 ns | 7.1464 ns | 475.204 ns | 0.2174 |     - |     - |     912 B |
|  Class_OpenSend_TwoWayKey | 169.139 ns | 0.3083 ns | 0.4421 ns | 169.105 ns | 0.0498 |     - |     - |     208 B |
| Class_OpenSend8_TwoWayKey | 673.111 ns | 5.3696 ns | 7.8708 ns | 670.528 ns | 0.2365 |     - |     - |     992 B |
