using CacheManager.Core;
using CacheManager.Core.Configuration;

namespace CacheManager.AppFabricCache
{
    public class AppFabricCacheHandle : AppFabricCacheHandle<object>
    {
        public AppFabricCacheHandle(ICacheManager<object> manager, ICacheHandleConfiguration configuration)
            : base(manager, configuration)
        {
        }
    }
}