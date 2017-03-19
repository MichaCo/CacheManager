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

        private readonly string _cacheName = string.Empty;

        private volatile MemoryCache _cache = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryCacheHandle{TCacheValue}"/> class.
        /// </summary>
        /// <param name="managerConfiguration">The manager configuration.</param>
        /// <param name="configuration">The cache handle configuration.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        [CLSCompliant(false)]
        public MemoryCacheHandle(ICacheManagerConfiguration managerConfiguration, CacheHandleConfiguration configuration, ILoggerFactory loggerFactory)
            : this(managerConfiguration, configuration, loggerFactory, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryCacheHandle{TCacheValue}"/> class.
        /// </summary>
        /// <param name="managerConfiguration">The manager configuration.</param>
        /// <param name="configuration">The cache handle configuration.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="memoryCacheOptions">The vendor specific options.</param>
        [CLSCompliant(false)]
        public MemoryCacheHandle(ICacheManagerConfiguration managerConfiguration, CacheHandleConfiguration configuration, ILoggerFactory loggerFactory, MemoryCacheOptions memoryCacheOptions)
            : base(managerConfiguration, configuration)
        {
            NotNull(configuration, nameof(configuration));
            NotNull(loggerFactory, nameof(loggerFactory));

            Logger = loggerFactory.CreateLogger(this);
            _cacheName = configuration.Name;
            MemoryCacheOptions = memoryCacheOptions ?? new MemoryCacheOptions();
            _cache = new MemoryCache(MemoryCacheOptions);
        }

        /// <inheritdoc/>
        public override int Count => _cache.Count;

        /// <inheritdoc/>
        protected override ILogger Logger { get; }

        internal MemoryCacheOptions MemoryCacheOptions { get; }

        /// <inheritdoc/>
        public override void Clear()
        {
            _cache = new MemoryCache(MemoryCacheOptions);
        }

        /// <inheritdoc/>
        public override void ClearRegion(string region)
        {
            _cache.RemoveChilds(region);
            _cache.Remove(region);
        }

        /// <inheritdoc />
        public override bool Exists(string key)
        {
            return _cache.Contains(GetItemKey(key));
        }

        /// <inheritdoc />
        public override bool Exists(string key, string region)
        {
            NotNullOrWhiteSpace(region, nameof(region));

            return _cache.Contains(GetItemKey(key, region));
        }

        /// <inheritdoc/>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key)
        {
            return GetCacheItemInternal(key, null);
        }

        /// <inheritdoc/>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key, string region)
        {
            var fullKey = GetItemKey(key, region);
            var item = _cache.Get(fullKey) as CacheItem<TCacheValue>;

            if (item == null)
            {
                return null;
            }

            if (item.IsExpired)
            {
                RemoveInternal(item.Key, item.Region);
                TriggerCacheSpecificRemove(item.Key, item.Region, CacheItemRemovedReason.Expired);
                return null;
            }

            if (item.ExpirationMode == ExpirationMode.Sliding)
            {
                // item = this.GetItemExpiration(item); // done by basecachehandle already
                _cache.Set(fullKey, item, GetOptions(item));
            }

            return item;
        }

        /// <inheritdoc/>
        protected override bool RemoveInternal(string key)
        {
            return RemoveInternal(key, null);
        }

        /// <inheritdoc/>
        protected override bool RemoveInternal(string key, string region)
        {
            var fullKey = GetItemKey(key, region);
            var result = _cache.Contains(fullKey);
            if (result)
            {
                _cache.Remove(fullKey);
            }

            return result;
        }

        /// <inheritdoc/>
        protected override bool AddInternalPrepared(CacheItem<TCacheValue> item)
        {
            var key = GetItemKey(item);

            if (_cache.Contains(key))
            {
                return false;
            }

            var options = GetOptions(item);
            _cache.Set(key, item, options);

            if (item.Region != null)
            {
                _cache.RegisterChild(item.Region, key);
            }

            return true;
        }

        /// <inheritdoc/>
        protected override void PutInternalPrepared(CacheItem<TCacheValue> item)
        {
            var key = GetItemKey(item);

            var options = GetOptions(item);
            _cache.Set(key, item, options);

            if (item.Region != null)
            {
                _cache.RegisterChild(item.Region, key);
            }
        }

        private string GetItemKey(CacheItem<TCacheValue> item) => GetItemKey(item?.Key, item?.Region);

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
                if (!_cache.Contains(item.Region))
                {
                    CreateRegionToken(item.Region);
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
                options.RegisterPostEvictionCallback(ItemRemoved, Tuple.Create(item.Key, item.Region));
            }

            if (item.ExpirationMode == ExpirationMode.Sliding)
            {
                options.SlidingExpiration = item.ExpirationTimeout;
                options.RegisterPostEvictionCallback(ItemRemoved, Tuple.Create(item.Key, item.Region));
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

            _cache.Set(region, new HashSet<object>(), options);
        }

        private void ItemRemoved(object key, object value, EvictionReason reason, object state)
        {
            var strKey = key as string;
            if (string.IsNullOrWhiteSpace(strKey))
            {
                return;
            }

            // don't trigger stuff on manual remove
            if (reason == EvictionReason.Removed)
            {
                return;
            }

            var keyRegionTupple = state as Tuple<string, string>;

            if (keyRegionTupple != null)
            {
                if (keyRegionTupple.Item2 != null)
                {
                    Stats.OnRemove(keyRegionTupple.Item2);
                }
                else
                {
                    Stats.OnRemove();
                }

                if (reason == EvictionReason.Capacity)
                {
                    TriggerCacheSpecificRemove(keyRegionTupple.Item1, keyRegionTupple.Item2, CacheItemRemovedReason.Evicted);
                }
                else if (reason == EvictionReason.Expired)
                {
                    TriggerCacheSpecificRemove(keyRegionTupple.Item1, keyRegionTupple.Item2, CacheItemRemovedReason.Expired);
                }
            }
            else
            {
                Stats.OnRemove();
            }
        }
    }
}