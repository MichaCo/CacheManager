``` ini

BenchmarkDotNet=v0.10.1, OS=Microsoft Windows NT 6.2.9200.0
Processor=Intel(R) Core(TM) i7-6700 CPU 3.40GHz, ProcessorCount=8
Frequency=3328120 Hz, Resolution=300.4699 ns, Timer=TSC
  [Host]     : Clr 4.0.30319.42000, 64bit RyuJIT-v4.6.1586.0
  Job-DZPVZX : Clr 4.0.30319.42000, 64bit RyuJIT-v4.6.1586.0

Platform=X64  LaunchCount=2  TargetCount=15  
WarmupCount=10  

```
       Method |      Mean |    StdDev | Scaled | Scaled-StdDev |   Gen 0 | Allocated |
------------- |---------- |---------- |------- |-------------- |-------- |---------- |
        Naive | 1.3792 ms | 0.0211 ms |   1.00 |          0.00 | 29.6875 | 301.06 kB |
       Manuel | 1.3892 ms | 0.0465 ms |   1.01 |          0.04 |  5.4688 | 191.09 kB |
 ManuelPooled | 1.3653 ms | 0.0510 ms |   0.99 |          0.04 |       - | 106.26 kB |
