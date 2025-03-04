
## Benchmarks

TODO: needs update

Benchmarks have been implemented with [BenchmarkDotNet](https://github.com/PerfDotNet/BenchmarkDotNet).

All CacheManager instances used in the benchmarks have only one cache handle configured, either the Dictionary, System.Runtime or Redis handle.

We are using the same configuration for all benchmarks and running two jobs each, one for x86 and one for x64. Regarding the different platforms, the conclusion is obviously that x64 is always faster than the x86 platform, but x64 consumes slightly more memory of course.

```ini
BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.3194)
12th Gen Intel Core i7-12700KF, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.200
  [Host]     : .NET 8.0.13 (8.0.1325.6609), X64 RyuJIT AVX2
  Job-BIBDFC : .NET 8.0.13 (8.0.1325.6609), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=2
```

### Add 
*Adding one item per run*

Redis will be a lot slower in this scenario because CacheManager waits for the response to be able to return the `bool` value if the key has been added or not.
In general, it is good to see how fast the Dictionary handle is compared to the `System.Runtime` one. One thing you cannot see here is that also the memory footprint of the Dictionary handle is much lower.


| Method     | Mean         | Error       | StdDev      | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------- |-------------:|------------:|------------:|-------:|--------:|-------:|-------:|----------:|------------:|
| Dictionary |     121.0 ns |     2.00 ns |     1.04 ns |   1.00 |    0.01 | 0.0153 |      - |     200 B |        1.00 |
| Runtime    |     637.2 ns |    28.46 ns |    16.94 ns |   5.27 |    0.14 | 0.2384 | 0.0010 |    3120 B |       15.60 |
| MsMemory   |     198.8 ns |     4.59 ns |     3.04 ns |   1.64 |    0.03 | 0.0260 |      - |     340 B |        1.70 |
| Redis      | 105,395.6 ns | 3,758.09 ns | 2,485.74 ns | 871.20 |   20.86 |      - |      - |    1256 B |        6.28 |

*Adding one item per run with using region*

| Method     | Mean         | Error       | StdDev      | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------- |-------------:|------------:|------------:|-------:|--------:|-------:|----------:|------------:|
| Dictionary |     144.2 ns |     4.65 ns |     3.07 ns |   1.00 |    0.03 | 0.0231 |     304 B |        1.00 |
| Runtime    |     342.6 ns |    18.01 ns |    11.91 ns |   2.38 |    0.09 | 0.0610 |     800 B |        2.63 |
| MsMemory   |     164.1 ns |     5.74 ns |     3.80 ns |   1.14 |    0.03 | 0.0231 |     304 B |        1.00 |
| Redis      | 139,200.0 ns | 4,863.92 ns | 2,894.44 ns | 966.01 |   27.25 |      - |    1528 B |        5.03 |


### Put
*Put 1 item per run*
Redis is as fast as the other handles in this scenario because CacheManager uses fire and forget for those operations. For Put it doesn't matter to know if the item has been added or updated...

| Method     | Mean        | Error      | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------- |------------:|-----------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Dictionary |    95.01 ns |   1.888 ns |   1.249 ns |  1.00 |    0.02 | 0.0122 |      - |     160 B |        1.00 |
| Runtime    |   887.35 ns |  19.929 ns |  13.181 ns |  9.34 |    0.18 | 0.4263 | 0.0095 |    5576 B |       34.85 |
| MsMemory   |   172.40 ns |   5.030 ns |   3.327 ns |  1.81 |    0.04 | 0.0336 |      - |     440 B |        2.75 |
| Redis      | 4,136.37 ns | 301.420 ns | 199.371 ns | 43.54 |    2.07 | 0.0839 | 0.0610 |    1095 B |        6.84 |

   
### Get
*Get 1 item per run*
With `Get` operations we can clearly see how much faster an in-memory cache is, compared to the distributed variant. That's why it makes so much sense to use CacheManager with a first and secondary cache layer.


| Method     | Mean         | Error        | StdDev       | Ratio    | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------- |-------------:|-------------:|-------------:|---------:|--------:|-------:|----------:|------------:|
| Dictionary |     34.97 ns |     0.532 ns |     0.317 ns |     1.00 |    0.01 |      - |         - |          NA |
| Runtime    |    179.45 ns |     2.104 ns |     1.392 ns |     5.13 |    0.06 | 0.0153 |     200 B |          NA |
| MsMemory   |     75.12 ns |     0.338 ns |     0.201 ns |     2.15 |    0.02 |      - |         - |          NA |
| Redis      | 75,534.71 ns | 5,303.351 ns | 3,507.838 ns | 2,160.12 |   97.47 | 0.1221 |    2008 B |          NA |


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


| Method                   | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------- |----------:|---------:|---------:|------:|--------:|--------:|-------:|----------:|------------:|
| JsonSerializer           |  83.21 us | 1.163 us | 0.769 us |  1.00 |    0.01 | 14.8926 | 2.4414 | 191.16 KB |        1.00 |
| JsonGzSerializer         | 346.37 us | 4.785 us | 3.165 us |  4.16 |    0.05 | 21.9727 | 2.9297 | 280.75 KB |        1.47 |
| ProtoBufSerializer       |  39.18 us | 0.701 us | 0.463 us |  0.47 |    0.01 |  8.6060 | 1.0986 |  110.7 KB |        0.58 |
| BondBinarySerializer     |  19.03 us | 0.398 us | 0.263 us |  0.23 |    0.00 |  4.7302 | 0.6714 |  60.53 KB |        0.32 |
| BondFastBinarySerializer |  19.42 us | 0.431 us | 0.285 us |  0.23 |    0.00 |  4.7607 | 0.7324 |  60.84 KB |        0.32 |
| BondSimpleJsonSerializer |  63.56 us | 1.132 us | 0.748 us |  0.76 |    0.01 | 11.9629 | 1.9531 | 153.35 KB |        0.80 |


 As expected the protobuf serialization outperforms everything else by a huge margin!
 The compression overhead of the JsonGz serializer seems to be pretty large and may have some potential for optimizations...
