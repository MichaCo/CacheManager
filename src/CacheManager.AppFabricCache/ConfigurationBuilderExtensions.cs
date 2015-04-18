using System;
using CacheManager.AppFabricCache;
using CacheManager.Core.Configuration;

namespace CacheManager.Core
{
    /// <summary>
    /// Extensions for the configuration builder specific to AppFabricCache.
    /// </summary>
    public static class ConfigurationBuilderExtensions
    {
        /// <summary>
        /// Add a <see cref="AppFabricCacheHandle"/> with the required name.
        /// </summary>
        /// <param name="part">The builder.</param>
        /// <param name="handleName">The name to be used for the cache handle.</param>
        /// <returns>The builder part.</returns>
        /// <exception cref="ArgumentNullException">Thrown if handleName is null.</exception>
        public static ConfigurationBuilderCacheHandlePart WithAppFabricCacheHandle(this ConfigurationBuilderCachePart part, string handleName)
        {
            return WithAppFabricCacheHandle(part, handleName, false);
        }

        /// <summary>
        /// Add a <see cref="AppFabricCacheHandle"/> with the required name.
        /// </summary>
        /// <param name="part">The builder.</param>
        /// <param name="handleName">The name to be used for the cache handle.</param>
        /// <param name="isBackPlateSource">
        /// Set this to true if this cache handle should be the source of the back plate.
        /// <para>This setting will be ignored if no back plate is configured.</para>
        /// </param>
        /// <returns>The builder part.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if handleName or handleType are null.
        /// </exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Not for extenions.")]
        public static ConfigurationBuilderCacheHandlePart WithAppFabricCacheHandle(this ConfigurationBuilderCachePart part, string handleName, bool isBackPlateSource)
        {
            return part.WithHandle(typeof(AppFabricCacheHandle<>), handleName, isBackPlateSource);
        }
    }
}