``` ini

BenchmarkDotNet=v0.10.1, OS=Microsoft Windows NT 6.2.9200.0
Processor=Intel(R) Core(TM) i7-6700 CPU 3.40GHz, ProcessorCount=8
Frequency=3328120 Hz, Resolution=300.4699 ns, Timer=TSC
  [Host]     : Clr 4.0.30319.42000, 64bit RyuJIT-v4.6.1586.0
  Job-DZPVZX : Clr 4.0.30319.42000, 64bit RyuJIT-v4.6.1586.0

Platform=X64  LaunchCount=2  TargetCount=15  
WarmupCount=10  

```
                   Method |        Mean |    StdDev | Scaled | Scaled-StdDev |   Gen 0 | Allocated |
------------------------- |------------ |---------- |------- |-------------- |-------- |---------- |
           JsonSerializer | 316.8737 us | 2.9366 us |   1.00 |          0.00 | 26.9531 | 157.65 kB |
       ProtoBufSerializer | 329.1610 us | 2.8577 us |   1.04 |          0.01 | 37.9557 | 201.92 kB |
     BondBinarySerializer |  86.3952 us | 1.1801 us |   0.27 |          0.00 | 12.7767 |  65.27 kB |
 BondFastBinarySerializer |  87.5721 us | 2.1179 us |   0.28 |          0.01 | 12.6465 |   65.5 kB |
 BondSimpleJsonSerializer | 240.1620 us | 4.0579 us |   0.76 |          0.01 | 28.1901 | 160.36 kB |
