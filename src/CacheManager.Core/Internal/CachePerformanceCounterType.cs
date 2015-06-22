namespace CacheManager.Core.Internal
{
    /// <summary>
    /// Supported performance counter types.
    /// </summary>
    internal enum CachePerformanceCounterType
    {
        /// <summary>
        /// The number of items.
        /// </summary>
        Items,

        /// <summary>
        /// The hit ratio.
        /// </summary>
        HitRatio,

        /// <summary>
        /// The hit ratio base.
        /// </summary>
        HitRatioBase,

        /// <summary>
        /// The total hits.
        /// </summary>
        TotalHits,

        /// <summary>
        /// The total misses.
        /// </summary>
        TotalMisses,

        /// <summary>
        /// The total writes.
        /// </summary>
        TotalWrites,

        /// <summary>
        /// The reads per second.
        /// </summary>
        ReadsPerSecond,

        /// <summary>
        /// The writes per second.
        /// </summary>
        WritesPerSecond,

        /// <summary>
        /// The hits per second.
        /// </summary>
        HitsPerSecond,
    }
}