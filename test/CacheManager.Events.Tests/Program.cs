using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CacheManager.Core;
using CacheManager.Core.Internal;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CacheManager.Events.Tests
{
    internal class Program
    {
        private static int _runTime;
        private static int _maxConcurrentTasks;
        private static CancellationTokenSource _source;

        private static void Main(string[] args)
        {
            _runTime = 60;
            _maxConcurrentTasks = 500;

            _source = new CancellationTokenSource(_runTime * 1000);

            RunFullRedis();

            Thread.Sleep(500);

            Console.WriteLine("Done");
            Console.ReadKey();
        }

        public static void RunFullRedis()
        {
            var loggerFactory = new LoggerFactory()
               .AddConsole(LogLevel.Information);

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

            var rnd = new Random(42);

            RunWithConfiguration<int?>(
                memoryAndRedis,
                (cacheA, cacheB, hA, hB) =>
                {
                    var rndNumber = rnd.Next(42, 420);
                    var rndKey = Guid.NewGuid().ToString();

                    hA.Expect(CacheEvent.Add, rndKey);
                    if (!cacheA.Add(rndKey, rndNumber))
                    {
                        throw new Exception("add or update failed");
                    }

                    return rndKey;
                },
                async (key, cacheA, cacheB, hA, hB) =>
                {
                    hA.Expect(CacheEvent.Upd, key);
                    if (!cacheA.TryUpdate(key, (oldVal) => oldVal + 1, out int? newValue))
                    {
                        throw new Exception("Update failed");
                    }

                    await Task.Delay(0);

                    hA.Expect(CacheEvent.Get, key);
                    hB.Expect(CacheEvent.Get, key);
                    if (cacheA[key] != cacheB[key] && cacheA[key] != null)
                    {
                        throw new Exception("value in cacheA not equal cacheB");
                    }
                },
                async (key, cacheA, cacheB, hA, hB) =>
                {
                    hA.Expect(CacheEvent.Rem, key);
                    hB.Expect(CacheEvent.Rem, key);
                    hA.Expect(CacheEvent.ReH, key);
                    hB.Expect(CacheEvent.ReH, key);
                    redis.GetDatabase(0).KeyDelete(key, CommandFlags.HighPriority);
                    await Task.Delay(0);
                }
                ).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private static async Task RunWithConfiguration<TCacheItem>(
            ICacheManagerConfiguration configuration,
            Func<ICacheManager<TCacheItem>, ICacheManager<TCacheItem>, EventHandling<TCacheItem>, EventHandling<TCacheItem>, string> init,
            params Func<string, ICacheManager<TCacheItem>, ICacheManager<TCacheItem>, EventHandling<TCacheItem>, EventHandling<TCacheItem>, Task>[] stages)
        {
            var cache = CacheFactory.FromConfiguration<TCacheItem>("CacheA", configuration);
            var cache2 = CacheFactory.FromConfiguration<TCacheItem>("CacheB", configuration);
            cache.Clear();
            cache2.Clear();

            var handlingA = new EventHandling<TCacheItem>(cache);
            var handlingB = new EventHandling<TCacheItem>(cache2);

            Func<Task> task = () => Task.Run(async () =>
            {
                var key = init(cache, cache2, handlingA, handlingB);
                foreach (var stage in stages)
                {
                    var t = stage(key, cache, cache2, handlingA, handlingB);
                    await t.ConfigureAwait(false);
                }
            });

            await Runner(task, handlingA, handlingB);
        }

        private static async Task Runner<TCacheValue>(Func<Task> task, params EventHandling<TCacheValue>[] handlings)
        {
            var swatch = Stopwatch.StartNew();
            var tasks = new List<Task>();

            var reportTask = Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(1000);

                    var currentTasks = tasks.ToArray();

                    Console.WriteLine($"Waiting tasks: " + currentTasks.Count(p => p.Status == TaskStatus.WaitingForActivation));

                    foreach (var handling in handlings)
                    {
                        var status = handling.GetExpectedState();

                        var report = new StringBuilder();
                        foreach (var kv in status)
                        {
                            report.Append($"{kv.Key}:{string.Join(":", kv.Value)} ");
                        }

                        Console.WriteLine("Expected: " + report.ToString());
                    }

                    if (_source.IsCancellationRequested)
                    {
                        return;
                    }
                }
            });

            while (!_source.IsCancellationRequested)
            {
                tasks.Add(task());

                if (tasks.Count >= _maxConcurrentTasks)
                {
                    await Task.WhenAll(tasks);
                    tasks.Clear();
                }
            }

            await Task.WhenAll(tasks);
        }

        private class EventHandling<TCacheValue>
        {
            private object locki = new object();
            private readonly Dictionary<CacheEvent, string[]> _expected = new Dictionary<CacheEvent, string[]>();
            private readonly Dictionary<CacheEvent, int[]> _updates = new Dictionary<CacheEvent, int[]>();

            public EventHandling(ICacheManager<TCacheValue> cache)
            {
                Cache = cache ?? throw new ArgumentNullException(nameof(cache));

                Cache.OnRemove += OnRemove;
                Cache.OnRemoveByHandle += OnRemoveByHandle;
                Cache.OnAdd += OnAdd;
                Cache.OnGet += OnGet;
                Cache.OnPut += OnPut;
                Cache.OnUpdate += OnUpdate;

                _updates.Add(CacheEvent.Add, new int[2]);
                _updates.Add(CacheEvent.Put, new int[2]);
                _updates.Add(CacheEvent.Rem, new int[2]);
                _updates.Add(CacheEvent.Get, new int[2]);
                _updates.Add(CacheEvent.ReH, new int[2]);
                _updates.Add(CacheEvent.Upd, new int[2]);
            }

            public void Expect(CacheEvent ev, string key)
            {
                lock (locki)
                {
                    string[] values = null;
                    while (!_expected.TryGetValue(ev, out values))
                    {
                        _expected.Add(ev, new string[0]);
                    }

                    if (!values.Contains(key))
                    {
                        var l = values.ToList();
                        l.Add(key);
                        _expected[ev] = l.ToArray();
                    }
                }

                Interlocked.Increment(ref _updates[ev][0]);
            }

            public Dictionary<CacheEvent, int[]> GetExpectedState()
            {
                var result = new Dictionary<CacheEvent, int[]>();

                foreach (var kv in _expected.ToArray())
                {
                    var counts = _updates[kv.Key];
                    result.Add(kv.Key, new[] { kv.Value.Length, counts[0], counts[1] });
                }

                return result;
            }

            private void RemoveExpected(CacheEvent ev, string key)
            {
                lock (locki)
                {
                    if (_expected.ContainsKey(ev))
                    {
                        if (_expected.TryGetValue(ev, out string[] values))
                        {
                            if (values.Contains(key))
                            {
                                var l = values.ToList();
                                l.Remove(key);
                                _expected[ev] = l.ToArray();
                            }
                        }
                    }
                }

                Interlocked.Increment(ref _updates[ev][1]);
            }

            public ICacheManager<TCacheValue> Cache { get; }

            private void OnUpdate(object sender, CacheActionEventArgs e)
            {
                RemoveExpected(CacheEvent.Upd, e.Key);
            }

            private void OnPut(object sender, CacheActionEventArgs e)
            {
                RemoveExpected(CacheEvent.Put, e.Key);
            }

            private void OnGet(object sender, CacheActionEventArgs e)
            {
                RemoveExpected(CacheEvent.Get, e.Key);
                //Console.WriteLine(e);
            }

            private void OnAdd(object sender, CacheActionEventArgs e)
            {
                RemoveExpected(CacheEvent.Add, e.Key);
            }

            private void OnRemove(object sender, CacheActionEventArgs e)
            {
                RemoveExpected(CacheEvent.Rem, e.Key);
            }

            private void OnRemoveByHandle(object sender, CacheItemRemovedEventArgs e)
            {
                RemoveExpected(CacheEvent.ReH, e.Key);
                //Console.WriteLine($"{cache?.Name} OnRemoveByHandle: {e} - removed value: '{e.Value}' still exists? {exists}.");
            }
        }

        private enum CacheEvent
        {
            Get,
            Add,
            Put,
            Rem,
            Upd,
            ClA,
            ClR,
            ReH
        }
    }
}