using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.Core.Logging;
using static CacheManager.Core.Utility.Guard;
using StackRedis = StackExchange.Redis;

namespace CacheManager.Redis
{
    /// <summary>
    /// Implementation of the cache back plate with Redis Pub/Sub feature.
    /// <para>
    /// Pub/Sub is used to send messages to the redis server on any Update, cache Clear, Region
    /// Clear or Remove operation. Every cache manager with the same configuration subscribes to the
    /// same chanel and can react on those messages to keep other cache handles in sync with the 'master'.
    /// </para>
    /// </summary>
    public sealed class RedisCacheBackPlate : CacheBackPlate
    {
        private readonly string channelName;
        private readonly string identifier;
        private readonly ILogger logger;
        private StackRedis.ISubscriber redisSubscriper;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCacheBackPlate"/> class.
        /// </summary>
        /// <param name="configuration">The cache manager configuration.</param>
        public RedisCacheBackPlate(CacheManagerConfiguration configuration)
            : base(configuration)
        {
            NotNull(configuration, nameof(configuration));

            this.logger = configuration.LoggerFactory.CreateLogger(this);
            this.channelName = configuration.BackPlateChannelName ?? "CacheManagerBackPlate";
            this.identifier = Guid.NewGuid().ToString();

            RetryHelper.Retry(
                () =>
                {
                    // throws an exception if not found for the name
                    var cfg = RedisConfigurations.GetConfiguration(this.ConfigurationKey);

                    var connection = RedisConnectionPool.Connect(cfg);

                    this.redisSubscriper = connection.GetSubscriber();
                },
                configuration.RetryTimeout,
                configuration.MaxRetries,
                this.logger);

            this.Subscribe();
        }

        /// <summary>
        /// Notifies other cache clients about a changed cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        public override void NotifyChange(string key)
        {
            this.PublishMessage(BackPlateMessage.ForChanged(this.identifier, key));
        }

        /// <summary>
        /// Notifies other cache clients about a changed cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        public override void NotifyChange(string key, string region)
        {
            this.PublishMessage(BackPlateMessage.ForChanged(this.identifier, key, region));
        }

        /// <summary>
        /// Notifies other cache clients about a cache clear.
        /// </summary>
        public override void NotifyClear()
        {
            this.PublishMessage(BackPlateMessage.ForClear(this.identifier));
        }

        /// <summary>
        /// Notifies other cache clients about a cache clear region call.
        /// </summary>
        /// <param name="region">The region.</param>
        public override void NotifyClearRegion(string region)
        {
            this.PublishMessage(BackPlateMessage.ForClearRegion(this.identifier, region));
        }

        /// <summary>
        /// Notifies other cache clients about a removed cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        public override void NotifyRemove(string key)
        {
            this.PublishMessage(BackPlateMessage.ForRemoved(this.identifier, key));
        }

        /// <summary>
        /// Notifies other cache clients about a removed cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        public override void NotifyRemove(string key, string region)
        {
            this.PublishMessage(BackPlateMessage.ForRemoved(this.identifier, key, region));
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="managed">
        /// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release
        /// only unmanaged resources.
        /// </param>
        protected override void Dispose(bool managed)
        {
            if (managed)
            {
                this.redisSubscriper.Unsubscribe(this.channelName);
            }

            base.Dispose(managed);
        }

        private void Publish(string message)
        {
            this.redisSubscriper.Publish(this.channelName, message, StackRedis.CommandFlags.FireAndForget);
        }

        //private Stack<string> messages = new Stack<string>();
        //private StringBuilder messages = null;
        //private long lastRun = 0L;
        private long lastLog = 0L;
        private long messagesCount = 0L;

        private void PublishMessage(BackPlateMessage message)
        {
            this.Publish(message.Serialize());

            if (this.logger.IsEnabled(LogLevel.Information))
            {
                const int logInterval = 1000;
                Interlocked.Increment(ref messagesCount);

                if(Environment.TickCount > lastLog + logInterval)
                {
                    lastLog = Environment.TickCount;
                    this.logger.LogInfo("Backplate Received {0} int the past {1}sec.", messagesCount, logInterval / 1000);
                    Interlocked.Exchange(ref messagesCount, 0);
                }
            }

            //if (Environment.TickCount > lastRun + 0 && messages != null)
            //{
            //    lock (messages)
            //    {
            //        if (Environment.TickCount > lastRun + 0 && messages != null)
            //        {
            //            var msgs = messages.ToString();
            //            //this.logger.LogInfo("Backplate sending {0} messages.", msgs.Split(',').Length);
            //            this.Publish(msgs);
            //            lastRun = Environment.TickCount;
            //            messages = null;                        
            //        }
            //    }
            //}
            //var msg = message.Serialize();

            //if (messages == null)
            //{
            //    messages = new StringBuilder(msg);
            //}
            //else
            //{
            //    messages.Append(",");
            //    messages.Append(msg);
            //    ////messages += "," + msg;
            //}
        }

        private void Subscribe()
        {
            this.redisSubscriper.Subscribe(
                this.channelName,
                (channel, msg) =>
                {
                    var fullMessageStr = ((string)msg).Split(',');
                    //this.logger.LogInfo("Backplate got notified with {0} new messages.", fullMessageStr.Length);

                    foreach (var messageStr in fullMessageStr)
                    {
                        if (messageStr.StartsWith(this.identifier, StringComparison.Ordinal))
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
                    }
                },
                StackRedis.CommandFlags.FireAndForget);
        }
    }
}