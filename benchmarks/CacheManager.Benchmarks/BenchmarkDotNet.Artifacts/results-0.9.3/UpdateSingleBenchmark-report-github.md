``` ini

BenchmarkDotNet=v0.10.1, OS=Microsoft Windows NT 6.2.9200.0
Processor=Intel(R) Core(TM) i7-6700 CPU 3.40GHz, ProcessorCount=8
Frequency=3328120 Hz, Resolution=300.4699 ns, Timer=TSC
  [Host]     : Clr 4.0.30319.42000, 64bit RyuJIT-v4.6.1586.0
  Job-DZPVZX : Clr 4.0.30319.42000, 64bit RyuJIT-v4.6.1586.0

Platform=X64  LaunchCount=2  TargetCount=15  
WarmupCount=10  

```
     Method |            Mean |        StdErr |         StdDev | Scaled | Scaled-StdDev |  Gen 0 | Allocated |
----------- |---------------- |-------------- |--------------- |------- |-------------- |------- |---------- |
 Dictionary |     404.0928 ns |     1.7180 ns |      9.0906 ns |   1.00 |          0.00 | 0.0285 |     224 B |
    Runtime |   2,844.5756 ns |    11.2859 ns |     58.6432 ns |   7.04 |          0.21 | 1.2360 |   5.99 kB |
   MsMemory |   1,811.6219 ns |     4.2517 ns |     22.4980 ns |   4.49 |          0.11 | 0.2060 |   1.26 kB |
      Redis | 126,840.3081 ns |   831.6598 ns |  4,240.6498 ns | 314.04 |         12.35 |      - |   2.78 kB |
  Memcached | 219,499.2918 ns | 3,823.4166 ns | 20,941.7152 ns | 543.45 |         52.33 |      - |  27.46 kB |
