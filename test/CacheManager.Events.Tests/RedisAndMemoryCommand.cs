using System;
using System.Threading.Tasks;
using CacheManager.Core;
using CacheManager.Core.Internal;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CacheManager.Events.Tests
{
    public class RedisAndMemoryCommand : EventCommand
    {
        private ICacheManagerConfiguration _configuration;
        private ConnectionMultiplexer _multiplexer;

        public RedisAndMemoryCommand(CommandLineApplication app, ILoggerFactory loggerFactory) : base(app, loggerFactory)
        {
        }

        protected override void Configure()
        {
            base.Configure();

            _multiplexer = ConnectionMultiplexer.Connect("localhost,allowAdmin=true");
            _configuration = new ConfigurationBuilder()
                .WithMicrosoftLogging(LoggerFactory)
                .WithMicrosoftMemoryCacheHandle("in-memory")
                .And
                .WithRedisBackplane("redisConfig")
                .WithJsonSerializer()
                .WithRedisConfiguration("redisConfig", _multiplexer, enableKeyspaceNotifications: true)
                .WithRedisCacheHandle("redisConfig")
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

                        while (!cacheA.Add(key, rndNumber))
                        {
                            key = Guid.NewGuid().ToString();
                        }

                        didUpdate = true;
                        cacheA.TryUpdate(key, (oldVal) => oldVal + 1, out int? newValue);
                        //while (!updateTriggeredA || !updateTriggeredB)
                        //{
                        //    await Task.Delay(1);
                        //}

                        if (cacheA[key] != cacheB[key] && cacheA[key] != null)
                        {
                            // log warn?
                        }

                        await Task.Delay(0);
                        didRemove = true;
                        _multiplexer.GetDatabase(0).KeyDelete(key, CommandFlags.HighPriority);

                        //while (!removeTriggeredA || !removeTriggeredB)
                        //{
                        //    await Task.Delay(1);
                        //}

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