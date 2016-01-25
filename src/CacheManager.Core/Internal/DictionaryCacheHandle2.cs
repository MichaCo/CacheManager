using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CacheManager.Core.Logging;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core.Internal
{
    /// <summary>
    /// This handle is for internal use and testing. It does not implement any expiration.
    /// </summary>
    /// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
    public class DictionaryCacheHandle2<TCacheValue> : BaseCacheHandle<TCacheValue>
    {
        private readonly IDictionary<string, CacheItem<TCacheValue>> cache;
        private readonly object cacheWriteLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="DictionaryCacheHandle2{TCacheValue}"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="configuration">The configuration.</param>
        public DictionaryCacheHandle2(ICacheManager<TCacheValue> manager, CacheHandleConfiguration configuration)
            : base(manager, configuration)
        {
            NotNull(manager, nameof(manager));
            this.Logger = manager.Configuration.LoggerFactory.CreateLogger(this);
            this.cache = new Dictionary<string, CacheItem<TCacheValue>>();
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>The count.</value>
        public override int Count => this.cache.Count;

        /// <inheritdoc />
        protected override ILogger Logger { get; }

        /// <summary>
        /// Clears this cache, removing all items in the base cache and all regions.
        /// </summary>
        public override void Clear()
        {
            lock (this.cacheWriteLock)
            {
                this.cache.Clear();
            }
        }

        /// <summary>
        /// Clears the cache region, removing all items from the specified <paramref name="region"/> only.
        /// </summary>
        /// <param name="region">The cache region.</param>
        /// <exception cref="System.ArgumentNullException">If region is null.</exception>
        public override void ClearRegion(string region)
        {
            NotNullOrWhiteSpace(region, nameof(region));

            var regionKey = string.Concat(region, ":");
            lock (this.cacheWriteLock)
            {
                foreach (var item in this.cache
                    .Where(p => p.Key.StartsWith(regionKey, StringComparison.Ordinal))
                    .ToArray())
                {
                    this.cache.Remove(item.Key);
                }
            }
        }

        /// <summary>
        /// Adds a value to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">If item is null.</exception>
        protected override bool AddInternalPrepared(CacheItem<TCacheValue> item)
        {
            NotNull(item, nameof(item));

            var fullKey = GetKey(item.Key, item.Region);

            // quick exit
            if (this.cache.ContainsKey(fullKey))
            {
                return false;
            }

            lock (this.cacheWriteLock)
            {
                if (this.cache.ContainsKey(fullKey))
                {
                    return false;
                }

                this.cache.Add(fullKey, item);
                return true;
            }
        }

        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key) =>
            this.GetCacheItemInternal(key, null);

        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key, string region)
        {
            var fullKey = GetKey(key, region);

            CacheItem<TCacheValue> result = null;
            if (this.cache.TryGetValue(fullKey, out result))
            {
                if (IsExpired(result))
                {
                    this.RemoveInternal(key, region);
                    return null;
                }
            }

            return result;
        }

        /// <summary>
        /// Puts the <paramref name="item"/> into the cache. If the item exists it will get updated
        /// with the new value. If the item doesn't exist, the item will be added to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        /// <exception cref="System.ArgumentNullException">If item is null.</exception>
        protected override void PutInternalPrepared(CacheItem<TCacheValue> item)
        {
            NotNull(item, nameof(item));

            var fullKey = GetKey(item.Key, item.Region);

            this.cache[fullKey] = item;
        }

        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        protected override bool RemoveInternal(string key) => this.RemoveInternal(key, null);

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
            var fullKey = GetKey(key, region);

            lock (this.cacheWriteLock)
            {
                return this.cache.Remove(fullKey);
            }
        }

        /// <summary>
        /// Gets the key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        /// <returns>The full key.</returns>
        /// <exception cref="System.ArgumentException">If Key is empty.</exception>
        private static string GetKey(string key, string region)
        {
            NotNullOrWhiteSpace(key, nameof(key));

            if (string.IsNullOrWhiteSpace(region))
            {
                return key;
            }

            return string.Concat(region, ":", key);
        }

        private static bool IsExpired(CacheItem<TCacheValue> item)
        {
            var now = DateTime.UtcNow;
            if (item.ExpirationMode == ExpirationMode.Absolute
                && item.CreatedUtc.Add(item.ExpirationTimeout) < now)
            {
                return true;
            }
            else if (item.ExpirationMode == ExpirationMode.Sliding
                && item.LastAccessedUtc.Add(item.ExpirationTimeout) < now)
            {
                return true;
            }

            //// TODO: schedule cleanup
            return false;
        }
    }
}