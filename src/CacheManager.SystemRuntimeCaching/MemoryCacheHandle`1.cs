using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Runtime.Caching;
using System.Text.RegularExpressions;
using CacheManager.Core;
using CacheManager.Core.Cache;
using CacheManager.Core.Configuration;

namespace CacheManager.SystemRuntimeCaching
{
    /// <summary>
    /// Simple implementation for the <see cref="System.Runtime.Caching.MemoryCache"/>.
    /// </summary>
    /// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
    /// <remarks>
    /// Although the MemoryCache doesn't support regions nor a RemoveAll/Clear method, we will
    /// implement it via cache dependencies.
    /// </remarks>
    public class MemoryCacheHandle<TCacheValue> : BaseCacheHandle<TCacheValue>
    {
        private const string DefaultName = "default";

        // can be default or any other name
        private readonly string cacheName = string.Empty;

        private volatile MemoryCache cache = null;
        private string instanceKey = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryCacheHandle{TCacheValue}"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="configuration">The configuration.</param>
        public MemoryCacheHandle(ICacheManager<TCacheValue> manager, ICacheHandleConfiguration configuration)
            : base(manager, configuration)
        {
            cacheName = this.Configuration.HandleName;

            if (cacheName.ToUpper(CultureInfo.InvariantCulture).Equals(DefaultName.ToUpper(CultureInfo.InvariantCulture)))
            {
                this.cache = System.Runtime.Caching.MemoryCache.Default;
            }
            else
            {
                this.cache = new System.Runtime.Caching.MemoryCache(cacheName);
            }

            instanceKey = Guid.NewGuid().ToString();

            CreateInstanceToken();
        }

        /// <summary>
        /// Gets the cache settings.
        /// </summary>
        /// <value>The cache settings.</value>
        public NameValueCollection CacheSettings
        {
            get
            {
                return GetSettings(this.cache);
            }
        }

        /// <summary>
        /// Gets the number of items the cache handle currently maintains.
        /// </summary>
        /// <value>The count.</value>
        public override int Count
        {
            get { return (int)this.cache.GetCount(); }
        }

        /// <summary>
        /// Clears this cache, removing all items in the base cache and all regions.
        /// </summary>
        public override void Clear()
        {
            this.cache.Remove(this.instanceKey);
            CreateInstanceToken();
        }

        /// <summary>
        /// Clears the cache region, removing all items from the specified <paramref name="region"/> only.
        /// </summary>
        /// <param name="region">The cache region.</param>
        public override void ClearRegion(string region)
        {
            cache.Remove(GetRegionTokenKey(region));
        }

        /// <summary>
        /// Adds a value to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
        /// </returns>
        protected override bool AddInternalPrepared(CacheItem<TCacheValue> item)
        {
            var key = GetItemKey(item);

            if (this.cache.Contains(key))
            {
                return false;
            }

            CacheItemPolicy policy = GetPolicy(item);
            return cache.Add(key, item, policy);
        }

        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key)
        {
            return GetCacheItemInternal(key, null);
        }

        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key, string region)
        {
            string fullKey = GetItemKey(key, region);
            var item = cache.Get(fullKey) as CacheItem<TCacheValue>;

            if (item == null)
            {
                return null;
            }

            // maybe the item is already expired because MemoryCache implements a default interval
            // of 20 seconds! to check for expired items on each store, we do it on access to also
            // reflect smaller time frames especially for sliding expiration...
            if (IsExpired(item))
            {
                // if so remove it
                this.RemoveInternal(item.Key, item.Region);
                return null;
            }

            if (item.ExpirationMode == ExpirationMode.Sliding)
            {
                // because we don't use UpdateCallback because of some multithreading issues lets
                // try to simply reset the item by setting it again.
                item = this.GetItemExpiration(item);
                cache.Set(fullKey, item, GetPolicy(item));
            }

            return item;
        }

        /// <summary>
        /// Puts the <paramref name="item"/> into the cache. If the item exists it will get updated
        /// with the new value. If the item doesn't exist, the item will be added to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        protected override void PutInternalPrepared(CacheItem<TCacheValue> item)
        {
            var key = GetItemKey(item);
            CacheItemPolicy policy = GetPolicy(item);
            cache.Set(key, item, policy);
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
            return RemoveInternal(key, null);
        }

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
            var fullKey = this.GetItemKey(key, region);
            var obj = cache.Remove(fullKey);

            return obj != null;
        }

        private static NameValueCollection GetSettings(MemoryCache instance)
        {
            var cacheCfg = new NameValueCollection();

            cacheCfg.Add("CacheMemoryLimitMegabytes", (instance.CacheMemoryLimit / 1024 / 1024).ToString(CultureInfo.InvariantCulture));
            cacheCfg.Add("PhysicalMemoryLimitPercentage", (instance.PhysicalMemoryLimit).ToString(CultureInfo.InvariantCulture));
            cacheCfg.Add("PollingInterval", (instance.PollingInterval).ToString());

            return cacheCfg;
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

            return false;
        }

        private void CreateInstanceToken()
        {
            // don't add a new key while we are disposing our instance
            if (!this.Disposing)
            {
                var instanceItem = new CacheItem<string>(instanceKey, instanceKey);
                CacheItemPolicy policy = new CacheItemPolicy()
                {
                    Priority = CacheItemPriority.Default,
                    RemovedCallback = new CacheEntryRemovedCallback(InstanceTokenRemoved),
                    AbsoluteExpiration = System.Runtime.Caching.ObjectCache.InfiniteAbsoluteExpiration,
                    SlidingExpiration = System.Runtime.Caching.ObjectCache.NoSlidingExpiration,
                };

                this.cache.Add(instanceItem.Key, instanceItem, policy);
            }
        }

        private void CreateRegionToken(string region)
        {
            var key = GetRegionTokenKey(region);
            // add region token with dependency on our instance token, so that all regions get
            // removed whenever the instance gets cleared.
            CacheItemPolicy policy = new CacheItemPolicy()
            {
                Priority = CacheItemPriority.Default,
                AbsoluteExpiration = System.Runtime.Caching.ObjectCache.InfiniteAbsoluteExpiration,
                SlidingExpiration = System.Runtime.Caching.ObjectCache.NoSlidingExpiration,
                ChangeMonitors = { cache.CreateCacheEntryChangeMonitor(new[] { instanceKey }) },
            };
            this.cache.Add(key, region, policy);
        }

        private string GetItemKey(CacheItem<TCacheValue> item)
        {
            return GetItemKey(item.Key, item.Region);
        }

        private string GetItemKey(string key, string region = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }

            if (string.IsNullOrWhiteSpace(region))
            {
                return instanceKey + ":" + key;
            }

            key = key.Replace("@", "!!").Replace(":", "!!");
            return instanceKey + "@" + region + ":" + key;
        }

        private CacheItemPolicy GetPolicy(CacheItem<TCacheValue> item)
        {
            var monitorKeys = new[] { instanceKey };

            if (!string.IsNullOrWhiteSpace(item.Region))
            {
                // this should be the only place to create the region token if it doesn't exist it
                // might got removed by clearRegion but next time put or add gets called, the region
                // should be re added...
                var key = GetRegionTokenKey(item.Region);
                if (!this.cache.Contains(key))
                {
                    CreateRegionToken(item.Region);
                }
                monitorKeys = new[] { instanceKey, key };
            }

            var policy = new CacheItemPolicy()
            {
                Priority = CacheItemPriority.Default,
                ChangeMonitors = { cache.CreateCacheEntryChangeMonitor(monitorKeys) },
                AbsoluteExpiration = System.Runtime.Caching.ObjectCache.InfiniteAbsoluteExpiration,
                SlidingExpiration = System.Runtime.Caching.ObjectCache.NoSlidingExpiration,
            };

            if (item.ExpirationMode == ExpirationMode.Absolute)
            {
                policy.AbsoluteExpiration = new DateTimeOffset(DateTime.UtcNow.Add(item.ExpirationTimeout));
                policy.RemovedCallback = new CacheEntryRemovedCallback(ItemRemoved);
            }

            if (item.ExpirationMode == ExpirationMode.Sliding)
            {
                policy.SlidingExpiration = item.ExpirationTimeout;
                policy.RemovedCallback = new CacheEntryRemovedCallback(ItemRemoved);
                // for some reason, we'll get issues with multithreading if we set this...
                //policy.UpdateCallback = new CacheEntryUpdateCallback(ItemUpdated); // must be set, otherwise sliding doesn't work at all.
            }

            item.LastAccessedUtc = DateTime.UtcNow;

            return policy;
        }

        private string GetRegionTokenKey(string region)
        {
            var key = string.Concat(this.instanceKey, "@", region);
            return key;
        }

        private void InstanceTokenRemoved(CacheEntryRemovedArguments arguments)
        {
            instanceKey = Guid.NewGuid().ToString();
        }

        private void ItemRemoved(CacheEntryRemovedArguments arguments)
        {
            var key = arguments.CacheItem.Key;
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            // only handle auto removes
            if (arguments.RemovedReason != CacheEntryRemovedReason.Expired)
            {
                return;
            }

            // identify item keys and ignore region or instance key
            if (key.Contains(":"))
            {
                if (key.Contains("@"))
                {
                    // example instanceKey@region:itemkey , instanceKey:itemKey
                    var region = Regex.Match(key, "@(.+?):").Groups[1].Value;
                    this.Stats.OnRemove(region);
                }
                else
                {
                    this.Stats.OnRemove();
                }
            }
        }
    }
}