using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly RedisConnectionManager connection;
        private readonly bool isLuaAllowed = true;
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
        public RedisCacheHandle(CacheManagerConfiguration managerConfiguration, CacheHandleConfiguration configuration, ILoggerFactory loggerFactory, ICacheSerializer serializer)
            : base(managerConfiguration, configuration)
        {
            NotNull(loggerFactory, nameof(loggerFactory));
            NotNull(managerConfiguration, nameof(managerConfiguration));
            NotNull(configuration, nameof(configuration));
            EnsureNotNull(serializer, "A serializer is required for the redis cache handle");

            this.managerConfiguration = managerConfiguration;
            this.Logger = loggerFactory.CreateLogger(this);
            this.valueConverter = new RedisValueConverter(serializer);
            this.redisConfiguration = RedisConfigurations.GetConfiguration(configuration.Key);
            this.connection = new RedisConnectionManager(this.redisConfiguration, loggerFactory);
            this.isLuaAllowed = this.connection.Features.Scripting;
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
                    count += (int)server.DatabaseSize(this.redisConfiguration.Database);
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
        public IEnumerable<StackRedis.IServer> Servers => this.connection.Servers;

        /// <summary>
        /// Gets the features the redis server supports.
        /// </summary>
        /// <value>The server features.</value>
        public StackRedis.RedisFeatures Features => this.connection.Features;
#pragma warning restore CS3003 // Type is not CLS-compliant

        /// <inheritdoc />
        protected override ILogger Logger { get; }

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
                        server.FlushDatabase(this.redisConfiguration.Database);
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
                var hashKeys = this.connection.Database.HashKeys(region);

                if (hashKeys.Length > 0)
                {
                    // lets remove all keys which where in the region
                    // 01/32/16 changed to remove one by one because on clusters the keys could belong to multiple slots
                    foreach (var key in hashKeys.Where(p => p.HasValue))
                    {
                        this.connection.Database.KeyDelete(key.ToString(), StackRedis.CommandFlags.FireAndForget);
                    }
                }

                // now delete the region
                this.connection.Database.KeyDelete(region);
            });
        }

        /// <inheritdoc />
        public override UpdateItemResult<TCacheValue> Update(string key, Func<TCacheValue, TCacheValue> updateValue, int maxRetries)
            => this.Update(key, null, updateValue, maxRetries);

        /// <inheritdoc />
        public override UpdateItemResult<TCacheValue> Update(string key, string region, Func<TCacheValue, TCacheValue> updateValue, int maxRetries)
        {
            if (!this.isLuaAllowed)
            {
                return this.UpdateNoScript(key, region, updateValue, maxRetries);
            }

            var tries = 0;
            var fullKey = GetKey(key, region);

            return this.Retry(() =>
            {
                do
                {
                    tries++;

                    var item = this.GetCacheItemInternal(key, region);

                    if (item == null)
                    {
                        return UpdateItemResult.ForItemDidNotExist<TCacheValue>();
                    }

                    var oldValue = this.ToRedisValue(item.Value);

                    // run update
                    var newValue = updateValue(item.Value);

                    // added null check, throw explicit to me more consistent. Otherwise it would throw within the script exec
                    if (newValue == null)
                    {
                        throw new InvalidOperationException("Factory value must not be null.");
                    }

                    var result = this.Eval(ScriptType.Update, fullKey, new[]
                    {
                        this.ToRedisValue(newValue),
                        oldValue
                    });

                    if (result != null && !result.IsNull)
                    {
                        return UpdateItemResult.ForSuccess(newValue, tries > 1, tries);
                    }

                    this.Logger.LogDebug("Update of {0} {1} failed with version conflict, retrying {2}/{3}", key, region, tries, maxRetries);
                }
                while (tries <= maxRetries);

                this.Logger.LogWarn("Update of {0} {1} failed with version conflict exiting because of too many retries.", key, region);
                return UpdateItemResult.ForTooManyRetries<TCacheValue>(tries);
            });
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        protected UpdateItemResult<TCacheValue> UpdateNoScript(string key, string region, Func<TCacheValue, TCacheValue> updateValue, int maxRetries)
        {
            var committed = false;
            var tries = 0;
            var fullKey = GetKey(key, region);

            return this.Retry(() =>
            {
                do
                {
                    tries++;

                    var item = this.GetCacheItemInternal(key, region);

                    if (item == null)
                    {
                        return UpdateItemResult.ForItemDidNotExist<TCacheValue>();
                    }

                    var oldValue = this.ToRedisValue(item.Value);

                    var tran = this.connection.Database.CreateTransaction();
                    tran.AddCondition(StackRedis.Condition.HashEqual(fullKey, HashFieldValue, oldValue));

                    // run update
                    var newValue = updateValue(item.Value);
                    
                    // added null check, throw explicit to me more consistent. Otherwise it would throw later
                    if (newValue == null)
                    {
                        throw new InvalidOperationException("Factory value must not be null.");
                    }

                    tran.HashSetAsync(fullKey, HashFieldValue, this.ToRedisValue(newValue));

                    committed = tran.Execute();

                    if (committed)
                    {
                        return UpdateItemResult.ForSuccess<TCacheValue>(newValue, tries > 1, tries);
                    }
                }
                while (committed == false && tries <= maxRetries);

                return UpdateItemResult.ForTooManyRetries<TCacheValue>(tries);
            });
        }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

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
                this.connection.RemoveConnection();
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
            if (!this.isLuaAllowed)
            {
                return this.GetCacheItemInternalNoScript(key, region);
            }

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

                return cacheItem;
            });
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        protected CacheItem<TCacheValue> GetCacheItemInternalNoScript(string key, string region)
        {
            return this.Retry(() =>
            {
                var fullKey = GetKey(key, region);

                // getting both, the value and, if exists, the expiration mode. if that one is set
                // and it is sliding, we also retrieve the timeout later
                var values = this.connection.Database.HashGet(
                    fullKey,
                    new StackRedis.RedisValue[]
                    {
                        HashFieldValue,
                        HashFieldExpirationMode,
                        HashFieldExpirationTimeout,
                        HashFieldCreated,
                        HashFieldType
                    });

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

                // update sliding
                if (expirationMode == ExpirationMode.Sliding && expirationTimeout != default(TimeSpan))
                {
                    this.connection.Database.KeyExpire(fullKey, cacheItem.ExpirationTimeout, StackRedis.CommandFlags.FireAndForget);
                }

                return cacheItem;
            });
        }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
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
                    this.connection.Database.HashDelete(region, fullKey, StackRedis.CommandFlags.FireAndForget);
                }

                // remove key
                var result = this.connection.Database.KeyDelete(fullKey);

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
            if (!this.isLuaAllowed)
            {
                return this.SetNoScript(item, when, sync);
            }

            var fullKey = GetKey(item.Key, item.Region);
            var value = this.ToRedisValue(item.Value);

            var flags = sync ? StackRedis.CommandFlags.None : StackRedis.CommandFlags.FireAndForget;

            // ARGV [1]: value, [2]: type, [3]: expirationMode, [4]: expirationTimeout(millis), [5]: created(ticks)
            var parameters = new StackRedis.RedisValue[]
            {
                value,
                item.ValueType.AssemblyQualifiedName,
                (int)item.ExpirationMode,
                (long)item.ExpirationTimeout.TotalMilliseconds,
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
                    if (!string.IsNullOrWhiteSpace(item.Region))
                    {
                        // setting region lookup key if region is being used
                        this.connection.Database.HashSet(item.Region, fullKey, "regionKey", StackRedis.When.Always, StackRedis.CommandFlags.FireAndForget);
                    }

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
                    this.Logger.LogInfo("DB {0} | Failed to add item {1} because it exists.", this.connection.Database.Database, fullKey);
                    return false;
                }

                var resultValue = (StackRedis.RedisValue)result;

                if (resultValue.HasValue && resultValue.ToString().Equals("OK", StringComparison.OrdinalIgnoreCase))
                {
                    // Added successfully:
                    if (!string.IsNullOrWhiteSpace(item.Region))
                    {
                        // setting region lookup key if region is being used
                        // we cannot do that within the lua because the region could be on another cluster node!
                        this.connection.Database.HashSet(item.Region, fullKey, "regionKey", StackRedis.When.Always, StackRedis.CommandFlags.FireAndForget);
                    }

                    return true;
                }

                this.Logger.LogWarn("DB {0} | Failed to set item {1}: {2}.", this.connection.Database.Database, fullKey, resultValue.ToString());
                return false;
            }
        }

        private bool SetNoScript(CacheItem<TCacheValue> item, StackRedis.When when, bool sync = false)
        {
            return this.Retry(() =>
            {
                var fullKey = GetKey(item.Key, item.Region);
                var value = this.ToRedisValue(item.Value);

                StackRedis.HashEntry[] metaValues = new[]
                {
                    new StackRedis.HashEntry(HashFieldType, item.ValueType.AssemblyQualifiedName),
                    new StackRedis.HashEntry(HashFieldExpirationMode, (int)item.ExpirationMode),
                    new StackRedis.HashEntry(HashFieldExpirationTimeout, (long)item.ExpirationTimeout.TotalMilliseconds),
                    new StackRedis.HashEntry(HashFieldCreated, item.CreatedUtc.Ticks)
                };

                var flags = sync ? StackRedis.CommandFlags.None : StackRedis.CommandFlags.FireAndForget;

                var setResult = this.connection.Database.HashSet(fullKey, HashFieldValue, value, when, flags);

                // setResult from fire and forget is alwys false, so we have to assume it works...
                setResult = flags == StackRedis.CommandFlags.FireAndForget ? true : setResult;

                if (setResult)
                {
                    if (!string.IsNullOrWhiteSpace(item.Region))
                    {
                        // setting region lookup key if region is being used
                        this.connection.Database.HashSet(item.Region, fullKey, "regionKey", StackRedis.When.Always, StackRedis.CommandFlags.FireAndForget);
                    }

                    // set the additional fields in case sliding expiration should be used in this
                    // case we have to store the expiration mode and timeout on the hash, too so
                    // that we can extend the expiration period every time we do a get
                    if (metaValues != null)
                    {
                        this.connection.Database.HashSet(fullKey, metaValues, flags);
                    }

                    if (item.ExpirationMode != ExpirationMode.None)
                    {
                        this.connection.Database.KeyExpire(fullKey, item.ExpirationTimeout, StackRedis.CommandFlags.FireAndForget);
                    }
                    else
                    {
                        // bugfix #9
                        this.connection.Database.KeyExpire(fullKey, default(TimeSpan?), StackRedis.CommandFlags.FireAndForget);
                    }
                }

                return setResult;
            });
        }

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
                this.scriptsLoaded = false;
                throw new InvalidOperationException("Something is wrong with the Lua scripts. Seem to be not loaded.");
            }

            try
            {
                return this.connection.Database.ScriptEvaluate(script.Hash, new[] { redisKey }, values, flags);
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