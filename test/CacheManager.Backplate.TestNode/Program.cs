using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CacheManager.Core;
using CacheManager.Core.Cache;
using CacheManager.Core.Configuration;

namespace CacheManager.Backplate.TestNode
{
    class Program
    {
        static ICacheManager<int> cache = new BaseCacheManager<int>(
            "cache",
            ConfigurationBuilder.BuildConfiguration(c =>
            {
                c
                    .WithSystemRuntimeCacheHandle(Guid.NewGuid().ToString())
                    .And
                    .WithRedisCacheHandle("redis", true)
                    .And
                    .WithRedisBackPlate("redis")
                    .WithRedisConfiguration("redis", "localhost:6379,allowAdmin=true");
            }));

        static void Main(string[] args)
        {
            // README: 
            // Run me multiple times and at least once with some arguments so that the first condition hits
            // You should see one the console with args adding and removing the key
            // All other consoles should receive the remove event and counting the counter
            // counter should reset each remove, because the key was removed

            cache.OnAdd += CacheOnAdd;
            cache.OnRemove += CacheOnRemove;

            if (args.Length > 0)
            {
                while (true)
                {
                    cache.Add("backplateTest", 0);
                    Thread.Sleep(2000);
                    cache.Remove("backplateTest");
                }
            }
            else
            {
                while (true)
                {
                    var value = cache.Update("backplateTest", v => v + 1);
                    Console.WriteLine("Value: " + value);
                    Thread.Sleep(500);
                }
            }
        }

        static void CacheOnRemove(object sender, CacheActionEventArgs e)
        {
            Console.WriteLine("Removing " + e.Key);
        }

        static void CacheOnAdd(object sender, CacheActionEventArgs e)
        {
            Console.WriteLine("Adding " + e.Key);
        }
    }
}
