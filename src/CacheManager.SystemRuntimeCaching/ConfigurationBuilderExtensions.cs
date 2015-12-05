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
        /// Add a <see cref="MemoryCacheHandle" /> with the required name.
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="handleName">The name to be used for the cache handle.</param>
        /// <returns>The part.</returns>
        /// <exception cref="ArgumentNullException">Thrown if handleName is null.</exception>
        public static ConfigurationBuilderCacheHandlePart WithSystemRuntimeCacheHandle(this ConfigurationBuilderCachePart part, string handleName)
            => WithSystemRuntimeCacheHandle(part, handleName, false);

        /// <summary>
        /// Add a <see cref="MemoryCacheHandle" /> with the required name.
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="handleName">The name to be used for the cache handle.</param>
        /// <param name="isBackPlateSource">Set this to true if this cache handle should be the source of the back plate.
        /// <para>This setting will be ignored if no back plate is configured.</para></param>
        /// <returns>
        /// The part.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">If part is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown if handleName or handleType are null.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Not for extenions.")]
        public static ConfigurationBuilderCacheHandlePart WithSystemRuntimeCacheHandle(this ConfigurationBuilderCachePart part, string handleName, bool isBackPlateSource)
            => part.WithHandle(typeof(MemoryCacheHandle<>), handleName, isBackPlateSource);
    }
}