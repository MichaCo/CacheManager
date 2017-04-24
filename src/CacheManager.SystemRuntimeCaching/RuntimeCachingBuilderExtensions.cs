using System;
using CacheManager.SystemRuntimeCaching;

namespace CacheManager.Core
{
    /// <summary>
    /// Extensions for the configuration builder specific to System.Runtime.Caching cache handle.
    /// </summary>
    public static class RuntimeCachingBuilderExtensions
    {
        private const string DefaultName = "default";

        /// <summary>
        /// Adds a <see cref="MemoryCacheHandle{TCacheValue}" /> using a <see cref="System.Runtime.Caching.MemoryCache"/>.
        /// The name of the cache instance will be 'default'.
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="isBackplaneSource">Set this to true if this cache handle should be the source of the backplane.
        /// This setting will be ignored if no backplane is configured.</param>
        /// <returns>
        /// The builder part.
        /// </returns>
        /// <returns>The builder part.</returns>
        public static ConfigurationBuilderCacheHandlePart WithSystemRuntimeCacheHandle(this ConfigurationBuilderCachePart part, bool isBackplaneSource = false)
            => part?.WithHandle(typeof(MemoryCacheHandle<>), DefaultName, isBackplaneSource);

        /// <summary>
        /// Adds a <see cref="MemoryCacheHandle{TCacheValue}" /> using a <see cref="System.Runtime.Caching.MemoryCache"/> instance with the given <paramref name="instanceName"/>.
        /// The named cache instance can be configured via <c>app/web.config</c> <c>system.runtime.caching</c> section.
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="instanceName">The name to be used for the cache instance.</param>
        /// <param name="isBackplaneSource">Set this to true if this cache handle should be the source of the backplane.
        /// This setting will be ignored if no backplane is configured.</param>
        /// <returns>
        /// The builder part.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">If part is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="instanceName"/> is null.</exception>
        public static ConfigurationBuilderCacheHandlePart WithSystemRuntimeCacheHandle(this ConfigurationBuilderCachePart part, string instanceName, bool isBackplaneSource = false)
            => part?.WithHandle(typeof(MemoryCacheHandle<>), instanceName, isBackplaneSource);
    }
}