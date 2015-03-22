using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using CacheManager.Core;
using CacheManager.Core.Cache;
using CacheManager.Core.Configuration;
using StackRedis = StackExchange.Redis;

namespace CacheManager.StackExchange.Redis
{
    public class RedisCacheHandle<TCacheValue> : BaseCacheHandle<TCacheValue>
    {
        // the object value
        private const string HashFieldValue = "value";

        // expiration mode enum stored as int
        private const string HashFieldExpirationMode = "expiration";

        // expiration timeout stored as long
        private const string HashFieldExpirationTimeout = "timeout";

        private static readonly IRedisValueConverter<TCacheValue> valueConverter = new RedisValueConverter() as IRedisValueConverter<TCacheValue>;
        private StackRedis.ConnectionMultiplexer connection = null;
        private StackRedis.IDatabase databaseBack = null;
        private StackRedis.ISubscriber subscriber = null;
        private readonly Guid Identifier = Guid.NewGuid();
        private RedisConfiguration redisConfiguration = null;

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
                if (connection == null)
                {
                    this.Connect();
                }

                return connection;
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

        private StackRedis.ISubscriber Subscriber
        {
            get
            {
                if (subscriber == null)
                {
                    Retry(() =>
                    {
                        Connection.PreserveAsyncOrder = false;
                        subscriber = Connection.GetSubscriber();
                    });
                }

                return subscriber;
            }
        }

        private readonly string channelName = string.Empty;

        public RedisCacheHandle(ICacheManager<TCacheValue> manager, ICacheHandleConfiguration configuration)
            : base(manager, configuration)
        {
            // default should be enabled, although this costs some performance
            this.EnableBackPlateUpdates();

            this.channelName = string.Format(
                CultureInfo.InvariantCulture, 
                "CacheManager_{0}_{1}", 
                this.Manager.Configuration.Name, 
                configuration.HandleName);
        }

        /// <summary>
        /// Enables the back plate feature based on redis pub/sub in this case.
        /// <para>
        /// It will cost a little bit write performance, but only this way, multiple cache instances with
        /// another in process cache infront of the redis cache will work correctly (e.g. keys will get removed on all servers, 
        /// not only locally...).
        /// </para>
        /// </summary>
        public void EnableBackPlateUpdates()
        {
            this.BackPlate = new CacheBackPlate();
        }

        public void DisableBackPlateUpdates()
        {
            this.BackPlate = null;
        }

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                this.Connection.Dispose();
            }

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

            this.NotifyChannel(ChannelAction.Clear, null, null);
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

                // lets remove all keys which where in the region
                var keys = hashKeys.Where(p => p.HasValue).Select(p => (StackRedis.RedisKey)GetKey(p, region)).ToArray();
                var delKeysResult = this.Database.KeyDelete(keys);
                
                // now delete the region
                var delRegion = this.Database.KeyDelete(region);

                this.NotifyChannel(ChannelAction.ClearRegion, null, region);
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
            var result = this.Set(item, StackRedis.When.Always);
            if (result)
            {
                this.NotifyChannel(ChannelAction.Changed, item.Key, item.Region);
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


                StackRedis.HashEntry[] metaValues = null;
                if (item.ExpirationMode == ExpirationMode.Sliding)
                {
                    metaValues = new[]
                    {
                        new StackRedis.HashEntry(HashFieldExpirationMode, (int)item.ExpirationMode),
                        new StackRedis.HashEntry(HashFieldExpirationTimeout, item.ExpirationTimeout.Ticks)
                    };
                }

                if (!string.IsNullOrWhiteSpace(item.Region))
                {
                    this.Database.HashSet(item.Region, item.Key, "regionKey", when, StackRedis.CommandFlags.FireAndForget);
                }
                
                var flags = sync ? StackRedis.CommandFlags.None : StackRedis.CommandFlags.FireAndForget;

                var result = this.Database.HashSet(fullKey, HashFieldValue, value, when, flags);

                // result from fire and forget is alwys false, this would leed to bad processing up the chain if we return false...
                result = flags == StackRedis.CommandFlags.FireAndForget ? true : result;

                // set the additional fields in case sliding expiration should be used
                // in this case we have to store the expiration mode and timeout on the hash, too
                // so that we can extend the expiration period every time we do a get
                if (result && metaValues != null)
                {
                    this.Database.HashSet(fullKey, metaValues, flags);
                }

                if (item.ExpirationMode != ExpirationMode.None)
                {
                    this.Database.KeyExpire(fullKey, item.ExpirationTimeout, StackRedis.CommandFlags.FireAndForget);
                }

                return result;
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
                        this.NotifyChannel(ChannelAction.Changed, key, region);
                        
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
                var hash = this.Database.HashGet(fullKey, new StackRedis.RedisValue[] { HashFieldValue, HashFieldExpirationMode });

                // the first item stores the value
                var item = hash[0];
                
                if (!item.HasValue || item.IsNullOrEmpty || item.IsNull)
                {
                    return null;
                }

                var expirationMode = ExpirationMode.None;
                var expirationTimeout = TimeSpan.Zero;

                // checking if the expiration mode is set on the hash
                var expirationModeItem = hash[1];
                if (expirationModeItem.HasValue)
                {
                    expirationMode = (ExpirationMode)(int)expirationModeItem;

                    // only sliding will be handled on get because we have to extend via EXPIRE
                    if (expirationMode == ExpirationMode.Sliding)
                    {
                        var timeoutItem = this.Database.HashGet(fullKey, HashFieldExpirationTimeout);
                        if (timeoutItem.HasValue)
                        {
                            expirationTimeout = TimeSpan.FromTicks((long)timeoutItem);
                        }
                    }
                }

                var value = FromRedisValue(item);
                
                var cacheItem = string.IsNullOrWhiteSpace(region) ?
                    new CacheItem<TCacheValue>(key, value) : 
                    new CacheItem<TCacheValue>(key, value, region);

                if (expirationMode == ExpirationMode.Sliding && expirationTimeout != default(TimeSpan))
                {
                    cacheItem = string.IsNullOrWhiteSpace(region) ?
                        new CacheItem<TCacheValue>(key, value, expirationMode, expirationTimeout) : 
                        new CacheItem<TCacheValue>(key, value, region, expirationMode, expirationTimeout);

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
                
                if (result)
                {
                    this.NotifyChannel(ChannelAction.Removed, key, region);
                }

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

        private void Connect()
        {
            if (string.IsNullOrWhiteSpace(this.RedisConfiguration.ConnectionString))
            {
                var options = this.CreateTargetOptions();
                this.connection = StackRedis.ConnectionMultiplexer.Connect(options);
            }
            else
            {
                this.connection = StackRedis.ConnectionMultiplexer.Connect(this.RedisConfiguration.ConnectionString);
            }
            
            this.Subscribe();
        }

        private StackRedis.ConfigurationOptions CreateTargetOptions()
        {
            var redisConfiguration = this.RedisConfiguration;
            var configurationOptions = new StackRedis.ConfigurationOptions()
            {
                AllowAdmin = redisConfiguration.AllowAdmin,
                ConnectTimeout = redisConfiguration.ConnectionTimeout,
                Password = redisConfiguration.Password,
                Ssl = redisConfiguration.IsSsl,
                SslHost = redisConfiguration.SslHost,
                ConnectRetry = 100,
                AbortOnConnectFail = false,
            };

            foreach (var endpoint in redisConfiguration.Endpoints)
            {
                configurationOptions.EndPoints.Add(endpoint.Host, endpoint.Port);
            }

            return configurationOptions;
        }
        
        private void Subscribe()
        {
            this.Subscriber.Subscribe(this.channelName, (channel, msg) =>
            {
                if (this.BackPlate == null)
                {
                    return;
                }

                if (((string)msg).StartsWith(this.Identifier.ToString()))
                {
                    // do not notify ourself (might be faster than the second method?
                    return;
                }

                var message = ChannelMessage.FromMsg(msg);
                if (message.IdentOwner == this.Identifier)
                {
                    // do not notify ourself
                    return;
                }

                //Trace.WriteLine(this.Identifier + " receiving: " + msg);

                switch (message.Action)
                {
                    case ChannelAction.Clear:
                        this.BackPlate.NotifyClear();
                        break;
                    case ChannelAction.ClearRegion:
                        this.BackPlate.NotifyClearRegion(message.Region);
                        break;
                    case ChannelAction.Changed:
                        if (string.IsNullOrWhiteSpace(message.Region))
                        {
                            this.BackPlate.NotifyChange(message.Key);
                        }
                        else
                        {
                            this.BackPlate.NotifyChange(message.Key, message.Region);
                        }
                        break;
                    case ChannelAction.Removed:
                        if (string.IsNullOrWhiteSpace(message.Region))
                        {
                            this.BackPlate.NotifyRemove(message.Key);
                        }
                        else
                        {
                            this.BackPlate.NotifyRemove(message.Key, message.Region);
                        }
                        break;
                }

            }, StackRedis.CommandFlags.FireAndForget);
        }

        private void NotifyChannel(ChannelAction action, string key, string region)
        {
            if (this.BackPlate == null)
            {
                return;
            }

            ChannelMessage message;
            if (action == ChannelAction.Clear)
            {
                message = new ChannelMessage(this.Identifier, ChannelAction.Clear);
            }
            else if (action == ChannelAction.ClearRegion)
            {
                message = new ChannelMessage(this.Identifier, ChannelAction.ClearRegion)
                {
                    Region = region
                };
            }
            else if (string.IsNullOrWhiteSpace(region))
            {
                message = new ChannelMessage(this.Identifier, action, key);
            }
            else
            {
                message = new ChannelMessage(this.Identifier, action, key, region);
            }

            var msg = message.ToMsg();
            //Trace.WriteLine(this.Identifier + " sending: " + msg);

            this.Subscriber.Publish(this.channelName, msg, StackRedis.CommandFlags.FireAndForget);
        }

        private T Retry<T>(Func<T> retryme)
        {
            var tries = 0;
            do
            {
                try
                {
                    return retryme();
                }
                catch (StackRedis.RedisConnectionException)
                {
                    Task.Delay(this.Manager.Configuration.RetryTimeout).Wait();
                }
                catch (AggregateException ag)
                {
                    ag.Handle(e =>
                    {
                        if (e is StackRedis.RedisConnectionException)
                        {
                            Task.Delay(this.Manager.Configuration.RetryTimeout).Wait();
                            return true;
                        }

                        return false;
                    });
                }
            } while (tries < this.Manager.Configuration.MaxRetries);

            return default(T);
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
            
            return (StackRedis.RedisValue)ToBytes(value);
        }

        private static TCacheValue FromRedisValue(StackRedis.RedisValue value)
        {
            if (value.IsNull || value.IsNullOrEmpty || !value.HasValue)
            {
                return default(TCacheValue);
            }
                        
            if(valueConverter != null)
            {
                return valueConverter.FromRedisValue(value);
            }

            return FromBytes(value);
        }

        //private static string ToJson(TCacheValue value)
        //{
        //    return JsonConvert.SerializeObject(value);
        //}

        //private static TCacheValue FromJson(string jsonString)
        //{
        //    return JsonConvert.DeserializeObject<TCacheValue>(
        //        jsonString, new CacheItemConverter<TCacheValue>());
        //}

        private static byte[] ToBytes(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                binaryFormatter.Serialize(memoryStream, obj);
                byte[] objectDataAsStream = memoryStream.ToArray();
                return objectDataAsStream;
            }
        }

        private static TCacheValue FromBytes(byte[] stream)
        {
            if (stream == null)
            {
                return default(TCacheValue);
            }

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream(stream))
            {
                return (TCacheValue)binaryFormatter.Deserialize(memoryStream);
            }
        }
    }
    
    internal enum ChannelAction
    {
        Removed,
        Changed,
        Clear,
        ClearRegion
    }

    internal class ChannelMessage
    {
        public ChannelMessage(Guid owner, ChannelAction action)
        {
            this.IdentOwner = owner;
            this.Action = action;
        }

        public ChannelMessage(Guid owner, ChannelAction action, string key)
            : this(owner, action)
        {
            this.Key = key;
        }

        public ChannelMessage(Guid owner, ChannelAction action, string key, string region)
            : this(owner, action, key)
        {
            this.Region = region;
        }

        public static ChannelMessage FromMsg(string msg)
        {
            var tokens = msg.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);

            var ident = Guid.Parse(tokens[0]);
            var action = (ChannelAction)Enum.Parse(typeof(ChannelAction), tokens[1]);

            if (action == ChannelAction.Clear)
            {
                return new ChannelMessage(ident, ChannelAction.Clear);
            }
            else if (action == ChannelAction.ClearRegion)
            {
                return new ChannelMessage(ident, ChannelAction.ClearRegion) { Region = Decode(tokens[2]) };
            }
            else if (tokens.Length == 3)
            {
                return new ChannelMessage(ident, action, Decode(tokens[2]));
            }

            return new ChannelMessage(ident, action, Decode(tokens[2]), Decode(tokens[3]));
        }

        public string ToMsg()
        {
            var action = Enum.GetName(typeof(ChannelAction), this.Action);
            if (this.Action == ChannelAction.Clear)
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}:{1}", this.IdentOwner.ToString(), action); 

            }
            else if (this.Action == ChannelAction.ClearRegion)
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}:{1}:{2}", this.IdentOwner.ToString(), action, Encode(this.Region)); 
            }
            else if (string.IsNullOrWhiteSpace(this.Region))
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}:{1}:{2}", this.IdentOwner.ToString(), action, Encode(this.Key));
            }

            return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}:{1}:{2}:{3}", this.IdentOwner.ToString(), action, Encode(this.Key), Encode(this.Region));
        }

        private static string Encode(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }

        private static string Decode(string value)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }

        public Guid IdentOwner { get; set; }

        public ChannelAction Action { get; set; }

        public string Key { get; set; }

        public string Region { get; set; }
    }

    // this looks strange but yes it has a reason.
    // I could use a serializer to get and set values from redis
    // but using the "native" supported types from Stackexchange.Redis, instead of using a 
    // serializer is up to 10x faster... so its more than worth the effort...

    // basically I have to cast the values to and from RedisValue which has implicit conversions to/from
    // those types defined internally...
    // I cannot simply cast to my TCacheValue, because its generic, and not defined as class or struct or anything...
    // so there is basically no other way

    internal interface IRedisValueConverter<T>
    {
        StackRedis.RedisValue ToRedisValue(T value);
        T FromRedisValue(StackRedis.RedisValue value);
    }

    internal class RedisValueConverter : 
        IRedisValueConverter<byte[]>,
        IRedisValueConverter<string>, 
        IRedisValueConverter<int>,
        IRedisValueConverter<int?>,
        IRedisValueConverter<double>,
        IRedisValueConverter<double?>,
        IRedisValueConverter<bool>,
        IRedisValueConverter<bool?>,
        IRedisValueConverter<long>,
        IRedisValueConverter<long?>
    {
        StackRedis.RedisValue IRedisValueConverter<byte[]>.ToRedisValue(byte[] value)
        {
            return value;
        }

        byte[] IRedisValueConverter<byte[]>.FromRedisValue(StackRedis.RedisValue value)
        {
            return value;
        }

        StackRedis.RedisValue IRedisValueConverter<string>.ToRedisValue(string value)
        {
            return value;
        }

        string IRedisValueConverter<string>.FromRedisValue(StackRedis.RedisValue value)
        {
            return value;
        }

        StackRedis.RedisValue IRedisValueConverter<int>.ToRedisValue(int value)
        {
            return value;
        }

        int IRedisValueConverter<int>.FromRedisValue(StackRedis.RedisValue value)
        {
            return (int)value;
        }

        StackRedis.RedisValue IRedisValueConverter<int?>.ToRedisValue(int? value)
        {
            return value;
        }

        int? IRedisValueConverter<int?>.FromRedisValue(StackRedis.RedisValue value)
        {
            return (int?)value;
        }

        StackRedis.RedisValue IRedisValueConverter<double>.ToRedisValue(double value)
        {
            return value;
        }

        double IRedisValueConverter<double>.FromRedisValue(StackRedis.RedisValue value)
        {
            return (double)value;
        }

        StackRedis.RedisValue IRedisValueConverter<double?>.ToRedisValue(double? value)
        {
            return value;
        }

        double? IRedisValueConverter<double?>.FromRedisValue(StackRedis.RedisValue value)
        {
            return (double?)value;
        }

        StackRedis.RedisValue IRedisValueConverter<bool>.ToRedisValue(bool value)
        {
            return value;
        }

        bool IRedisValueConverter<bool>.FromRedisValue(StackRedis.RedisValue value)
        {
            return (bool)value;
        }

        StackRedis.RedisValue IRedisValueConverter<bool?>.ToRedisValue(bool? value)
        {
            return value;
        }

        bool? IRedisValueConverter<bool?>.FromRedisValue(StackRedis.RedisValue value)
        {
            return (bool?)value;
        }

        StackRedis.RedisValue IRedisValueConverter<long>.ToRedisValue(long value)
        {
            return value;
        }

        long IRedisValueConverter<long>.FromRedisValue(StackRedis.RedisValue value)
        {
            return (long)value;
        }

        StackRedis.RedisValue IRedisValueConverter<long?>.ToRedisValue(long? value)
        {
            return value;
        }

        long? IRedisValueConverter<long?>.FromRedisValue(StackRedis.RedisValue value)
        {
            return (long?)value;
        }
    }
}