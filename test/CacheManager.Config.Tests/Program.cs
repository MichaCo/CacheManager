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
                cfg.WithRetryTimeout(10);
                cfg.WithMaxRetries(10);

#if DNXCORE50
                cfg.WithDictionaryHandle()
                    .EnablePerformanceCounters();

                Console.WriteLine("Using Dictionary cache handle");
#else
                cfg.WithSystemRuntimeCacheHandle()
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
                });

                Console.WriteLine("Using Redis cache handle");
#endif
            });

            for (int i = 0; i < iterations; i++)
            {
                ////Tests.RandomRWTest(CacheFactory.FromConfiguration<Item>(cacheConfiguration));
                
                try
                {
                    Tests.SimpleAddGetTest(
                        CacheFactory.FromConfiguration<object>(cacheConfiguration));

                    Tests.CacheThreadTest(
                        CacheFactory.FromConfiguration<string>(cacheConfiguration),
                        i + 10);
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

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("We are done...");
            Console.ReadLine();
        }
    }
}