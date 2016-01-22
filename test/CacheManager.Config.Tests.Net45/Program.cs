using System;
using System.Diagnostics;
using CacheManager.Core;

namespace CacheManager.Config.Tests
{
#if !NET40
    using System.IO;
    using System.Web;
    using CacheManager.Web;

    internal class SystemWebCacheHandleWrapper<TCacheValue> : SystemWebCacheHandle<TCacheValue>
    {
        public SystemWebCacheHandleWrapper(ICacheManager<TCacheValue> manager, CacheHandleConfiguration configuration)
            : base(manager, configuration)
        {
        }

        protected override HttpContextBase Context
        {
            get
            {
                if (HttpContext.Current == null)
                {
                    HttpContext.Current = new HttpContext(new HttpRequest("test", "http://test", string.Empty), new HttpResponse(new StringWriter()));
                }

                return new HttpContextWrapper(HttpContext.Current);
            }
        }
    }
#endif

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

                cfg.WithHandle(typeof(SystemWebCacheHandleWrapper<>), "WebCache")
                    .EnablePerformanceCounters();

                cfg.WithJsonSerializer();

                ////cfg.WithSystemRuntimeCacheHandle("default")
                ////    .EnablePerformanceCounters();

                cfg.WithRedisCacheHandle("redis", true)
                    .EnablePerformanceCounters();

                cfg.WithRedisBackPlate("redis");

                cfg.WithRedisConfiguration("redis", config =>
                {
                    config.WithAllowAdmin()
                        .WithDatabase(0)
                        .WithEndpoint("localhost", 6379)
                        .WithEndpoint("localhost", 6380)
                        .WithConnectionTimeout(1000);
                });

                cfg.WithMaxRetries(10);
                cfg.WithRetryTimeout(10);
            });

            for (int i = 0; i < iterations; i++)
            {
                Tests.RandomRWTest(CacheFactory.FromConfiguration<Item>("perfCache", cacheConfiguration));

                //// CacheThreadTest(
                ////    CacheFactory.FromConfiguration<string>("cache", cacheConfiguration),
                ////    i + 10);

                ////Tests.SimpleAddGetTest(
                ////    CacheFactory.FromConfiguration<object>("cache", cacheConfiguration));

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