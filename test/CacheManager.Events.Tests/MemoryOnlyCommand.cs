using System;
using System.Threading.Tasks;
using CacheManager.Core;
using CacheManager.Core.Internal;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace CacheManager.Events.Tests
{
    public class MemoryOnlyCommand : EventCommand
    {
        private ICacheManagerConfiguration _configuration;

        public MemoryOnlyCommand(CommandLineApplication app, ILoggerFactory loggerFactory) : base(app, loggerFactory)
        {
        }

        protected override void Configure()
        {
            base.Configure();

            _configuration = new ConfigurationBuilder()
                .WithMicrosoftLogging(LoggerFactory)
                .WithDictionaryHandle("in-memory", isBackplaneSource: true)
                .And
                .WithRedisBackplane("redisConfig")
                .WithRedisConfiguration("redisConfig", "localhost", enableKeyspaceNotifications: true)
                .Build();
        }

        public override async Task<int> Execute()
        {
            var rnd = new Random(42);

            try
            {
                await RunWithConfigurationTwoCaches<int?>(
                    _configuration,
                    async (cacheA, cacheB, hA, hB) =>
                    {
                        var rndNumber = rnd.Next(42, 420);
                        var key = Guid.NewGuid().ToString();

                        bool didRemove = false;
                        bool didUpdate = false;
                        bool removeTriggeredA = false;
                        bool updateTriggeredA = false;
                        bool removeTriggeredB = false;
                        bool updateTriggeredB = false;

                        void OnUpdate(object sender, CacheActionEventArgs args)
                        {
                            if (args.Key.Equals(key))
                            {
                                if (!didUpdate)
                                {
                                    Console.WriteLine("Key has been updated without me calling it");
                                }
                                else if (sender.Equals(cacheA))
                                {
                                    updateTriggeredA = true;
                                }
                                else if (sender.Equals(cacheB))
                                {
                                    updateTriggeredB = true;
                                }
                            }
                        }

                        void OnRemove(object sender, CacheActionEventArgs args)
                        {
                            if (args.Key.Equals(key))
                            {
                                if (!didRemove)
                                {
                                    Console.WriteLine("Key has been removed without me removing it");
                                }
                                else if (sender.Equals(cacheA))
                                {
                                    removeTriggeredA = true;
                                }
                                else if (sender.Equals(cacheB))
                                {
                                    removeTriggeredB = true;
                                }
                            }
                        }

                        cacheA.OnUpdate += OnUpdate;
                        cacheA.OnRemove += OnRemove;
                        cacheB.OnUpdate += OnUpdate;
                        cacheB.OnRemove += OnRemove;
                        
                        if (!cacheA.Add(key, rndNumber) || !cacheB.Add(key, rndNumber))
                        {
                            throw new Exception("could not add key");
                        }
                        
                        await Task.Delay(0);

                        didUpdate = true;
                        cacheA.TryUpdate(key, (oldVal) => oldVal + 1, out int? newValue);

                        while (!updateTriggeredA || !updateTriggeredB)
                        {
                            await Task.Delay(5);
                        }

                        var a = cacheA[key];
                        var b = cacheB[key];

                        if (a == null || b == null)
                        {
                            Console.WriteLine($"a:{a} b:{b}");
                        }

                        didRemove = true;
                        cacheA.Remove(key);

                        while (!removeTriggeredA || !removeTriggeredB)
                        {
                            await Task.Delay(5);
                        }
                        
                        if (cacheA[key] != null || cacheB[key] != null)
                        {
                            Console.WriteLine($"value still there");
                        }

                        cacheA.OnUpdate -= OnUpdate;
                        cacheA.OnRemove -= OnRemove;
                        cacheB.OnUpdate -= OnUpdate;
                        cacheB.OnRemove -= OnRemove;
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 500;
            }
            return 0;
        }
    }
}