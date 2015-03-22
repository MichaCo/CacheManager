using System;
using System.Collections.Concurrent;
using System.Linq;
using CacheManager.Core.Configuration;

namespace CacheManager.Core.Cache
{
    /// <summary>
    /// This handle is for internal use and testing.
    /// It does not implement any expiration.
    /// </summary>
    /// <typeparam name="TCacheValue"></typeparam>
    public class DictionaryCacheHandle<TCacheValue> : BaseCacheHandle<TCacheValue>
    {
        private ConcurrentDictionary<string, CacheItem<TCacheValue>> cache;

        public override int Count
        {
            get { return this.cache.Count; }
        }

        public DictionaryCacheHandle(ICacheManager<TCacheValue> manager, ICacheHandleConfiguration configuration)
            : base(manager, configuration)
        {
            this.cache = new ConcurrentDictionary<string, CacheItem<TCacheValue>>();
        }

        public override void Clear()
        {
            this.cache.Clear();
        }

        public override void ClearRegion(string region)
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                throw new ArgumentNullException("region");
            }

            var key = string.Concat(region, ":");
            foreach (var item in this.cache.Where(p => p.Key.StartsWith(key, StringComparison.Ordinal)))
            {
                CacheItem<TCacheValue> val = null;
                this.cache.TryRemove(item.Key, out val);
            }
        }

        protected override bool AddInternalPrepared(CacheItem<TCacheValue> item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            var key = GetKey(item.Key, item.Region);
            return this.cache.TryAdd(key, item);
        }

        protected override void PutInternalPrepared(CacheItem<TCacheValue> item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            
            this.cache[GetKey(item.Key, item.Region)] = item;
        }

        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key)
        {
            return this.GetCacheItemInternal(key, null);
        }

        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key, string region)
        {
            var fullKey = GetKey(key, region);

            CacheItem<TCacheValue> result = null;
            this.cache.TryGetValue(fullKey, out result);

            return result;
        }

        protected override bool RemoveInternal(string key)
        {
            return this.RemoveInternal(key, null);
        }

        protected override bool RemoveInternal(string key, string region)
        {
            var fullKey = GetKey(key, region);
            CacheItem<TCacheValue> val = null;
            return this.cache.TryRemove(fullKey, out val);
        }
        
        public override UpdateItemResult Update(string key, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config)
        {
            return base.Update(key, updateValue, config);
        }

        public override UpdateItemResult Update(string key, string region, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config)
        {
            if (updateValue == null)
            {
                throw new ArgumentNullException("updateValue");
            }
            
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            var retries = 0;
            do
            {
                var fullKey = GetKey(key, region);
                var item = this.GetCacheItemInternal(key, region);
                if (item == null)
                {
                    break;
                }

                var newValue = updateValue(item.Value);
                var newItem = item.WithValue(newValue);

                if (this.cache.TryUpdate(fullKey, newItem, item))
                {
                    return new UpdateItemResult(retries > 0, true, retries);
                }

                retries++;
            } while (retries <= config.MaxRetries);

            return new UpdateItemResult(retries > 0, false, retries);
        }
        
        private static string GetKey(string key, string region)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key should not be empty.", "key");
            }

            if (string.IsNullOrWhiteSpace(region))
            {
                return key;
            }

            return string.Concat(region, ":", key);
        }
    }
}