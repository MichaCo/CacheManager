using System;
using System.Threading.Tasks;
using CacheManager.Core;
using CacheManager.Core.Internal;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CacheManager.Events.Tests
{
    public class RedisAndMemoryNoMessagingCommand : RedisAndMemoryCommand
    {
        public RedisAndMemoryNoMessagingCommand(CommandLineApplication app, ILoggerFactory loggerFactory) : base(app, loggerFactory)
        {
        }

        protected override void Configure()
        {
            base.Configure();

            _multiplexer = ConnectionMultiplexer.Connect("127.0.0.1,allowAdmin=true");
            _configuration = new ConfigurationBuilder()
                .WithMicrosoftLogging(LoggerFactory)
                .WithMicrosoftMemoryCacheHandle("in-memory")
                .And
                .WithJsonSerializer()
                .WithRedisConfiguration("redisConfig", _multiplexer, enableKeyspaceNotifications: false)
                .WithRedisCacheHandle("redisConfig")
                .Build();
        }
    }

    public class RedisCommand : RedisAndMemoryCommand
    {
        public RedisCommand(CommandLineApplication app, ILoggerFactory loggerFactory) : base(app, loggerFactory)
        {
        }

        protected override void Configure()
        {
            base.Configure();

            _multiplexer = ConnectionMultiplexer.Connect("127.0.0.1,allowAdmin=true");
            _configuration = new ConfigurationBuilder()
                .WithMicrosoftLogging(LoggerFactory)
                .WithRedisBackplane("redisConfig")
                .WithJsonSerializer()
                .WithRedisConfiguration("redisConfig", _multiplexer, enableKeyspaceNotifications: true)
                .WithRedisCacheHandle("redisConfig")
                .Build();
        }
    }

    public class RedisAndMemoryCommand : EventCommand
    {
        protected ICacheManagerConfiguration _configuration;
        protected ConnectionMultiplexer _multiplexer;

        public RedisAndMemoryCommand(CommandLineApplication app, ILoggerFactory loggerFactory) : base(app, loggerFactory)
        {
        }

        protected override void Configure()
        {
            base.Configure();

            _multiplexer = ConnectionMultiplexer.Connect("127.0.0.1,allowAdmin=true");
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

                        cacheA.Add(key, rndNumber);

                        cacheA.TryUpdate(key, (oldVal) => oldVal + 1, out int? newValue);

                        _multiplexer.GetDatabase(0).KeyDelete(key, CommandFlags.HighPriority);

                        await Task.Delay(0);
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