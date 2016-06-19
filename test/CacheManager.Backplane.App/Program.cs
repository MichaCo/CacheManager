using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CacheManager.Core;
using CacheManager.Core.Internal;
using Microsoft.Extensions.Configuration;

namespace CacheManager.Backplane.App
{
    public class Program
    {
        private static IDictionary<string, TestItem> caches = new Dictionary<string, TestItem>();
        private static long changesCacheA;
        private static long changesCacheB;
        private static long changesReceivedA;
        private static long changesReceivedB;
        private static object lockUpdate = new object();

        public static void Main(string[] args)
        {
            Console.WriteLine(Environment.NewLine);
            try
            {
                var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                    .AddJsonFile("cache.json")
                    .Build();

                InitCache(config.GetCacheConfigurations());

                UpdateStatus(0, 0, 0, 0);

                Test();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            Console.WriteLine("done");
            Console.Read();
        }

        private static void InitCache(IEnumerable<CacheManagerConfiguration> configurations)
        {
            foreach (var config in configurations)
            {
                var cache = new BaseCacheManager<string>(config);
                caches.Add(cache.Name, new TestItem(cache));
            }
        }

        private static void Test()
        {
            // Add to A
            // Check Fired Changed on B
            var count = 0L;
            while (true)
            {
                count++;
                foreach (var cache in caches)
                {
                    cache.Value.Put("Key" + count, "Value");
                    Task.Delay(0).Wait();
                    if (count % 100 == 0)
                    {
                        UpdateConsole();
                    }
                    if (count % 100000 == 0)
                    {
                        Task.Delay(1000).Wait();
                        UpdateConsole();
                    }
                }
            }
        }

        private static void UpdateStatus(int addA, int addB, int receivedA, int receivedB)
        {
            Interlocked.Add(ref changesCacheA, addA);
            Interlocked.Add(ref changesCacheB, addB);
            Interlocked.Add(ref changesReceivedA, receivedA);
            Interlocked.Add(ref changesReceivedB, receivedB);
        }

        private static void UpdateConsole()
        {
            lock (lockUpdate)
            {
                var left = Console.CursorLeft;
                var top = Console.CursorTop;
                var bg = Console.BackgroundColor;
                var fg = Console.ForegroundColor;

                Console.SetCursorPosition(0, 0);
                Console.BackgroundColor = ConsoleColor.Blue;
                Console.ForegroundColor = ConsoleColor.White;

                /* message goes here */
                Console.WriteLine($"[{changesCacheA}] [{changesCacheB}] [{changesReceivedA}] [{changesReceivedB}]");

                Console.SetCursorPosition(left, top);
                Console.BackgroundColor = bg;
                Console.ForegroundColor = fg;
            }
        }

        class TestItem
        {
            public TestItem(ICacheManager<string> cache)
            {
                this.Cache = cache as BaseCacheManager<string>;
                this.Cache.Backplane.Changed += BackplaneChanged;
                this.Cache.Backplane.Cleared += BackplaneCleared;
                this.Cache.Backplane.ClearedRegion += BackplaneClearedRegion;
                this.Cache.Backplane.Removed += BackplaneRemoved;
            }

            public BaseCacheManager<string> Cache { get; }

            public List<CacheItemEventArgs> ChangedEvents { get; } = new List<CacheItemEventArgs>();

            public List<CacheItemEventArgs> RemovedEvents { get; } = new List<CacheItemEventArgs>();

            public List<bool> ClearedEvents { get; } = new List<bool>();

            public List<RegionEventArgs> ClearedRegionEvents { get; } = new List<RegionEventArgs>();

            public void Put(string key, string value)
            {
                this.Cache.Put(key, value);
                if (this.Cache.Name == "CacheA")
                {
                    UpdateStatus(1, 0, 0, 0);
                }
                else
                {
                    UpdateStatus(0, 1, 0, 0);
                }
            }

            private void BackplaneRemoved(object sender, CacheItemEventArgs e)
            {
                //Console.WriteLine($"Removed {e.Key} {e.Region} from {this.Cache.Name}.");
                //RemovedEvents.Add(e);
            }

            private void BackplaneClearedRegion(object sender, RegionEventArgs e)
            {
                //ClearedRegionEvents.Add(e);
            }

            private void BackplaneCleared(object sender, EventArgs e)
            {
                //ClearedEvents.Add(true);
            }

            private void BackplaneChanged(object sender, CacheItemEventArgs e)
            {
                if (this.Cache.Name == "CacheA")
                {
                    UpdateStatus(0, 0, 1, 0);
                }
                else
                {
                    UpdateStatus(0, 0, 0, 1);
                }

                //Console.WriteLine($"Changed {e.Key} {e.Region} on {this.Cache.Name}.");
                //ChangedEvents.Add(e);
            }
        }
    }
}