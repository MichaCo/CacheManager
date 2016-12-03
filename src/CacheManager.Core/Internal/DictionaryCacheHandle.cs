using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        private const int ScanInterval = 10000;
        private long lastScan = 0L;
        private bool scanRunning;
        private object startScanLock = new object();
        private ConcurrentDictionary<string, CacheItem<TCacheValue>> cache;

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
            this.Logger = loggerFactory.CreateLogger(this);
            this.cache = new ConcurrentDictionary<string, CacheItem<TCacheValue>>();
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
        public override void Clear() => this.cache.Clear();

        /// <summary>
        /// Clears the cache region, removing all items from the specified <paramref name="region"/> only.
        /// </summary>
        /// <param name="region">The cache region.</param>
        /// <exception cref="System.ArgumentNullException">If region is null.</exception>
        public override void ClearRegion(string region)
        {
            NotNullOrWhiteSpace(region, nameof(region));

            var key = string.Concat(region, ":");
            foreach (var item in this.cache.Where(p => p.Key.StartsWith(key, StringComparison.Ordinal)))
            {
                CacheItem<TCacheValue> val = null;
                this.cache.TryRemove(item.Key, out val);
            }

            this.StartScanExpiredItems();
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

            this.StartScanExpiredItems();
            return this.cache.TryAdd(key, item);
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
                if (IsExpired(result, DateTime.UtcNow))
                {
                    this.cache.TryRemove(fullKey, out result);
                    return null;
                }
            }

            this.StartScanExpiredItems();
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

            this.cache[GetKey(item.Key, item.Region)] = item;
            this.StartScanExpiredItems();
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
            CacheItem<TCacheValue> val = null;
            return this.cache.TryRemove(fullKey, out val);
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

        private static void ScanForExpiredItems(DictionaryCacheHandle<TCacheValue> cache)
        {
            cache.scanRunning = true;
            var removed = 0;
            var now = DateTime.UtcNow;
            foreach (var item in cache.cache.Values)
            {
                if (IsExpired(item, now))
                {
                    cache.RemoveInternal(item.Key, item.Region);

                    // fix stats
                    cache.Stats.OnRemove(item.Region);
                    removed++;
                }
            }

            if (removed > 0 && cache.Logger.IsEnabled(LogLevel.Information))
            {
                cache.Logger.LogInfo("Removed {0} expired items.", removed);
            }

            cache.scanRunning = false;
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

        private void StartScanExpiredItems()
        {
            var currentTicks = Environment.TickCount;
            if (!this.scanRunning && this.lastScan + ScanInterval < currentTicks)
            {
                lock (this.startScanLock)
                {
                    if (!this.scanRunning && this.lastScan + ScanInterval < currentTicks)
                    {
                        this.lastScan = currentTicks;

                        this.Logger.LogInfo("Starting scan for expired items. Next scan in {0}sec.", ScanInterval / 1000);
#if NET40
                        Task.Factory.StartNew(
                            state => ScanForExpiredItems((DictionaryCacheHandle<TCacheValue>)state),
                            this,
                            CancellationToken.None,
                            TaskCreationOptions.None,
                            TaskScheduler.Default);
#else
                        Task.Factory.StartNew(
                            state => ScanForExpiredItems((DictionaryCacheHandle<TCacheValue>)state),
                            this,
                            CancellationToken.None,
                            TaskCreationOptions.DenyChildAttach,
                            TaskScheduler.Default);
#endif
                    }
                }
            }
        }
    }
}