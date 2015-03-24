using System;
using System.Security.Cryptography;
using System.Text;
using CacheManager.Core;
using CacheManager.Core.Cache;
using CacheManager.Core.Configuration;
using Enyim.Caching;
using Enyim.Caching.Memcached;
using Enyim.Caching.Memcached.Results;

namespace CacheManager.Memcached
{
    public abstract class MemcachedClientHandle<TCacheValue> : BaseCacheHandle<TCacheValue>
    {
        protected MemcachedClient Cache { get; set; }

        public override int Count
        {
            get
            {
                return (int)this.Stats.GetStatistic(CacheStatsCounterType.Items);
            }
        }

        public ServerStats ServerStats
        {
            get
            {
                return this.Cache.Stats();
            }
        }

        public MemcachedClientHandle(ICacheManager<TCacheValue> manager, ICacheHandleConfiguration configuration)
            : base(manager, configuration)
        {
            if (!typeof(TCacheValue).IsSerializable)
            {
                throw new InvalidOperationException("To use memcached, the inner type must be serializable but " + typeof(TCacheValue).ToString() + " is not.");
            }
        }

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                this.Cache.Dispose();
            }

            base.Dispose(disposeManaged);
        }

        public override void Clear()
        {
            this.Cache.FlushAll();
        }

        public override void ClearRegion(string region)
        {
            // not supported, clearing all instead 
            // TODO: find workaround
            // this.Clear();
        }

        protected override bool AddInternalPrepared(CacheItem<TCacheValue> item)
        {
            return this.Store(StoreMode.Add, item).Success;
        }

        protected override void PutInternalPrepared(CacheItem<TCacheValue> item)
        {
            this.Store(StoreMode.Set, item);
        }

        public override UpdateItemResult Update(string key, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config)
        {
            return this.Update(key, null, updateValue, config);
        }

        public override UpdateItemResult Update(string key, string region, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config)
        {
            return this.Set(key, region, updateValue, config);
        }

        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key)
        {
            return GetCacheItemInternal(key, null);
        }

        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key, string region)
        {
            var item = this.Cache.Get(this.GetKey(key, region));
            return item as CacheItem<TCacheValue>;
        }

        protected override bool RemoveInternal(string key)
        {
            return RemoveInternal(key, null);
        }

        protected override bool RemoveInternal(string key, string region)
        {
            return this.Cache.Remove(this.GetKey(key, region));
        }

        protected virtual IStoreOperationResult Store(StoreMode mode, CacheItem<TCacheValue> item)
        {
            var key = this.GetKey(item.Key, item.Region);

            if (item.ExpirationMode == ExpirationMode.Absolute)
            {
                var timeoutDate = DateTime.Now.Add(item.ExpirationTimeout);
                var result = this.Cache.ExecuteStore(mode, key, item, timeoutDate);
                return result;
            }
            else if (item.ExpirationMode == ExpirationMode.Sliding)
            {
                var result = this.Cache.ExecuteStore(mode, key, item, item.ExpirationTimeout);
                return result;
            }
            else
            {
                var result = this.Cache.ExecuteStore(mode, key, item);
                return result;
            }
        }

        private UpdateItemResult Set(string key, string region, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config)
        {
            var fullyKey = this.GetKey(key, region);
            var tries = 0;
            IStoreOperationResult result;

            do
            {
                tries++;
                var getTries = 0;
                StatusCode getStatus;
                CacheItem<TCacheValue> item;
                CasResult<CacheItem<TCacheValue>> cas;
                do
                {
                    getTries++;
                    cas = this.Cache.GetWithCas<CacheItem<TCacheValue>>(fullyKey);

                    item = cas.Result;
                    getStatus = (StatusCode)cas.StatusCode;
                } while (ShouldRetry(getStatus) && getTries <= config.MaxRetries);

                // break operation if we cannot retrieve the object (maybe it has expired already).
                if (getStatus != StatusCode.Success || item == null)
                {
                    return new UpdateItemResult(tries > 1, false, tries);
                }

                item = item.WithValue(updateValue(item.Value));

                if (item.ExpirationMode == ExpirationMode.Absolute)
                {
                    var timeoutDate = item.ExpirationTimeout;
                    result = this.Cache.ExecuteCas(StoreMode.Set, fullyKey, item, timeoutDate, cas.Cas);
                }
                else if (item.ExpirationMode == ExpirationMode.Sliding)
                {
                    result = this.Cache.ExecuteCas(StoreMode.Set, fullyKey, item, item.ExpirationTimeout, cas.Cas);
                }
                else
                {
                    result = this.Cache.ExecuteCas(StoreMode.Set, fullyKey, item, cas.Cas);
                }


            } while (!result.Success && result.StatusCode.HasValue && result.StatusCode.Value == 2 && tries <= config.MaxRetries);

            return new UpdateItemResult(tries > 1, result.Success, tries);
        }

        private bool ShouldRetry(StatusCode statusCode)
        {
            switch (statusCode)
            {
                case StatusCode.NodeShutdown:
                case StatusCode.OperationTimeout:
                case StatusCode.OutOfMemory:
                case StatusCode.Busy:
                case StatusCode.SocketPoolTimeout:
                case StatusCode.UnableToLocateNode:
                case StatusCode.VBucketBelongsToAnotherServer:
                    return true;
            }

            return false;
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