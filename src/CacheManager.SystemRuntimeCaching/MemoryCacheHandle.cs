using CacheManager.Core;
using CacheManager.Core.Configuration;

namespace CacheManager.SystemRuntimeCaching
{
    /// <summary>
    /// Cache handle implementation based on System.Runtime.Caching.MemoryCache.
    /// </summary>
    public class MemoryCacheHandle : MemoryCacheHandle<object>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryCacheHandle"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="configuration">The configuration.</param>
        public MemoryCacheHandle(ICacheManager<object> manager, CacheHandleConfiguration configuration)
            : base(manager, configuration)
        {
        }
    }
}