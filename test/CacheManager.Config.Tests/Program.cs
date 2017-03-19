using System;
using System.Linq;
using System.Threading;
using CacheManager.Core;
using CacheManager.Redis;
#if !NETCOREAPP
using Enyim.Caching;
using Enyim.Caching.Configuration;
#endif
using Microsoft.Extensions.Logging;

namespace CacheManager.Config.Tests
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var iterations = 10;
            try
            {
                var builder = new Core.ConfigurationBuilder("myCache");
                builder.WithMicrosoftLogging(f =>
                {
                    f.AddConsole(LogLevel.Warning);
                    f.AddDebug(LogLevel.Debug);
                });

                builder.WithRetryTimeout(100);
                builder.WithMaxRetries(5);
                builder.WithDictionaryHandle()
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(20))
                    .DisableStatistics();

                builder.WithRedisCacheHandle("redis", true)
                    .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(60))
                    .DisableStatistics();

                builder.WithRedisBackplane("redis");

                builder.WithRedisConfiguration("redis", config =>
                {
                    config
                        .WithAllowAdmin()
                        .WithDatabase(0)
                        .WithConnectionTimeout(5000)
                        .WithEndpoint("127.0.0.1", 6379);
                });

                //builder.WithGzJsonSerializer();
                builder.WithBondCompactBinarySerializer();

#if !NETCOREAPP
                //var memcachedCfg = new MemcachedClientConfiguration();
                //memcachedCfg.AddServer("localhost", 11211);
                //builder.WithMemcachedCacheHandle(memcachedCfg);
#endif

                var cacheA = new BaseCacheManager<string>(builder.Build());
                cacheA.Clear();

                for (var i = 0; i < iterations; i++)
                {
                    var redisHandle = cacheA.CacheHandles.OfType<RedisCacheHandle<string>>().First();
                    foreach (var server in redisHandle.Servers)
                    {
                        Console.WriteLine($"{server.ToString()}=>{server.EndPoint} connected:{server.IsConnected} isSlave:{server.IsSlave}");
                    }

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