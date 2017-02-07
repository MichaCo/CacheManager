``` ini

BenchmarkDotNet=v0.10.1, OS=Microsoft Windows NT 6.2.9200.0
Processor=Intel(R) Core(TM) i7-6700 CPU 3.40GHz, ProcessorCount=8
Frequency=3328120 Hz, Resolution=300.4699 ns, Timer=TSC
  [Host]     : Clr 4.0.30319.42000, 64bit RyuJIT-v4.6.1586.0
  Job-DZPVZX : Clr 4.0.30319.42000, 64bit RyuJIT-v4.6.1586.0

Platform=X64  LaunchCount=2  TargetCount=15  
WarmupCount=10  

```
               Method |        Mean |    StdErr |     StdDev | Scaled | Scaled-StdDev |   Gen 0 |  Gen 1 | Allocated |
--------------------- |------------ |---------- |----------- |------- |-------------- |-------- |------- |---------- |
     BinarySerializer | 118.2656 us | 0.9849 us |  5.3947 us |   1.82 |          0.13 |  8.3333 |      - |  58.05 kB |
       JsonSerializer |  65.1832 us | 0.6379 us |  3.4942 us |   1.00 |          0.00 |  7.1615 |      - |  41.57 kB |
     JsonGzSerializer | 263.4349 us | 2.6759 us | 14.4100 us |   4.05 |          0.31 | 68.2292 | 5.7292 | 329.67 kB |
   ProtoBufSerializer |  41.7841 us | 0.2641 us |  1.4466 us |   0.64 |          0.04 |  3.8818 |      - |  22.42 kB |
 BondBinarySerializer |   8.1186 us | 0.0674 us |  0.3632 us |   0.12 |          0.01 |  0.8240 |      - |   5.02 kB |
