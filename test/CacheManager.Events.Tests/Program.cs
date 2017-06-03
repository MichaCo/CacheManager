using System;
using System.Threading;
using CacheManager.Core;
using CacheManager.Core.Internal;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CacheManager.Events.Tests
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var loggerFactory = new LoggerFactory()
               .AddConsole(LogLevel.Debug);

            var redis = ConnectionMultiplexer.Connect("localhost,allowAdmin=true");
            var memoryAndRedis = new ConfigurationBuilder()
                .WithMicrosoftLogging(loggerFactory)
                .WithMicrosoftMemoryCacheHandle("in-memory")
                .And
                .WithRedisBackplane("redisConfig")
                .WithJsonSerializer()
                .WithRedisConfiguration("redisConfig", redis, enableKeyspaceNotifications: true)
                .WithRedisCacheHandle("redisConfig")
                .Build();

            var cache = CacheFactory.FromConfiguration<int>("CacheA", memoryAndRedis);
            var cache2 = CacheFactory.FromConfiguration<int>("CacheB", memoryAndRedis);
            cache.Clear();
            cache2.Clear();
            cache.OnRemove += OnRemove;
            cache.OnRemoveByHandle += OnRemoveByHandle;
            cache.OnAdd += OnAdd;
            cache.OnGet += OnGet;
            cache.OnPut += OnPut;
            cache.OnUpdate += OnUpdate;
            cache2.OnRemove += OnRemove;
            cache2.OnRemoveByHandle += OnRemoveByHandle;
            cache2.OnAdd += OnAdd;
            cache2.OnGet += OnGet;
            cache2.OnPut += OnPut;
            cache2.OnUpdate += OnUpdate;

            var rnd = new Random(42);
            
            var rndNumber = rnd.Next(42, 420);
            var rndKey = "key" + rndNumber;
            if (cache.TryGetOrAdd(rndKey, key => rndNumber, out int value))
            {
                if (cache[rndKey] != cache2[rndKey])
                {
                    throw new Exception();
                }

                if (!cache.TryUpdate(rndKey, (oldVal) => oldVal + 1, out int newValue))
                {
                    throw new Exception();
                }
                
                Thread.Sleep(1000);
                var result = cache[rndKey];
                var resultB = cache2[rndKey];
                if (result != resultB || result == 0)
                {
                    throw new Exception("Unexpected values");
                }
            }

            redis.GetDatabase(0).KeyDelete(rndKey, CommandFlags.HighPriority);
            Thread.Sleep(500);

            Console.WriteLine("Done");
            Console.ReadKey();
        }

        private static void OnUpdate(object sender, CacheActionEventArgs e)
        {
            var cache = sender as BaseCacheManager<int>;
            Console.WriteLine($"{cache?.Name} OnUpdate: {e}");
        }

        private static void OnPut(object sender, CacheActionEventArgs e)
        {
            var cache = sender as BaseCacheManager<int>;
            Console.WriteLine($"{cache?.Name} OnPut: {e}");
        }

        private static void OnGet(object sender, CacheActionEventArgs e)
        {
            var cache = sender as BaseCacheManager<int>;
            Console.WriteLine($"{cache?.Name} OnGet: {e}");
        }

        private static void OnAdd(object sender, CacheActionEventArgs e)
        {
            var cache = sender as BaseCacheManager<int>;
            Console.WriteLine($"{cache?.Name} OnAdd: {e}");
        }

        private static void OnRemove(object sender, CacheActionEventArgs e)
        {
            var cache = sender as BaseCacheManager<int>;
            Console.WriteLine($"{cache?.Name} OnRemove: {e}");
        }

        private static void OnRemoveByHandle(object sender, CacheItemRemovedEventArgs e)
        {
            var cache = sender as BaseCacheManager<int>;
            var exists = cache.Exists(e.Key);
            Console.WriteLine($"{cache?.Name} OnRemoveByHandle: {e} - removed value: '{e.Value}' still exists? {exists}.");
        }
    }
}