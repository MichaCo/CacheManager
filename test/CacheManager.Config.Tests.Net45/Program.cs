using System;
using System.Diagnostics;
using CacheManager.Core;

namespace CacheManager.Config.Tests
{
    internal class Program
    {
        public static void Main()
        {
            var swatch = Stopwatch.StartNew();
            int iterations = int.MaxValue;
            swatch.Restart();
            var cacheConfiguration = ConfigurationBuilder.BuildConfiguration(cfg =>
            {
                cfg.WithUpdateMode(CacheUpdateMode.Up);

                cfg.WithSystemRuntimeCacheHandle("default")
                    .EnablePerformanceCounters();

                cfg.WithRedisCacheHandle("redis", true)
                    .EnablePerformanceCounters();

                //// cfg.WithRedisBackPlate("redis");

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
                //// CacheThreadTest(
                ////    CacheFactory.FromConfiguration<string>("cache", cacheConfiguration),
                ////    i + 10);

                Tests.SimpleAddGetTest(
                    CacheFactory.FromConfiguration<object>("cache", cacheConfiguration));

                //// CacheUpdateTest(cache);

                //// Console.WriteLine(string.Format("Iterations ended after {0}ms.", swatch.ElapsedMilliseconds));
                Console.WriteLine("---------------------------------------------------------");
                swatch.Restart();
            }

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("We are done...");
            Console.ReadLine();
        }
    }
}