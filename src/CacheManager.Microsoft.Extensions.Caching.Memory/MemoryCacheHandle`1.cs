using System;
using System.Collections.Generic;
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

        private volatile MemoryCache cache = null;
        internal readonly MemoryCacheOptions memoryCacheOptions;

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
        /// <param name="memoryCacheOptions">The vendor specific options.</param>
        [CLSCompliant(false)]
        public MemoryCacheHandle(ICacheManagerConfiguration managerConfiguration, CacheHandleConfiguration configuration, ILoggerFactory loggerFactory, MemoryCacheOptions memoryCacheOptions = null)
            : base(managerConfiguration, configuration)
        {
            NotNull(configuration, nameof(configuration));
            NotNull(loggerFactory, nameof(loggerFactory));

            this.Logger = loggerFactory.CreateLogger(this);
            this.cacheName = configuration.Name;
            this.memoryCacheOptions = memoryCacheOptions ?? new MemoryCacheOptions();
            this.cache = new MemoryCache(this.memoryCacheOptions);
        }

        /// <inheritdoc/>
        public override void Clear()
        {
            this.cache = new MemoryCache(this.memoryCacheOptions);
        }

        /// <inheritdoc/>
        public override void ClearRegion(string region)
        {
            this.cache.RemoveChilds(region);
            this.cache.Remove(region);
        }

        /// <inheritdoc />
        public override bool Exists(string key)
        {
            return this.cache.Contains(GetItemKey(key));
        }

        /// <inheritdoc />
        public override bool Exists(string key, string region)
        {
            NotNullOrWhiteSpace(region, nameof(region));

            return this.cache.Contains(GetItemKey(key, region));
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

            if (item.IsExpired)
            {
                this.RemoveInternal(item.Key, item.Region);
                return null;
            }

            if (item.ExpirationMode == ExpirationMode.Sliding)
            {
                // item = this.GetItemExpiration(item); // done by basecachehandle already
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

            if (item.Region != null)
            {
                this.cache.RegisterChild(item.Region, key);
            }

            return true;
        }

        /// <inheritdoc/>
        protected override void PutInternalPrepared(CacheItem<TCacheValue> item)
        {
            var key = this.GetItemKey(item);

            var options = this.GetOptions(item);
            this.cache.Set(key, item, options);

            if (item.Region != null)
            {
                this.cache.RegisterChild(item.Region, key);
            }
        }

        private string GetItemKey(CacheItem<TCacheValue> item) => this.GetItemKey(item?.Key, item?.Region);

        private string GetItemKey(string key, string region = null)
        {
            NotNullOrWhiteSpace(key, nameof(key));

            if (string.IsNullOrWhiteSpace(region))
            {
                return key;
            }

            return region + ":" + key;
        }

        private MemoryCacheEntryOptions GetOptions(CacheItem<TCacheValue> item)
        {
            if (item.Region != null)
            {
                if (!this.cache.Contains(item.Region))
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
                options.RegisterPostEvictionCallback(this.ItemRemoved, item.Region);
            }

            if (item.ExpirationMode == ExpirationMode.Sliding)
            {
                options.SlidingExpiration = item.ExpirationTimeout;
                options.RegisterPostEvictionCallback(this.ItemRemoved, item.Region);
            }

            item.LastAccessedUtc = DateTime.UtcNow;

            return options;
        }

        private void CreateRegionToken(string region)
        {
            var options = new MemoryCacheEntryOptions
            {
                Priority = CacheItemPriority.Normal,
                AbsoluteExpiration = DateTimeOffset.MaxValue,
                SlidingExpiration = TimeSpan.MaxValue,
            };

            this.cache.Set(region, new HashSet<object>(), options);
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

            // storing region in the state field for simple usage
            if (state != null)
            {
                this.Stats.OnRemove((string)state);
            }
            else
            {
                this.Stats.OnRemove();
            }
        }
    }
}