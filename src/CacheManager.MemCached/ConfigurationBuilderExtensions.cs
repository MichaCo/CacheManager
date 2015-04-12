using System;
using CacheManager.Core.Configuration;
using CacheManager.Memcached;

namespace CacheManager.Core
{
    /// <summary>
    /// Extensions for the configuration builder specific to the memcached cache handle.
    /// </summary>
    public static class ConfigurationBuilderExtensions
    {
        /// <summary>
        /// Add a <see cref="MemcachedCacheHandle"/> with the required name.
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="handleName">The name to be used for the cache handle.</param>
        /// <returns>The part.</returns>
        /// <exception cref="ArgumentNullException">Thrown if handleName is null.</exception>
        public static ConfigurationBuilderCacheHandlePart WithMemcachedCacheHandle(this ConfigurationBuilderCachePart part, string handleName)
        {
            return WithMemcachedCacheHandle(part, handleName, false);
        }

        /// <summary>
        /// Add a <see cref="MemcachedCacheHandle"/> with the required name.
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="handleName">The name to be used for the cache handle.</param>
        /// <param name="isBackPlateSource">
        /// Set this to true if this cache handle should be the source of the back plate.
        /// <para>This setting will be ignored if no back plate is configured.</para>
        /// </param>
        /// <returns>The part.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if handleName or handleType are null.
        /// </exception>
        public static ConfigurationBuilderCacheHandlePart WithMemcachedCacheHandle(this ConfigurationBuilderCachePart part, string handleName, bool isBackPlateSource)
        {
            return part.WithHandle(typeof(MemcachedCacheHandle<>), handleName, isBackPlateSource);
        }
    }
}