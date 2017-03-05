using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Runtime.Caching;
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
        private string instanceKey;
        private int instanceKeyLength;

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
            this.instanceKeyLength = this.instanceKey.Length;
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

        /// <inheritdoc />
        public override bool Exists(string key, string region)
        {
            NotNullOrWhiteSpace(region, nameof(region));
            var fullKey = this.GetItemKey(key, region);
            return this.cache.Contains(fullKey);
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
            // cache.Get eventually triggers eviction callback, but just in case...
            if (item.IsExpired)
            {
                this.RemoveInternal(item.Key, item.Region);
                this.TriggerCacheSpecificRemove(item.Key, item.Region, CacheItemRemovedReason.Expired);
                return null;
            }

            if (item.ExpirationMode == ExpirationMode.Sliding)
            {
                // because we don't use UpdateCallback because of some multithreading issues lets
                // try to simply reset the item by setting it again.
                // item = this.GetItemExpiration(item); // done via base cache handle
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

        private CacheItemPolicy GetPolicy(CacheItem<TCacheValue> item)
        {
            var monitorKeys = new[] { this.instanceKey };

            if (!string.IsNullOrWhiteSpace(item.Region))
            {
                // this should be the only place to create the region token if it doesn't exist it
                // might got removed by clearRegion but next time put or add gets called, the region
                // should be re added...
                var regionToken = this.GetRegionTokenKey(item.Region);
                if (!this.cache.Contains(regionToken))
                {
                    this.CreateRegionToken(item.Region);
                }
                monitorKeys = new[] { this.instanceKey, regionToken };
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

        private string GetItemKey(CacheItem<TCacheValue> item) => this.GetItemKey(item?.Key, item?.Region);

        private string GetItemKey(string key, string region = null)
        {
            NotNullOrWhiteSpace(key, nameof(key));

            if (string.IsNullOrWhiteSpace(region))
            {
                return this.instanceKey + ":" + key;
            }

            // key without region
            // <instance>:key
            // key with region
            // <instance>@<regionlen><regionstring>:<keystring>
            // <instance>@6region:key
            return string.Concat(this.instanceKey, "@", region.Length, "@", region, ":", key);
        }

        private string GetRegionTokenKey(string region)
        {
            var key = string.Concat(this.instanceKey, "_", region);
            return key;
        }

        private void InstanceTokenRemoved(CacheEntryRemovedArguments arguments)
        {
            this.instanceKey = Guid.NewGuid().ToString();
            this.instanceKeyLength = this.instanceKey.Length;
        }

        private void ItemRemoved(CacheEntryRemovedArguments arguments)
        {
            var fullKey = arguments.CacheItem.Key;
            if (string.IsNullOrWhiteSpace(fullKey))
            {
                return;
            }

            // ignore manual removes, stats will be updated already
            if (arguments.RemovedReason == CacheEntryRemovedReason.Removed)
            {
                return;
            }

            // root@region:key;
            // root@key;

            bool isToken; bool hasRegion; string key; string region;
            ParseKeyParts(this.instanceKeyLength, fullKey, out isToken, out hasRegion, out region, out key);

            if (!isToken)
            {
                if (hasRegion)
                {
                    this.Stats.OnRemove(region);
                }
                else
                {
                    this.Stats.OnRemove();
                }

                // trigger cachemanager's remove on evicted and expired items
                if (arguments.RemovedReason == CacheEntryRemovedReason.Evicted || arguments.RemovedReason == CacheEntryRemovedReason.CacheSpecificEviction)
                {
                    this.TriggerCacheSpecificRemove(key, region, CacheItemRemovedReason.Evicted);
                }
                else if (arguments.RemovedReason == CacheEntryRemovedReason.Expired)
                {
                    this.TriggerCacheSpecificRemove(key, region, CacheItemRemovedReason.Expired);
                }
            }
        }

        private static void ParseKeyParts(int instanceKeyLength, string fullKey, out bool isToken, out bool hasRegion, out string region, out string key)
        {
            var relevantKey = fullKey.Substring(instanceKeyLength);
            isToken = relevantKey[0] == '_';
            hasRegion = false;
            region = null;
            key = null;

            if (!isToken)
            {
                hasRegion = relevantKey[0] == '@';
                var regionLenEnd = hasRegion ? relevantKey.IndexOf('@', 1) : -1;

                int regionLen;
                regionLen = hasRegion && regionLenEnd > 0 ? int.TryParse(relevantKey.Substring(1, regionLenEnd - 1), out regionLen) ? regionLen : 0 : 0;
                hasRegion = hasRegion && regionLen > 0;

                var restKey = hasRegion ? relevantKey.Substring(regionLenEnd + 1) : relevantKey;
                region = hasRegion ? restKey.Substring(0, regionLen) : null;
                key = restKey.Substring(regionLen + 1);
            }
        }
    }
}