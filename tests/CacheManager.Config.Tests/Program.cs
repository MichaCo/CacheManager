using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CacheManager.Core;
using CacheManager.Core.Cache;
using CacheManager.Core.Configuration;
using ProtoBuf;

namespace CacheManager.Config.Tests
{
    [Serializable]
    [ProtoContract]
    public class Item
    {
        public Item()
        {
        }

        [ProtoMember(1)]
        public string Name { get; set; }

        [ProtoMember(2)]
        public int Number { get; set; }
    }

    internal class Program
    {
        public static void CacheThreadTest(ICacheManager<string> cache, int seed)
        {
            var threads = 10;
            var numItems = 55000;
            var eventAddCount = 0;
            var eventRemoveCount = 0;
            var eventGetCount = 0;

            cache.OnAdd += (sender, args) => { Interlocked.Increment(ref eventAddCount); };
            cache.OnRemove += (sender, args) => { Interlocked.Increment(ref eventRemoveCount); };
            cache.OnGet += (sender, args) => { Interlocked.Increment(ref eventGetCount); };

            Action test = () =>
            {
                for (int i = 0; i < numItems; i++)
                {
                    var key = "key" + ((i + 1) * seed);
                    cache.Add(key, i.ToString());
                }

                cache.CacheHandles.First().Clear();

                for (int i = 0; i < numItems; i++)
                {
                    var key = "key" + ((i + 1) * seed);
                    string intVal = cache.Get(key);
                }

                Thread.Yield();
            };

            Parallel.Invoke(new ParallelOptions() { MaxDegreeOfParallelism = 8 }, Enumerable.Repeat(test, threads).ToArray());

            foreach (var handle in cache.CacheHandles)
            {
                var stats = handle.Stats;
                Console.WriteLine(string.Format(
                        "Items: {0}, Hits: {1}, Miss: {2}, Remove: {3}, ClearRegion: {4}, Clear: {5}, Adds: {6}, Puts: {7}, Gets: {8}",
                            stats.GetStatistic(CacheStatsCounterType.Items),
                            stats.GetStatistic(CacheStatsCounterType.Hits),
                            stats.GetStatistic(CacheStatsCounterType.Misses),
                            stats.GetStatistic(CacheStatsCounterType.RemoveCalls),
                            stats.GetStatistic(CacheStatsCounterType.ClearRegionCalls),
                            stats.GetStatistic(CacheStatsCounterType.ClearCalls),
                            stats.GetStatistic(CacheStatsCounterType.AddCalls),
                            stats.GetStatistic(CacheStatsCounterType.PutCalls),
                            stats.GetStatistic(CacheStatsCounterType.GetCalls)));
            }

            Console.WriteLine(string.Format("Event - Adds {0} Gets {1} Removes {2}",
                eventAddCount,
                eventGetCount,
                eventRemoveCount));
        }

        public static void SimpleAddGetTest(params ICacheManager<object>[] caches)
        {
            var swatch = Stopwatch.StartNew();
            var threads = 10000;
            var items = 1000;
            var ops = threads * items * caches.Length;

            var rand = new Random();
            var key = "key";

            foreach (var cache in caches)
            {
                for (var ta = 0; ta < items; ta++)
                {
                    cache.Add(key + ta, "val" + ta);
                }

                for (var t = 0; t < threads; t++)
                {
                    for (var ta = 0; ta < items; ta++)
                    {
                        var x = cache.Get(key + ta);
                    }

                    Thread.Sleep(0);

                    if (t % 1000 == 0)
                    {
                        Console.Write(".");
                    }

                    cache.Update("key" + rand.Next(0, items - 1), v => "222");
                }

                cache.Dispose();
            }

            var elapsed = swatch.ElapsedMilliseconds;
            var opsPerSec = Math.Round(ops / swatch.Elapsed.TotalSeconds, 0);
            Console.WriteLine("\nSimpleAddGetTest completed \tafter: {0:C} ms. \twith {1:C0} Ops/s.", elapsed, opsPerSec);
        }

        private static void Main(string[] args)
        {
            var swatch = Stopwatch.StartNew();
            int iterations = int.MaxValue;
            swatch.Restart();
            var cacheConfiguration = ConfigurationBuilder.BuildConfiguration(cfg =>
            {
                cfg.WithUpdateMode(CacheUpdateMode.Up);

                cfg.WithSystemRuntimeCacheHandle("default")
                    .DisableStatistics();

                cfg.WithRedisCacheHandle("redis", true)
                    .DisableStatistics();

                cfg.WithRedisBackPlate("redis");

                cfg.WithRedisConfiguration("redis", config =>
                {
                    config.WithAllowAdmin()
                        .WithDatabase(0)
                        .WithEndpoint("localhost", 6379)
                        .WithConnectionTimeout(1000);
                });
            });

            for (int i = 0; i < iterations; i++)
            {
                // CacheThreadTest(cache, i + 10);
                SimpleAddGetTest(
                    // CacheFactory.FromConfiguration(cacheConfiguration),
                    CacheFactory.FromConfiguration<object>("cache", cacheConfiguration));
                // CacheUpdateTest(cache);

                // Console.WriteLine(string.Format("Iterations ended after {0}ms.", swatch.ElapsedMilliseconds));
                Console.WriteLine("---------------------------------------------------------");
                swatch.Restart();

                GC.Collect();
            }

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("We are done...");
            Console.ReadKey();
        }
    }
}