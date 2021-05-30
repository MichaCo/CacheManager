``` ini

BenchmarkDotNet=v0.10.1, OS=Microsoft Windows NT 6.2.9200.0
Processor=Intel(R) Core(TM) i7-6700 CPU 3.40GHz, ProcessorCount=8
Frequency=3328120 Hz, Resolution=300.4699 ns, Timer=TSC
  [Host]     : Clr 4.0.30319.42000, 64bit RyuJIT-v4.6.1586.0
  Job-DZPVZX : Clr 4.0.30319.42000, 64bit RyuJIT-v4.6.1586.0

Platform=X64  LaunchCount=2  TargetCount=15  
WarmupCount=10  

```
               Method |        Mean |    StdDev | Scaled | Scaled-StdDev |   Gen 0 |  Gen 1 | Allocated |
--------------------- |------------ |---------- |------- |-------------- |-------- |------- |---------- |
     BinarySerializer | 105.5278 us | 1.5448 us |   1.92 |          0.04 |  8.4635 |      - |  58.05 kB |
       JsonSerializer |  55.0184 us | 0.9774 us |   1.00 |          0.00 |  7.0964 |      - |  41.56 kB |
     JsonGzSerializer | 228.2791 us | 3.1088 us |   4.15 |          0.09 | 68.2292 | 5.7292 | 329.66 kB |
   ProtoBufSerializer |  37.6229 us | 1.0164 us |   0.68 |          0.02 |  3.8493 |      - |  22.42 kB |
 BondBinarySerializer |   7.3235 us | 0.2123 us |   0.13 |          0.00 |  0.8321 |      - |   5.02 kB |
