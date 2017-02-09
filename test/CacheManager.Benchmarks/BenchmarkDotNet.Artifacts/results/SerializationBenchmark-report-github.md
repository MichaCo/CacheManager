``` ini

BenchmarkDotNet=v0.10.1, OS=Microsoft Windows NT 6.2.9200.0
Processor=Intel(R) Core(TM) i7-6700 CPU 3.40GHz, ProcessorCount=8
Frequency=3328120 Hz, Resolution=300.4699 ns, Timer=TSC
  [Host]     : Clr 4.0.30319.42000, 64bit RyuJIT-v4.6.1586.0
  Job-DZPVZX : Clr 4.0.30319.42000, 64bit RyuJIT-v4.6.1586.0

Platform=X64  LaunchCount=2  TargetCount=15  
WarmupCount=10  

```
                   Method |          Mean |     StdDev | Scaled | Scaled-StdDev |   Gen 0 | Allocated |
------------------------- |-------------- |----------- |------- |-------------- |-------- |---------- |
         BinarySerializer |   494.3521 us | 21.7862 us |   1.57 |          0.08 | 56.9010 | 327.16 kB |
           JsonSerializer |   315.5607 us | 10.7359 us |   1.00 |          0.00 | 27.2135 | 157.08 kB |
         JsonGzSerializer | 1,251.3457 us | 13.1597 us |   3.97 |          0.13 | 43.2292 | 371.17 kB |
       ProtoBufSerializer |   135.5862 us |  1.5707 us |   0.43 |          0.01 | 30.9245 | 152.68 kB |
     BondBinarySerializer |    85.9038 us |  0.7079 us |   0.27 |          0.01 | 12.6465 |  65.41 kB |
 BondFastBinarySerializer |    82.7233 us |  1.7417 us |   0.26 |          0.01 | 13.0208 |   65.7 kB |
 BondSimpleJsonSerializer |   238.8491 us |  6.8009 us |   0.76 |          0.03 | 28.1901 | 160.55 kB |
