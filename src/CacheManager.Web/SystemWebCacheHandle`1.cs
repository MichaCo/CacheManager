using System;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Caching;
using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.Core.Logging;
using static CacheManager.Core.Utility.Guard;

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
        private string instanceKey = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemWebCacheHandle{TCacheValue}"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="configuration">The configuration.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "Should be fine.")]
        public SystemWebCacheHandle(ICacheManager<TCacheValue> manager, CacheHandleConfiguration configuration)
            : base(manager, configuration)
        {
            NotNull(configuration, nameof(configuration));
            NotNull(manager, nameof(manager));

            this.Logger = manager.Configuration.LoggerFactory.CreateLogger(this);

            this.instanceKey = Guid.NewGuid().ToString();

            this.CreateInstanceToken();
        }

        /// <summary>
        /// Gets the number of items the cache handle currently maintains.
        /// </summary>
        /// <value>The count.</value>
        public override int Count => (int)this.Context.Cache.Count;

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
            this.Context.Cache.Remove(this.instanceKey);
            this.CreateInstanceToken();
        }

        /// <summary>
        /// Clears the cache region, removing all items from the specified <paramref name="region"/> only.
        /// </summary>
        /// <param name="region">The cache region.</param>
        public override void ClearRegion(string region) =>
            this.Context.Cache.Remove(this.GetRegionTokenKey(region));

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
            var settings = this.GetCacheSettings(item);
            var result = this.Context.Cache.Add(
                key: key,
                value: item,
                dependencies: settings.Dependency,
                absoluteExpiration: settings.AbsoluteExpire,
                slidingExpiration: settings.SlidingExpire,
                priority: CacheItemPriority.NotRemovable,
                onRemoveCallback: this.ItemRemoved);

            // result will be the existing value if the key is already stored, the new value will not override the key
            return result == null;
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
            var item = this.Context.Cache.Get(fullKey) as CacheItem<TCacheValue>;

            if (item == null)
            {
                return null;
            }

            //// precision for System.Web.Caching is pretty good but still in seconds, e.g. sliding of milliseconds doesn't work
            //// TODO: consider enable custom expiration check
            ////if (IsExpired(item))
            ////{
            ////    // if so remove it
            ////    this.RemoveInternal(item.Key, item.Region);
            ////    return null;
            ////}

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
            var settings = this.GetCacheSettings(item);
            this.Context.Cache.Insert(
                key: key,
                value: item,
                dependencies: settings.Dependency,
                absoluteExpiration: settings.AbsoluteExpire,
                slidingExpiration: settings.SlidingExpire,
                priority: CacheItemPriority.NotRemovable,
                onRemoveCallback: this.ItemRemoved);
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
            var obj = this.Context.Cache.Remove(fullKey);

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
            if (!this.Disposing)
            {
                CacheItemRemovedCallback callback = (key, item, reason) =>
                {
                    this.instanceKey = Guid.NewGuid().ToString();
                };

                var instanceItem = new CacheItem<string>(this.instanceKey, this.instanceKey);
                this.Context.Cache.Add(
                    this.instanceKey,
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
            var key = this.GetRegionTokenKey(region);

            // add region token with dependency on our instance token, so that all regions get
            // removed whenever the instance gets cleared.
            var dependency = new CacheDependency(null, new[] { this.instanceKey });
            this.Context.Cache.Add(key, region, dependency, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration, CacheItemPriority.NotRemovable, null);
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

        private CacheDependency CreateDependency(CacheItem<TCacheValue> item)
        {
            string[] cacheKeys;

            if (string.IsNullOrWhiteSpace(item.Region))
            {
                cacheKeys = new string[] { this.instanceKey };
            }
            else
            {
                var regionKey = this.GetRegionTokenKey(item.Region);
                if (this.Context.Cache[regionKey] == null)
                {
                    this.CreateRegionToken(item.Region);
                }

                cacheKeys = new string[] { this.instanceKey, regionKey };
            }

            return new CacheDependency(null, cacheKeys);
        }

        private string GetRegionTokenKey(string region)
        {
            var key = string.Concat(this.instanceKey, "@", region);
            return key;
        }

        private void ItemRemoved(string key, object item, CacheItemRemovedReason reason)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            // ignore manually removed items, stats will be updated already
            if (reason == CacheItemRemovedReason.Removed)
            {
                return;
            }

            // TODO: use the value, cast to CacheItem<T> and check region...
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

        private CacheSettings GetCacheSettings(CacheItem<TCacheValue> item)
        {
            var settings = new CacheSettings()
            {
                Dependency = this.CreateDependency(item),
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