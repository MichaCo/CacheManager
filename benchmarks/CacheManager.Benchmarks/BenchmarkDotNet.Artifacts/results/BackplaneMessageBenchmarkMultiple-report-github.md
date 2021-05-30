``` ini

BenchmarkDotNet=v0.10.8, OS=Windows 10 Redstone 2 (10.0.15063)
Processor=Intel Core i7-6700 CPU 3.40GHz (Skylake), ProcessorCount=8
Frequency=3328125 Hz, Resolution=300.4695 ns, Timer=TSC
  [Host]     : Clr 4.0.30319.42000, 64bit RyuJIT-v4.7.2098.0
  Job-VUBPRE : Clr 4.0.30319.42000, 64bit RyuJIT-v4.7.2098.0

Runtime=Clr  LaunchCount=1  TargetCount=10  
WarmupCount=4  

```
 |          Method |     Mean |     Error |    StdDev |   Median |      Op/s | Rank |  Gen 0 | Allocated |
 |---------------- |---------:|----------:|----------:|---------:|----------:|-----:|-------:|----------:|
 |   SerializeMany | 5.650 us | 0.1873 us | 0.1239 us | 5.626 us | 177,004.2 |    1 | 2.3804 |   9.78 KB |
 | DeserializeMany | 7.132 us | 0.3477 us | 0.2300 us | 7.115 us | 140,203.9 |    2 | 1.7548 |   7.22 KB |
