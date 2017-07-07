﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using CacheManager.Core.Logging;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core.Internal
{
    /// <summary>
    /// This handle is for internal use and testing. It does not implement any expiration.
    /// </summary>
    /// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
    public class DictionaryCacheHandle<TCacheValue> : BaseCacheHandle<TCacheValue>
    {
        private const int ScanInterval = 5000;
        private readonly static Random _random = new Random();
        private readonly ConcurrentDictionary<string, CacheItem<TCacheValue>> _cache;
        private readonly Timer _timer;

        //private long _lastScan = 0L;
        private int _scanRunning;

        //private object _startScanLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="DictionaryCacheHandle{TCacheValue}"/> class.
        /// </summary>
        /// <param name="managerConfiguration">The manager configuration.</param>
        /// <param name="configuration">The cache handle configuration.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public DictionaryCacheHandle(ICacheManagerConfiguration managerConfiguration, CacheHandleConfiguration configuration, ILoggerFactory loggerFactory)
            : base(managerConfiguration, configuration)
        {
            NotNull(loggerFactory, nameof(loggerFactory));
            Logger = loggerFactory.CreateLogger(this);
            _cache = new ConcurrentDictionary<string, CacheItem<TCacheValue>>();
            _timer = new Timer(TimerLoop, null, _random.Next(1000, ScanInterval), ScanInterval);
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>The count.</value>
        public override int Count => _cache.Count;

        /// <inheritdoc />
        protected override ILogger Logger { get; }

        /// <summary>
        /// Clears this cache, removing all items in the base cache and all regions.
        /// </summary>
        public override void Clear() => _cache.Clear();

        /// <summary>
        /// Clears the cache region, removing all items from the specified <paramref name="region"/> only.
        /// </summary>
        /// <param name="region">The cache region.</param>
        /// <exception cref="System.ArgumentNullException">If region is null.</exception>
        public override void ClearRegion(string region)
        {
            NotNullOrWhiteSpace(region, nameof(region));

            var key = string.Concat(region, ":");
            foreach (var item in _cache.Where(p => p.Key.StartsWith(key, StringComparison.OrdinalIgnoreCase)))
            {
                _cache.TryRemove(item.Key, out CacheItem<TCacheValue> val);
            }
        }

        /// <inheritdoc />
        public override bool Exists(string key)
        {
            NotNullOrWhiteSpace(key, nameof(key));

            return _cache.ContainsKey(key);
        }

        /// <inheritdoc />
        public override bool Exists(string key, string region)
        {
            NotNullOrWhiteSpace(region, nameof(region));
            var fullKey = GetKey(key, region);
            return _cache.ContainsKey(fullKey);
        }

        /// <inheritdoc />
        protected override IEnumerable<string> AllKeys(string region)
        {
            if (region == null)
                return _cache.Keys;
            var skip = region.Length + 1;
            return _cache.Keys.Select(k => k.Substring(skip));
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

            var key = GetKey(item.Key, item.Region);

            return _cache.TryAdd(key, item);
        }

        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key) =>
            GetCacheItemInternal(key, null);

        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key, string region)
        {
            var fullKey = GetKey(key, region);

            if (_cache.TryGetValue(fullKey, out CacheItem<TCacheValue> result))
            {
                if (result.ExpirationMode != ExpirationMode.None && IsExpired(result, DateTime.UtcNow))
                {
                    _cache.TryRemove(fullKey, out result);
                    TriggerCacheSpecificRemove(key, region, CacheItemRemovedReason.Expired, result.Value);
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

            _cache[GetKey(item.Key, item.Region)] = item;
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
            var fullKey = GetKey(key, region);
            return _cache.TryRemove(fullKey, out CacheItem<TCacheValue> val);
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

        private static bool IsExpired(CacheItem<TCacheValue> item, DateTime now)
        {
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

        private void TimerLoop(object state)
        {
            if (_scanRunning > 0)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _scanRunning, 1, 0) == 0)
            {
                try
                {
                    if (Logger.IsEnabled(LogLevel.Debug))
                    {
                        Logger.LogDebug("'{0}' starting eviction scan.", Configuration.Name);
                    }

                    ScanForExpiredItems();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error occurred during eviction scan.");
                }
                finally
                {
                    Interlocked.Exchange(ref _scanRunning, 0);
                }
            }
        }

        private int ScanForExpiredItems()
        {
            var removed = 0;
            var now = DateTime.UtcNow;
            foreach (var item in _cache.Values)
            {
                if (IsExpired(item, now))
                {
                    RemoveInternal(item.Key, item.Region);

                    // trigger global eviction event
                    TriggerCacheSpecificRemove(item.Key, item.Region, CacheItemRemovedReason.Expired, item.Value);

                    // fix stats
                    Stats.OnRemove(item.Region);
                    removed++;
                }
            }

            if (removed > 0 && Logger.IsEnabled(LogLevel.Information))
            {
                Logger.LogInfo("'{0}' removed '{1}' expired items during eviction run.", Configuration.Name, removed);
            }

            return removed;
        }
    }
}