using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CacheManager.AppFabricCache;
using CacheManager.Core;
using CacheManager.Core.Cache;
using CacheManager.Core.Configuration;
using CacheManager.Memcached;
using CacheManager.StackExchange.Redis;
using CacheManager.SystemRuntimeCaching;
using ProtoBuf;

namespace CacheManager.Config.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            ICacheManager<object> cache = null;
            try
            {
                var swatch = Stopwatch.StartNew();
                int iterations = int.MaxValue;
                swatch.Restart();
                cache = CacheFactory.Build<object>("myCache", cfg =>
                {
                    cfg.WithUpdateMode(CacheUpdateMode.Up);

                    //managerConfiguration.WithHandle<DictionaryCacheHandle<int>>("default")
                    //    .EnablePerformanceCounters()
                    //    //.WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(1))
                    //;

                    //cfg.WithHandle<MemoryCacheHandle<int>>("default")
                    //    //.DisableStatistics()
                    //    //.EnablePerformanceCounters()
                    //    //.WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMilliseconds(20)
                    //;

                    //cfg.WithHandle<RedisCacheHandle>("redis")
                    //    //.EnablePerformanceCounters()
                    //    //.EnablePerformanceCounters()
                    //    //.WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(30))
                    //;

                    cfg.WithHandle<MemcachedCacheHandle<object>>("enyim.com/local-memcached");

                    //managerConfiguration.WithHandle<AppFabricCacheHandle<string>>("default")
                    //    .DisableStatistics()
                    //    ;
                        
                    cfg.WithRedisConfiguration(new RedisConfiguration(
                        "redis",
                        new List<ServerEndPoint>() { new ServerEndPoint("127.0.0.1", 6379) },
                        allowAdmin: true
                        , connectionTimeout: 10000 /*<- for testing connection timeout this is handy*/
                        ));
                });

                cache.Clear();
                for (int i = 0; i < iterations; i++)
                {
                   // CacheThreadTest(cache, i + 10);
                    SimpleAddGetTest(cache);
                    //CacheUpdateTest(cache);

                    //Console.WriteLine(string.Format("Iterations ended after {0}ms.", swatch.ElapsedMilliseconds));
                    Console.WriteLine("---------------------------------------------------------");
                    swatch.Restart();
                }
            }
            finally
            {
                cache.Dispose();
            }

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("We are done...");
            Console.ReadKey();
        }

        public static void SimpleAddGetTest(ICacheManager<object> cache)
        {
            var swatch = Stopwatch.StartNew();
            var threads = 500;
            var items = 100;
            var ops = 1 /* add ,update*2, get in our test method */
                * threads * items + (items);

            var rand = new Random();
            var key = "key";

            cache.Put("key", "value");
            var obj = cache.Get("key");

            for (var ta = 0; ta < items; ta++)
            {
                cache.Put(key + ta, "val" + ta);
            }

            for (var t = 0; t < threads; t++)
            {
                for (var ta = 0; ta < items; ta++)
                {
                    var v = cache.Get(key + ta);
                    //cache.Put(key + ta, false);
                }

                Thread.Sleep(0);

                cache.Put("key" + rand.Next(0, items - 1), "222");
            }

            var item = cache.Get(key);

            var elapsed = swatch.ElapsedMilliseconds;
            var opsPerSec = Math.Round(ops / swatch.Elapsed.TotalSeconds, 0);
            Console.WriteLine("SimpleAddGetTest completed \tafter: {0:C} ms. \twith {1:C0} ops. \tResult is {2}.", elapsed, opsPerSec, item);
        }

        public static void CacheUpdateTest(ICacheManager<int> cache)
        {
            var swatch = Stopwatch.StartNew();
            var threads = 8;
            var tasksPerThread = 20;
            var numItems = 2000;
            var ops = 2 /* add ,update*2, get in our test method */
                * threads * tasksPerThread * numItems;

            var eventAddCount = 0;
            var eventRemoveCount = 0;
            var eventGetCount = 0;
            var eventUpdateCount = 0;

            cache.OnAdd += (sender, args) => { Interlocked.Increment(ref eventAddCount); };
            cache.OnRemove += (sender, args) => { Interlocked.Increment(ref eventRemoveCount); };
            cache.OnGet += (sender, args) => { Interlocked.Increment(ref eventGetCount); };
            cache.OnUpdate += (sender, args) => { Interlocked.Increment(ref eventUpdateCount); };
            
            cache.Clear();

            for (int i = 0; i < numItems; i++)
            {
                cache.Put("key" + i, i);
            }

            var parallelTasks = new List<Action>();
            for (var t = 0; t < threads; t++)
            {
                parallelTasks.Add(()=> 
                {
                    for (int tt = 0; tt < tasksPerThread; tt++)
                    {
                        Task.Delay(0);

                        for (int i = 0; i < numItems; i++)
                        {
                            cache.Update("key" + i, v => v + i, new UpdateItemConfig(VersionConflictHandling.EvictItemFromOtherCaches));
                        }

                        //for (int i = 0; i < numItems; i++)
                        //{
                        //    cache.Put("key" + i, tt);
                        //}
                        Task.Delay(0);

                        for (int i = 0; i < numItems; i++)
                        {
                            var value = cache.Get("key" + i);
                        }
                    }
                });
            }

            Parallel.Invoke(
                new ParallelOptions() { MaxDegreeOfParallelism = threads },
                parallelTasks.ToArray()    
                );

            var elapsed = swatch.ElapsedMilliseconds;
            var opsPerSec = Math.Round(ops / (elapsed / 1000d), 0);
            Console.WriteLine("CacheUpdateTest completed after: {0}ms. with {1}ops/s.", elapsed, opsPerSec);


            foreach (var handle in cache.CacheHandles)
            {
                var stats = handle.Stats;
                Console.WriteLine(string.Format(
                        "Items: {0}, Hits: {1}, Miss: {2}, Remove: {3}, ClearRegion: {4}, Clear: {5}, Adds: {6}, Puts: {7}, Gets: {8}, H+M: {9}",
                            stats.GetStatistic(CacheStatsCounterType.Items),
                            stats.GetStatistic(CacheStatsCounterType.Hits),
                            stats.GetStatistic(CacheStatsCounterType.Misses),
                            stats.GetStatistic(CacheStatsCounterType.RemoveCalls),
                            stats.GetStatistic(CacheStatsCounterType.ClearRegionCalls),
                            stats.GetStatistic(CacheStatsCounterType.ClearCalls),
                            stats.GetStatistic(CacheStatsCounterType.AddCalls),
                            stats.GetStatistic(CacheStatsCounterType.PutCalls),
                            stats.GetStatistic(CacheStatsCounterType.GetCalls),
                            stats.GetStatistic(CacheStatsCounterType.Hits) + stats.GetStatistic(CacheStatsCounterType.Misses)
                        ));
            }

            Console.WriteLine(string.Format("Event - Adds {0} Gets {1} Removes {2} Updates {3}",
                eventAddCount,
                eventGetCount,
                eventRemoveCount,
                eventUpdateCount));
        }

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
                    var key = "key" + (i + 1) * seed;
                    //var item = new CacheItem<Item>(key, new Item() { Name = "Name" + i, Number = i });
                    cache.Add(key, i.ToString());
                }
                cache.CacheHandles.First().Clear();
                for (int i = 0; i < numItems; i++)
                {
                    var key = "key" + (i + 1) * seed;
                    string intVal = cache.Get(key);
                }

                Thread.Yield();
                //for (int i = 0; i < numItems; i++)
                //{
                //    var key = "key" + (i + 1) * seed;
                //    cache.Remove(key);
                //}
                Thread.Yield();
                //cache.Clear();
            };

            Parallel.Invoke(new ParallelOptions() { MaxDegreeOfParallelism = 8 }, Enumerable.Repeat(test, threads).ToArray());

            foreach (var handle in cache.CacheHandles)
            {
                var stats = handle.Stats;
                Console.WriteLine(string.Format(
                        "Items: {0}, Hits: {1}, Miss: {2}, Remove: {3}, ClearRegion: {4}, Clear: {5}, Adds: {6}, Puts: {7}, Gets: {8}, H+M: {9}",
                            stats.GetStatistic(CacheStatsCounterType.Items),
                            stats.GetStatistic(CacheStatsCounterType.Hits),
                            stats.GetStatistic(CacheStatsCounterType.Misses),
                            stats.GetStatistic(CacheStatsCounterType.RemoveCalls),
                            stats.GetStatistic(CacheStatsCounterType.ClearRegionCalls),
                            stats.GetStatistic(CacheStatsCounterType.ClearCalls),
                            stats.GetStatistic(CacheStatsCounterType.AddCalls),
                            stats.GetStatistic(CacheStatsCounterType.PutCalls),
                            stats.GetStatistic(CacheStatsCounterType.GetCalls),
                            stats.GetStatistic(CacheStatsCounterType.Hits) + stats.GetStatistic(CacheStatsCounterType.Misses)
                        ));
            }

            Console.WriteLine(string.Format("Event - Adds {0} Gets {1} Removes {2}",
                eventAddCount,
                eventGetCount,
                eventRemoveCount));
        }
    }

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
}
