using System;
using System.Collections.Generic;
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

        private readonly RedisValueConverter valueConverter;
        private StackRedis.IDatabase database = null;
        private RedisConfiguration redisConfiguration = null;

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
                this.Retry(() => server.FlushDatabase(this.RedisConfiguration.Database));
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
                    var keys = hashKeys.Where(p => p.HasValue).Select(p => (StackRedis.RedisKey)GetKey(p, region)).ToArray();
                    this.Database.KeyDelete(keys);
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

                    var tran = this.Database.CreateTransaction();
                    tran.AddCondition(StackRedis.Condition.HashEqual(fullKey, HashFieldValue, oldValue));

                    // run update
                    var newValue = updateValue(item.Value);

                    tran.HashSetAsync(fullKey, HashFieldValue, this.ToRedisValue(newValue));

                    committed = tran.Execute();

                    if (committed)
                    {
                        return UpdateItemResult.ForSuccess<TCacheValue>(newValue, tries > 1, tries);
                    }
                    else
                    {
                        //// just for debugging one bug in the redis client
                        //// var checkItem = this.GetCacheItemInternal(key, region);
                        //// if (newValue.Equals(checkItem.Value))
                        //// {
                        ////     throw new InvalidOperationException("Updated although not committed.");
                        //// }
                    }
                }
                while (committed == false && tries <= config.MaxRetries);

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
            this.Set(item, StackRedis.When.NotExists, true);

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

                // getting both, the value and, if exists, the expiration mode. if that one is set
                // and it is sliding, we also retrieve the timeout later
                var values = this.Database.HashGet(
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
                    expirationTimeout = TimeSpan.FromTicks((long)timeoutItem);
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
                    this.Database.KeyExpire(fullKey, cacheItem.ExpirationTimeout, StackRedis.CommandFlags.FireAndForget);
                }

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
            this.Set(item, StackRedis.When.Always, false);

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
                // clean up region
                if (!string.IsNullOrWhiteSpace(region))
                {
                    this.Database.HashDelete(region, key, StackRedis.CommandFlags.FireAndForget);
                }

                // remove key
                var fullKey = GetKey(key, region);
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

#pragma warning disable CSE0003
        private bool Set(CacheItem<TCacheValue> item, StackRedis.When when, bool sync = false)
        {
            // TODO: move the whole logic into a script to make it atomic
            return this.Retry(() =>
            {
                var fullKey = GetKey(item.Key, item.Region);
                var value = this.ToRedisValue(item.Value);

                StackRedis.HashEntry[] metaValues = new[]
                {
                    new StackRedis.HashEntry(HashFieldType, item.ValueType.AssemblyQualifiedName),
                    new StackRedis.HashEntry(HashFieldExpirationMode, (int)item.ExpirationMode),
                    new StackRedis.HashEntry(HashFieldExpirationTimeout, item.ExpirationTimeout.Ticks),
                    new StackRedis.HashEntry(HashFieldCreated, item.CreatedUtc.Ticks)
                };

                var flags = sync ? StackRedis.CommandFlags.None : StackRedis.CommandFlags.FireAndForget;

                var setResult = this.Database.HashSet(fullKey, HashFieldValue, value, when, flags);

                // setResult from fire and forget is alwys false, so we have to assume it works...
                setResult = flags == StackRedis.CommandFlags.FireAndForget ? true : setResult;

                if (setResult)
                {
                    // update region lookup
                    if (!string.IsNullOrWhiteSpace(item.Region))
                    {
                        this.Database.HashSet(item.Region, item.Key, "regionKey", when, StackRedis.CommandFlags.FireAndForget);
                    }

                    // set the additional fields in case sliding expiration should be used in this
                    // case we have to store the expiration mode and timeout on the hash, too so
                    // that we can extend the expiration period every time we do a get
                    if (metaValues != null)
                    {
                        this.Database.HashSet(fullKey, metaValues, flags);
                    }

                    if (item.ExpirationMode != ExpirationMode.None)
                    {
                        this.Database.KeyExpire(fullKey, item.ExpirationTimeout, StackRedis.CommandFlags.FireAndForget);
                    }
                    else
                    {
                        // bugfix #9
                        this.Database.KeyExpire(fullKey, default(TimeSpan?), StackRedis.CommandFlags.FireAndForget);
                    }
                }

                return setResult;
            });
        }
#pragma warning restore CSE0003
    }
}