``` ini

BenchmarkDotNet=v0.10.1, OS=Microsoft Windows NT 6.2.9200.0
Processor=Intel(R) Core(TM) i7-6700 CPU 3.40GHz, ProcessorCount=8
Frequency=3328120 Hz, Resolution=300.4699 ns, Timer=TSC
  [Host]     : Clr 4.0.30319.42000, 64bit RyuJIT-v4.6.1586.0
  Job-DZPVZX : Clr 4.0.30319.42000, 64bit RyuJIT-v4.6.1586.0

Platform=X64  LaunchCount=2  TargetCount=15  
WarmupCount=10  Allocated=0 B  

```
              Method |       Mean |    StdDev | Scaled | Scaled-StdDev |
-------------------- |----------- |---------- |------- |-------------- |
           Framework | 22.1431 ns | 0.2677 ns |   1.00 |          0.00 |
     ManualCalcNaive | 11.4425 ns | 0.2960 ns |   0.52 |          0.01 |
 ManualCalcOptimized |  7.8302 ns | 0.1593 ns |   0.35 |          0.01 |
