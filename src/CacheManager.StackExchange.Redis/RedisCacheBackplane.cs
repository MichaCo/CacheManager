using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.Core.Logging;
using StackExchange.Redis;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Redis
{
    /// <summary>
    /// Implementation of the cache backplane using a Redis Pub/Sub channel.
    /// <para>
    /// Redis Pub/Sub is used to send messages to the redis server on any key change, cache clear, region
    /// clear or key remove operation.
    /// Every cache manager with the same configuration subscribes to the
    /// same channel and can react on those messages to keep other cache handles in sync with the 'master'.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The cache manager must have at least one cache handle configured with <see cref="CacheHandleConfiguration.IsBackplaneSource"/> set to <c>true</c>.
    /// Usually this is the redis cache handle, if configured. It should be the distributed and bottom most cache handle.
    /// </remarks>
    public sealed class RedisCacheBackplane : CacheBackplane
    {
        private readonly string _channelName;
        private readonly string _identifier;
        private readonly ILogger _logger;
        private readonly RedisConnectionManager _connection;
        private HashSet<string> _messages = new HashSet<string>();
        private object _messageLock = new object();
        private int _skippedMessages = 0;
        private bool _sending = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCacheBackplane"/> class.
        /// </summary>
        /// <param name="configuration">The cache manager configuration.</param>
        /// <param name="loggerFactory">The logger factory</param>
        public RedisCacheBackplane(ICacheManagerConfiguration configuration, ILoggerFactory loggerFactory)
            : base(configuration)
        {
            NotNull(configuration, nameof(configuration));
            NotNull(loggerFactory, nameof(loggerFactory));

            _logger = loggerFactory.CreateLogger(this);
            _channelName = configuration.BackplaneChannelName ?? "CacheManagerBackplane";
            _identifier = Guid.NewGuid().ToString();

            var cfg = RedisConfigurations.GetConfiguration(ConfigurationKey);
            _connection = new RedisConnectionManager(
                cfg,
                loggerFactory);

            RetryHelper.Retry(() => Subscribe(), configuration.RetryTimeout, configuration.MaxRetries, _logger);
        }

        /// <summary>
        /// Notifies other cache clients about a changed cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="action">The cache action.</param>
        public override void NotifyChange(string key, CacheItemChangedEventAction action)
        {
            PublishMessage(BackplaneMessage.ForChanged(_identifier, key, action));
        }

        /// <summary>
        /// Notifies other cache clients about a changed cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        /// <param name="action">The cache action.</param>
        public override void NotifyChange(string key, string region, CacheItemChangedEventAction action)
        {
            PublishMessage(BackplaneMessage.ForChanged(_identifier, key, region, action));
        }

        /// <summary>
        /// Notifies other cache clients about a cache clear.
        /// </summary>
        public override void NotifyClear()
        {
            PublishMessage(BackplaneMessage.ForClear(_identifier));
        }

        /// <summary>
        /// Notifies other cache clients about a cache clear region call.
        /// </summary>
        /// <param name="region">The region.</param>
        public override void NotifyClearRegion(string region)
        {
            PublishMessage(BackplaneMessage.ForClearRegion(_identifier, region));
        }

        /// <summary>
        /// Notifies other cache clients about a removed cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        public override void NotifyRemove(string key)
        {
            PublishMessage(BackplaneMessage.ForRemoved(_identifier, key));
        }

        /// <summary>
        /// Notifies other cache clients about a removed cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        public override void NotifyRemove(string key, string region)
        {
            PublishMessage(BackplaneMessage.ForRemoved(_identifier, key, region));
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
                try
                {
                    _connection.Subscriber.Unsubscribe(_channelName);
                }
                catch
                {
                }
            }

            base.Dispose(managed);
        }

        private void Publish(string message)
        {
            _connection.Subscriber.Publish(_channelName, message, CommandFlags.HighPriority);
        }

        private void PublishMessage(BackplaneMessage message)
        {
            var msg = message.Serialize();

            lock (_messageLock)
            {
                if (message.Action == BackplaneAction.Clear)
                {
                    Interlocked.Exchange(ref _skippedMessages, _messages.Count);
                    _messages.Clear();
                }

                if (!_messages.Add(msg))
                {
                    Interlocked.Increment(ref _skippedMessages);
                    if (_logger.IsEnabled(LogLevel.Trace))
                    {
                        _logger.LogTrace("Skipped duplicate message: {0}.", msg);
                    }
                }

                SendMessages();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "No other way")]
        private void SendMessages()
        {
            if (_sending || _messages == null || _messages.Count == 0)
            {
                return;
            }

            Task.Factory.StartNew(
                async (obj) =>
                {
                    if (_sending || _messages == null || _messages.Count == 0)
                    {
                        return;
                    }

                    _sending = true;
#if !NET40
                    await Task.Delay(10).ConfigureAwait(false);
#endif
                    var msgs = string.Empty;
                    lock (_messageLock)
                    {
                        if (_messages != null && _messages.Count > 0)
                        {
                            msgs = string.Join(",", _messages);

                            if (_logger.IsEnabled(LogLevel.Debug))
                            {
                                _logger.LogDebug("Backplane is sending {0} messages ({1} skipped).", _messages.Count, _skippedMessages);
                            }

                            Interlocked.Add(ref MessagesSent, _messages.Count);
                            _skippedMessages = 0;
                            _messages.Clear();
                        }

                        try
                        {
                            if (msgs.Length > 0)
                            {
                                Publish(msgs);
                                Interlocked.Increment(ref SentChunks);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error occurred sending backplane messages.");
                        }

                        _sending = false;
                    }
#if NET40
                },
                this,
                CancellationToken.None,
                TaskCreationOptions.None,
                TaskScheduler.Default)
                .ConfigureAwait(false);
#else
                },
                this,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default)
                .ConfigureAwait(false);
#endif
        }

        private void Subscribe()
        {
            _connection.Subscriber.Unsubscribe(_channelName);
            _connection.Subscriber.Subscribe(
                _channelName,
                (channel, msg) =>
                {
                    var fullMessage = ((string)msg).Split(',')
                        .Where(s => !s.StartsWith(_identifier, StringComparison.Ordinal))
                        .ToArray();

                    if (fullMessage.Length == 0)
                    {
                        // no messages for this instance
                        return;
                    }

                    Interlocked.Add(ref MessagesReceived, fullMessage.Length);

                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInfo("Backplane got notified with {0} new messages.", fullMessage.Length);
                    }

                    foreach (var messageStr in fullMessage)
                    {
                        var message = BackplaneMessage.Deserialize(messageStr);

                        switch (message.Action)
                        {
                            case BackplaneAction.Clear:
                                TriggerCleared();
                                break;

                            case BackplaneAction.ClearRegion:
                                TriggerClearedRegion(message.Region);
                                break;

                            case BackplaneAction.Changed:
                                if (string.IsNullOrWhiteSpace(message.Region))
                                {
                                    TriggerChanged(message.Key, message.ChangeAction);
                                }
                                else
                                {
                                    TriggerChanged(message.Key, message.Region, message.ChangeAction);
                                }
                                break;

                            case BackplaneAction.Removed:
                                if (string.IsNullOrWhiteSpace(message.Region))
                                {
                                    TriggerRemoved(message.Key);
                                }
                                else
                                {
                                    TriggerRemoved(message.Key, message.Region);
                                }
                                break;
                        }
                    }
                },
                CommandFlags.FireAndForget);
        }
    }
}