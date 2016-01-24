using System;
using System.Diagnostics;
using CacheManager.Core;
using System.Threading;

namespace CacheManager.Config.Tests
{
    internal class Program
    {
        public Program()
        {
        }

        public void Main(string[] args)
        {
            var swatch = Stopwatch.StartNew();
            int iterations = int.MaxValue;
            swatch.Restart();
            var cacheConfiguration = ConfigurationBuilder.BuildConfiguration(cfg =>
            {
                cfg.WithUpdateMode(CacheUpdateMode.Up);
                cfg.WithRetryTimeout(100);
                cfg.WithMaxRetries(50);
                ////cfg.WithHandle(typeof(Core.Internal.DictionaryCacheHandle2<>))
                ////    .DisableStatistics();
#if DNXCORE50
                cfg.WithDictionaryHandle()
                    .DisableStatistics();

                //Console.WriteLine("Using Dictionary cache handle");
#else
                cfg.WithDictionaryHandle()
                    .DisableStatistics();

                Console.WriteLine("Using System Runtime cache handle");

                cfg.WithRedisCacheHandle("redis", true)
                    .DisableStatistics();

                cfg.WithRedisConfiguration("redis", config =>
                {
                    config
                        .WithAllowAdmin()
                        .WithDatabase(0)
                        .WithConnectionTimeout(1000)
                        .WithEndpoint("127.0.0.1", 6380)
                        .WithEndpoint("127.0.0.1", 6379);
                    ////.WithEndpoint("192.168.178.32", 6379);
                });

                cfg.WithJsonSerializer();

                Console.WriteLine("Using Redis cache handle");
#endif
            });
            try
            {
                var cache = CacheFactory.FromConfiguration<object>(cacheConfiguration);

                for (int i = 0; i < iterations; i++)
                {
                    try
                    {
                        cache.Clear();
                        cache.Add("key", "value", "region");
                        cache.Add(new CacheItem<object>("key2", "region", "value", ExpirationMode.Sliding, TimeSpan.FromMinutes(10)));
                        cache.Add("key3", "value", "region");
                        var val = cache.Get("key2", "region");

                        cache.ClearRegion("region");

                        Tests.SimpleAddGetTest(
                            CacheFactory.FromConfiguration<object>(cacheConfiguration));
                        ////Tests.RandomRWTest(
                        ////    CacheFactory.FromConfiguration<Item>(cacheConfiguration));
                        ////Tests.CacheThreadTest(
                        ////    CacheFactory.FromConfiguration<string>(cacheConfiguration),
                        ////    i + 10);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error: " + e.Message + "\n" + e.StackTrace);
                        Thread.Sleep(1000);
                    }

                    // Console.WriteLine(string.Format("Iterations ended after {0}ms.", swatch.ElapsedMilliseconds));
                    Console.WriteLine("---------------------------------------------------------");
                    swatch.Restart();
                }
            }
            catch
            {
                throw;
            }
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("We are done...");
            Console.ReadLine();
        }
    }
}