using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.Core.Logging;
using static CacheManager.Core.Utility.Guard;
using StackRedis = StackExchange.Redis;

namespace CacheManager.Redis
{
    internal enum ScriptType
    {
        Put,
        Add,
        Update,
        Get
    }

    /// <summary>
    /// Cache handle implementation for Redis.
    /// </summary>
    /// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
    public class RedisCacheHandle<TCacheValue> : BaseCacheHandle<TCacheValue>
    {
        private const string HashFieldCreated = "created";
        private const string HashFieldExpirationMode = "expiration";
        private const string HashFieldExpirationTimeout = "timeout";
        private const string HashFieldType = "type";
        private const string HashFieldValue = "value";

        private static readonly string ScriptAdd = $@"
if redis.call('HSETNX', KEYS[1], '{HashFieldValue}', ARGV[1]) == 1 then
    local result=redis.call('HMSET', KEYS[1], '{HashFieldType}', ARGV[2], '{HashFieldExpirationMode}', ARGV[3], '{HashFieldExpirationTimeout}', ARGV[4], '{HashFieldCreated}', ARGV[5])
    if ARGV[3] ~= '0' and ARGV[4] ~= '0' then
        redis.call('PEXPIRE', KEYS[1], ARGV[4])
    else
        redis.call('PERSIST', KEYS[1])
    end
    return result
else 
    return nil
end";

        private static readonly string ScriptPut = $@"
local result=redis.call('HMSET', KEYS[1], '{HashFieldValue}', ARGV[1], '{HashFieldType}', ARGV[2], '{HashFieldExpirationMode}', ARGV[3], '{HashFieldExpirationTimeout}', ARGV[4], '{HashFieldCreated}', ARGV[5])
if ARGV[3] ~= '0' and ARGV[4] ~= '0' then
    redis.call('PEXPIRE', KEYS[1], ARGV[4])
else
    redis.call('PERSIST', KEYS[1])
end
return result";

        private static readonly string ScriptUpdate = $@"
if redis.call('HGET', KEYS[1], '{HashFieldValue}') == ARGV[2] then
    return redis.call('HSET', KEYS[1], '{HashFieldValue}', ARGV[1])
else
    return nil
end";

        private static readonly string ScriptGet = $@"
local result = redis.call('HMGET', KEYS[1], '{HashFieldValue}', '{HashFieldExpirationMode}', '{HashFieldExpirationTimeout}', '{HashFieldCreated}', '{HashFieldType}')
if (result[2] and result[2] == '1') then 
    if (result[3] and result[3] ~= '' and result[3] ~= '0') then
        redis.call('PEXPIRE', KEYS[1], result[3])
    end
end
return result";

        private readonly IDictionary<ScriptType, StackRedis.LoadedLuaScript> shaScripts = new Dictionary<ScriptType, StackRedis.LoadedLuaScript>();
        private readonly CacheManagerConfiguration managerConfiguration;
        private readonly RedisValueConverter valueConverter;
        private StackRedis.IDatabase database = null;
        private RedisConfiguration redisConfiguration = null;

        // flag if scripts are initially loaded to the server
        private bool scriptsLoaded = false;
        private object lockObject = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCacheHandle{TCacheValue}"/> class.
        /// </summary>
        /// <param name="managerConfiguration">The manager configuration.</param>
        /// <param name="configuration">The cache handle configuration.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="serializer">The serializer.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "redis", Justification = "That's the name...")]
        public RedisCacheHandle(CacheManagerConfiguration managerConfiguration, CacheHandleConfiguration configuration, ILoggerFactory loggerFactory, ICacheSerializer serializer)
            : base(managerConfiguration, configuration)
        {
            NotNull(loggerFactory, nameof(loggerFactory));
            NotNull(managerConfiguration, nameof(managerConfiguration));
            EnsureNotNull(serializer, "A serializer is required for the redis cache handle");

            this.managerConfiguration = managerConfiguration;
            this.Logger = loggerFactory.CreateLogger(this);
            this.valueConverter = new RedisValueConverter(serializer);
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
                var count = 0;
                foreach (var server in this.Servers.Where(p => !p.IsSlave && p.IsConnected))
                {
                    count += (int)server.DatabaseSize(this.RedisConfiguration.Database);
                }

                // aprox size, only size on the master..
                return count;
            }
        }

        /// <summary>
        /// Gets the servers.
        /// </summary>
        /// <returns>The list of servers.</returns>
        public IEnumerable<StackRedis.IServer> Servers
        {
            get
            {
                var connection = this.Connection;

                EndPoint[] endpoints = connection.GetEndPoints();
                foreach (var endpoint in endpoints)
                {
                    var server = connection.GetServer(endpoint);
                    yield return server;
                }
            }
        }

        /// <inheritdoc />
        protected override ILogger Logger { get; }

        private StackRedis.ConnectionMultiplexer Connection
        {
            get
            {
                return RedisConnectionPool.Connect(this.RedisConfiguration);
            }
        }

        private StackRedis.IDatabase Database
        {
            get
            {
                if (this.database == null)
                {
                    lock (this.lockObject)
                    {
                        if (this.database == null)
                        {
                            this.Retry(() =>
                            {
                                this.database = this.Connection.GetDatabase(this.RedisConfiguration.Database);

                                this.database.Ping();
                            });
                        }
                    }
                }

                return this.database;
            }
        }

        private RedisConfiguration RedisConfiguration
        {
            get
            {
                if (this.redisConfiguration == null)
                {
                    // throws an exception if not found for the name
                    this.redisConfiguration = RedisConfigurations.GetConfiguration(this.Configuration.Key);
                }

                return this.redisConfiguration;
            }
        }

        /// <summary>
        /// Clears this cache, removing all items in the base cache and all regions.
        /// </summary>
        public override void Clear()
        {
            foreach (var server in this.Servers.Where(p => !p.IsSlave))
            {
                this.Retry(() =>
                {
                    if (server.IsConnected)
                    {
                        server.FlushDatabase(this.RedisConfiguration.Database);
                    }
                });
            }
        }

        /// <summary>
        /// Clears the cache region, removing all items from the specified <paramref name="region"/> only.
        /// </summary>
        /// <param name="region">The cache region.</param>
        public override void ClearRegion(string region)
        {
            this.Retry(() =>
            {
                // we are storing all keys stored in the region in the hash for key=region
                var hashKeys = this.Database.HashKeys(region);

                if (hashKeys.Length > 0)
                {
                    // lets remove all keys which where in the region
                    // 01/32/16 changed to remove one by one because on clusters the keys could belong to multiple slots
                    foreach (var key in hashKeys.Where(p => p.HasValue))
                    {
                        this.Database.KeyDelete(key.ToString(), StackRedis.CommandFlags.FireAndForget);
                    }
                    //// TODO: log result <> key length
                }

                // now delete the region
                this.Database.KeyDelete(region);
            });
        }

        /// <inheritdoc />
        public override UpdateItemResult<TCacheValue> Update(string key, Func<TCacheValue, TCacheValue> updateValue, int maxRetries) =>
            this.Update(key, null, updateValue, maxRetries);

        /// <inheritdoc />
        public override UpdateItemResult<TCacheValue> Update(string key, string region, Func<TCacheValue, TCacheValue> updateValue, int maxRetries)
        {
            var tries = 0;
            var fullKey = GetKey(key, region);

            return this.Retry(() =>
            {
                do
                {
                    tries++;

                    ////// actually slower than using the real value field, maybe suffers if the value is larger
                    ////var version = this.Database.HashIncrement(fullKey, "version", 1L);
                    ////var oldValueAndType = this.Database.HashGet(fullKey, new StackRedis.RedisValue[] { HashFieldValue, HashFieldType });
                    ////var oldValue = oldValueAndType[0];
                    ////var valueType = oldValueAndType[1];
                    ////if (oldValue.IsNull || !oldValue.HasValue || valueType.IsNull || !valueType.HasValue)
                    ////{
                    ////    return UpdateItemResult.ForItemDidNotExist<TCacheValue>();
                    ////}
                    ////var newValue = updateValue(
                    ////    this.FromRedisValue(oldValue, valueType.ToString()));

                    var item = this.GetCacheItemInternal(key, region);

                    if (item == null)
                    {
                        return UpdateItemResult.ForItemDidNotExist<TCacheValue>();
                    }

                    var oldValue = this.ToRedisValue(item.Value);

                    // run update
                    var newValue = updateValue(item.Value);

                    var result = this.Eval(ScriptType.Update, fullKey, new[]
                    {
                        this.ToRedisValue(newValue),
                        oldValue
                    });

                    if (result != null && !result.IsNull)
                    {
                        return UpdateItemResult.ForSuccess<TCacheValue>(newValue, tries > 1, tries);
                    }

                    this.Logger.LogDebug("Update of {0} {1} failed with version conflict, retrying {2}/{3}", key, region, tries, maxRetries);
                }
                while (tries <= maxRetries);

                this.Logger.LogWarn("Update of {0} {1} failed with version conflict exiting because of too many retries.", key, region);
                return UpdateItemResult.ForTooManyRetries<TCacheValue>(tries);
            });
        }

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
            this.Retry(() => this.Set(item, StackRedis.When.NotExists, true));

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
                RedisConnectionPool.DisposeConnection(this.RedisConfiguration);
            }
        }

        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key)
            => this.GetCacheItemInternal(key, null);

#pragma warning disable CSE0003
        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key, string region)
        {
            return this.Retry(() =>
            {
                var fullKey = GetKey(key, region);

                var result = this.Eval(ScriptType.Get, fullKey);
                if (result == null || result.IsNull)
                {
                    // something went wrong. HMGET should return at least a null result for each requested field
                    throw new InvalidOperationException("Error retrieving " + fullKey);
                }

                var values = (StackRedis.RedisValue[])result;

                // the first item stores the value
                var item = values[0];
                var expirationModeItem = values[1];
                var timeoutItem = values[2];
                var createdItem = values[3];
                var valueTypeItem = values[4];

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
                    expirationMode = (ExpirationMode)(int)expirationModeItem;
                    expirationTimeout = TimeSpan.FromMilliseconds((long)timeoutItem);
                }

                var value = this.FromRedisValue(item, (string)valueTypeItem);

                var cacheItem = string.IsNullOrWhiteSpace(region) ?
                        new CacheItem<TCacheValue>(key, value, expirationMode, expirationTimeout) :
                        new CacheItem<TCacheValue>(key, region, value, expirationMode, expirationTimeout);

                if (createdItem.HasValue)
                {
                    cacheItem = cacheItem.WithCreated(new DateTime((long)createdItem));
                }

                //// update sliding
                ////if (expirationMode == ExpirationMode.Sliding && expirationTimeout != default(TimeSpan))
                ////{
                ////    this.Database.KeyExpire(fullKey, cacheItem.ExpirationTimeout, StackRedis.CommandFlags.FireAndForget);
                ////}

                return cacheItem;
            });
        }
#pragma warning restore CSE0003

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
            this.Retry(() => this.Set(item, StackRedis.When.Always, false));

        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        protected override bool RemoveInternal(string key) => this.RemoveInternal(key, null);

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
            return this.Retry(() =>
            {
                var fullKey = GetKey(key, region);

                // clean up region
                if (!string.IsNullOrWhiteSpace(region))
                {
                    this.Database.HashDelete(region, fullKey, StackRedis.CommandFlags.FireAndForget);
                }

                // remove key
                var result = this.Database.KeyDelete(fullKey);

                return result;
            });
        }
#pragma warning restore CSE0003

        private static string GetKey(string key, string region = null)
        {
            var fullKey = key;

            if (!string.IsNullOrWhiteSpace(region))
            {
                fullKey = string.Concat(region, ":", key);
            }

            return fullKey;
        }

        private TCacheValue FromRedisValue(StackRedis.RedisValue value, string valueType)
        {
            if (value.IsNull || value.IsNullOrEmpty || !value.HasValue)
            {
                return default(TCacheValue);
            }

            var typedConverter = this.valueConverter as IRedisValueConverter<TCacheValue>;
            if (typedConverter != null)
            {
                return typedConverter.FromRedisValue(value, valueType);
            }

            return this.valueConverter.FromRedisValue<TCacheValue>(value, valueType);
        }

        private StackRedis.RedisValue ToRedisValue(TCacheValue value)
        {
            var typedConverter = this.valueConverter as IRedisValueConverter<TCacheValue>;
            if (typedConverter != null)
            {
                return typedConverter.ToRedisValue(value);
            }

            return this.valueConverter.ToRedisValue(value);
        }

        private T Retry<T>(Func<T> retryme) =>
            RetryHelper.Retry(retryme, this.managerConfiguration.RetryTimeout, this.managerConfiguration.MaxRetries, this.Logger);

        private void Retry(Action retryme)
            => this.Retry<bool>(
                () =>
                {
                    retryme();
                    return true;
                });

        private bool Set(CacheItem<TCacheValue> item, StackRedis.When when, bool sync = false)
        {
            var fullKey = GetKey(item.Key, item.Region);
            var value = this.ToRedisValue(item.Value);

            var flags = sync ? StackRedis.CommandFlags.None : StackRedis.CommandFlags.FireAndForget;

            // ARGV [1]: value, [2]: type, [3]: expirationMode, [4]: expirationTimeout(millis), [5]: created(ticks)
            var parameters = new StackRedis.RedisValue[]
            {
                value,
                item.ValueType.AssemblyQualifiedName,
                (int)item.ExpirationMode,
                item.ExpirationTimeout.TotalMilliseconds,
                item.CreatedUtc.Ticks
            };

            StackRedis.RedisResult result;
            if (when == StackRedis.When.NotExists)
            {
                result = this.Eval(ScriptType.Add, fullKey, parameters, flags);
            }
            else
            {
                result = this.Eval(ScriptType.Put, fullKey, parameters, flags);
            }

            if (result == null)
            {
                if (flags.HasFlag(StackRedis.CommandFlags.FireAndForget))
                {
                    // put runs via fire and forget, so we don't get a result back
                    return true;
                }

                // should never happen, something went wrong with the script
                throw new InvalidOperationException("Something went wrong adding an item, result must not be null.");
            }
            else
            {
                if (result.IsNull && when == StackRedis.When.NotExists)
                {
                    // add failed because element exists already
                    return false;
                }

                var resultValue = (StackRedis.RedisValue)result;

                if (resultValue.HasValue && resultValue.ToString().Equals("OK", StringComparison.OrdinalIgnoreCase))
                {
                    // Added successfully:
                    if (!string.IsNullOrWhiteSpace(item.Region))
                    {
                        // now update region lookup if region is set
                        // we cannot do that within the lua because the region could be on another cluster node!
                        this.Database.HashSet(item.Region, fullKey, "regionKey", when, StackRedis.CommandFlags.FireAndForget);
                    }

                    return true;
                }

                return false;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "Lua", Justification = "That's the name")]
        private StackRedis.RedisResult Eval(ScriptType scriptType, StackRedis.RedisKey redisKey, StackRedis.RedisValue[] values = null, StackRedis.CommandFlags flags = StackRedis.CommandFlags.None)
        {
            if (!this.scriptsLoaded)
            {
                lock (this.lockObject)
                {
                    if (!this.scriptsLoaded)
                    {
                        this.LoadScripts();
                        this.scriptsLoaded = true;
                    }
                }
            }

            StackRedis.LoadedLuaScript script;
            if (!this.shaScripts.TryGetValue(scriptType, out script))
            {
                this.Logger.LogCritical("Something is wrong with the Lua scripts. Seem to be not loaded.");
                throw new InvalidOperationException("Something is wrong with the Lua scripts. Seem to be not loaded.");
            }

            try
            {
                return this.Database.ScriptEvaluate(script.Hash, new[] { redisKey }, values, flags);
            }
            catch (StackRedis.RedisServerException ex) when (ex.Message.StartsWith("NOSCRIPT", StringComparison.OrdinalIgnoreCase))
            {
                this.Logger.LogInfo("Received NOSCRIPT from server. Reloading scripts...");
                this.LoadScripts();

                // retry
                throw;
            }
        }

        private void LoadScripts()
        {
            lock (this.lockObject)
            {
                this.Logger.LogInfo("Loading scripts.");

                var putLua = StackRedis.LuaScript.Prepare(ScriptPut);
                var addLua = StackRedis.LuaScript.Prepare(ScriptAdd);
                var updateLua = StackRedis.LuaScript.Prepare(ScriptUpdate);
                var getLua = StackRedis.LuaScript.Prepare(ScriptGet);

                foreach (var server in this.Servers)
                {
                    if (server.IsConnected)
                    {
                        this.shaScripts[ScriptType.Put] = putLua.Load(server);
                        this.shaScripts[ScriptType.Add] = addLua.Load(server);
                        this.shaScripts[ScriptType.Update] = updateLua.Load(server);
                        this.shaScripts[ScriptType.Get] = getLua.Load(server);
                    }
                }
            }
        }
    }
}