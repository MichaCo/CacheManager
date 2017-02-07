using System;
using CacheManager.MicrosoftCachingMemory;
using Microsoft.Extensions.Caching.Memory;

namespace CacheManager.Core
{
    /// <summary>
    /// Extensions for the configuration builder specific to <see cref="Microsoft.Extensions.Caching.Memory"/> based caching.
    /// </summary>
    public static class MicrosoftMemoryCachingBuilderExtensions
    {
        /// <summary>
        /// Adds a cache handle using <see cref="MemoryCache"/>.
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="instanceName">The name to be used for the cache handle instance.</param>
        /// <returns>The builder part.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="instanceName"/> is null.</exception>
        public static ConfigurationBuilderCacheHandlePart WithMicrosoftMemoryCacheHandle(
            this ConfigurationBuilderCachePart part, string instanceName)
            => WithMicrosoftMemoryCacheHandle(part, instanceName, false, new MemoryCacheOptions());

        /// <summary>
        /// Adds a cache handle using <see cref="MemoryCache"/>.
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <returns>The builder part.</returns>
        public static ConfigurationBuilderCacheHandlePart WithMicrosoftMemoryCacheHandle(
            this ConfigurationBuilderCachePart part)
            => WithMicrosoftMemoryCacheHandle(part, Guid.NewGuid().ToString(), false, new MemoryCacheOptions());

        /// <summary>
        /// Adds a cache handle using <see cref="MemoryCache"/>.
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="instanceName">The name to be used for the cache handle instance.</param>
        /// <param name="isBackplaneSource">Set this to true if this cache handle should be the source of the backplane.
        /// This setting will be ignored if no backplane is configured.</param>
        /// <returns>
        /// The builder part.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">If part is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="instanceName"/> is null.</exception>
        public static ConfigurationBuilderCacheHandlePart WithMicrosoftMemoryCacheHandle(
            this ConfigurationBuilderCachePart part, string instanceName, bool isBackplaneSource)
            => WithMicrosoftMemoryCacheHandle(part, instanceName, isBackplaneSource, new MemoryCacheOptions());

        /// <summary>
        /// Adds a cache handle using <see cref="MemoryCache"/>.
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="instanceName">The name to be used for the cache handle instance.</param>
        /// <param name="options">The <see cref="MemoryCacheOptions"/> which should be used to initiate or reset this cache.</param>
        /// <returns>The builder part.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="instanceName"/> is null.</exception>
        [CLSCompliant(false)]
        public static ConfigurationBuilderCacheHandlePart WithMicrosoftMemoryCacheHandle(
            this ConfigurationBuilderCachePart part, string instanceName, MemoryCacheOptions options)
            => WithMicrosoftMemoryCacheHandle(part, instanceName, false, options);

        /// <summary>
        /// Adds a cache handle using <see cref="MemoryCache"/>.
        /// The name of the cache instance will be 'default'.
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="options">The <see cref="MemoryCacheOptions"/> which should be used to initiate or reset this cache.</param>
        /// <returns>The builder part.</returns>
        [CLSCompliant(false)]
        public static ConfigurationBuilderCacheHandlePart WithMicrosoftMemoryCacheHandle(
            this ConfigurationBuilderCachePart part, MemoryCacheOptions options)
            => WithMicrosoftMemoryCacheHandle(part, Guid.NewGuid().ToString(), false, options);

        /// <summary>
        /// Adds a cache handle using <see cref="MemoryCache"/>.
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="instanceName">The name to be used for the cache handle instance.</param>
        /// <param name="isBackplaneSource">Set this to true if this cache handle should be the source of the backplane.
        /// This setting will be ignored if no backplane is configured.</param>
        /// <param name="options">
        /// The <see cref="MemoryCacheOptions"/> which should be used to initiate or reset this cache.
        /// If <c>Null</c>, default options will be used.
        /// </param>
        /// <returns>The builder part.</returns>
        /// <exception cref="System.ArgumentNullException">If part is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="instanceName"/> is null.</exception>
        [CLSCompliant(false)]
        public static ConfigurationBuilderCacheHandlePart WithMicrosoftMemoryCacheHandle(
            this ConfigurationBuilderCachePart part, string instanceName, bool isBackplaneSource, MemoryCacheOptions options)
            => part?.WithHandle(typeof(MemoryCacheHandle<>), instanceName, isBackplaneSource, options);
    }
}