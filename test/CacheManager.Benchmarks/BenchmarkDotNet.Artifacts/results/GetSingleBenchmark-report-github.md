``` ini

BenchmarkDotNet=v0.10.1, OS=Microsoft Windows NT 6.2.9200.0
Processor=Intel(R) Core(TM) i7-6700 CPU 3.40GHz, ProcessorCount=8
Frequency=3328120 Hz, Resolution=300.4699 ns, Timer=TSC
  [Host]     : Clr 4.0.30319.42000, 64bit RyuJIT-v4.6.1586.0
  Job-DZPVZX : Clr 4.0.30319.42000, 64bit RyuJIT-v4.6.1586.0

Platform=X64  LaunchCount=2  TargetCount=15  
WarmupCount=10  

```
     Method |            Mean |        StdErr |        StdDev |   Scaled | Scaled-StdDev |  Gen 0 | Allocated |
----------- |---------------- |-------------- |-------------- |--------- |-------------- |------- |---------- |
 Dictionary |     111.6339 ns |     0.4091 ns |     2.2407 ns |     1.00 |          0.00 |      - |       0 B |
    Runtime |     398.4676 ns |     1.2185 ns |     6.4478 ns |     3.57 |          0.09 | 0.0257 |     208 B |
   MsMemory |     317.5561 ns |     1.9286 ns |    10.3859 ns |     2.85 |          0.11 |      - |       0 B |
      Redis |  67,112.2939 ns |   207.8052 ns | 1,059.6030 ns |   601.41 |         14.91 |      - |   2.18 kB |
  Memcached | 117,983.4603 ns | 1,484.3434 ns | 7,854.4070 ns | 1,057.28 |         72.10 |      - |  14.29 kB |
