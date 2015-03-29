using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;
using CacheManager.Core;
using CacheManager.Core.Cache;
using CacheManager.Core.Configuration;
using Couchbase;
using Couchbase.Configuration.Client;
using Couchbase.Core;
using Newtonsoft.Json.Linq;

namespace CacheManager.Couchbase
{
    public class BucketCacheHandle : BucketCacheHandle<object>
    {
        public BucketCacheHandle(ICacheManager<object> manager, ICacheHandleConfiguration configuration)
            : base(manager, configuration)
        {
        }
    }

    public class BucketCacheHandle<TCacheValue> : BaseCacheHandle<TCacheValue>
    {
        private readonly IBucket bucket;
        private readonly BucketConfiguration bucketConfiguration;
        private readonly ClientConfiguration configuration;
        private readonly string bucketName = "default";
        private readonly string configurationName = string.Empty;

        public BucketCacheHandle(ICacheManager<TCacheValue> manager, ICacheHandleConfiguration configuration)
            : base(manager, configuration)
        {
            // we can configure the bucket name by having "<configKey>:<bucketName>" as handle's name value
            var nameParts = configuration.HandleName.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
            if (nameParts.Length == 0)
            {
                throw new InvalidOperationException("Configuration error with the handle name " + configuration.HandleName);
            }

            this.configurationName = nameParts[0];
            
            if (nameParts.Length == 2)
            {
                this.bucketName = nameParts[1];
            }

            this.configuration = CouchbaseConfigurationManager.GetConfiguration(this.configurationName);
            this.bucketConfiguration = CouchbaseConfigurationManager.GetBucketConfiguration(this.configuration, this.bucketName);
            this.bucket = CouchbaseConfigurationManager.GetBucket(this.configuration, this.configurationName, this.bucketName);
        }

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
            }

            base.Dispose(disposeManaged);
        }

        public override int Count
        {
            get { return (int)this.Stats.GetStatistic(CacheStatsCounterType.Items); }
        }

        protected override bool AddInternalPrepared(CacheItem<TCacheValue> item)
        {
            var fullKey = this.GetKey(item.Key, item.Region);
            if (item.ExpirationMode != ExpirationMode.None)
            {
                return this.bucket.Insert(fullKey, item, item.ExpirationTimeout).Success;
            }

            return this.bucket.Insert(fullKey, item).Success;
        }

        protected override void PutInternalPrepared(CacheItem<TCacheValue> item)
        {
            var fullKey = this.GetKey(item.Key, item.Region);
            if (item.ExpirationMode != ExpirationMode.None)
            {
                this.bucket.Upsert(fullKey, item, item.ExpirationTimeout);
            }
            else
            {
                this.bucket.Upsert(fullKey, item);
            }            
        }

        public override void Clear()
        {
            // warning: takes ~20seconds to flush the bucket... thats rigged
            var manager = this.bucket.CreateManager(this.bucketConfiguration.Username, this.bucketConfiguration.Password);
            if (manager != null)
            {
                manager.Flush();
            }
        }

        public override void ClearRegion(string region)
        {
            // TODO: not supported?
            throw new NotImplementedException();
        }

        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key)
        {
            return this.GetCacheItemInternal(key, null);
        }

        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key, string region)
        {
            var fullkey = this.GetKey(key, region);
            var result = this.bucket.Get<CacheItem<TCacheValue>>(fullkey);
            
            //TODO: implement sliding expiration whenever the guys from couchbase actually implement that feature into that client...

            if (result.Success)
            {
                var cacheItem = result.Value;
                if (cacheItem.Value.GetType() == typeof(JValue))
                {
                    var value = cacheItem.Value as JValue;
                    cacheItem = cacheItem.WithValue((TCacheValue)value.ToObject(cacheItem.ValueType));
                } 
                else if (cacheItem.Value.GetType() == typeof(JObject))
                {
                    var value = cacheItem.Value as JObject;
                    cacheItem = cacheItem.WithValue((TCacheValue)value.ToObject(cacheItem.ValueType));
                }

                return cacheItem;
            }

            return null;
        }

        protected override bool RemoveInternal(string key)
        {
            return this.RemoveInternal(key, null);
        }

        protected override bool RemoveInternal(string key, string region)
        {
            var fullKey = this.GetKey(key, region);
            var result = this.bucket.Remove(fullKey);
            return result.Success;
        }

        private string GetKey(string key, string region = null)
        {
            var fullKey = key;

            if (!string.IsNullOrWhiteSpace(region))
            {
                fullKey = string.Concat(region, ":", key);
            }

            // Memcached still has a 250 character limit
            if (fullKey.Length >= 250)
            {
                return GetSHA256Key(fullKey);
            }

            return fullKey;
        }

        private static string GetSHA256Key(string key)
        {
            using (var sha = SHA256Managed.Create())
            {
                byte[] hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
                return Convert.ToBase64String(hashBytes);
            }
        }
    }
}