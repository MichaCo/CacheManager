using System;
using CacheManager.Core.Configuration;
using CacheManager.SystemRuntimeCaching;

namespace CacheManager.Core
{    
    /// <summary>
    /// Extensions for the configuration builder specific to System.Runtime.Caching cache handle.
    /// </summary>
    public static class ConfigurationBuilderExtensions
    {
        /// <summary>
        /// Add a <see cref="MemoryCacheHandle"/> with the required name.
        /// </summary>
        /// <param name="handleName">The name to be used for the cache handle.</param>
        /// <param name="part">The builder part</param>
        /// <exception cref="ArgumentNullException">Thrown if handleName is null.</exception>
        public static ConfigurationBuilderCacheHandlePart<TCacheValue> WithSystemRuntimeCacheHandle<TCacheValue>(this ConfigurationBuilderCachePart<TCacheValue> part, string handleName)
        {
            return part.WithHandle<MemoryCacheHandle<TCacheValue>>(handleName);
        }

        /// <summary>
        /// Add a <see cref="MemoryCacheHandle"/> with the required name.
        /// </summary>
        /// <param name="part">The builder part</param>
        /// <param name="handleName">The name to be used for the cache handle.</param>
        /// <param name="isBackPlateSource">Set this to true if this cache handle should be the source of the back plate.
        /// <para>This setting will be ignored if no back plate is configured.</para>
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown if handleName or handleType are null.</exception>
        public static ConfigurationBuilderCacheHandlePart<TCacheValue> WithSystemRuntimeCacheHandle<TCacheValue>(this ConfigurationBuilderCachePart<TCacheValue> part, string handleName, bool isBackPlateSource)
        {
            return part.WithHandle<MemoryCacheHandle<TCacheValue>>(handleName, isBackPlateSource);
        }
    }
}