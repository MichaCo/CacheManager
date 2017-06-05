using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.Core.Logging;
using StackExchange.Redis;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Redis
{
    /// <summary>
    /// Cache handle implementation for Redis.
    /// </summary>
    /// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
    [RequiresSerializer]
    public class RedisCacheHandle<TCacheValue> : BaseCacheHandle<TCacheValue>
    {
        private static readonly TimeSpan MinimumExpirationTimeout = TimeSpan.FromMilliseconds(1);
        private const string Base64Prefix = "base64\0";
        private const string HashFieldCreated = "created";
        private const string HashFieldExpirationMode = "expiration";
        private const string HashFieldExpirationTimeout = "timeout";
        private const string HashFieldType = "type";
        private const string HashFieldValue = "value";
        private const string HashFieldVersion = "version";
        private const string HashFieldUsesDefaultExp = "defaultExpiration";

        private static readonly string _scriptAdd = $@"
if redis.call('HSETNX', KEYS[1], '{HashFieldValue}', ARGV[1]) == 1 then
    local result=redis.call('HMSET', KEYS[1], '{HashFieldType}', ARGV[2], '{HashFieldExpirationMode}', ARGV[3], '{HashFieldExpirationTimeout}', ARGV[4], '{HashFieldCreated}', ARGV[5], '{HashFieldVersion}', 1, '{HashFieldUsesDefaultExp}', ARGV[6])
    if ARGV[3] > '1' and ARGV[4] ~= '0' then
        redis.call('PEXPIRE', KEYS[1], ARGV[4])
    else
        redis.call('PERSIST', KEYS[1])
    end
    return result
else
    return nil
end";

        private static readonly string _scriptPut = $@"
local result=redis.call('HMSET', KEYS[1], '{HashFieldValue}', ARGV[1], '{HashFieldType}', ARGV[2], '{HashFieldExpirationMode}', ARGV[3], '{HashFieldExpirationTimeout}', ARGV[4], '{HashFieldCreated}', ARGV[5], '{HashFieldUsesDefaultExp}', ARGV[6])
redis.call('HINCRBY', KEYS[1], '{HashFieldVersion}', 1)
if ARGV[3] > '1' and ARGV[4] ~= '0' then
    redis.call('PEXPIRE', KEYS[1], ARGV[4])
else
    redis.call('PERSIST', KEYS[1])
end
return result";

        // script should also update expire now. If sliding, update the sliding window
        private static readonly string _scriptUpdate = $@"
if redis.call('HGET', KEYS[1], '{HashFieldVersion}') == ARGV[2] then
    local result=redis.call('HSET', KEYS[1], '{HashFieldValue}', ARGV[1])
    redis.call('HINCRBY', KEYS[1], '{HashFieldVersion}', 1)
    if ARGV[3] == '2' and ARGV[4] ~= '0' then
        redis.call('PEXPIRE', KEYS[1], ARGV[4])
    end
    return result;
else
    return nil
end";

        private static readonly string _scriptGet = $@"
local result = redis.call('HMGET', KEYS[1], '{HashFieldValue}', '{HashFieldExpirationMode}', '{HashFieldExpirationTimeout}', '{HashFieldCreated}', '{HashFieldType}', '{HashFieldVersion}', '{HashFieldUsesDefaultExp}')
if (result[2] and result[2] == '2') then
    if (result[3] and result[3] ~= '' and result[3] ~= '0') then
        redis.call('PEXPIRE', KEYS[1], result[3])
    end
end
return result";

        private readonly IDictionary<ScriptType, LoadedLuaScript> _shaScripts = new Dictionary<ScriptType, LoadedLuaScript>();
        private readonly IDictionary<ScriptType, LuaScript> _luaScripts = new Dictionary<ScriptType, LuaScript>();
        private readonly ICacheManagerConfiguration _managerConfiguration;
        private readonly RedisValueConverter _valueConverter;
        private readonly RedisConnectionManager _connection;
        private bool _isLuaAllowed;
        private bool _canPreloadScripts = true;
        private RedisConfiguration _redisConfiguration = null;

        // flag if scripts are initially loaded to the server
        private bool _scriptsLoaded = false;

        private object _lockObject = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCacheHandle{TCacheValue}"/> class.
        /// </summary>
        /// <param name="managerConfiguration">The manager configuration.</param>
        /// <param name="configuration">The cache handle configuration.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="serializer">The serializer.</param>
        public RedisCacheHandle(ICacheManagerConfiguration managerConfiguration, CacheHandleConfiguration configuration, ILoggerFactory loggerFactory, ICacheSerializer serializer)
            : base(managerConfiguration, configuration)
        {
            NotNull(loggerFactory, nameof(loggerFactory));
            NotNull(managerConfiguration, nameof(managerConfiguration));
            NotNull(configuration, nameof(configuration));
            EnsureNotNull(serializer, "A serializer is required for the redis cache handle");

            Logger = loggerFactory.CreateLogger(this);
            _managerConfiguration = managerConfiguration;
            _valueConverter = new RedisValueConverter(serializer);
            _redisConfiguration = RedisConfigurations.GetConfiguration(configuration.Key);
            _connection = new RedisConnectionManager(_redisConfiguration, loggerFactory);
            _isLuaAllowed = _connection.Features.Scripting;

            // disable preloading right away if twemproxy mode, as this is not supported.
            _canPreloadScripts = _redisConfiguration.TwemproxyEnabled ? false : true;

            if (_redisConfiguration.KeyspaceNotificationsEnabled)
            {
                // notify-keyspace-events needs to be set to "Exe" at least! Otherwise we will not receive any events.
                // this must be configured per server and should probably not be done automagically as this needs admin rights!
                // Let's try to check at least if those settings are configured (the check also works only if useAdmin is set to true though).
                try
                {
                    var configurations = _connection.GetConfiguration("notify-keyspace-events");
                    foreach (var cfg in configurations)
                    {
                        if (!cfg.Value.Contains("E"))
                        {
                            Logger.LogWarn("Server {0} is missing configuration value 'E' in notify-keyspace-events to enable keyevents.", cfg.Key);
                        }

                        if (!(cfg.Value.Contains("A") ||
                            (cfg.Value.Contains("x") && cfg.Value.Contains("e"))))
                        {
                            Logger.LogWarn("Server {0} is missing configuration value 'A' or 'x' and 'e' in notify-keyspace-events to enable keyevents for expired and evicted keys.", cfg.Key);
                        }
                    }
                }
                catch
                {
                    Logger.LogDebug("Could not read configuration from redis to validate notify-keyspace-events. Most likely useAdmin is not set to true.");
                }

                SubscribeKeyspaceNotifications();
            }
        }

        /// <inheritdoc />
        public override bool IsDistributedCache
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the number of items the cache handle currently maintains.
        /// </summary>
        /// <value>The count.</value>
        /// <exception cref="System.InvalidOperationException">No active master found.</exception>
        public override int Count
        {
            get
            {
                if (_redisConfiguration.TwemproxyEnabled)
                {
                    Logger.LogWarn("'Count' cannot be calculated. Twemproxy mode is enabled which does not support accessing the servers collection.");
                    return 0;
                }

                var count = 0;
                foreach (var server in Servers.Where(p => !p.IsSlave && p.IsConnected))
                {
                    count += (int)server.DatabaseSize(_redisConfiguration.Database);
                }

                // approx size, only size on the master..
                return count;
            }
        }

#pragma warning disable CS3003 // Type is not CLS-compliant

        /// <summary>
        /// Gets the servers.
        /// </summary>
        /// <value>The list of servers.</value>
        public IEnumerable<IServer> Servers => _connection.Servers;

        /// <summary>
        /// Gets the features the redis server supports.
        /// </summary>
        /// <value>The server features.</value>
        public RedisFeatures Features => _connection.Features;

#pragma warning restore CS3003 // Type is not CLS-compliant

        /// <summary>
        /// Gets a value indicating whether we can use the lua implementation instead of manual.
        /// This flag will be set automatically via feature detection based on the Redis server version
        /// or via <see cref="RedisConfiguration.StrictCompatibilityModeVersion"/> if set to a version which does not support lua scripting.
        /// </summary>
        public bool IsLuaAllowed => _isLuaAllowed;

        /// <inheritdoc />
        protected override ILogger Logger { get; }

        /// <summary>
        /// Clears this cache, removing all items in the base cache and all regions.
        /// </summary>
        public override void Clear()
        {
            try
            {
                foreach (var server in Servers.Where(p => !p.IsSlave))
                {
                    Retry(() =>
                    {
                        if (server.IsConnected)
                        {
                            server.FlushDatabase(_redisConfiguration.Database);
                        }
                    });
                }
            }
            catch (NotSupportedException ex)
            {
                throw new NotSupportedException($"Clear is not available because '{ex.Message}'", ex);
            }
        }

        /// <summary>
        /// Clears the cache region, removing all items from the specified <paramref name="region"/> only.
        /// </summary>
        /// <param name="region">The cache region.</param>
        public override void ClearRegion(string region)
        {
            Retry(() =>
            {
                // we are storing all keys stored in the region in the hash for key=region
                var hashKeys = _connection.Database.HashKeys(region);

                if (hashKeys.Length > 0)
                {
                    // lets remove all keys which where in the region
                    // 01/32/16 changed to remove one by one because on clusters the keys could belong to multiple slots
                    foreach (var key in hashKeys.Where(p => p.HasValue))
                    {
                        _connection.Database.KeyDelete(key.ToString(), CommandFlags.FireAndForget);
                    }
                }

                // now delete the region
                _connection.Database.KeyDelete(region);
            });
        }

        /// <inheritdoc />
        public override bool Exists(string key)
        {
            var fullKey = GetKey(key);
            return Retry(() => _connection.Database.KeyExists(fullKey));
        }

        /// <inheritdoc />
        public override bool Exists(string key, string region)
        {
            NotNullOrWhiteSpace(region, nameof(region));

            var fullKey = GetKey(key, region);
            return Retry(() => _connection.Database.KeyExists(fullKey));
        }

        /// <inheritdoc />
        public override UpdateItemResult<TCacheValue> Update(string key, Func<TCacheValue, TCacheValue> updateValue, int maxRetries)
            => Update(key, null, updateValue, maxRetries);

        /// <inheritdoc />
        public override UpdateItemResult<TCacheValue> Update(string key, string region, Func<TCacheValue, TCacheValue> updateValue, int maxRetries)
        {
            if (!_isLuaAllowed)
            {
                return UpdateNoScript(key, region, updateValue, maxRetries);
            }

            var tries = 0;
            var fullKey = GetKey(key, region);

            return Retry(() =>
            {
                do
                {
                    tries++;

                    var item = GetCacheItemAndVersion(key, region, out int version);

                    if (item == null)
                    {
                        return UpdateItemResult.ForItemDidNotExist<TCacheValue>();
                    }

                    ValidateExpirationTimeout(item);

                    // run update
                    var newValue = updateValue(item.Value);

                    // added null check, throw explicit to me more consistent. Otherwise it would throw within the script exec
                    if (newValue == null)
                    {
                        return UpdateItemResult.ForFactoryReturnedNull<TCacheValue>();
                    }

                    // resetting TTL on update, too
                    var result = Eval(ScriptType.Update, fullKey, new[]
                    {
                        ToRedisValue(newValue),
                        version,
                        (int)item.ExpirationMode,
                        (long)item.ExpirationTimeout.TotalMilliseconds,
                    });

                    if (result != null && !result.IsNull)
                    {
                        // optimizing not retrieving the item again after update (could have changed already, too)
                        var newItem = item.WithValue(newValue);
                        newItem.LastAccessedUtc = DateTime.UtcNow;

                        return UpdateItemResult.ForSuccess(newItem, tries > 1, tries);
                    }

                    Logger.LogDebug("Update of {0} {1} failed with version conflict, retrying {2}/{3}", key, region, tries, maxRetries);
                }
                while (tries <= maxRetries);

                return UpdateItemResult.ForTooManyRetries<TCacheValue>(tries);
            });
        }

#pragma warning disable SA1600
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        protected UpdateItemResult<TCacheValue> UpdateNoScript(string key, string region, Func<TCacheValue, TCacheValue> updateValue, int maxRetries)
        {
            var committed = false;
            var tries = 0;
            var fullKey = GetKey(key, region);

            return Retry(() =>
            {
                do
                {
                    tries++;

                    var item = GetCacheItemInternal(key, region);

                    if (item == null)
                    {
                        return UpdateItemResult.ForItemDidNotExist<TCacheValue>();
                    }

                    ValidateExpirationTimeout(item);

                    var oldValue = ToRedisValue(item.Value);

                    var tran = _connection.Database.CreateTransaction();
                    tran.AddCondition(Condition.HashEqual(fullKey, HashFieldValue, oldValue));

                    // run update
                    var newValue = updateValue(item.Value);

                    // added null check, throw explicit to me more consistent. Otherwise it would throw later
                    if (newValue == null)
                    {
                        return UpdateItemResult.ForFactoryReturnedNull<TCacheValue>();
                    }

                    tran.HashSetAsync(fullKey, HashFieldValue, ToRedisValue(newValue));

                    committed = tran.Execute();

                    if (committed)
                    {
                        var newItem = item.WithValue(newValue);
                        newItem.LastAccessedUtc = DateTime.UtcNow;

                        if (newItem.ExpirationMode == ExpirationMode.Sliding && newItem.ExpirationTimeout != TimeSpan.Zero)
                        {
                            _connection.Database.KeyExpire(fullKey, newItem.ExpirationTimeout, CommandFlags.FireAndForget);
                        }

                        return UpdateItemResult.ForSuccess(newItem, tries > 1, tries);
                    }

                    Logger.LogDebug("Update of {0} {1} failed with version conflict, retrying {2}/{3}", key, region, tries, maxRetries);
                }
                while (committed == false && tries <= maxRetries);

                return UpdateItemResult.ForTooManyRetries<TCacheValue>(tries);
            });
        }

#pragma warning restore CS1591
#pragma warning restore SA1600

        /// <summary>
        /// Adds a value to the cache.
        /// <para>
        /// Add call is synced, so might be slower than put which is fire and forget but we want to
        /// return true|false if the operation was successfully or not. And always returning true
        /// could be misleading if the item already exists
        /// </para>
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
        /// </returns>
        protected override bool AddInternalPrepared(CacheItem<TCacheValue> item) =>
            Retry(() => Set(item, When.NotExists, true));

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        /// <param name="disposeManaged">Indicator if managed resources should be released.</param>
        protected override void Dispose(bool disposeManaged)
        {
            base.Dispose(disposeManaged);
            if (disposeManaged)
            {
                // this.connection.RemoveConnection();
            }
        }

        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key)
            => GetCacheItemInternal(key, null);

        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key, string region)
        {
            return GetCacheItemAndVersion(key, region, out int version);
        }

        private CacheItem<TCacheValue> GetCacheItemAndVersion(string key, string region, out int version)
        {
            version = -1;
            if (!_isLuaAllowed)
            {
                return GetCacheItemInternalNoScript(key, region);
            }

            var fullKey = GetKey(key, region);

            var result = Retry(() => Eval(ScriptType.Get, fullKey));
            if (result == null || result.IsNull)
            {
                // something went wrong. HMGET should return at least a null result for each requested field
                throw new InvalidOperationException("Error retrieving " + fullKey);
            }

            var values = (RedisValue[])result;

            // the first item stores the value
            var item = values[0];
            var expirationModeItem = values[1];
            var timeoutItem = values[2];
            var createdItem = values[3];
            var valueTypeItem = values[4];
            version = (int)values[5];
            var usesDefaultExpiration = values[6].HasValue ? (bool)values[6] : true;

            if (!item.HasValue || !valueTypeItem.HasValue /* partially removed? */
                || item.IsNullOrEmpty || item.IsNull)
            {
                return null;
            }

            var expirationMode = ExpirationMode.None;
            var expirationTimeout = default(TimeSpan);

            // checking if the expiration mode is set on the hash
            if (expirationModeItem.HasValue && timeoutItem.HasValue)
            {
                if (!timeoutItem.IsNullOrEmpty && !expirationModeItem.IsNullOrEmpty)
                {
                    expirationMode = (ExpirationMode)(int)expirationModeItem;
                    expirationTimeout = TimeSpan.FromMilliseconds((long)timeoutItem);
                }
                else
                {
                    Logger.LogWarn("Expiration mode and timeout are set but are not valid '{0}', '{1}'.", expirationModeItem, timeoutItem);
                }
            }

            var value = FromRedisValue(item, (string)valueTypeItem);

            var cacheItem =
                usesDefaultExpiration ?
                string.IsNullOrWhiteSpace(region) ?
                    new CacheItem<TCacheValue>(key, value) :
                    new CacheItem<TCacheValue>(key, region, value) :
                string.IsNullOrWhiteSpace(region) ?
                    new CacheItem<TCacheValue>(key, value, expirationMode, expirationTimeout) :
                    new CacheItem<TCacheValue>(key, region, value, expirationMode, expirationTimeout);

            if (createdItem.HasValue)
            {
                cacheItem = cacheItem.WithCreated(new DateTime((long)createdItem, DateTimeKind.Utc));
            }

            if (cacheItem.IsExpired)
            {
                TriggerCacheSpecificRemove(key, region, CacheItemRemovedReason.Expired, cacheItem.Value);

                return null;
            }

            return cacheItem;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1600

        protected CacheItem<TCacheValue> GetCacheItemInternalNoScript(string key, string region)
        {
            return Retry(() =>
            {
                var fullKey = GetKey(key, region);

                // getting both, the value and, if exists, the expiration mode. if that one is set
                // and it is sliding, we also retrieve the timeout later
                var values = _connection.Database.HashGet(
                    fullKey,
                    new RedisValue[]
                    {
                        HashFieldValue,
                        HashFieldExpirationMode,
                        HashFieldExpirationTimeout,
                        HashFieldCreated,
                        HashFieldType,
                        HashFieldUsesDefaultExp
                    });

                // the first item stores the value
                var item = values[0];
                var expirationModeItem = values[1];
                var timeoutItem = values[2];
                var createdItem = values[3];
                var valueTypeItem = values[4];
                var usesDefaultExpiration = values[5].HasValue ? (bool)values[5] : true;

                if (!item.HasValue || !valueTypeItem.HasValue /* partially removed? */
                    || item.IsNullOrEmpty || item.IsNull)
                {
                    return null;
                }

                var expirationMode = ExpirationMode.None;
                var expirationTimeout = default(TimeSpan);

                // checking if the expiration mode is set on the hash
                if (expirationModeItem.HasValue && timeoutItem.HasValue)
                {
                    // adding sanity check for empty string results. Could happen in rare cases like #74
                    if (!timeoutItem.IsNullOrEmpty && !expirationModeItem.IsNullOrEmpty)
                    {
                        expirationMode = (ExpirationMode)(int)expirationModeItem;
                        expirationTimeout = TimeSpan.FromMilliseconds((long)timeoutItem);
                    }
                    else
                    {
                        Logger.LogWarn("Expiration mode and timeout are set but are not valid '{0}', '{1}'.", expirationModeItem, timeoutItem);
                    }
                }

                var value = FromRedisValue(item, (string)valueTypeItem);

                var cacheItem =
                    usesDefaultExpiration ?
                    string.IsNullOrWhiteSpace(region) ?
                        new CacheItem<TCacheValue>(key, value) :
                        new CacheItem<TCacheValue>(key, region, value) :
                    string.IsNullOrWhiteSpace(region) ?
                        new CacheItem<TCacheValue>(key, value, expirationMode, expirationTimeout) :
                        new CacheItem<TCacheValue>(key, region, value, expirationMode, expirationTimeout);

                if (createdItem.HasValue)
                {
                    cacheItem = cacheItem.WithCreated(new DateTime((long)createdItem, DateTimeKind.Utc));
                }

                if (cacheItem.IsExpired)
                {
                    TriggerCacheSpecificRemove(key, region, CacheItemRemovedReason.Expired, cacheItem.Value);

                    return null;
                }

                // update sliding
                if (expirationMode == ExpirationMode.Sliding && expirationTimeout != default(TimeSpan))
                {
                    _connection.Database.KeyExpire(fullKey, cacheItem.ExpirationTimeout, CommandFlags.FireAndForget);
                }

                return cacheItem;
            });
        }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore SA1600

        /// <summary>
        /// Puts the <paramref name="item"/> into the cache. If the item exists it will get updated
        /// with the new value. If the item doesn't exist, the item will be added to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        protected override void PutInternal(CacheItem<TCacheValue> item)
            => base.PutInternal(item);

        /// <summary>
        /// Puts the <paramref name="item"/> into the cache. If the item exists it will get updated
        /// with the new value. If the item doesn't exist, the item will be added to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        protected override void PutInternalPrepared(CacheItem<TCacheValue> item) =>
            Retry(() => Set(item, When.Always, false));

        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        protected override bool RemoveInternal(string key) => RemoveInternal(key, null);

#pragma warning disable CSE0003

        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        protected override bool RemoveInternal(string key, string region)
        {
            return Retry(() =>
            {
                var fullKey = GetKey(key, region);

                // clean up region
                if (!string.IsNullOrWhiteSpace(region))
                {
                    _connection.Database.HashDelete(region, fullKey, CommandFlags.FireAndForget);
                }

                // remove key
                var result = _connection.Database.KeyDelete(fullKey);

                return result;
            });
        }

        private void SubscribeKeyspaceNotifications()
        {
            _connection.Subscriber.Subscribe(
                 $"__keyevent@{_redisConfiguration.Database}__:expired",
                 (channel, key) =>
                 {
                     var tupple = ParseKey(key);
                     if (Logger.IsEnabled(LogLevel.Debug))
                     {
                         Logger.LogDebug("Got expired event for key '{0}:{1}'", tupple.Item2, tupple.Item1);
                     }

                     // we cannot return the original value here because we don't have it
                     TriggerCacheSpecificRemove(tupple.Item1, tupple.Item2, CacheItemRemovedReason.Expired, null);
                 });

            _connection.Subscriber.Subscribe(
                $"__keyevent@{_redisConfiguration.Database}__:evicted",
                (channel, key) =>
                {
                    var tupple = ParseKey(key);
                    if (Logger.IsEnabled(LogLevel.Debug))
                    {
                        Logger.LogDebug("Got evicted event for key '{0}:{1}'", tupple.Item2, tupple.Item1);
                    }

                    // we cannot return the original value here because we don't have it
                    TriggerCacheSpecificRemove(tupple.Item1, tupple.Item2, CacheItemRemovedReason.Evicted, null);
                });

            _connection.Subscriber.Subscribe(
                $"__keyevent@{_redisConfiguration.Database}__:del",
                (channel, key) =>
                {
                    var tupple = ParseKey(key);
                    if (Logger.IsEnabled(LogLevel.Debug))
                    {
                        Logger.LogDebug("Got del event for key '{0}:{1}'", tupple.Item2, tupple.Item1);
                    }

                    // we cannot return the original value here because we don't have it
                    TriggerCacheSpecificRemove(tupple.Item1, tupple.Item2, CacheItemRemovedReason.ExternalDelete, null);
                });
        }

#pragma warning restore CSE0003

        private static Tuple<string, string> ParseKey(string value)
        {
            if (value == null)
            {
                return Tuple.Create<string, string>(null, null);
            }

            var sepIndex = value.IndexOf(':');
            var hasRegion = sepIndex > 0;
            var key = value;
            string region = null;

            if (hasRegion)
            {
                region = value.Substring(0, sepIndex);
                key = value.Substring(sepIndex + 1);

                if (region.StartsWith(Base64Prefix))
                {
                    region = region.Substring(Base64Prefix.Length);
                    region = Encoding.UTF8.GetString(Convert.FromBase64String(region));
                }
            }

            if (key.StartsWith(Base64Prefix))
            {
                key = key.Substring(Base64Prefix.Length);
                key = Encoding.UTF8.GetString(Convert.FromBase64String(key));
            }

            return Tuple.Create(key, region);
        }

        private static void ValidateExpirationTimeout(CacheItem<TCacheValue> item)
        {
            if ((item.ExpirationMode == ExpirationMode.Absolute || item.ExpirationMode == ExpirationMode.Sliding) && item.ExpirationTimeout < MinimumExpirationTimeout)
            {
                throw new ArgumentException("Timeout lower than one millisecond is not supported.", nameof(item.ExpirationTimeout));
            }
        }

        private string GetKey(string key, string region = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            // for notifications, we have to get key and region back from the key stored in redis.
            // in case the key and or region itself contains the separator, there would be no way to do so...
            // So, only if that feature is enabled, we'll encode the key and/or region in that case
            // and the ParseKey method will respect that, too, and decodes the key and/or region.
            if (_redisConfiguration.KeyspaceNotificationsEnabled && key.Contains(":"))
            {
                key = Base64Prefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(key));
            }

            var fullKey = key;

            if (!string.IsNullOrWhiteSpace(region))
            {
                if (_redisConfiguration.KeyspaceNotificationsEnabled && region.Contains(":"))
                {
                    region = Base64Prefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(region));
                }

                fullKey = string.Concat(region, ":", key);
            }

            return fullKey;
        }

        private TCacheValue FromRedisValue(RedisValue value, string valueType)
        {
            if (value.IsNull || value.IsNullOrEmpty || !value.HasValue)
            {
                return default(TCacheValue);
            }

            if (_valueConverter is IRedisValueConverter<TCacheValue> typedConverter)
            {
                return typedConverter.FromRedisValue(value, valueType);
            }

            return _valueConverter.FromRedisValue<TCacheValue>(value, valueType);
        }

        private RedisValue ToRedisValue(TCacheValue value)
        {
            if (_valueConverter is IRedisValueConverter<TCacheValue> typedConverter)
            {
                return typedConverter.ToRedisValue(value);
            }

            return _valueConverter.ToRedisValue(value);
        }

        private T Retry<T>(Func<T> retryme) =>
            RetryHelper.Retry(retryme, _managerConfiguration.RetryTimeout, _managerConfiguration.MaxRetries, Logger);

        private void Retry(Action retryme)
            => Retry(
                () =>
                {
                    retryme();
                    return true;
                });

        private bool Set(CacheItem<TCacheValue> item, When when, bool sync = false)
        {
            if (!_isLuaAllowed)
            {
                return SetNoScript(item, when, sync);
            }

            var fullKey = GetKey(item.Key, item.Region);
            var value = ToRedisValue(item.Value);

            var flags = sync ? CommandFlags.None : CommandFlags.FireAndForget;

            ValidateExpirationTimeout(item);

            // ARGV [1]: value, [2]: type, [3]: expirationMode, [4]: expirationTimeout(millis), [5]: created(ticks)
            var parameters = new RedisValue[]
            {
                value,
                item.ValueType.AssemblyQualifiedName,
                (int)item.ExpirationMode,
                (long)item.ExpirationTimeout.TotalMilliseconds,
                item.CreatedUtc.Ticks,
                item.UsesExpirationDefaults
            };

            RedisResult result;
            if (when == When.NotExists)
            {
                result = Eval(ScriptType.Add, fullKey, parameters, flags);
            }
            else
            {
                result = Eval(ScriptType.Put, fullKey, parameters, flags);
            }

            if (result == null)
            {
                if (flags.HasFlag(CommandFlags.FireAndForget))
                {
                    if (!string.IsNullOrWhiteSpace(item.Region))
                    {
                        // setting region lookup key if region is being used
                        _connection.Database.HashSet(item.Region, fullKey, "regionKey", When.Always, CommandFlags.FireAndForget);
                    }

                    // put runs via fire and forget, so we don't get a result back
                    return true;
                }

                // should never happen, something went wrong with the script
                throw new InvalidOperationException("Something went wrong adding an item, result must not be null.");
            }
            else
            {
                if (result.IsNull && when == When.NotExists)
                {
                    // add failed because element exists already
                    if (Logger.IsEnabled(LogLevel.Information))
                    {
                        Logger.LogInfo("DB {0} | Failed to add item [{1}] because it exists.", _connection.Database.Database, item.ToString());
                    }

                    return false;
                }

                var resultValue = (RedisValue)result;

                if (resultValue.HasValue && resultValue.ToString().Equals("OK", StringComparison.OrdinalIgnoreCase))
                {
                    // Added successfully:
                    if (!string.IsNullOrWhiteSpace(item.Region))
                    {
                        // setting region lookup key if region is being used
                        // we cannot do that within the lua because the region could be on another cluster node!
                        _connection.Database.HashSet(item.Region, fullKey, "regionKey", When.Always, CommandFlags.FireAndForget);
                    }

                    return true;
                }

                Logger.LogWarn("DB {0} | Failed to set item [{1}]: {2}.", _connection.Database.Database, item.ToString(), resultValue.ToString());
                return false;
            }
        }

        private bool SetNoScript(CacheItem<TCacheValue> item, When when, bool sync = false)
        {
            return Retry(() =>
            {
                var fullKey = GetKey(item.Key, item.Region);
                var value = ToRedisValue(item.Value);

                ValidateExpirationTimeout(item);

                var metaValues = new[]
                {
                    new HashEntry(HashFieldType, item.ValueType.AssemblyQualifiedName),
                    new HashEntry(HashFieldExpirationMode, (int)item.ExpirationMode),
                    new HashEntry(HashFieldExpirationTimeout, (long)item.ExpirationTimeout.TotalMilliseconds),
                    new HashEntry(HashFieldCreated, item.CreatedUtc.Ticks),
                    new HashEntry(HashFieldUsesDefaultExp, item.UsesExpirationDefaults)
                };

                var flags = sync ? CommandFlags.None : CommandFlags.FireAndForget;

                var setResult = _connection.Database.HashSet(fullKey, HashFieldValue, value, when, flags);

                // setResult from fire and forget is alwys false, so we have to assume it works...
                setResult = flags == CommandFlags.FireAndForget ? true : setResult;

                if (setResult)
                {
                    if (!string.IsNullOrWhiteSpace(item.Region))
                    {
                        // setting region lookup key if region is being used
                        _connection.Database.HashSet(item.Region, fullKey, "regionKey", When.Always, CommandFlags.FireAndForget);
                    }

                    // set the additional fields in case sliding expiration should be used in this
                    // case we have to store the expiration mode and timeout on the hash, too so
                    // that we can extend the expiration period every time we do a get
                    if (metaValues != null)
                    {
                        _connection.Database.HashSet(fullKey, metaValues, flags);
                    }

                    if (item.ExpirationMode != ExpirationMode.None && item.ExpirationMode != ExpirationMode.Default)
                    {
                        _connection.Database.KeyExpire(fullKey, item.ExpirationTimeout, CommandFlags.FireAndForget);
                    }
                    else
                    {
                        // bugfix #9
                        _connection.Database.KeyPersist(fullKey, CommandFlags.FireAndForget);
                    }
                }

                return setResult;
            });
        }

        private RedisResult Eval(ScriptType scriptType, RedisKey redisKey, RedisValue[] values = null, CommandFlags flags = CommandFlags.None)
        {
            if (!_scriptsLoaded)
            {
                lock (_lockObject)
                {
                    if (!_scriptsLoaded)
                    {
                        LoadScripts();
                        _scriptsLoaded = true;
                    }
                }
            }

            LoadedLuaScript script = null;
            if (!_luaScripts.TryGetValue(scriptType, out LuaScript luaScript)
                || (_canPreloadScripts && !_shaScripts.TryGetValue(scriptType, out script)))
            {
                Logger.LogCritical("Something is wrong with the Lua scripts. Seem to be not loaded.");
                _scriptsLoaded = false;
                throw new InvalidOperationException("Something is wrong with the Lua scripts. Seem to be not loaded.");
            }

            try
            {
                if (_canPreloadScripts && script != null)
                {
                    return _connection.Database.ScriptEvaluate(script.Hash, new[] { redisKey }, values, flags);
                }
                else
                {
                    return _connection.Database.ScriptEvaluate(luaScript.ExecutableScript, new[] { redisKey }, values, flags);
                }
            }
            catch (RedisServerException ex) when (ex.Message.StartsWith("NOSCRIPT", StringComparison.OrdinalIgnoreCase))
            {
                Logger.LogInfo("Received NOSCRIPT from server. Reloading scripts...");
                LoadScripts();

                // retry
                throw;
            }
        }

        private void LoadScripts()
        {
            lock (_lockObject)
            {
                Logger.LogInfo("Loading scripts.");

                var putLua = LuaScript.Prepare(_scriptPut);
                var addLua = LuaScript.Prepare(_scriptAdd);
                var updateLua = LuaScript.Prepare(_scriptUpdate);
                var getLua = LuaScript.Prepare(_scriptGet);
                _luaScripts.Clear();
                _luaScripts.Add(ScriptType.Add, addLua);
                _luaScripts.Add(ScriptType.Put, putLua);
                _luaScripts.Add(ScriptType.Update, updateLua);
                _luaScripts.Add(ScriptType.Get, getLua);

                // servers feature might be disabled
                if (_canPreloadScripts)
                {
                    try
                    {
                        foreach (var server in Servers)
                        {
                            if (server.IsConnected)
                            {
                                _shaScripts[ScriptType.Put] = putLua.Load(server);
                                _shaScripts[ScriptType.Add] = addLua.Load(server);
                                _shaScripts[ScriptType.Update] = updateLua.Load(server);
                                _shaScripts[ScriptType.Get] = getLua.Load(server);
                            }
                        }
                    }
                    catch (NotSupportedException)
                    {
                        _canPreloadScripts = false;
                    }
                }
            }
        }
    }
}