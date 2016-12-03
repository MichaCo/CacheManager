using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.Core.Logging;
using Microsoft.Extensions.Caching.Memory;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.MicrosoftCachingMemory
{
    /// <summary>
    /// Implementation of a cache handle using <see cref="Microsoft.Extensions.Caching.Memory"/>.
    /// </summary>
    /// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
    public class MemoryCacheHandle<TCacheValue> : BaseCacheHandle<TCacheValue>
    {
        private const string DefaultName = "default";

        private readonly string cacheName = string.Empty;
        private string instanceKey = null;

        private volatile MemoryCache cache = null;

        /// <inheritdoc/>
        public override int Count => this.cache.Count;

        /// <inheritdoc/>
        protected override ILogger Logger { get; }

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
            this.cache = new MemoryCache(new MemoryCacheOptions());
            this.instanceKey = Guid.NewGuid().ToString();

            this.CreateInstanceToken();
        }

        /// <inheritdoc/>
        public override void Clear()
        {
            this.cache.Remove(this.instanceKey);
            this.CreateInstanceToken();
        }

        /// <inheritdoc/>
        public override void ClearRegion(string region)
        {
            var regionTokenKey = this.GetRegionTokenKey(region);
            this.cache.RemoveChilds(regionTokenKey);
            this.cache.Remove(regionTokenKey);
        }

        /// <inheritdoc/>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key)
        {
            return this.GetCacheItemInternal(key, null);
        }

        /// <inheritdoc/>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key, string region)
        {
            string fullKey = this.GetItemKey(key, region);
            var item = this.cache.Get(fullKey) as CacheItem<TCacheValue>;

            if (item == null)
            {
                return null;
            }

            if (IsExpired(item))
            {
                this.RemoveInternal(item.Key, item.Region);
                return null;
            }

            if (item.ExpirationMode == ExpirationMode.Sliding)
            {
                item = this.GetItemExpiration(item);
                this.cache.Set(fullKey, item, this.GetOptions(item));
            }

            return item;
        }

        /// <inheritdoc/>
        protected override bool RemoveInternal(string key)
        {
            return this.RemoveInternal(key, null);
        }

        /// <inheritdoc/>
        protected override bool RemoveInternal(string key, string region)
        {
            var fullKey = this.GetItemKey(key, region);
            bool result = this.cache.Contains(fullKey);
            if (result)
            {
                this.cache.Remove(fullKey);
            }

            return result;
        }

        /// <inheritdoc/>
        protected override bool AddInternalPrepared(CacheItem<TCacheValue> item)
        {
            var key = this.GetItemKey(item);

            if (this.cache.Contains(key))
            {
                return false;
            }

            var options = this.GetOptions(item);
            this.cache.Set(key, item, options);

            this.cache.RegisterChild(this.GetRegionTokenKey(item.Region), key);
            return true;
        }

        /// <inheritdoc/>
        protected override void PutInternalPrepared(CacheItem<TCacheValue> item)
        {
            var key = this.GetItemKey(item);

            var options = this.GetOptions(item);
            this.cache.Set(key, item, options);

            this.cache.RegisterChild(this.GetRegionTokenKey(item.Region), key);
        }

        private void CreateInstanceToken()
        {
            // don't add a new key while we are disposing our instance
            if (!this.Disposing)
            {
                MemoryCacheEntryOptions options = new MemoryCacheEntryOptions
                {
                    Priority = CacheItemPriority.Normal,
                    AbsoluteExpiration = DateTimeOffset.MaxValue,
                    SlidingExpiration = TimeSpan.MaxValue,
                };

                options.RegisterPostEvictionCallback(this.InstanceTokenRemoved);
                this.cache.Set(this.instanceKey, new HashSet<object>(), options);
            }
        }

        private void InstanceTokenRemoved(object key, object value, EvictionReason reason, object state)
        {
            var set = (HashSet<object>)value;
            foreach (var item in set)
            {
                this.cache.Remove(item);
            }

            this.instanceKey = Guid.NewGuid().ToString();
        }

        private string GetRegionTokenKey(string region)
        {
            var key = string.Concat(this.instanceKey, "@", region);
            return key;
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

        private MemoryCacheEntryOptions GetOptions(CacheItem<TCacheValue> item)
        {
            if (!string.IsNullOrWhiteSpace(item.Region))
            {
                var key = this.GetRegionTokenKey(item.Region);

                if (!this.cache.Contains(key))
                {
                    this.CreateRegionToken(item.Region);
                }
            }

            var options = new MemoryCacheEntryOptions()
            {
                Priority = CacheItemPriority.Normal,
                AbsoluteExpiration = DateTimeOffset.MaxValue,
                SlidingExpiration = TimeSpan.MaxValue,
            };

            if (item.ExpirationMode == ExpirationMode.Absolute)
            {
                options.AbsoluteExpiration = new DateTimeOffset(DateTime.UtcNow.Add(item.ExpirationTimeout));
                options.RegisterPostEvictionCallback(this.ItemRemoved);
            }

            if (item.ExpirationMode == ExpirationMode.Sliding)
            {
                options.SlidingExpiration = item.ExpirationTimeout;
                options.RegisterPostEvictionCallback(this.ItemRemoved);
            }

            item.LastAccessedUtc = DateTime.UtcNow;

            return options;
        }

        private void CreateRegionToken(string region)
        {
            var key = this.GetRegionTokenKey(region);
            this.cache.RegisterChild(this.instanceKey, key);
            var options = new MemoryCacheEntryOptions
            {
                Priority = CacheItemPriority.Normal,
                AbsoluteExpiration = DateTimeOffset.MaxValue,
                SlidingExpiration = TimeSpan.MaxValue,
            };

            this.cache.Set(key, new HashSet<object>(), options);
        }

        private void ItemRemoved(object key, object value, EvictionReason reason, object state)
        {
            var strKey = key as string;
            if (string.IsNullOrWhiteSpace(strKey))
            {
                return;
            }

            if (reason == EvictionReason.Removed)
            {
                return;
            }

            if (strKey.Contains(":"))
            {
                if (strKey.Contains("@"))
                {
                    var region = Regex.Match(strKey, "@(.+?):").Groups[1].Value;
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