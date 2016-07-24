using System;
using System.Linq;
using System.Threading;
using CacheManager.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CacheManager.Config.Tests
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var configBuilder = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .AddJsonFile("cache.json");

            var configuration = configBuilder.Build();

            ////var cacheCfgf = configuration.GetCacheConfigurations().First();

            ////var mgr = new BaseCacheManager<string>(cacheCfgf);

            int iterations = int.MaxValue;
            try
            {
                var builder = new Core.ConfigurationBuilder("myCache");
                builder.WithMicrosoftLogging(f =>
                {
                    f.AddConsole(LogLevel.Warning);
                    f.AddDebug(LogLevel.Debug);
                });

                builder.WithUpdateMode(CacheUpdateMode.Up);
                builder.WithRetryTimeout(1000);
                builder.WithMaxRetries(10);

//#if NETCORE
//                builder.WithDictionaryHandle("dic")
//                    .DisableStatistics();

//                //Console.WriteLine("Using Dictionary cache handle");
//#else
                builder.WithDictionaryHandle()
                    .DisableStatistics();

                builder.WithRedisCacheHandle("redis", true)
                    .DisableStatistics();

                builder.WithRedisBackplane("redis");

                builder.WithRedisConfiguration("redis", config =>
                {
                    config
                        .WithAllowAdmin()
                        .WithDatabase(0)
                        .WithConnectionTimeout(5000)
                        //.WithEndpoint("ubuntu-local", 7024);
                        //.WithEndpoint("127.0.0.1", 6380)
                        .WithEndpoint("127.0.0.1", 6379);
                        //.WithEndpoint("192.168.178.34", 7001);
                });

                builder.WithJsonSerializer();

                Console.WriteLine("Using Redis cache handle");
//#endif
                var cacheA = new BaseCacheManager<object>(builder.Build());
                cacheA.Clear();

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
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("We are done...");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.ReadKey();
        }
    }
}