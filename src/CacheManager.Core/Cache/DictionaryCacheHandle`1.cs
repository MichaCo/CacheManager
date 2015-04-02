using System;
using System.Collections.Concurrent;
using System.Linq;
using CacheManager.Core.Configuration;

namespace CacheManager.Core.Cache
{
    /// <summary>
    /// This handle is for internal use and testing. It does not implement any expiration.
    /// </summary>
    /// <typeparam name="TCacheValue"></typeparam>
    public class DictionaryCacheHandle<TCacheValue> : BaseCacheHandle<TCacheValue>
    {
        private ConcurrentDictionary<string, CacheItem<TCacheValue>> cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="DictionaryCacheHandle{TCacheValue}"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="configuration">The configuration.</param>
        public DictionaryCacheHandle(ICacheManager<TCacheValue> manager, ICacheHandleConfiguration configuration)
            : base(manager, configuration)
        {
            this.cache = new ConcurrentDictionary<string, CacheItem<TCacheValue>>();
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>The count.</value>
        public override int Count
        {
            get { return this.cache.Count; }
        }

        /// <summary>
        /// Clears this cache, removing all items in the base cache and all regions.
        /// </summary>
        public override void Clear()
        {
            this.cache.Clear();
        }

        /// <summary>
        /// Clears the cache region, removing all items from the specified <paramref name="region"/> only.
        /// </summary>
        /// <param name="region">The cache region.</param>
        /// <exception cref="System.ArgumentNullException">If region is null.</exception>
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

        /// <summary>
        /// Updates the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="updateValue">The update value.</param>
        /// <param name="config">The configuration.</param>
        /// <returns>The result.</returns>
        public override UpdateItemResult Update(string key, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config)
        {
            return base.Update(key, updateValue, config);
        }

        /// <summary>
        /// Updates the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        /// <param name="updateValue">The update value.</param>
        /// <param name="config">The configuration.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">If updateValue or config is null</exception>
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
            }
            while (retries <= config.MaxRetries);

            return new UpdateItemResult(retries > 0, false, retries);
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

        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key)
        {
            return this.GetCacheItemInternal(key, null);
        }

        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region"></param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key, string region)
        {
            var fullKey = GetKey(key, region);

            CacheItem<TCacheValue> result = null;
            this.cache.TryGetValue(fullKey, out result);

            return result;
        }

        protected override void PutInternalPrepared(CacheItem<TCacheValue> item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            this.cache[GetKey(item.Key, item.Region)] = item;
        }

        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        protected override bool RemoveInternal(string key)
        {
            return this.RemoveInternal(key, null);
        }

        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region"></param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        protected override bool RemoveInternal(string key, string region)
        {
            var fullKey = GetKey(key, region);
            CacheItem<TCacheValue> val = null;
            return this.cache.TryRemove(fullKey, out val);
        }

        /// <summary>
        /// Gets the full key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Key should not be empty.;key</exception>
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