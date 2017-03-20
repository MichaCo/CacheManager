
## Benchmarks

Benchmarks have been implemented with [BenchmarkDotNet](https://github.com/PerfDotNet/BenchmarkDotNet).

All CacheManager instances used in the benchmarks have only one cache handle configured, either the Dictionary, System.Runtime or Redis handle.

We are using the same configuration for all benchmarks and running two jobs each, one for x86 and one for x64. Regarding the different platforms, the conclusion is obviously that x64 is always faster than the x86 platform, but x64 consumes slightly more memory of course.

```ini
BenchmarkDotNet=v0.9.1.0
OS=Microsoft Windows NT 6.2.9200.0
Processor=Intel(R) Core(TM) i7-6700 CPU @ 3.40GHz, ProcessorCount=8
Frequency=3328117 ticks, Resolution=300.4702 ns
HostCLR=MS.NET 4.0.30319.42000, Arch=32-bit RELEASE

Type=PutWithRegionSingleBenchmark  Mode=Throughput  Platform=X64  
Jit=RyuJit  LaunchCount=1  WarmupCount=2  
TargetCount=100  
```

### Add 
*Adding one item per run*

Redis will be a lot slower in this scenario because CacheManager waits for the response to be able to return the `bool` value if the key has been added or not.
In general, it is good to see how fast the Dictionary handle is compared to the `System.Runtime` one. One thing you cannot see here is that also the memory footprint of the Dictionary handle is much lower.

Method |  Platform |  Median |    StdDev | Scaled |
-----------: |:-----------: |-----------: |----------: |-------: |
Dictionary |      X64 |  1.7254 us | 0.0511 us |   1.00 |
Dictionary |      X86 |  1.9563 us | 0.0399 us |   1.00 |
Runtime |      X64 |  4.9839 us | 0.0778 us |   2.89 |
Runtime |      X86 |  7.0324 us | 0.2012 us |   3.59 |
Redis |      X64 | 56.6671 us | 1.7202 us |  32.84 |
Redis |      X86 | 58.0775 us | 0.9517 us |  29.69 |      

*Adding one item per run with using region*

Method |  Platform |  Median |    StdDev | Scaled |
-----------: |:-----------: |-----------: |----------: |-------: |
Dictionary |      X64 |  1.8094 us | 0.0386 us |   1.00 |
Dictionary |      X86 |  2.5029 us | 0.1179 us |   1.00 |
Runtime |      X64 |  6.6934 us | 0.1275 us |   3.70 |
Runtime |      X86 |  9.2334 us | 0.1637 us |   3.69 |
Redis |      X64 | 58.5355 us | 1.7054 us |  32.35 |
Redis |      X86 | 61.0272 us | 1.4178 us |  24.38 |      

### Put
*Put 1 item per run*
Redis is as fast as the other handles in this scenario because CacheManager uses fire and forget for those operations. For Put it doesn't matter to know if the item has been added or updated...

Method |  Platform |  Median |    StdDev | Scaled |
-----------: |:-----------: |-----------: |----------: |-------: |
Dictionary |      X64 | 1.6802 us | 0.0235 us |   1.00 |
Dictionary |      X86 | 1.9445 us | 0.0341 us |   1.00 |
Runtime |      X64 | 4.4431 us | 0.0651 us |   2.64 |
Runtime |      X86 | 6.5231 us | 0.1063 us |   3.35 |
Redis |      X64 | 2.6869 us | 0.0934 us |   1.60 |
Redis |      X86 | 3.5490 us | 0.0848 us |   1.83 |
      
*Put 1 item per run with region*

Method |  Platform |  Median |    StdDev | Scaled |
-----------: |:-----------: |-----------: |----------: |-------: |
Dictionary |      X64 | 1.7401 us | 0.0365 us |   1.00 |
Dictionary |      X86 | 2.4589 us | 0.1022 us |   1.00 |
Runtime |      X64 | 6.1772 us | 0.3683 us |   3.55 |
Runtime |      X86 | 9.9298 us | 0.5574 us |   4.04 |
Redis |      X64 | 3.0807 us | 0.0906 us |   1.77 |
Redis |      X86 | 3.6385 us | 0.2123 us |   1.48 |

### Get
*Get 1 item per run*
With `Get` operations we can clearly see how much faster an in-memory cache is, compared to the distributed variant. That's why it makes so much sense to use CacheManager with a first and secondary cache layer.

Method |  Platform |  Median |    StdDev | Scaled |
-----------: |:-----------: |-----------: |----------: |-------: |
Dictionary |      X64 |    169.8708 ns |     4.6186 ns |   1.00 |
Dictionary |      X86 |    540.0879 ns |     8.9363 ns |   1.00 |
Runtime |      X64 |    310.4025 ns |     8.2928 ns |   1.83 |
Runtime |      X86 |  1,030.5911 ns |     8.0532 ns |   1.91 |
Redis |      X64 | 56,917.3537 ns | 2,035.4765 ns | 335.06 |
Redis |      X86 | 59,519.5459 ns | 1,527.9613 ns | 110.20 |

*Get 1 item per run with region*

Method |  Platform |  Median |    StdDev | Scaled |
-----------: |:-----------: |-----------: |----------: |-------: |
Dictionary |      X64 |    234.9676 ns |     7.9172 ns |   1.00 |
Dictionary |      X86 |    600.0429 ns |    13.7090 ns |   1.00 |
Runtime |      X64 |    497.4116 ns |    15.6998 ns |   2.12 |
Runtime |      X86 |  1,191.6452 ns |    19.8484 ns |   1.99 |
Redis |      X64 | 57,257.9868 ns | 1,705.0148 ns | 243.68 |
Redis |      X86 | 61,789.3944 ns | 1,775.6064 ns | 102.97 |


### Serializer comparison

For this, I only used the bare serializer without the cache layer overhead (e.g. Redis) which could yield wrong results.
Each single performance run does 1000 iterations of serializing and deserializing the same object.
Object structure was the following:

```
{
	"L" : 1671986962,
	"S" : "1625c0a0-86ce-4fd5-9047-cf2fb1d145b2",
	"SList" : ["98a62a89-f3e9-49d7-93ad-a4295b21c1a1", "47a86f42-64b0-4e6d-9f18-ecb20abff2a3", "7de26dfc-57a5-4f16-b421-8999b73c9afb", "e29a8f8a-feb8-4f3f-9825-78c067215339", "5b2e1923-8a76-4f39-9366-4700c7d0d408", "febea78f-ca5e-49d6-99c9-18738e4fb36f", "7c87b429-e931-4f1a-a59a-433504c87a1c", "bf288ff7-e6c0-4df1-bfcf-677ff31cdf45", "9b7fcd6c-45ee-4584-98b6-b30d32e52f72", "2729610c-d6ce-4960-b83b-b5fd4230cc7e"],
	"OList" : [{
			"Id" : 1210151618,
			"Val" : "6d2871c9-c5f8-44b1-bad9-4eba68683510"
		}, {
			"Id" : 1171177179,
			"Val" : "6b12cd3f-2726-4bf9-a25c-35533de3910c"
		}, {
			"Id" : 1676910093,
			"Val" : "66f52534-92f3-4ef4-b555-48a993a9df7a"
		}, {
			"Id" : 977965209,
			"Val" : "80a20081-a2a5-4dcc-8d07-162f697588b4"
		}, {
			"Id" : 2075961031,
			"Val" : "35f8710a-64e5-481d-9f18-899c65abd675"
		}, {
			"Id" : 328057441,
			"Val" : "d17277e2-ca25-42b1-a4b4-efc00deef358"
		}, {
			"Id" : 2046696720,
			"Val" : "4fa32d5e-f770-4d44-a55b-f6479633839c"
		}, {
			"Id" : 422544189,
			"Val" : "de39c21e-8cb3-4f5c-bf5c-a3d228bc4c25"
		}, {
			"Id" : 1887998603,
			"Val" : "22b00459-7820-46a6-8514-10e901810bbd"
		}, {
			"Id" : 852015288,
			"Val" : "09cc3bd8-da23-42cb-b700-02ec461beb3f"
		}
	]
}

```  

The values are randomly generated the object has one list of strings (Guids) and a list of child objects with an integer and string.
Pretty simple but large enough to analyze the performance.

**Results:**


Method | Platform |      Median |    StdDev | Scaled | Scaled-SD |
-----: |:-------: |-----------: |----------: |-------: |-------: |
BinarySerializer |      X64 |  62.4158 ms | 1.8668 ms |   2.20 |      0.07 |
JsonSerializer |      X64 |  28.5521 ms | 0.4633 ms |   1.00 |      0.00 |
JsonGzSerializer |      X64 | 102.5552 ms | 4.8584 ms |   3.60 |      0.18 |
ProtoBufSerializer |      X64 |  11.1276 ms | 0.1252 ms |   0.39 |      0.01 |


 As expected the protobuf serialization outperforms everything else by a huge margin!
 The compression overhead of the JsonGz serializer seems to be pretty large and may have some potential for optimizations...
