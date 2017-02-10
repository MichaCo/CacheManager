``` ini

BenchmarkDotNet=v0.10.1, OS=Microsoft Windows NT 6.2.9200.0
Processor=Intel(R) Core(TM) i7-6700 CPU 3.40GHz, ProcessorCount=8
Frequency=3328120 Hz, Resolution=300.4699 ns, Timer=TSC
  [Host]     : Clr 4.0.30319.42000, 64bit RyuJIT-v4.6.1586.0
  Job-DZPVZX : Clr 4.0.30319.42000, 64bit RyuJIT-v4.6.1586.0

Platform=X64  LaunchCount=2  TargetCount=15  
WarmupCount=10  

```
|                   Method |          Mean |  Scaled | Allocated |
|------------------------- |-------------- |-------- |---------- |
|           JsonSerializer |   319.7445 us |    1.00 | 157.08 kB |
|         BinarySerializer |   498.2847 us |    1.56 | 327.16 kB |
|         JsonGzSerializer | 1,018.0015 us |    3.19 |  312.9 kB |
|       ProtoBufSerializer |   135.8551 us |    0.43 | 152.68 kB |
|     BondBinarySerializer |    85.7551 us |    0.27 |  65.41 kB |
| BondFastBinarySerializer |    83.4832 us |    0.26 |   65.7 kB |
| BondSimpleJsonSerializer |   232.3750 us |    0.73 | 160.55 kB |
