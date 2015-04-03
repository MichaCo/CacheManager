using System;
using System.Globalization;
using System.Linq;
using CacheManager.Core.Cache;
using CacheManager.Core.Configuration;
using StackRedis = StackExchange.Redis;

namespace CacheManager.Redis
{
    /// <summary>
    /// Implementation of the cache back plate with Redis Pub/Sub feature.
    /// <para>
    /// Pub/Sub is used to send messages to the redis server on any Update, Cache Clear, Region Clear or Remove operation.
    /// Every cache manager with the same configuration subscribes to the same chanel and can react on those messages to keep 
    /// other cache handles in sync with the 'master'.
    /// </para>
    /// </summary>
    public sealed class RedisCacheBackPlate : CacheBackPlate
    {
        private readonly string channelName;
        private readonly string identifier;
        private StackRedis.ISubscriber redisSubscriper;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCacheBackPlate"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="configuration">The configuration.</param>
        public RedisCacheBackPlate(string name, ICacheManagerConfiguration configuration)
            : base(name, configuration)
        {
            this.channelName = string.Format(
                CultureInfo.InvariantCulture,
                "CacheManagerBackPlate_{0}",
                configuration.Name);

            this.identifier = Guid.NewGuid().ToString();

            RetryHelper.Retry(
                () =>
                {
                    // throws an exception if not found for the name
                    var cfg = RedisConfigurations.GetConfiguration(name);

                    var connection = RedisConnectionPool.Connect(cfg);

                    this.redisSubscriper = connection.GetSubscriber();
                }, 
                0, 
                10);

            this.Subscribe();
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="managed"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release
        /// only unmanaged resources.</param>
        public override void Dispose(bool managed)
        {
            if (managed)
            {
                this.redisSubscriper.Unsubscribe(this.channelName);
            }

            base.Dispose(managed);
        }

        /// <summary>
        /// Notifies other cache clients about a changed cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        public override void NotifyChange(string key)
        {
            var message = BackPlateMessage.ForChanged(this.identifier, key);
            this.Publish(message.Serialize());
        }

        /// <summary>
        /// Notifies other cache clients about a changed cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        public override void NotifyChange(string key, string region)
        {
            var message = BackPlateMessage.ForChanged(this.identifier, key, region);
            this.Publish(message.Serialize());
        }

        /// <summary>
        /// Notifies other cache clients about a cache clear.
        /// </summary>
        public override void NotifyClear()
        {
            var message = BackPlateMessage.ForClear(this.identifier).Serialize();
            this.Publish(message);
        }

        /// <summary>
        /// Notifies other cache clients about a cache clear region call.
        /// </summary>
        /// <param name="region">The region.</param>
        public override void NotifyClearRegion(string region)
        {
            var message = BackPlateMessage.ForClearRegion(this.identifier, region);
            this.Publish(message.Serialize());
        }

        /// <summary>
        /// Notifies other cache clients about a removed cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        public override void NotifyRemove(string key)
        {
            var message = BackPlateMessage.ForRemoved(this.identifier, key);
            this.Publish(message.Serialize());
        }

        /// <summary>
        /// Notifies other cache clients about a removed cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        public override void NotifyRemove(string key, string region)
        {
            var message = BackPlateMessage.ForRemoved(this.identifier, key, region);
            this.Publish(message.Serialize());
        }

        private void Publish(string message)
        {
            this.redisSubscriper.Publish(this.channelName, message, StackRedis.CommandFlags.FireAndForget);
        }

        private void Subscribe()
        {
            this.redisSubscriper.Subscribe(
                this.channelName, 
                (channel, msg) =>
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
                }, 
                StackRedis.CommandFlags.FireAndForget);
        }
    }
}