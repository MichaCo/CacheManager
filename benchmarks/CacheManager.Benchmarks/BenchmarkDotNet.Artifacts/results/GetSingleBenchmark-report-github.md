``` ini

BenchmarkDotNet=v0.10.1, OS=Microsoft Windows NT 6.2.9200.0
Processor=Intel(R) Core(TM) i7-6700 CPU 3.40GHz, ProcessorCount=8
Frequency=3328120 Hz, Resolution=300.4699 ns, Timer=TSC
  [Host]     : Clr 4.0.30319.42000, 64bit RyuJIT-v4.6.1586.0
  Job-DZPVZX : Clr 4.0.30319.42000, 64bit RyuJIT-v4.6.1586.0

Platform=X64  LaunchCount=2  TargetCount=15  
WarmupCount=10  

```

|     Method |            Mean |        StdDev | Scaled | Scaled-StdDev |  Gen 0 | Allocated |
| ----------- |---------------- |-------------- |------- |-------------- |------- |---------- |
| Dictionary |     126.8698 ns |     2.7517 ns |   1.00 |          0.00 |      - |       0 B |
|    Runtime |     458.1133 ns |    10.6768 ns |   3.61 |          0.11 | 0.0247 |     208 B |
|   MsMemory |     310.6273 ns |    11.6469 ns |   2.45 |          0.10 |      - |       0 B |
|      Redis |  61,794.6588 ns | 1,762.1822 ns | 487.29 |         17.10 |      - |   2.18 kB |
|  Memcached | 108,869.9158 ns | 3,183.5684 ns | 858.51 |         30.60 |      - |  14.29 kB |
