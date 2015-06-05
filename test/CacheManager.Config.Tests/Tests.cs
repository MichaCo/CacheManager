using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CacheManager.Core;
using CacheManager.Core.Cache;

namespace CacheManager.Config.Tests
{
    public static class Tests
    {
        public static void CacheThreadTest(ICacheManager<string> cache, int seed)
        {
            var threads = 10;
            var numItems = 100;
            var eventAddCount = 0;
            var eventRemoveCount = 0;
            var eventGetCount = 0;

            cache.OnAdd += (sender, args) => { Interlocked.Increment(ref eventAddCount); };
            cache.OnRemove += (sender, args) => { Interlocked.Increment(ref eventRemoveCount); };
            cache.OnGet += (sender, args) => { Interlocked.Increment(ref eventGetCount); };

            Func<int, string> keyGet = (index) => "key" + ((index + 1) * seed);

            Action test = () =>
            {
                for (int i = 0; i < numItems; i++)
                {
                    cache.Add(keyGet(i), i.ToString());
                }

                for (int i = 0; i < numItems; i++)
                {
                    if (i % 10 == 0)
                    {
                        cache.Remove(keyGet(i));
                    }
                }

                for (int i = 0; i < numItems; i++)
                {
                    string val = cache.Get(keyGet(i));
                }
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

            Console.WriteLine(string.Format("Event - Adds {0} Hits {1} Removes {2}",
                eventAddCount,
                eventGetCount,
                eventRemoveCount));

            cache.Clear();
            cache.Dispose();
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
                    var value = cache.AddOrUpdate(key + ta, "val" + ta, (v) => "val" + ta);
                    if (value == null)
                    {
                        throw new InvalidOperationException("really?");
                    }

                    //// cache.Add(key + ta, "val" + ta);
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

                    object value;
                    if (!cache.TryUpdate("key" + rand.Next(0, items - 1), v => Guid.NewGuid().ToString(), out value))
                    {
                    }
                }

                cache.Clear();
                cache.Dispose();
            }

            var elapsed = swatch.ElapsedMilliseconds;
            var opsPerSec = Math.Round(ops / swatch.Elapsed.TotalSeconds, 0);
            Console.WriteLine("\nSimpleAddGetTest completed \tafter: {0:N} ms. \twith {1:N0} Ops/s.", elapsed, opsPerSec);
        }
    }

    public class Item
    {
        public Item()
        {
        }

        public string Name { get; set; }

        public int Number { get; set; }
    }
}