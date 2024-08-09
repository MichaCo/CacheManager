using System;
using System.Threading;
using CacheManager.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace CacheManager.Examples
{
    public class Program
    {
        private static void Main()
        {
            EventsExample();
            MostSimpleCacheManagerWithLogging();
            SimpleCustomBuildConfigurationUsingConfigBuilder();
            SimpleCustomBuildConfigurationUsingFactory();
            UpdateTest();
            UpdateCounterTest();
            LoggingSample();
        }

        private static void MostSimpleCacheManager()
        {
            var config = new CacheConfigurationBuilder()
                .WithSystemRuntimeCacheHandle()
                .Build();

            var cache = new BaseCacheManager<string>(config);
            // or
            var cache2 = CacheFactory.FromConfiguration<string>(config);
        }

        private static void MostSimpleCacheManagerB()
        {
            var cache = new BaseCacheManager<string>(
                new CacheManagerConfiguration()
                    .Builder
                    .WithSystemRuntimeCacheHandle()
                    .Build());
        }

        private static void MostSimpleCacheManagerC()
        {
            var cache = CacheFactory.Build<string>(
                p => p.WithSystemRuntimeCacheHandle());
        }

        private static void MostSimpleCacheManagerWithLogging()
        {
            var services = new ServiceCollection();
            services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
            var loggerFactory = services.BuildServiceProvider().GetService<ILoggerFactory>();

            var config = new CacheConfigurationBuilder()
                .WithSystemRuntimeCacheHandle()
                .Build();

            ICacheManager<string> cache = new BaseCacheManager<string>(config, loggerFactory);
            cache.Add("test", "test");
            cache.Exists("test no");
            cache.Remove("test");

            // or
            cache = CacheFactory.FromConfiguration<string>(config, loggerFactory);

            cache.Add("test", "test");
            cache.Exists("test no");
            cache.Remove("test");
        }

        private static void EditExistingConfiguration()
        {
            var config = new CacheConfigurationBuilder()
                .WithSystemRuntimeCacheHandle()
                    .EnableStatistics()
                .Build();

            config = new CacheConfigurationBuilder(config)
                .Build();
        }

        private static void LoggingSample()
        {
            var services = new ServiceCollection();
            services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Trace));
            var loggerFactory = services.BuildServiceProvider().GetService<ILoggerFactory>();

            var cache = CacheFactory.Build<string>(
                c => c.WithDictionaryHandle()
                    .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10)), 
                loggerFactory);

            cache.AddOrUpdate("myKey", "someregion", "value", _ => "new value");
            cache.AddOrUpdate("myKey", "someregion", "value", _ => "new value");
            cache.Expire("myKey", "someregion", TimeSpan.FromMinutes(10));
            var val = cache.Get("myKey", "someregion");
        }

        private static void AppConfigLoadInstalledCacheCfg()
        {
            var services = new ServiceCollection();
            services.AddLogging(c => c.AddConsole());
            var loggerFactory = services.BuildServiceProvider().GetService<ILoggerFactory>();

            var cache = CacheFactory.FromConfiguration<object>("myCache", loggerFactory);
            cache.Add("key", "value");
        }

        private static void EventsExample()
        {
            var services = new ServiceCollection();
            services.AddLogging(c => c.AddConsole());
            var loggerFactory = services.BuildServiceProvider().GetService<ILoggerFactory>();

            var cache = CacheFactory.Build<string>(s => s.WithDictionaryHandle(), loggerFactory);
            cache.OnAdd += (sender, args) => Console.WriteLine("Added " + args.Key);
            cache.OnGet += (sender, args) => Console.WriteLine("Got " + args.Key);
            cache.OnRemove += (sender, args) => Console.WriteLine("Removed " + args.Key);

            cache.Add("key", "value");
            var val = cache.Get("key");
            cache.Remove("key");
        }

        private static void RedisSample()
        {
            var services = new ServiceCollection();
            services.AddLogging(c => c.AddConsole());
            var loggerFactory = services.BuildServiceProvider().GetService<ILoggerFactory>();

            var cache = CacheFactory.Build<int>(settings =>
            {
                settings
                    .WithSystemRuntimeCacheHandle()
                    .And
                    .WithRedisConfiguration("redis", config =>
                    {
                        config.WithAllowAdmin()
                            .WithDatabase(0)
                            .WithEndpoint("localhost", 6379);
                    })
                    .WithMaxRetries(1000)
                    .WithRetryTimeout(100)
                    .WithRedisBackplane("redis")
                    .WithRedisCacheHandle("redis", true);
            },
            loggerFactory);

            cache.Add("test", 123456);

            cache.Update("test", p => p + 1);

            var result = cache.Get("test");
        }

        private static void SimpleCustomBuildConfigurationUsingConfigBuilder()
        {
            // this is using the CacheManager.Core.Configuration.ConfigurationBuilder to build a
            // custom config you can do the same with the CacheFactory
            var cfg = CacheConfigurationBuilder.BuildConfiguration(settings =>
                {
                    settings.WithUpdateMode(CacheUpdateMode.Up)
                        .WithDictionaryHandle()
                            .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10));
                });

            var cache = CacheFactory.FromConfiguration<string>(cfg);
            cache.Add("key", "value");

            // reusing the configuration and using the same cache for different types:
            var numbers = CacheFactory.FromConfiguration<int>(cfg);
            numbers.Add("intKey", 2323);
            numbers.Update("intKey", v => v + 1);
        }

        private static void SimpleCustomBuildConfigurationUsingFactory()
        {
            var cache = CacheFactory.Build(settings =>
            {
                settings
                    .WithUpdateMode(CacheUpdateMode.Up)
                    .WithDictionaryHandle()
                        .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10));
            });

            cache.Add("key", "value");
        }

        private static void UpdateTest()
        {
            var cache = CacheFactory.Build<string>(s => s.WithDictionaryHandle());

            Console.WriteLine("Testing update...");

            if (!cache.TryUpdate("test", v => "item has not yet been added", out string newValue))
            {
                Console.WriteLine("Value not added?: {0}", newValue == null);
            }

            cache.Add("test", "start");
            Console.WriteLine("Initial value: {0}", cache["test"]);

            cache.AddOrUpdate("test", "adding again?", v => "updating and not adding");
            Console.WriteLine("After AddOrUpdate: {0}", cache["test"]);

            cache.Remove("test");
            try
            {
                var removeValue = cache.Update("test", v => "updated?");
            }
            catch
            {
                Console.WriteLine("Error as expected because item didn't exist.");
            }

            // use try update to not deal with exceptions
            if (!cache.TryUpdate("test", v => v, out string removedValue))
            {
                Console.WriteLine("Value after remove is null?: {0}", removedValue == null);
            }
        }

        private static void UpdateCounterTest()
        {
            var cache = CacheFactory.Build<long>(s => s.WithDictionaryHandle());

            Console.WriteLine("Testing update counter...");

            cache.AddOrUpdate("counter", 0, v => v + 1);

            Console.WriteLine("Initial value: {0}", cache.Get("counter"));

            for (var i = 0; i < 12345; i++)
            {
                cache.Update("counter", v => v + 1);
            }

            Console.WriteLine("Final value: {0}", cache.Get("counter"));
        }

        private static void MultiCacheEvictionWithoutRedisCacheHandle()
        {
            var config = new CacheConfigurationBuilder("Redis with Redis Backplane")
                .WithDictionaryHandle(true)
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(5))
                .And
                .WithRedisBackplane("redisConfig")
                .WithRedisConfiguration("redisConfig", "localhost,allowadmin=true", enableKeyspaceNotifications: true)
                //.WithMicrosoftLogging(new LoggerFactory().AddConsole(LogLevel.Debug))
                .Build();

            var cacheA = new BaseCacheManager<string>(config);
            var cacheB = new BaseCacheManager<string>(config);

            var key = "someKey";

            cacheA.OnRemove += (s, args) =>
            {
                Console.WriteLine("A triggered remove: " + args.ToString() + " - key still exists? " + cacheA.Exists(key));
            };
            cacheB.OnRemove += (s, args) =>
            {
                Console.WriteLine("B triggered remove: " + args.ToString() + " - key still exists? " + cacheB.Exists(key));
            };

            cacheA.OnRemoveByHandle += (s, args) =>
            {
                cacheA.Remove(args.Key);
                Console.WriteLine("A triggered removeByHandle: " + args.ToString() + " - key still exists? " + cacheA.Exists(key));
            };

            cacheB.OnRemoveByHandle += (s, args) =>
            {
                Console.WriteLine("B triggered removeByHandle: " + args.ToString() + " - key still exists? " + cacheA.Exists(key) + " in A? " + cacheA.Exists(key));
            };

            cacheA.OnAdd += (s, args) =>
            {
                Console.WriteLine("A triggered add: " + args.ToString());
            };

            cacheB.OnAdd += (s, args) =>
            {
                Console.WriteLine("B triggered add: " + args.ToString());
            };

            Console.WriteLine("Add to A: " + cacheA.Add(key, "some value"));
            Console.WriteLine("Add to B: " + cacheB.Add(key, "some value"));

            Thread.Sleep(2000);
            cacheA.Remove(key);
        }
    }
}
