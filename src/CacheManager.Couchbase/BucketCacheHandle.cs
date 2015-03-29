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
        private IBucket bucket;
        private BucketConfiguration bucketConfiguration;
        private ClientConfiguration configuration;
        private string bucketName = "default";

        public BucketCacheHandle(ICacheManager<TCacheValue> manager, ICacheHandleConfiguration configuration)
            : base(manager, configuration)
        {
            this.configuration = CouchbaseConfigurationManager.GetConfiguration(configuration.HandleName);
            this.bucketConfiguration = CouchbaseConfigurationManager.GetBucketConfiguration(this.configuration, this.bucketName);            
            this.bucket = CouchbaseConfigurationManager.GetBucket(this.configuration, configuration.HandleName, this.bucketName);
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
            var result = this.bucket.Insert(fullKey, item);
            return result.Success;
        }

        protected override void PutInternalPrepared(CacheItem<TCacheValue> item)
        {
            var fullKey = this.GetKey(item.Key, item.Region);
            var result = this.bucket.Upsert(fullKey, item);
        }

        public override void Clear()
        {
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
            if (result.Success)
            {
                return result.Value;
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