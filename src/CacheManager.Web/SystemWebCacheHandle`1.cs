using System;
using System.Web;
using System.Web.Caching;
using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.Core.Logging;
using static CacheManager.Core.Utility.Guard;

// TODO: Make this thing obsolete, not tested, outdated.
namespace CacheManager.Web
{
    /// <summary>
    /// Implementation based on <see cref="System.Web.Caching.Cache"/>.
    /// </summary>
    /// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
    /// <remarks>
    /// Although the MemoryCache doesn't support regions nor a RemoveAll/Clear method, we will
    /// implement it via cache dependencies.
    /// </remarks>
    public class SystemWebCacheHandle<TCacheValue> : BaseCacheHandle<TCacheValue>
    {
        private string _instanceKey = null;
        private int _instanceKeyLength;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemWebCacheHandle{TCacheValue}"/> class.
        /// </summary>
        /// <param name="managerConfiguration">The manager configuration.</param>
        /// <param name="configuration">The cache handle configuration.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public SystemWebCacheHandle(ICacheManagerConfiguration managerConfiguration, CacheHandleConfiguration configuration, ILoggerFactory loggerFactory)
            : base(managerConfiguration, configuration)
        {
            NotNull(loggerFactory, nameof(loggerFactory));
            Logger = loggerFactory.CreateLogger(this);

            _instanceKey = Guid.NewGuid().ToString();
            _instanceKeyLength = _instanceKey.Length;

            CreateInstanceToken();
        }

        /// <summary>
        /// Gets the number of items the cache handle currently maintains.
        /// </summary>
        /// <value>The count.</value>
        public override int Count => (int)Context.Cache.Count;

        /// <summary>
        /// Gets the http context being used to get the <c>Cache</c> instance.
        /// This implementation requires <see cref="HttpContext.Current"/> to be not null.
        /// </summary>
        /// <value>The http context instance.</value>
        protected virtual HttpContextBase Context => ContextFactory.CreateContext();

        /// <inheritdoc />
        protected override ILogger Logger { get; }

        /// <summary>
        /// Clears this cache, removing all items in the base cache and all regions.
        /// </summary>
        public override void Clear()
        {
            Context.Cache.Remove(_instanceKey);
            CreateInstanceToken();
        }

        /// <summary>
        /// Clears the cache region, removing all items from the specified <paramref name="region"/> only.
        /// </summary>
        /// <param name="region">The cache region.</param>
        public override void ClearRegion(string region) =>
            Context.Cache.Remove(GetRegionTokenKey(region));

        /// <inheritdoc />
        public override bool Exists(string key)
        {
            return GetCacheItemInternal(key) != null;
        }

        /// <inheritdoc />
        public override bool Exists(string key, string region)
        {
            NotNullOrWhiteSpace(region, nameof(region));

            return GetCacheItemInternal(key, region) != null;
        }

        /// <summary>
        /// Adds a value to the cache.
        /// </summary>
        /// <remarks>
        /// Be aware that sliding expiration for this cache works only if the timeout is set to more than 2000ms.
        /// </remarks>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
        /// </returns>
        protected override bool AddInternalPrepared(CacheItem<TCacheValue> item)
        {
            var key = GetItemKey(item);
            var settings = GetCacheSettings(item);

            if (settings.SlidingExpire.TotalMilliseconds > 0 && settings.SlidingExpire.TotalMilliseconds < 2000)
            {
                Logger.LogWarn(
                    "System.Web.Caching.Cache sliding expiration works only with a value larger than 2000ms, "
                    + $"but you configured '{settings.SlidingExpire.TotalMilliseconds}' for key {item.Key}:{item.Region}.");
            }

            var result = Context.Cache.Add(
                key: key,
                value: item,
                dependencies: settings.Dependency,
                absoluteExpiration: settings.AbsoluteExpire,
                slidingExpiration: settings.SlidingExpire,
                priority: CacheItemPriority.Normal,
                onRemoveCallback: ItemRemoved);

            // result will be the existing value if the key is already stored, the new value will not override the key
            return result == null;
        }

        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key) => GetCacheItemInternal(key, null);

        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key, string region)
        {
            var fullKey = GetItemKey(key, region);
            var item = Context.Cache.Get(fullKey) as CacheItem<TCacheValue>;

            if (item == null)
            {
                return null;
            }

            // cache.Get eventually triggers eviction callback, but just in case...
            if (item.IsExpired)
            {
                RemoveInternal(item.Key, item.Region);
                TriggerCacheSpecificRemove(item.Key, item.Region, Core.Internal.CacheItemRemovedReason.Expired, item.Value);
                return null;
            }

            return item;
        }

        /// <summary>
        /// Puts the <paramref name="item"/> into the cache. If the item exists it will get updated
        /// with the new value. If the item doesn't exist, the item will be added to the cache.
        /// </summary>
        /// <remarks>
        /// Be aware that sliding expiration for this cache works only if the timeout is set to more than 2000ms.
        /// </remarks>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        protected override void PutInternalPrepared(CacheItem<TCacheValue> item)
        {
            var key = GetItemKey(item);
            var settings = GetCacheSettings(item);

            if (settings.SlidingExpire.TotalMilliseconds > 0 && settings.SlidingExpire.TotalMilliseconds < 2000)
            {
                Logger.LogWarn(
                    "System.Web.Caching.Cache sliding expiration works only with a value larger than 2000ms, "
                    + $"but you configured '{settings.SlidingExpire.TotalMilliseconds}' for key {item.Key}:{item.Region}.");
            }

            Context.Cache.Insert(
                key: key,
                value: item,
                dependencies: settings.Dependency,
                absoluteExpiration: settings.AbsoluteExpire,
                slidingExpiration: settings.SlidingExpire,
                priority: CacheItemPriority.Normal,
                onRemoveCallback: ItemRemoved);
        }

        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        protected override bool RemoveInternal(string key) => RemoveInternal(key, null);

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
            var fullKey = GetItemKey(key, region);
            var obj = Context.Cache.Remove(fullKey);

            return obj != null;
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
            if (!Disposing)
            {
                CacheItemRemovedCallback callback = (key, item, reason) =>
                {
                    _instanceKey = Guid.NewGuid().ToString();
                    _instanceKeyLength = _instanceKey.Length;
                };

                var instanceItem = new CacheItem<string>(_instanceKey, _instanceKey);
                Context.Cache.Add(
                    _instanceKey,
                    instanceItem,
                    null,
                    Cache.NoAbsoluteExpiration,
                    Cache.NoSlidingExpiration,
                    CacheItemPriority.NotRemovable,
                    callback);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "We don't own the instance")]
        private void CreateRegionToken(string region)
        {
            var key = GetRegionTokenKey(region);

            // add region token with dependency on our instance token, so that all regions get
            // removed whenever the instance gets cleared.
            var dependency = new CacheDependency(null, new[] { _instanceKey });
            Context.Cache.Add(key, region, dependency, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration, CacheItemPriority.NotRemovable, null);
        }

        private string GetItemKey(CacheItem<TCacheValue> item) => GetItemKey(item?.Key, item?.Region);

        private string GetItemKey(string key, string region = null)
        {
            NotNullOrWhiteSpace(key, nameof(key));

            if (string.IsNullOrWhiteSpace(region))
            {
                return _instanceKey + ":" + key;
            }

            return string.Concat(_instanceKey, "@", region.Length, "@", region, ":", key);
        }

        private CacheDependency CreateDependency(CacheItem<TCacheValue> item)
        {
            string[] cacheKeys;

            if (string.IsNullOrWhiteSpace(item.Region))
            {
                cacheKeys = new string[] { _instanceKey };
            }
            else
            {
                var regionKey = GetRegionTokenKey(item.Region);
                if (Context.Cache[regionKey] == null)
                {
                    CreateRegionToken(item.Region);
                }

                cacheKeys = new string[] { _instanceKey, regionKey };
            }

            return new CacheDependency(null, cacheKeys);
        }

        private string GetRegionTokenKey(string region)
        {
            var key = string.Concat(_instanceKey, "_", region);
            return key;
        }

        private void ItemRemoved(string fullKey, object item, System.Web.Caching.CacheItemRemovedReason reason)
        {
            if (string.IsNullOrWhiteSpace(fullKey))
            {
                return;
            }

            // ignore manually removed items, stats will be updated already
            if (reason == System.Web.Caching.CacheItemRemovedReason.Removed)
            {
                return;
            }

            bool isToken;
            bool hasRegion;
            string key;
            string region;
            ParseKeyParts(_instanceKeyLength, fullKey, out isToken, out hasRegion, out region, out key);
            
            if (!isToken)
            {
                if (hasRegion)
                {
                    Stats.OnRemove(region);
                }
                else
                {
                    Stats.OnRemove();
                }

                var cacheItem = item as CacheItem<TCacheValue>;
                object originalValue = null;
                if (item != null)
                {
                    originalValue = cacheItem.Value;
                }

                // trigger cachemanager's remove on evicted and expired items
                if (reason == System.Web.Caching.CacheItemRemovedReason.Underused)
                {
                    TriggerCacheSpecificRemove(key, region, Core.Internal.CacheItemRemovedReason.Evicted, originalValue);
                }
                else if (reason == System.Web.Caching.CacheItemRemovedReason.Expired)
                {
                    TriggerCacheSpecificRemove(key, region, Core.Internal.CacheItemRemovedReason.Expired, originalValue);
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

        private CacheSettings GetCacheSettings(CacheItem<TCacheValue> item)
        {
            var settings = new CacheSettings()
            {
                Dependency = CreateDependency(item),
                AbsoluteExpire = item.ExpirationMode == ExpirationMode.Absolute ? DateTime.UtcNow.Add(item.ExpirationTimeout) : Cache.NoAbsoluteExpiration,
                SlidingExpire = item.ExpirationMode == ExpirationMode.Sliding ? item.ExpirationTimeout : Cache.NoSlidingExpiration,
            };

            return settings;
        }

        private class CacheSettings
        {
            public CacheDependency Dependency { get; set; }

            public DateTime AbsoluteExpire { get; set; } = Cache.NoAbsoluteExpiration;

            public TimeSpan SlidingExpire { get; set; } = Cache.NoSlidingExpiration;
        }
    }
}
