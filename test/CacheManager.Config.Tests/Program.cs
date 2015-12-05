using System;
using System.Diagnostics;
using CacheManager.Core;

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
                cfg.WithMaxRetries(10);

#if DNXCORE50
                cfg.WithDictionaryHandle("default")
                    .EnablePerformanceCounters();
                Console.WriteLine("Using Dictionary cache handle");
#else
                cfg.WithSystemRuntimeCacheHandle("default")
                    .EnablePerformanceCounters();
                Console.WriteLine("Using System Runtime cache handle");

                cfg.WithRedisCacheHandle("redis", true)
                    .EnablePerformanceCounters();

                //cfg.WithRedisBackPlate("redis");

                cfg.WithRedisConfiguration("redis", config =>
                {
                    config
                        .WithAllowAdmin()
                        .WithDatabase(0)
                        .WithConnectionTimeout(100)
                        .WithEndpoint("127.0.0.1", 6379);
                });
#endif
            });
            
            for (int i = 0; i < iterations; i++)
            {
                Tests.CacheThreadTest(
                    CacheFactory.FromConfiguration<string>("cache", cacheConfiguration),
                    i + 10);

                Tests.SimpleAddGetTest(
                    // CacheFactory.FromConfiguration(cacheConfiguration),
                    CacheFactory.FromConfiguration<object>("cache", cacheConfiguration));
                // CacheUpdateTest(cache);

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