using CacheManager.Core;
using CacheManager.Core.Configuration;

namespace CacheManager.AppFabricCache
{
    /// <summary>
    /// A 'default' cache handle implementing AppFabricCache typed for <c>object</c>.
    /// </summary>
    public class AppFabricCacheHandle : AppFabricCacheHandle<object>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppFabricCacheHandle"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="configuration">The configuration.</param>
        public AppFabricCacheHandle(ICacheManager<object> manager, CacheHandleConfiguration configuration)
            : base(manager, configuration)
        {
        }
    }
}