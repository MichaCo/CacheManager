using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Runtime.Caching;
using System.Text.RegularExpressions;
using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.Core.Logging;
using static CacheManager.Core.Utility.Guard;

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
        /// <param name="managerConfiguration">The manager configuration.</param>
        /// <param name="configuration">The cache handle configuration.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public MemoryCacheHandle(ICacheManagerConfiguration managerConfiguration, CacheHandleConfiguration configuration, ILoggerFactory loggerFactory)
            : base(managerConfiguration, configuration)
        {
            NotNull(configuration, nameof(configuration));
            NotNull(loggerFactory, nameof(loggerFactory));

            this.Logger = loggerFactory.CreateLogger(this);
            this.cacheName = configuration.Name;

            if (this.cacheName.ToUpper(CultureInfo.InvariantCulture).Equals(DefaultName.ToUpper(CultureInfo.InvariantCulture)))
            {
                this.cache = MemoryCache.Default;
            }
            else
            {
                this.cache = new MemoryCache(this.cacheName);
            }

            this.instanceKey = Guid.NewGuid().ToString();

            this.CreateInstanceToken();
        }

        /// <summary>
        /// Gets the cache settings.
        /// </summary>
        /// <value>The cache settings.</value>
        public NameValueCollection CacheSettings => GetSettings(this.cache);

        /// <summary>
        /// Gets the number of items the cache handle currently maintains.
        /// </summary>
        /// <value>The count.</value>
        public override int Count => (int)this.cache.GetCount();

        /// <inheritdoc />
        protected override ILogger Logger { get; }

        /// <summary>
        /// Clears this cache, removing all items in the base cache and all regions.
        /// </summary>
        public override void Clear()
        {
            this.cache.Remove(this.instanceKey);
            this.CreateInstanceToken();
        }

        /// <summary>
        /// Clears the cache region, removing all items from the specified <paramref name="region"/> only.
        /// </summary>
        /// <param name="region">The cache region.</param>
        public override void ClearRegion(string region) =>
            this.cache.Remove(this.GetRegionTokenKey(region));

        /// <inheritdoc />
        public override bool Exists(string key)
        {
            return this.cache.Contains(this.GetItemKey(key));
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
            var key = this.GetItemKey(item);

            if (this.cache.Contains(key))
            {
                return false;
            }

            CacheItemPolicy policy = this.GetPolicy(item);
            return this.cache.Add(key, item, policy);
        }

        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key) => this.GetCacheItemInternal(key, null);

        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key, string region)
        {
            string fullKey = this.GetItemKey(key, region);
            var item = this.cache.Get(fullKey) as CacheItem<TCacheValue>;

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
                this.cache.Set(fullKey, item, this.GetPolicy(item));
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
            var key = this.GetItemKey(item);
            CacheItemPolicy policy = this.GetPolicy(item);
            this.cache.Set(key, item, policy);
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
            var fullKey = this.GetItemKey(key, region);
            var obj = this.cache.Remove(fullKey);

            return obj != null;
        }

        private static NameValueCollection GetSettings(MemoryCache instance)
        {
            var cacheCfg = new NameValueCollection();

            cacheCfg.Add("CacheMemoryLimitMegabytes", (instance.CacheMemoryLimit / 1024 / 1024).ToString(CultureInfo.InvariantCulture));
            cacheCfg.Add("PhysicalMemoryLimitPercentage", instance.PhysicalMemoryLimit.ToString(CultureInfo.InvariantCulture));
            cacheCfg.Add("PollingInterval", instance.PollingInterval.ToString());

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
                var instanceItem = new CacheItem<string>(this.instanceKey, this.instanceKey);
                CacheItemPolicy policy = new CacheItemPolicy()
                {
                    Priority = CacheItemPriority.NotRemovable,
                    RemovedCallback = new CacheEntryRemovedCallback(this.InstanceTokenRemoved),
                    AbsoluteExpiration = System.Runtime.Caching.ObjectCache.InfiniteAbsoluteExpiration,
                    SlidingExpiration = System.Runtime.Caching.ObjectCache.NoSlidingExpiration,
                };

                this.cache.Add(instanceItem.Key, instanceItem, policy);
            }
        }

        private void CreateRegionToken(string region)
        {
            var key = this.GetRegionTokenKey(region);

            // add region token with dependency on our instance token, so that all regions get
            // removed whenever the instance gets cleared.
            CacheItemPolicy policy = new CacheItemPolicy()
            {
                Priority = CacheItemPriority.NotRemovable,
                AbsoluteExpiration = System.Runtime.Caching.ObjectCache.InfiniteAbsoluteExpiration,
                SlidingExpiration = System.Runtime.Caching.ObjectCache.NoSlidingExpiration,
                ChangeMonitors = { this.cache.CreateCacheEntryChangeMonitor(new[] { this.instanceKey }) },
            };
            this.cache.Add(key, region, policy);
        }

        private string GetItemKey(CacheItem<TCacheValue> item) => this.GetItemKey(item?.Key, item?.Region);

        private string GetItemKey(string key, string region = null)
        {
            NotNullOrWhiteSpace(key, nameof(key));

            if (string.IsNullOrWhiteSpace(region))
            {
                return this.instanceKey + ":" + key;
            }

            region = region.Replace("@", "!!").Replace(":", "!!");
            return this.instanceKey + "@" + region + ":" + key;
        }

        private CacheItemPolicy GetPolicy(CacheItem<TCacheValue> item)
        {
            var monitorKeys = new[] { this.instanceKey };

            if (!string.IsNullOrWhiteSpace(item.Region))
            {
                // this should be the only place to create the region token if it doesn't exist it
                // might got removed by clearRegion but next time put or add gets called, the region
                // should be re added...
                var key = this.GetRegionTokenKey(item.Region);
                if (!this.cache.Contains(key))
                {
                    this.CreateRegionToken(item.Region);
                }
                monitorKeys = new[] { this.instanceKey, key };
            }

            var policy = new CacheItemPolicy()
            {
                Priority = CacheItemPriority.Default,
                ChangeMonitors = { this.cache.CreateCacheEntryChangeMonitor(monitorKeys) },
                AbsoluteExpiration = System.Runtime.Caching.ObjectCache.InfiniteAbsoluteExpiration,
                SlidingExpiration = System.Runtime.Caching.ObjectCache.NoSlidingExpiration,
            };

            if (item.ExpirationMode == ExpirationMode.Absolute)
            {
                policy.AbsoluteExpiration = new DateTimeOffset(DateTime.UtcNow.Add(item.ExpirationTimeout));
                policy.RemovedCallback = new CacheEntryRemovedCallback(this.ItemRemoved);
            }

            if (item.ExpirationMode == ExpirationMode.Sliding)
            {
                policy.SlidingExpiration = item.ExpirationTimeout;
                policy.RemovedCallback = new CacheEntryRemovedCallback(this.ItemRemoved);

                //// for some reason, we'll get issues with multithreading if we set this...
                //// see http://stackoverflow.com/questions/21680429/why-does-memorycache-throw-nullreferenceexception
                ////policy.UpdateCallback = new CacheEntryUpdateCallback(ItemUpdated); // must be set, otherwise sliding doesn't work at all.
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
            this.instanceKey = Guid.NewGuid().ToString();
        }

        private void ItemRemoved(CacheEntryRemovedArguments arguments)
        {
            var key = arguments.CacheItem.Key;
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            // ignore manual removes, stats will be updated already
            if (arguments.RemovedReason == CacheEntryRemovedReason.Removed)
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