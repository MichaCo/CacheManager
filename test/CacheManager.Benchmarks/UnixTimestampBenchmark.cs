using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using CacheManager.Core.Utility;

namespace CacheManager.Benchmarks
{
    [Config(typeof(CacheManagerBenchConfig))]
    public class UnixTimestampBenchmark
    {
        [Benchmark(Baseline = true)]
        public long Framework()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private static readonly DateTime Date1970 = new DateTime(1970, 1, 1);

        [Benchmark()]
        public long ManualCalcNaive()
        {
            return (long)(DateTime.UtcNow - Date1970).TotalMilliseconds;
        }
        
        [Benchmark()]
        public long ManualCalcOptimized()
        {
            return Clock.GetUnixTimestampMillis();
        }
    }
}