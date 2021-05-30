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
 Dictionary |     348.8331 ns |     2.2450 ns |    11.6655 ns |   1.00 |          0.00 | 0.0309 |     184 B |
    Runtime |   2,087.0435 ns |    14.5742 ns |    79.8259 ns |   5.99 |          0.30 | 0.6480 |   3.16 kB |
   MsMemory |   1,450.8287 ns |     8.8780 ns |    47.8093 ns |   4.16 |          0.19 | 0.1284 |     764 B |
      Redis | 104,014.9027 ns |   443.6979 ns | 2,389.3865 ns | 298.50 |         11.90 |      - |   1.51 kB |
  Memcached | 147,246.4678 ns | 1,720.1031 ns | 9,421.3930 ns | 422.57 |         30.00 |      - |  13.77 kB |
