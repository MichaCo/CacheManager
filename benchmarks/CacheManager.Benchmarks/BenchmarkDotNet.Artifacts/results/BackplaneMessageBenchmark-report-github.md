``` ini

BenchmarkDotNet=v0.10.8, OS=Windows 10 Redstone 2 (10.0.15063)
Processor=Intel Core i7-6700 CPU 3.40GHz (Skylake), ProcessorCount=8
Frequency=3328125 Hz, Resolution=300.4695 ns, Timer=TSC
  [Host]     : Clr 4.0.30319.42000, 64bit RyuJIT-v4.7.2098.0
  Job-VUBPRE : Clr 4.0.30319.42000, 64bit RyuJIT-v4.7.2098.0

Runtime=Clr  LaunchCount=1  TargetCount=10  
WarmupCount=4  

```
 |                  Method |      Mean |     Error |   StdDev |    Median |         Op/s | Scaled | ScaledSD | Rank |  Gen 0 | Allocated |
 |------------------------ |----------:|----------:|---------:|----------:|-------------:|-------:|---------:|-----:|-------:|----------:|
 |         SerializeChange | 194.57 ns | 11.309 ns | 7.480 ns | 192.86 ns |  5,139,629.5 |   1.00 |     0.00 |    4 | 0.1523 |     640 B |
 |       DeserializeChange | 241.69 ns | 10.354 ns | 6.848 ns | 238.51 ns |  4,137,485.5 |   1.24 |     0.06 |    5 | 0.0875 |     368 B |
 |   SerializeChangeRegion | 249.23 ns |  7.700 ns | 4.582 ns | 249.67 ns |  4,012,334.1 |   1.28 |     0.05 |    6 | 0.1826 |     768 B |
 | DeserializeChangeRegion | 305.05 ns |  8.953 ns | 5.922 ns | 306.25 ns |  3,278,156.0 |   1.57 |     0.06 |    7 | 0.0987 |     416 B |
 |          SerializeClear |  96.21 ns |  4.459 ns | 2.949 ns |  95.74 ns | 10,393,877.9 |   0.50 |     0.02 |    1 | 0.1162 |     488 B |
 |        DeserializeClear | 158.92 ns |  7.949 ns | 5.258 ns | 159.25 ns |  6,292,374.9 |   0.82 |     0.04 |    2 | 0.0741 |     312 B |
 |    SerializeClearRegion | 169.66 ns |  7.262 ns | 3.798 ns | 169.60 ns |  5,894,312.1 |   0.87 |     0.04 |    3 | 0.1581 |     664 B |
 |  DeserializeClearRegion | 240.22 ns |  8.661 ns | 5.154 ns | 240.20 ns |  4,162,849.7 |   1.24 |     0.05 |    5 | 0.0892 |     376 B |
 |         SerializeRemove | 255.63 ns | 15.071 ns | 9.968 ns | 258.50 ns |  3,911,924.7 |   1.32 |     0.07 |    6 | 0.1826 |     768 B |
 |       DeserializeRemove | 300.98 ns |  9.420 ns | 5.606 ns | 299.66 ns |  3,322,483.1 |   1.55 |     0.06 |    7 | 0.0987 |     416 B |
