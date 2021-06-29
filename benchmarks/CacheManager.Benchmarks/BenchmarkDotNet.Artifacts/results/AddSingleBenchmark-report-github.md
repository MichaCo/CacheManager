``` ini

BenchmarkDotNet=v0.10.1, OS=Microsoft Windows NT 6.2.9200.0
Processor=Intel(R) Core(TM) i7-6700 CPU 3.40GHz, ProcessorCount=8
Frequency=3328120 Hz, Resolution=300.4699 ns, Timer=TSC
  [Host]     : Clr 4.0.30319.42000, 64bit RyuJIT-v4.6.1586.0
  Job-DZPVZX : Clr 4.0.30319.42000, 64bit RyuJIT-v4.6.1586.0

Platform=X64  LaunchCount=2  TargetCount=15  
WarmupCount=10  

```
     Method |            Mean |        StdErr |        StdDev | Scaled | Scaled-StdDev |  Gen 0 | Allocated |
----------- |---------------- |-------------- |-------------- |------- |-------------- |------- |---------- |
 Dictionary |     321.1602 ns |     1.6496 ns |     9.0353 ns |   1.00 |          0.00 | 0.0309 |     184 B |
    Runtime |   1,869.6133 ns |     5.2912 ns |    28.9811 ns |   5.83 |          0.18 | 0.6561 |   3.16 kB |
   MsMemory |   1,038.4112 ns |     3.5986 ns |    19.3789 ns |   3.24 |          0.11 | 0.0559 |     448 B |
      Redis |  92,822.7871 ns | 1,378.6964 ns | 7,551.4311 ns | 289.24 |         24.46 |      - |   1.51 kB |
  Memcached | 135,684.3124 ns | 1,764.1267 ns | 9,166.6710 ns | 422.80 |         30.33 |      - |  13.77 kB |
