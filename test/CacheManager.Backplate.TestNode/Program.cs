using System;
using System.Linq;
using System.Threading;
using CacheManager.Core;
using CacheManager.Core.Internal;

namespace CacheManager.Backplate.TestNode
{
    public class Program
    {
        private static ICacheManager<int> cache = new BaseCacheManager<int>(
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

        internal static void Main(string[] args)
        {
            //// README:
            //// Run me multiple times and at least once with some arguments so that the first condition hits
            //// You should see one the console with args adding and removing the key
            //// All other consoles should receive the remove event and counting the counter
            //// counter should reset each remove, because the key was removed

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
                    var value = cache.AddOrUpdate("backplateTest", 0, v => v + 1);
                    //Console.WriteLine("Value: " + value);
                    //Thread.Sleep(50);
                }
            }
        }

        private static void CacheOnRemove(object sender, CacheActionEventArgs e)
        {
            Console.WriteLine("Removing " + e.Key);
        }

        private static void CacheOnAdd(object sender, CacheActionEventArgs e)
        {
            Console.WriteLine("Adding " + e.Key);
        }
    }
}