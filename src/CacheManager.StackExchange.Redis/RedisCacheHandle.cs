using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using CacheManager.Core;
using CacheManager.Core.Internal;
using static CacheManager.Core.Utility.Guard;
using StackRedis = StackExchange.Redis;

namespace CacheManager.Redis
{
    /// <summary>
    /// Cache handle implementation for Redis.
    /// </summary>
    /// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
    public class RedisCacheHandle<TCacheValue> : BaseCacheHandle<TCacheValue>
    {
        private const string HashFieldCreated = "created";

        // expiration mode enum stored as int
        private const string HashFieldExpirationMode = "expiration";

        // expiration timeout stored as long
        private const string HashFieldExpirationTimeout = "timeout";

        private const string HashFieldType = "type";

        // the object value
        private const string HashFieldValue = "value";

        private const string ScriptAdd = @"
if redis.call('HSETNX', @key, @valField, @val) == 1 then
    local result=redis.call('HMSET', @key, @typeField, @type, @modeField, @mode, @timeoutField, @timeout, @createdField, @created)    
    local resultExp, resultReg
    if @expireMilli ~= '' then
        resultExp = redis.call('PEXPIRE', @key, @expireMilli)        
    else
        resultExp = redis.call('PERSIST', @key)
    end
    if @region ~= '' then
        resultReg = redis.call('HSET', @region, @key, 'regionKey')
    end
    return { result, resultExp, resultReg }
else 
    return nil
end";

        private const string ScriptPut = @"
local result = redis.call('HMSET', @key, @valField, @val, @typeField, @type, @modeField, @mode, @timeoutField, @timeout, @createdField, @created)
local resultExp, resultReg
if @expireMilli ~= '' then
    resultExp = redis.call('PEXPIRE', @key, @expireMilli)        
else
    resultExp = redis.call('PERSIST', @key)
end
if @region ~= '' then
    resultReg = redis.call('HSET', @region, @key, 'regionKey')
end
return { result, resultExp, resultReg }
";

        private const string ScriptUpdate = @"
if redis.call('HGET', @key, @valField) == @oldVal then
    return redis.call('HSET', @key, @valField, @val)
else
    return nil
end";

        private static readonly string ScriptGet = $@"
local result = redis.call('HMGET', @key, '{HashFieldValue}', '{HashFieldExpirationMode}', '{HashFieldExpirationTimeout}', '{HashFieldCreated}', '{HashFieldType}')
if (result[2] and result[2] == '1') then 
    if (result[3] and result[3] ~= '' and result[3] ~= '0') then
        redis.call('PEXPIRE', @key, result[3])
    end
end
return result";

        // the loaded lua script references
        private readonly IDictionary<ScriptType, StackRedis.LoadedLuaScript> shaScripts = new Dictionary<ScriptType, StackRedis.LoadedLuaScript>();

        private readonly RedisValueConverter valueConverter;
        private StackRedis.IDatabase database = null;
        private RedisConfiguration redisConfiguration = null;

        // flag if scripts are initially loaded to the server
        private bool scriptsLoaded = false;
        private object loadScriptLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCacheHandle{TCacheValue}"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="configuration">The configuration.</param>
        public RedisCacheHandle(ICacheManager<TCacheValue> manager, CacheHandleConfiguration configuration)
            : base(manager, configuration)
        {
            NotNull(manager, nameof(manager));
            EnsureNotNull(manager.Configuration.CacheSerializer, "A cache serializer must be defined for this cache handle.");
            this.valueConverter = new RedisValueConverter(manager.Configuration.CacheSerializer);
        }

        private enum ScriptType
        {
            Put,
            Add,
            Update,
            Get
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

        private StackRedis.ConnectionMultiplexer Connection => RedisConnectionPool.Connect(this.RedisConfiguration);

        private StackRedis.IDatabase Database
        {
            get
            {
                if (this.database == null)
                {
                    this.Retry(() =>
                    {
                        this.database = this.Connection.GetDatabase(this.RedisConfiguration.Database);

                        this.database.Ping();
                    });
                }

                return this.database;
                //// return this.Connection.GetDatabase(this.RedisConfiguration.Database);
            }
        }

        private RedisConfiguration RedisConfiguration
        {
            get
            {
                if (this.redisConfiguration == null)
                {
                    // throws an exception if not found for the name
                    this.redisConfiguration = RedisConfigurations.GetConfiguration(this.Configuration.HandleName);
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
                    var keys = hashKeys.Where(p => p.HasValue).Select(p => (StackRedis.RedisKey)p.ToString()).ToArray();
                    this.Database.KeyDelete(keys);
                    //// TODO: log result <> key length
                }

                // now delete the region
                this.Database.KeyDelete(region);
            });
        }

        /// <summary>
        /// Updates an existing key in the cache.
        /// <para>
        /// The cache manager will make sure the update will always happen on the most recent version.
        /// </para>
        /// <para>
        /// If version conflicts occur, if for example multiple cache clients try to write the same
        /// key, and during the update process, someone else changed the value for the key, the
        /// cache manager will retry the operation.
        /// </para>
        /// <para>
        /// The <paramref name="updateValue"/> function will get invoked on each retry with the most
        /// recent value which is stored in cache.
        /// </para>
        /// </summary>
        /// <param name="key">The key to update.</param>
        /// <param name="updateValue">The function to perform the update.</param>
        /// <param name="config">The cache configuration used to specify the update behavior.</param>
        /// <returns>The update result which is interpreted by the cache manager.</returns>
        /// <remarks>
        /// If the cache does not use a distributed cache system. Update is doing exactly the same
        /// as Get plus Put.
        /// </remarks>
        public override UpdateItemResult<TCacheValue> Update(string key, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config) =>
            this.Update(key, null, updateValue, config);

        /// <summary>
        /// Updates an existing key in the cache.
        /// <para>
        /// The cache manager will make sure the update will always happen on the most recent version.
        /// </para>
        /// <para>
        /// If version conflicts occur, if for example multiple cache clients try to write the same
        /// key, and during the update process, someone else changed the value for the key, the
        /// cache manager will retry the operation.
        /// </para>
        /// <para>
        /// The <paramref name="updateValue"/> function will get invoked on each retry with the most
        /// recent value which is stored in cache.
        /// </para>
        /// </summary>
        /// <param name="key">The key to update.</param>
        /// <param name="region">The cache region.</param>
        /// <param name="updateValue">The function to perform the update.</param>
        /// <param name="config">The cache configuration used to specify the update behavior.</param>
        /// <returns>The update result which is interpreted by the cache manager.</returns>
        /// <remarks>
        /// If the cache does not use a distributed cache system. Update is doing exactly the same
        /// as Get plus Put.
        /// </remarks>
        public override UpdateItemResult<TCacheValue> Update(string key, string region, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config)
        {
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

                    var result = this.Eval(ScriptType.Update, new
                    {
                        key = (StackRedis.RedisKey)fullKey,
                        valField = HashFieldValue,
                        val = this.ToRedisValue(newValue),
                        oldVal = oldValue
                    });

                    if (result != null && !result.IsNull)
                    {
                        return UpdateItemResult.ForSuccess<TCacheValue>(newValue, tries > 1, tries);
                    }
                }
                while (tries <= config.MaxRetries);

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

                var result = this.Eval(ScriptType.Get, new { key = fullKey });
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
            RetryHelper.Retry(retryme, this.Manager.Configuration.RetryTimeout, this.Manager.Configuration.MaxRetries);

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

            var parameters = new
            {
                key = (StackRedis.RedisKey)fullKey,
                valField = HashFieldValue,
                typeField = HashFieldType,
                modeField = HashFieldExpirationMode,
                timeoutField = HashFieldExpirationTimeout,
                createdField = HashFieldCreated,
                val = value,
                type = item.ValueType.AssemblyQualifiedName,
                mode = (int)item.ExpirationMode,
                timeout = item.ExpirationTimeout.TotalMilliseconds, // changed to millis
                created = item.CreatedUtc.Ticks,
                expireMilli = item.ExpirationMode == ExpirationMode.None ?
                    string.Empty :
                    item.ExpirationTimeout.TotalMilliseconds.ToString(CultureInfo.InvariantCulture),
                region = item.Region ?? string.Empty
            };

            StackRedis.RedisResult result;
            if (when == StackRedis.When.NotExists)
            {
                result = this.Eval(ScriptType.Add, parameters, flags);
            }
            else
            {
                result = this.Eval(ScriptType.Put, parameters, flags);
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

                var results = (StackRedis.RedisValue[])result;

                //// TODO: log
                //// System.Diagnostics.Debug.WriteLine("Set results:" + string.Join(", ", results));

                if (results[0].HasValue && results[0].ToString().Equals("OK", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                return false;
            }
        }

        private StackRedis.RedisResult Eval(ScriptType scriptType, object parameters, StackRedis.CommandFlags flags = StackRedis.CommandFlags.None)
        {
            if (!this.scriptsLoaded)
            {
                lock (this.loadScriptLock)
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
                throw new InvalidOperationException("Something went wrong during loading scripts to the server.");
            }

            try
            {
                return this.Database.ScriptEvaluate(script, parameters, flags);
            }
            catch (StackRedis.RedisServerException ex) when (ex.Message.StartsWith("NOSCRIPT", StringComparison.OrdinalIgnoreCase))
            {
                this.LoadScripts();
                throw;
            }
        }

        private void LoadScripts()
        {
            lock (this.loadScriptLock)
            {
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