``` ini

BenchmarkDotNet=v0.10.1, OS=Microsoft Windows NT 6.2.9200.0
Processor=Intel(R) Core(TM) i7-6700 CPU 3.40GHz, ProcessorCount=8
Frequency=3328120 Hz, Resolution=300.4699 ns, Timer=TSC
  [Host]     : Clr 4.0.30319.42000, 64bit RyuJIT-v4.6.1586.0
  Job-DZPVZX : Clr 4.0.30319.42000, 64bit RyuJIT-v4.6.1586.0

Platform=X64  LaunchCount=2  TargetCount=15  
WarmupCount=10  

```
     Method |            Mean |        StdDev | Scaled | Scaled-StdDev |  Gen 0 | Allocated |
----------- |---------------- |-------------- |------- |-------------- |------- |---------- |
 Dictionary |     116.6133 ns |     1.1805 ns |   1.00 |          0.00 |      - |       0 B |
    Runtime |     383.3126 ns |     6.2799 ns |   3.29 |          0.06 | 0.0242 |     208 B |
   MsMemory |     396.9492 ns |     9.4612 ns |   3.40 |          0.09 | 0.0181 |     176 B |
      Redis |  62,905.6330 ns | 1,565.1500 ns | 539.49 |         14.22 |      - |   2.19 kB |
  Memcached | 114,185.2557 ns | 2,292.9042 ns | 979.28 |         21.60 |      - |  14.29 kB |
