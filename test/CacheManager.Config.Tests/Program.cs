using System;
using System.Threading;
using CacheManager.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace CacheManager.Config.Tests
{
    internal class Program
    {
        public Program()
        {
        }

        public void Main(string[] args)
        {
            int iterations = int.MaxValue;
            try
            {
                var cfgBuilder = new Microsoft.Extensions.Configuration.ConfigurationBuilder();
                cfgBuilder.AddJsonFile("cache.json");
                var jsonConfig = cfgBuilder.Build();
                
                var cacheConfiguration = Core.ConfigurationBuilder.BuildConfiguration(cfg =>
                {
                    cfg.WithAspNetLogging(f =>
                    {
                        // TODO: remove after logging upgrade to RC2
                        f.MinimumLevel = LogLevel.Debug;

                        f.AddConsole(LogLevel.Information);

                        // TODO: change to Debug after logging upgrade to RC2
                        f.AddDebug(LogLevel.Verbose);
                    });

                    cfg.WithUpdateMode(CacheUpdateMode.Up);
                    cfg.WithRetryTimeout(500);
                    cfg.WithMaxRetries(50);

#if DNXCORE50
                    cfg.WithDictionaryHandle("dic")
                        .DisableStatistics();

                    //Console.WriteLine("Using Dictionary cache handle");
#else
                    cfg.WithSystemRuntimeDefaultCacheHandle()
                        .DisableStatistics();

                    cfg.WithRedisCacheHandle("redis", true)
                        .DisableStatistics();

                    cfg.WithRedisBackPlate("redis");

                    cfg.WithRedisConfiguration("redis", config =>
                    {
                        config
                            .WithAllowAdmin()
                            .WithDatabase(0)
                            .WithConnectionTimeout(1000)
                            .WithEndpoint("127.0.0.1", 6380)
                            .WithEndpoint("127.0.0.1", 6379);
                        //.WithEndpoint("192.168.178.34", 7001);
                    });
                    
                    cfg.WithJsonSerializer();

                    Console.WriteLine("Using Redis cache handle");
#endif
                });

                var cacheA = CacheFactory.FromConfiguration<object>("myCache", cacheConfiguration);
                cacheA.Clear();

                var manualConfig = new CacheManagerConfiguration();
                manualConfig.CacheHandleConfigurations.Add(new CacheHandleConfiguration()
                {
                    HandleType = typeof(Core.Internal.DictionaryCacheHandle<>)
                });
                var cacheB = new BaseCacheManager<string>("name", manualConfig);

                for (int i = 0; i < iterations; i++)
                {
                    try
                    {
                        Tests.PutAndMultiGetTest(cacheA);
                    }
                    catch (AggregateException ex)
                    {
                        ex.Handle((e) =>
                        {
                            Console.WriteLine(e);
                            return true;
                        });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error: " + e.Message + "\n" + e.StackTrace);
                        Thread.Sleep(1000);
                    }

                    Console.WriteLine("---------------------------------------------------------");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("We are done...");
            Console.ReadKey();
        }
    }
}