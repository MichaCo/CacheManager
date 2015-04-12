using CacheManager.Core.Configuration;

namespace CacheManager.Core.Cache
{
    /// <summary>
    /// Dictionary object cache.
    /// </summary>
    public class DictionaryCacheHandle : DictionaryCacheHandle<object>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DictionaryCacheHandle"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="configuration">The configuration.</param>
        public DictionaryCacheHandle(ICacheManager<object> manager, CacheHandleConfiguration configuration)
            : base(manager, configuration)
        {
        }
    }
}