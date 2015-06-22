namespace CacheManager.Core.Internal
{
    /// <summary>
    /// Defines the different counter types the cache manager supports.
    /// </summary>
    public enum CacheStatsCounterType
    {
        /// <summary>
        /// The number of hits.
        /// </summary>
        Hits,

        /// <summary>
        /// The number of misses.
        /// </summary>
        Misses,

        /// <summary>
        /// The total number of items.
        /// <para>
        /// This might not be accurate in distribute cache scenarios because we count only the items
        /// added or removed locally.
        /// </para>
        /// </summary>
        Items,

        /// <summary>
        /// The number of remove calls.
        /// </summary>
        RemoveCalls,

        /// <summary>
        /// The number of add calls.
        /// </summary>
        AddCalls,

        /// <summary>
        /// The number of put calls.
        /// </summary>
        PutCalls,

        /// <summary>
        /// The number of get calls.
        /// </summary>
        GetCalls,

        /// <summary>
        /// The number of clear calls.
        /// </summary>
        ClearCalls,

        /// <summary>
        /// The number of clear region calls.
        /// </summary>
        ClearRegionCalls
    }
}