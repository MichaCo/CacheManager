using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace CacheManager.Core.Cache
{
    /// <summary>
    /// <para>
    /// Stores statistical information for a <see cref="BaseCacheHandle{TCacheValue}"/>.
    /// </para>
    /// <para>
    /// Statistical counters are stored globally for the <see cref="BaseCacheHandle{TCacheValue}"/> and for each cache region!
    /// </para>
    /// <para>
    /// To retrieve a counter for a region only, specify the optional region attribute
    /// of GetStatistics.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The class is primarily used internally. Only the GetStatistics is visible. Therefore the
    /// class is sealed.
    /// </remarks>
    /// <typeparam name="T">Inherited object type of the owning cache handle.</typeparam>
    public sealed class CacheStats<T> : IDisposable
    {
        private static readonly string NullRegionKey = Guid.NewGuid().ToString();

        private readonly ConcurrentDictionary<string, CacheStatsCounter> counters;

        private readonly bool isStatsEnabled;

        private readonly bool isPerformanceCounterEnabled;

        private readonly object lockObject;

        private readonly CachePerformanceCounters<T> performanceCounters;

        public CacheStats(string cacheName, string handleName, bool enabled = true, bool enablePerformanceCounters = false)
        {
            if (string.IsNullOrWhiteSpace(cacheName))
            {
                throw new ArgumentNullException("cacheName");
            }

            if (string.IsNullOrWhiteSpace(handleName))
            {
                throw new ArgumentNullException("handleName");
            }

            this.lockObject = new object();

            // if performance counters are enabled, stats must be enabled, too.
            this.isStatsEnabled = enablePerformanceCounters ? true : enabled;
            this.isPerformanceCounterEnabled = enablePerformanceCounters;
            this.counters = new ConcurrentDictionary<string, CacheStatsCounter>();

            if (this.isPerformanceCounterEnabled)
            {
                this.performanceCounters = new CachePerformanceCounters<T>(cacheName, handleName, this);
            }
        }

        ~CacheStats()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// <para>
        /// Returns the corresponding statistical information of the <see cref="CacheStatsCounterType"/> type.
        /// </para>
        /// <para>
        /// If the cache handles is configured to disable statistics, the method will always return zero.
        /// </para>
        /// </summary>
        /// <remarks>
        /// In multi threaded environments the counters can be changed while reading.
        /// Do not rely on those counters as they might not be 100% accurate!
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
        ///}
        /// ]]>
        /// </code>
        /// </example>
        /// <param name="type">The stats type to retrieve the number for.</param>
        /// <param name="region">The region. The returned value will represent the counter of the region only.</param>
        /// <returns>A number representing the counts for the specified <see cref="CacheStatsCounterType"/> and region.</returns>
        public long GetStatistic(CacheStatsCounterType type, string region)
        {
            if (!isStatsEnabled)
            {
                return 0L;
            }

            if (string.IsNullOrWhiteSpace(region))
            {
                throw new ArgumentNullException("region");
            }

            var counter = this.GetCounter(region);
            return counter.Get(type);
        }

        /// <summary>
        /// <para>
        /// Returns the corresponding statistical information of the <see cref="CacheStatsCounterType"/> type.
        /// </para>
        /// <para>
        /// If the cache handles is configured to disable statistics, the method will always return zero.
        /// </para>
        /// </summary>
        /// <remarks>
        /// In multithreaded environments the counters can be changed while reading.
        /// Do not rely on those counters as they might not be 100% accurate!
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
        ///}
        /// ]]>
        /// </code>
        /// </example>
        /// <param name="type">The stats type to retrieve the number for.</param>
        /// <returns>A number representing the counts for the specified <see cref="CacheStatsCounterType"/>.</returns>
        public long GetStatistic(CacheStatsCounterType type)
        {
            return this.GetStatistic(type, NullRegionKey);
        }

        public void OnAdd(CacheItem<T> item)
        {
            if (!isStatsEnabled)
            {
                return;
            }

            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            foreach (var counter in this.GetWorkingCounters(item.Region))
            {
                counter.Increment(CacheStatsCounterType.AddCalls);
                counter.Increment(CacheStatsCounterType.Items);
            }
        }

        public void OnPut(CacheItem<T> item, bool itemAdded)
        {
            if (!isStatsEnabled)
            {
                return;
            }

            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            foreach (var counter in GetWorkingCounters(item.Region))
            {
                counter.Increment(CacheStatsCounterType.PutCalls);
                
                if (itemAdded)
                {
                    counter.Increment(CacheStatsCounterType.Items);
                }
            }
        }

        public void OnUpdate(string key, string region, UpdateItemResult result)
        {
            if (!isStatsEnabled)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }

            if (result == null)
            {
                throw new ArgumentNullException("result");
            }
            
            foreach (var counter in GetWorkingCounters(region))
            {
                counter.Add(CacheStatsCounterType.GetCalls, result.NumberOfRetriesNeeded);
                counter.Add(CacheStatsCounterType.Hits, result.NumberOfRetriesNeeded);
                counter.Increment(CacheStatsCounterType.PutCalls);
            }
        }

        public void OnRemove(string region = null)
        {
            if (!isStatsEnabled)
            {
                return;
            }

            foreach (var counter in GetWorkingCounters(region))
            {
                counter.Increment(CacheStatsCounterType.RemoveCalls);
                counter.Decrement(CacheStatsCounterType.Items);
            }
        }

        public void OnGet(string region = null)
        {
            if (!isStatsEnabled)
            {
                return;
            }

            foreach (var counter in GetWorkingCounters(region))
            {
                counter.Increment(CacheStatsCounterType.GetCalls);
            }
        }

        public void OnHit(string region = null)
        {
            if (!isStatsEnabled)
            {
                return;
            }

            foreach (var counter in GetWorkingCounters(region))
            {
                counter.Increment(CacheStatsCounterType.Hits);
            }
        }

        public void OnMiss(string region = null)
        {
            if (!isStatsEnabled) { return; }

            foreach (var counter in GetWorkingCounters(region))
            {
                counter.Increment(CacheStatsCounterType.Misses);
            }
        }

        public void OnClear()
        {
            if (!isStatsEnabled) { return; }

            // clear needs a lock, otherwise we might mess up the overall counts
            lock (lockObject)
            {
                foreach (var key in counters.Keys)
                {
                    CacheStatsCounter counter = null;
                    if (counters.TryGetValue(key, out counter))
                    {
                        counter.Set(CacheStatsCounterType.Items, 0L);
                        counter.Increment(CacheStatsCounterType.ClearCalls);
                    }
                }
            }
        }

        public void OnClearRegion(string region)
        {
            if (!isStatsEnabled) { return; }

            // clear needs a lock, otherwise we might mess up the overall counts
            lock (lockObject)
            {
                var regionCounter = GetCounter(region);
                var itemCount = regionCounter.Get(CacheStatsCounterType.Items);
                regionCounter.Increment(CacheStatsCounterType.ClearRegionCalls);
                regionCounter.Set(CacheStatsCounterType.Items, 0L);

                var defaultCounter = GetCounter(NullRegionKey);
                defaultCounter.Increment(CacheStatsCounterType.ClearRegionCalls);
                defaultCounter.Add(CacheStatsCounterType.Items, itemCount * -1);
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                if (isPerformanceCounterEnabled)
                {
                    this.performanceCounters.Dispose();
                }
            }
        }

        private CacheStatsCounter GetCounter(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(key);
            }

            CacheStatsCounter counter = null;
            if (!counters.TryGetValue(key, out counter))
            {
                // because of the lazy initialization of region counters, we
                // have to lock at this point even though the counters dictionary is thread safe
                // the method gets called so frequently that this is a real performance improvement...
                lock (lockObject)
                {
                    // check again after pooling threads on the lock
                    if (!counters.TryGetValue(key, out counter))
                    {
                        counter = new CacheStatsCounter();
                        if (!counters.TryAdd(key, counter))
                        {
                            throw new InvalidOperationException("Failed to initialize counter.");
                        }
                    }
                }
            }

            return counter;
        }

        private IEnumerable<CacheStatsCounter> GetWorkingCounters(string region)
        {
            yield return GetCounter(NullRegionKey);

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