using System;
using System.Threading;
using CacheManager.Core;
using Microsoft.Extensions.Logging;

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
                var cacheConfiguration = ConfigurationBuilder.BuildConfiguration(cfg =>
                {
                    cfg.WithAspNetLogging(f => f
                        .AddConsole(LogLevel.Error)
                        .AddDebug(LogLevel.Information));

                    cfg.WithUpdateMode(CacheUpdateMode.Up);
                    cfg.WithRetryTimeout(100);
                    cfg.WithMaxRetries(50);

#if DNXCORE50
                    cfg.WithDictionaryHandle("dic")
                        .DisableStatistics();

                    //Console.WriteLine("Using Dictionary cache handle");
#else
                    cfg.WithDictionaryHandle("dic")
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
                        ////.WithEndpoint("192.168.178.32", 6379);
                    });

                    cfg.WithJsonSerializer();

                    Console.WriteLine("Using Redis cache handle");
#endif
                });

                var cacheA = CacheFactory.FromConfiguration<object>("myCache", cacheConfiguration);
                cacheA.Clear();

                for (int i = 0; i < iterations; i++)
                {
                    try
                    {
                        Tests.SimpleAddGetTest(cacheA);
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