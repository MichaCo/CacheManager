using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CacheManager.Core;
using CacheManager.Core.Cache;
using CacheManager.Core.Configuration;
using StackRedis = StackExchange.Redis;

namespace CacheManager.StackExchange.Redis
{
    public class RedisCacheHandle : RedisCacheHandle<object>
    {
        public RedisCacheHandle(ICacheManager<object> manager, ICacheHandleConfiguration configuration)
            : base(manager, configuration)
        {
        }
    }

    public class RedisCacheHandle<TCacheValue> : BaseCacheHandle<TCacheValue>
    {
        // the object value
        private const string HashFieldValue = "value";

        // expiration mode enum stored as int
        private const string HashFieldExpirationMode = "expiration";

        // expiration timeout stored as long
        private const string HashFieldExpirationTimeout = "timeout";
        private const string HashFieldCreated = "created";
        private const string HashFieldType = "type";

        private static readonly IRedisValueConverter<TCacheValue> valueConverter = new RedisValueConverter() as IRedisValueConverter<TCacheValue>;
        private StackRedis.IDatabase databaseBack = null;
        private StackRedis.ISubscriber subscriber = null;
        private readonly string Identifier = Guid.NewGuid().ToString();
        private RedisConfiguration redisConfiguration = null;
        private string channelName = string.Empty;

        private RedisConfiguration RedisConfiguration
        {
            get
            {
                if (redisConfiguration == null)
                {
                    redisConfiguration = this.Manager.Configuration.RedisConfigurations
                                                .FirstOrDefault(p => p.Id == this.Configuration.HandleName);

                    if (redisConfiguration == null)
                    {
                        throw new ConfigurationException(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "No redis configuration for handle name {0} found. The id of the redisOption and the handle name must match.",
                                this.Configuration.HandleName));
                    }
                }

                return redisConfiguration;
            }
        }
        
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
                if (databaseBack == null)
                {
                    Retry(() =>
                    {
                        databaseBack = Connection.GetDatabase(this.RedisConfiguration.Database);

                        databaseBack.Ping();
                    });
                }

                return databaseBack;
            }
        }

        public RedisCacheHandle(ICacheManager<TCacheValue> manager, ICacheHandleConfiguration configuration)
            : base(manager, configuration)
        {
        }

        protected override void Dispose(bool disposeManaged)
        {
            base.Dispose(disposeManaged);
        }

        public override int Count
        {
            get
            {
                var server = this.GetServers(Connection).First(p => !p.IsSlave && p.IsConnected);
                if (server == null)
                {
                    throw new InvalidOperationException("No active master found.");
                }

                // aprox size, only size on the master..
                return (int)server.DatabaseSize(this.RedisConfiguration.Database);
            }
        }

        public override void Clear()
        {
            foreach (var server in this.GetServers(Connection)
                .Where(p=>!p.IsSlave))
            {
                server.FlushDatabase(this.RedisConfiguration.Database);
            }
        }

        public IEnumerable<StackRedis.IServer> GetServers(StackRedis.ConnectionMultiplexer muxer)
        {
            EndPoint[] endpoints = muxer.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = muxer.GetServer(endpoint);
                yield return server;
            }
        }

        public override void ClearRegion(string region)
        {
            Retry(() =>
            {
                // we are storing all keys stored in the region in the hash for key=region
                var hashKeys = this.Database.HashKeys(region);

                if (hashKeys.Length > 0)
                {
                    // lets remove all keys which where in the region
                    var keys = hashKeys.Where(p => p.HasValue).Select(p => (StackRedis.RedisKey)GetKey(p, region)).ToArray();
                    var delKeysResult = this.Database.KeyDelete(keys);
                }

                // now delete the region
                this.Database.KeyDelete(region);
            });
        }
        
        // Add call is synced, so might be slower than put which is fire and forget
        // but we want to retun true|false if the operation was successfully or not. And always returning true could be missleading if the 
        // item already exists
        protected override bool AddInternalPrepared(CacheItem<TCacheValue> item)
        {
            return this.Set(item, StackRedis.When.NotExists, true);
        }

        protected override void PutInternal(CacheItem<TCacheValue> item)
        {
            base.PutInternal(item);
        }

        protected override void PutInternalPrepared(CacheItem<TCacheValue> item)
        {
            // try to set the item
            var result = this.Set(item, StackRedis.When.NotExists, true);

            // it it does exist, lets try to modify it
            if (!result)
            {
                this.Set(item, StackRedis.When.Always, false);
            }
        }

        private bool Set(CacheItem<TCacheValue> item, StackRedis.When when, bool sync = false)
        {
            return Retry(() =>
            {
                TimeSpan? expiration = item.ExpirationTimeout;
                if (item.ExpirationMode == ExpirationMode.None)
                {
                    expiration = null;
                }

                var fullKey = this.GetKey(item.Key, item.Region);
                var value = ToRedisValue(item.Value);


                StackRedis.HashEntry[] metaValues = new[]
                {
                    new StackRedis.HashEntry(HashFieldType, item.ValueType.FullName)
                };

                if (item.ExpirationMode != ExpirationMode.None)
                {
                    metaValues = new[]
                    {
                        new StackRedis.HashEntry(HashFieldExpirationMode, (int)item.ExpirationMode),
                        new StackRedis.HashEntry(HashFieldExpirationTimeout, item.ExpirationTimeout.Ticks),
                        new StackRedis.HashEntry(HashFieldCreated, item.CreatedUtc.Ticks),
                        new StackRedis.HashEntry(HashFieldType, item.ValueType.FullName)
                    };
                }
                               
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

                    // set the additional fields in case sliding expiration should be used
                    // in this case we have to store the expiration mode and timeout on the hash, too
                    // so that we can extend the expiration period every time we do a get
                    if (metaValues != null)
                    {
                        this.Database.HashSet(fullKey, metaValues, flags);
                    }

                    if (item.ExpirationMode != ExpirationMode.None)
                    {
                        this.Database.KeyExpire(fullKey, item.ExpirationTimeout, StackRedis.CommandFlags.FireAndForget);
                    }
                }

                return setResult;
            });
        }

        public override UpdateItemResult Update(string key, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config)
        {
            return this.Update(key, null, updateValue, config);
        }

        public override UpdateItemResult Update(string key, string region, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config)
        {
            var committed = false;
            var tries = 0;
            var fullKey = this.GetKey(key, region);

            return Retry(() =>
            {
                do
                {
                    tries++;
                    
                    var item = this.GetCacheItemInternal(key, region);

                    if (item == null)
                    {
                        return new UpdateItemResult(false, false, 1);
                    }

                    var oldValue = ToRedisValue(item.Value);

                    var tran = this.Database.CreateTransaction();
                    tran.AddCondition(StackRedis.Condition.HashEqual(fullKey, HashFieldValue, oldValue));

                    // run update
                    var newValue = updateValue(item.Value);
                    
                    tran.HashSetAsync(fullKey, HashFieldValue, ToRedisValue(newValue));

                    committed = tran.Execute();

                    if (committed)
                    {
                        return new UpdateItemResult(tries > 1, true, tries);
                    }
                } while (committed == false && tries <= config.MaxRetries);

                return new UpdateItemResult(false, false, 1);
            });
        }

        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key)
        {
            return GetCacheItemInternal(key, null);
        }

        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key, string region)
        {
            return Retry(() =>
            {
                var fullKey = this.GetKey(key, region);

                // getting both, the value and, if exists, the expiration mode. 
                // if that one is set and it is sliding, we also retrieve the timeout later
                var values = this.Database.HashGet(fullKey, new StackRedis.RedisValue[] { 
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

                var value = FromRedisValue(item, (string)valueTypeItem);

                var cacheItem = string.IsNullOrWhiteSpace(region) ?
                        new CacheItem<TCacheValue>(key, value, expirationMode, expirationTimeout) :
                        new CacheItem<TCacheValue>(key, value, region, expirationMode, expirationTimeout);

                if (createdItem.HasValue)
                {
                    cacheItem.CreatedUtc = new DateTime((long)createdItem);
                }

                // update sliding
                if (expirationMode == ExpirationMode.Sliding && expirationTimeout != default(TimeSpan))
                {
                    this.Database.KeyExpire(fullKey, cacheItem.ExpirationTimeout, StackRedis.CommandFlags.FireAndForget);
                }

                return cacheItem;
            });
        }

        protected override bool RemoveInternal(string key)
        {
            return RemoveInternal(key, null);
        }

        protected override bool RemoveInternal(string key, string region)
        {
            return Retry(() =>
            {
                // clean up region
                if (!string.IsNullOrWhiteSpace(region))
                {
                    this.Database.HashDelete(region, key, StackRedis.CommandFlags.FireAndForget);
                }

                // remove key
                var fullKey = this.GetKey(key, region);
                var result = this.Database.KeyDelete(fullKey);
                
                return result;
            });
        }

        private string GetKey(string key, string region = null)
        {
            var fullKey = key;

            if (!string.IsNullOrWhiteSpace(region))
            {
                fullKey = string.Concat(region, ":", key);
            }

            return fullKey;
        }

        private T Retry<T>(Func<T> retryme)
        {
            return RetryHelper.Retry(retryme, this.Manager.Configuration.RetryTimeout, this.Manager.Configuration.MaxRetries);
        }

        private void Retry(Action retryme)
        {
            var result = Retry<bool>(() => { retryme(); return true; });
        }

        private static StackRedis.RedisValue ToRedisValue(TCacheValue value)
        {
            if(valueConverter != null)
            {
                return valueConverter.ToRedisValue(value);
            }
            
            return (StackRedis.RedisValue)RedisValueConverter.ToBytes(value);
        }

        private static TCacheValue FromRedisValue(StackRedis.RedisValue value, string valueType)
        {
            if (value.IsNull || value.IsNullOrEmpty || !value.HasValue)
            {
                return default(TCacheValue);
            }
                        
            if(valueConverter != null)
            {
                return valueConverter.FromRedisValue(value, valueType);
            }

            return RedisValueConverter.FromBytes<TCacheValue>(value);
        }
    }
}