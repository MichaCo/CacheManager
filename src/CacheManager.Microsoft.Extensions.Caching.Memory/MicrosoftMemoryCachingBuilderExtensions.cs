using System;
using CacheManager.MicrosoftCachingMemory;

namespace CacheManager.Core
{
    /// <summary>
    /// Extensions for the configuration builder specific to Microsoft.Extensions.Caching.Memory cache handle.
    /// </summary>
    public static class MicrosoftMemoryCachingBuilderExtensions
    {
        private const string DefaultName = "default";

        /// <summary>
        /// Adds a <see cref="MemoryCacheHandle{TCacheValue}" /> using a <see cref="Microsoft.Extensions.Caching.Memory"/> instance with the given <paramref name="instanceName"/>.
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="instanceName">The name to be used for the <see cref="Microsoft.Extensions.Caching.Memory"/> instance.</param>
        /// <returns>The builder part.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="instanceName"/> is null.</exception>
        public static ConfigurationBuilderCacheHandlePart WithMicrosoftMemoryCacheHandle(
            this ConfigurationBuilderCachePart part, string instanceName)
            => WithMicrosoftMemoryCacheHandle(part, instanceName, false);

        /// <summary>
        /// Adds a <see cref="MemoryCacheHandle{TCacheValue}" /> using a <see cref="Microsoft.Extensions.Caching.Memory"/>.
        /// The name of the cache instance will be 'default'.
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <returns>The builder part.</returns>
        public static ConfigurationBuilderCacheHandlePart WithMicrosoftMemoryCacheHandle(
            this ConfigurationBuilderCachePart part)
            => part?.WithHandle(typeof(MemoryCacheHandle<>), DefaultName, false);

        /// <summary>
        /// Adds a <see cref="MemoryCacheHandle{TCacheValue}" /> using a <see cref="Microsoft.Extensions.Caching.Memory"/> instance with the given <paramref name="instanceName"/>.
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
        public static ConfigurationBuilderCacheHandlePart WithMicrosoftMemoryCacheHandle(
            this ConfigurationBuilderCachePart part, string instanceName, bool isBackplaneSource)
            => part?.WithHandle(typeof(MemoryCacheHandle<>), instanceName, isBackplaneSource);
    }
}