using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core.Internal
{
    /// <summary>
    /// <para>Stores statistical information for a <see cref="BaseCacheHandle{TCacheValue}"/>.</para>
    /// <para>
    /// Statistical counters are stored globally for the <see cref="BaseCacheHandle{TCacheValue}"/>
    /// and for each cache region!
    /// </para>
    /// <para>
    /// To retrieve a counter for a region only, specify the optional region attribute of GetStatistics.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The class is primarily used internally. Only the GetStatistics is visible. Therefore the
    /// class is sealed.
    /// </remarks>
    /// <typeparam name="TCacheValue">Inherited object type of the owning cache handle.</typeparam>
    public sealed class CacheStats<TCacheValue> : IDisposable
    {
        private static readonly string _nullRegionKey = Guid.NewGuid().ToString();
        private readonly ConcurrentDictionary<string, CacheStatsCounter> _counters;
        private readonly bool _isPerformanceCounterEnabled;
        private readonly bool _isStatsEnabled;
        private readonly CachePerformanceCounters<TCacheValue> _performanceCounters;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheStats{TCacheValue}"/> class.
        /// </summary>
        /// <param name="cacheName">Name of the cache.</param>
        /// <param name="handleName">Name of the handle.</param>
        /// <param name="enabled">
        /// If set to <c>true</c> the stats are enabled. Otherwise any statistics and performance
        /// counters will be disabled.
        /// </param>
        /// <param name="enablePerformanceCounters">
        /// If set to <c>true</c> performance counters and statistics will be enabled.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// If cacheName or handleName are null.
        /// </exception>
        public CacheStats(string cacheName, string handleName, bool enabled = true, bool enablePerformanceCounters = false)
        {
            NotNullOrWhiteSpace(cacheName, nameof(cacheName));
            NotNullOrWhiteSpace(handleName, nameof(handleName));

            // if performance counters are enabled, stats must be enabled, too.
            _isStatsEnabled = enablePerformanceCounters ? true : enabled;
            _isPerformanceCounterEnabled = enablePerformanceCounters;
            _counters = new ConcurrentDictionary<string, CacheStatsCounter>();

            if (_isPerformanceCounterEnabled)
            {
                _performanceCounters = new CachePerformanceCounters<TCacheValue>(cacheName, handleName, this);
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="CacheStats{TCacheValue}"/> class.
        /// </summary>
        ~CacheStats()
        {
            Dispose(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// <para>
        /// Returns the corresponding statistical information of the
        /// <see cref="CacheStatsCounterType"/> type.
        /// </para>
        /// <para>
        /// If the cache handles is configured to disable statistics, the method will always return zero.
        /// </para>
        /// </summary>
        /// <remarks>
        /// In multi threaded environments the counters can be changed while reading. Do not rely on
        /// those counters as they might not be 100% accurate.
        /// </remarks>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// var cache = CacheFactory.FromConfiguration("myCache");
        ///
        /// foreach (var handle in cache.CacheHandles)
        /// {
        ///    var stats = handle.Stats;
        ///    var region = "myRegion";
        ///    Console.WriteLine(string.Format(
        ///            "Items: {0}, Hits: {1}, Miss: {2}, Remove: {3}, ClearRegion: {4}, Clear: {5}, Adds: {6}, Puts: {7}, Gets: {8}",
        ///                stats.GetStatistic(CacheStatsCounterType.Items, region),
        ///                stats.GetStatistic(CacheStatsCounterType.Hits, region),
        ///                stats.GetStatistic(CacheStatsCounterType.Misses, region),
        ///                stats.GetStatistic(CacheStatsCounterType.RemoveCalls, region),
        ///                stats.GetStatistic(CacheStatsCounterType.ClearRegionCalls, region),
        ///                stats.GetStatistic(CacheStatsCounterType.ClearCalls, region),
        ///                stats.GetStatistic(CacheStatsCounterType.AddCalls, region),
        ///                stats.GetStatistic(CacheStatsCounterType.PutCalls, region),
        ///                stats.GetStatistic(CacheStatsCounterType.GetCalls, region)
        ///            ));
        /// }
        /// ]]>
        /// </code>
        /// </example>
        /// <param name="type">The stats type to retrieve the number for.</param>
        /// <param name="region">
        /// The region. The returned value will represent the counter of the region only.
        /// </param>
        /// <returns>
        /// A number representing the counts for the specified <see cref="CacheStatsCounterType"/>
        /// and region.
        /// </returns>
        public long GetStatistic(CacheStatsCounterType type, string region)
        {
            if (!_isStatsEnabled)
            {
                return 0L;
            }

            NotNullOrWhiteSpace(region, nameof(region));

            var counter = GetCounter(region);
            return counter.Get(type);
        }

        /// <summary>
        /// <para>
        /// Returns the corresponding statistical information of the
        /// <see cref="CacheStatsCounterType"/> type.
        /// </para>
        /// <para>
        /// If the cache handles is configured to disable statistics, the method will always return zero.
        /// </para>
        /// </summary>
        /// <remarks>
        /// In multithreaded environments the counters can be changed while reading. Do not rely on
        /// those counters as they might not be 100% accurate.
        /// </remarks>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// var cache = CacheFactory.FromConfiguration("myCache");
        ///
        /// foreach (var handle in cache.CacheHandles)
        /// {
        ///    var stats = handle.Stats;
        ///    Console.WriteLine(string.Format(
        ///            "Items: {0}, Hits: {1}, Miss: {2}, Remove: {3}, ClearRegion: {4}, Clear: {5}, Adds: {6}, Puts: {7}, Gets: {8}",
        ///                stats.GetStatistic(CacheStatsCounterType.Items),
        ///                stats.GetStatistic(CacheStatsCounterType.Hits),
        ///                stats.GetStatistic(CacheStatsCounterType.Misses),
        ///                stats.GetStatistic(CacheStatsCounterType.RemoveCalls),
        ///                stats.GetStatistic(CacheStatsCounterType.ClearRegionCalls),
        ///                stats.GetStatistic(CacheStatsCounterType.ClearCalls),
        ///                stats.GetStatistic(CacheStatsCounterType.AddCalls),
        ///                stats.GetStatistic(CacheStatsCounterType.PutCalls),
        ///                stats.GetStatistic(CacheStatsCounterType.GetCalls)
        ///            ));
        /// }
        /// ]]>
        /// </code>
        /// </example>
        /// <param name="type">The stats type to retrieve the number for.</param>
        /// <returns>A number representing the counts for the specified <see cref="CacheStatsCounterType"/>.</returns>
        public long GetStatistic(CacheStatsCounterType type) => GetStatistic(type, _nullRegionKey);

        /// <summary>
        /// Called when an item gets added to the cache.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <exception cref="System.ArgumentNullException">If item is null.</exception>
        public void OnAdd(CacheItem<TCacheValue> item)
        {
            if (!_isStatsEnabled)
            {
                return;
            }

            NotNull(item, nameof(item));

            foreach (var counter in GetWorkingCounters(item.Region))
            {
                counter.Increment(CacheStatsCounterType.AddCalls);
                counter.Increment(CacheStatsCounterType.Items);
            }
        }

        /// <summary>
        /// Called when the cache got cleared.
        /// </summary>
        public void OnClear()
        {
            if (!_isStatsEnabled)
            {
                return;
            }

            // clear needs a lock, otherwise we might mess up the overall counts
            foreach (var key in _counters.Keys)
            {
                CacheStatsCounter counter = null;
                if (_counters.TryGetValue(key, out counter))
                {
                    counter.Set(CacheStatsCounterType.Items, 0L);
                    counter.Increment(CacheStatsCounterType.ClearCalls);
                }
            }
        }

        /// <summary>
        /// Called when a cache region got cleared.
        /// </summary>
        /// <param name="region">The region.</param>
        public void OnClearRegion(string region)
        {
            if (!_isStatsEnabled)
            {
                return;
            }

            // clear needs a lock, otherwise we might mess up the overall counts
            // lock (this.lockObject)
            {
                var regionCounter = GetCounter(region);
                var itemCount = regionCounter.Get(CacheStatsCounterType.Items);
                regionCounter.Increment(CacheStatsCounterType.ClearRegionCalls);
                regionCounter.Set(CacheStatsCounterType.Items, 0L);

                var defaultCounter = GetCounter(_nullRegionKey);
                defaultCounter.Increment(CacheStatsCounterType.ClearRegionCalls);
                defaultCounter.Add(CacheStatsCounterType.Items, itemCount * -1);
            }
        }

        /// <summary>
        /// Called when cache Get got invoked.
        /// </summary>
        /// <param name="region">The region.</param>
        public void OnGet(string region = null)
        {
            if (!_isStatsEnabled)
            {
                return;
            }

            foreach (var counter in GetWorkingCounters(region))
            {
                counter.Increment(CacheStatsCounterType.GetCalls);
            }
        }

        /// <summary>
        /// Called when a Get was successful.
        /// </summary>
        /// <param name="region">The region.</param>
        public void OnHit(string region = null)
        {
            if (!_isStatsEnabled)
            {
                return;
            }

            foreach (var counter in GetWorkingCounters(region))
            {
                counter.Increment(CacheStatsCounterType.Hits);
            }
        }

        /// <summary>
        /// Called when a Get was not successful.
        /// </summary>
        /// <param name="region">The region.</param>
        public void OnMiss(string region = null)
        {
            if (!_isStatsEnabled)
            {
                return;
            }

            foreach (var counter in GetWorkingCounters(region))
            {
                counter.Increment(CacheStatsCounterType.Misses);
            }
        }

        /// <summary>
        /// Called when an item got updated.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="itemAdded">If <c>true</c> the item didn't exist and has been added.</param>
        /// <exception cref="System.ArgumentNullException">If item is null.</exception>
        public void OnPut(CacheItem<TCacheValue> item, bool itemAdded)
        {
            if (!_isStatsEnabled)
            {
                return;
            }

            NotNull(item, nameof(item));

            foreach (var counter in GetWorkingCounters(item.Region))
            {
                counter.Increment(CacheStatsCounterType.PutCalls);

                if (itemAdded)
                {
                    counter.Increment(CacheStatsCounterType.Items);
                }
            }
        }

        /// <summary>
        /// Called when an item has been removed from the cache.
        /// </summary>
        /// <param name="region">The region.</param>
        public void OnRemove(string region = null)
        {
            if (!_isStatsEnabled)
            {
                return;
            }

            foreach (var counter in GetWorkingCounters(region))
            {
                counter.Increment(CacheStatsCounterType.RemoveCalls);
                counter.Decrement(CacheStatsCounterType.Items);
            }
        }

        /// <summary>
        /// Called when an item has been updated.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        /// <param name="result">The result.</param>
        /// <exception cref="System.ArgumentNullException">If key or result are null.</exception>
        public void OnUpdate(string key, string region, UpdateItemResult<TCacheValue> result)
        {
            if (!_isStatsEnabled)
            {
                return;
            }

            NotNullOrWhiteSpace(key, nameof(key));
            NotNull(result, nameof(result));

            foreach (var counter in GetWorkingCounters(region))
            {
                counter.Add(CacheStatsCounterType.GetCalls, result.NumberOfTriesNeeded);
                counter.Add(CacheStatsCounterType.Hits, result.NumberOfTriesNeeded);
                counter.Increment(CacheStatsCounterType.PutCalls);
            }
        }

        private void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                if (_isPerformanceCounterEnabled)
                {
                    _performanceCounters.Dispose();
                }
            }
        }

        private CacheStatsCounter GetCounter(string key)
        {
            NotNullOrWhiteSpace(key, nameof(key));

            CacheStatsCounter counter = null;
            if (!_counters.TryGetValue(key, out counter))
            {
                counter = new CacheStatsCounter();
                if (_counters.TryAdd(key, counter))
                {
                    return counter;
                }

                return GetCounter(key);
            }

            return counter;
        }

        private IEnumerable<CacheStatsCounter> GetWorkingCounters(string region)
        {
            yield return GetCounter(_nullRegionKey);

            if (!string.IsNullOrWhiteSpace(region))
            {
                var counter = GetCounter(region);
                if (counter != null)
                {
                    yield return counter;
                }
            }
        }
    }
}
