using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CacheManager.Core.Cache;
using CacheManager.Core.Configuration;
using StackRedis = StackExchange.Redis;

namespace CacheManager.StackExchange.Redis
{
    public sealed class RedisCacheBackPlate : CacheBackPlate
    {
        private StackRedis.ISubscriber redisSubscriper;
        private readonly string channelName;
        private readonly string identifier;

        public RedisCacheBackPlate(string name, ICacheManagerConfiguration configuration)
            : base(name, configuration)
        {
            this.channelName = string.Format(
                CultureInfo.InvariantCulture,
                "CacheManagerBackPlate_{0}",
                configuration.Name);

            this.identifier = Guid.NewGuid().ToString();

            RetryHelper.Retry(() =>
            {
                var cfg = configuration.RedisConfigurations.FirstOrDefault(p => p.Id == name);
                if (cfg == null)
                {
                    throw new InvalidOperationException("No redis configuration found for name " + name);
                }

                var connection = RedisConnectionPool.Connect(cfg);
                
                this.redisSubscriper = connection.GetSubscriber();
            }, 0, 10);

            this.Subscribe();
        }

        private void Subscribe()
        {
            this.redisSubscriper.Subscribe(this.channelName, (channel, msg) =>
            {
                string messageStr = (string)msg;

                if (messageStr.StartsWith(this.identifier))
                {
                    // do not notify ourself (might be faster than the second method?
                    return;
                }

                var message = BackPlateMessage.Deserialize(messageStr);

                switch (message.Action)
                {
                    case BackPlateAction.Clear:
                        this.OnClear();
                        break;
                    case BackPlateAction.ClearRegion:
                        this.OnClearRegion(message.Region);
                        break;
                    case BackPlateAction.Changed:
                        if (string.IsNullOrWhiteSpace(message.Region))
                        {
                            this.OnChange(message.Key);
                        }
                        else
                        {
                            this.OnChange(message.Key, message.Region);
                        }
                        break;
                    case BackPlateAction.Removed:
                        if (string.IsNullOrWhiteSpace(message.Region))
                        {
                            this.OnRemove(message.Key);
                        }
                        else
                        {
                            this.OnRemove(message.Key, message.Region);
                        }
                        break;
                }

            }, StackRedis.CommandFlags.FireAndForget);
        }

        public override void NotifyClear()
        {
            var message = BackPlateMessage.ForClear(this.identifier).Serialize();
            this.Publish(message);
        }

        public override void NotifyClearRegion(string region)
        {
            var message = BackPlateMessage.ForClearRegion(this.identifier, region);
            this.Publish(message.Serialize());
        }

        public override void NotifyChange(string key)
        {
            var message = BackPlateMessage.ForChanged(this.identifier, key);
            this.Publish(message.Serialize());
        }

        public override void NotifyChange(string key, string region)
        {
            var message = BackPlateMessage.ForChanged(this.identifier, key, region);
            this.Publish(message.Serialize());
        }

        public override void NotifyRemove(string key)
        {
            var message = BackPlateMessage.ForRemoved(this.identifier, key);
            this.Publish(message.Serialize());
        }

        public override void NotifyRemove(string key, string region)
        {
            var message = BackPlateMessage.ForRemoved(this.identifier, key, region);
            this.Publish(message.Serialize());
        }

        private void Publish(string message)
        {
            this.redisSubscriper.Publish(this.channelName, message, StackRedis.CommandFlags.FireAndForget);
        }

        public override void Dispose(bool managed)
        {
            if (managed)
            {
                this.redisSubscriper.Unsubscribe(this.channelName);
            }

            base.Dispose(managed);
        }
    }
}