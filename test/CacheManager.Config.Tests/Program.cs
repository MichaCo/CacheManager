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
            // Set up configuration sources.
            var builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .AddJsonFile("cache.json");
            
            this.Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        public void Main(string[] args)
        {
            int iterations = int.MaxValue;
            try
            {
                var jsonConfiguration = 
                    this.Configuration.GetCacheConfiguration("cachename")
                    .Builder
                    .WithMicrosoftLogging(f =>
                    {
                        // TODO: remove after logging upgrade to RC2
                        f.MinimumLevel = LogLevel.Debug;

                        f.AddConsole(LogLevel.Information);

                        // TODO: change to Debug after logging upgrade to RC2
                        f.AddDebug(LogLevel.Verbose);
                    })
                    .Build();

                var jsonCache = new CacheManager<string>(jsonConfiguration);
                jsonCache.Put("key", "value");
                
                var builder = new Core.ConfigurationBuilder("myCache");
                builder.WithMicrosoftLogging(f =>
                {
                    // TODO: remove after logging upgrade to RC2
                    f.MinimumLevel = LogLevel.Debug;

                    f.AddConsole(LogLevel.Information);

                    // TODO: change to Debug after logging upgrade to RC2
                    f.AddDebug(LogLevel.Verbose);
                });

                builder.WithUpdateMode(CacheUpdateMode.Up);
                builder.WithRetryTimeout(500);
                builder.WithMaxRetries(50);

#if DNXCORE50
                builder.WithDictionaryHandle("dic")
                    .DisableStatistics();

                //Console.WriteLine("Using Dictionary cache handle");
#else
                builder.WithSystemRuntimeDefaultCacheHandle()
                    .DisableStatistics();

                builder.WithRedisCacheHandle("redis", true)
                    .DisableStatistics();

                builder.WithRedisBackPlate("redis");

                builder.WithRedisConfiguration("redis", config =>
                {
                    config
                        .WithAllowAdmin()
                        .WithDatabase(0)
                        .WithConnectionTimeout(1000)
                        .WithEndpoint("127.0.0.1", 6380)
                        .WithEndpoint("127.0.0.1", 6379);
                    //.WithEndpoint("192.168.178.34", 7001);
                });

                builder.WithJsonSerializer();

                Console.WriteLine("Using Redis cache handle");
#endif
                var cacheA = new CacheManager<object>(builder.Build());
                cacheA.Clear();

                var manualConfig = new CacheManagerConfiguration();
                manualConfig.CacheHandleConfigurations.Add(new CacheHandleConfiguration()
                {
                    HandleType = typeof(Core.Internal.DictionaryCacheHandle<>)
                });
                var cacheB = new CacheManager<string>("name", manualConfig);

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
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.ReadKey();
        }
    }
}