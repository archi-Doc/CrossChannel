``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.203
  [Host]    : .NET Core 5.0.6 (CoreCLR 5.0.621.22011, CoreFX 5.0.621.22011), X64 RyuJIT
  MediumRun : .NET Core 5.0.6 (CoreCLR 5.0.621.22011, CoreFX 5.0.621.22011), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|                    Method |       Mean |     Error |    StdDev |     Median |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------- |-----------:|----------:|----------:|-----------:|-------:|------:|------:|----------:|
|                      Send |   3.975 ns | 0.1332 ns | 0.1953 ns |   3.810 ns |      - |     - |     - |         - |
|                  OpenSend |  53.632 ns | 0.0889 ns | 0.1303 ns |  53.584 ns | 0.0114 |     - |     - |      48 B |
|                 OpenSend8 |  90.794 ns | 0.1769 ns | 0.2593 ns |  90.813 ns | 0.0114 |     - |     - |      48 B |
|             OpenSend_Weak | 595.733 ns | 3.8738 ns | 5.4305 ns | 599.794 ns | 0.0877 |     - |     - |     368 B |
|            OpenSend8_Weak | 749.793 ns | 2.1842 ns | 3.0619 ns | 749.785 ns | 0.0877 |     - |     - |     368 B |
|                   SendKey |   5.982 ns | 0.0053 ns | 0.0078 ns |   5.983 ns |      - |     - |     - |         - |
|              OpenSend_Key |  87.366 ns | 0.0697 ns | 0.0999 ns |  87.330 ns | 0.0138 |     - |     - |      58 B |
|             OpenSend8_Key | 181.827 ns | 0.2567 ns | 0.3682 ns | 181.809 ns | 0.0138 |     - |     - |      58 B |
|           OpenSend_Result |  66.240 ns | 1.0661 ns | 1.4945 ns |  67.594 ns | 0.0286 |     - |     - |     120 B |
|          OpenSend8_Result | 194.660 ns | 1.0290 ns | 1.5083 ns | 193.745 ns | 0.1490 |     - |     - |     624 B |
|        OpenSend_KeyResult |  99.415 ns | 0.2987 ns | 0.4378 ns |  99.601 ns | 0.0310 |     - |     - |     130 B |
|       OpenSend8_KeyResult | 285.423 ns | 0.2287 ns | 0.3206 ns | 285.400 ns | 0.1512 |     - |     - |     634 B |
|                Class_Send |   9.717 ns | 0.3130 ns | 0.4587 ns |   9.308 ns |      - |     - |     - |         - |
|            Class_OpenSend |  87.135 ns | 0.3096 ns | 0.4440 ns |  87.068 ns | 0.0114 |     - |     - |      48 B |
|           Class_OpenSend8 | 230.560 ns | 0.3926 ns | 0.5755 ns | 230.549 ns | 0.0114 |     - |     - |      48 B |
|        Class_OpenSend_Key | 113.036 ns | 0.1487 ns | 0.2085 ns | 112.978 ns | 0.0267 |     - |     - |     112 B |
|       Class_OpenSend8_Key | 338.349 ns | 0.4241 ns | 0.6082 ns | 338.166 ns | 0.0801 |     - |     - |     336 B |
|     Class_OpenSend_Result | 137.838 ns | 1.3084 ns | 1.9179 ns | 136.981 ns | 0.0439 |     - |     - |     184 B |
|    Class_OpenSend8_Result | 483.472 ns | 0.7178 ns | 1.0063 ns | 483.173 ns | 0.2174 |     - |     - |     912 B |
|  Class_OpenSend_KeyResult | 141.717 ns | 0.5749 ns | 0.8245 ns | 142.230 ns | 0.0477 |     - |     - |     200 B |
| Class_OpenSend8_KeyResult | 495.034 ns | 0.6771 ns | 0.9711 ns | 495.387 ns | 0.2346 |     - |     - |     984 B |
