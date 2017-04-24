using System;
using CacheManager.Web;

namespace CacheManager.Core
{
    /// <summary>
    /// Extensions for the configuration builder specific to System.Runtime.Caching cache handle.
    /// </summary>
    public static class WebConfigurationBuilderExtensions
    {
        /// <summary>
        /// Adds a <see cref="SystemWebCacheHandle{TCacheValue}" /> to the cache manager.
        /// This handle uses <c>System.Web.Caching.Cache</c> and requires <c>HttpContext.Current</c> to be not null.
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="isBackplaneSource">Set this to true if this cache handle should be the source of the backplane.
        /// This setting will be ignored if no backplane is configured.</param>
        /// <returns>
        /// The builder part.
        /// </returns>
        /// <returns>The builder part.</returns>
        public static ConfigurationBuilderCacheHandlePart WithSystemWebCacheHandle(this ConfigurationBuilderCachePart part, bool isBackplaneSource = false)
            => WithSystemWebCacheHandle(part, Guid.NewGuid().ToString("N"), isBackplaneSource);
        
        /// <summary>
        /// Adds a <see cref="SystemWebCacheHandle{TCacheValue}" /> to the cache manager.
        /// This handle uses <c>System.Web.Caching.Cache</c> and requires <c>HttpContext.Current</c> to be not null.
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
        public static ConfigurationBuilderCacheHandlePart WithSystemWebCacheHandle(this ConfigurationBuilderCachePart part, string instanceName, bool isBackplaneSource = false)
            => part?.WithHandle(typeof(SystemWebCacheHandle<>), instanceName, isBackplaneSource);
    }
}