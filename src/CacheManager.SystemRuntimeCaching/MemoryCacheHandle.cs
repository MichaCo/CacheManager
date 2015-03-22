using CacheManager.Core;
using CacheManager.Core.Configuration;

namespace CacheManager.SystemRuntimeCaching
{
    public class MemoryCacheHandle : MemoryCacheHandle<object>
    {
        public MemoryCacheHandle(ICacheManager<object> manager, ICacheHandleConfiguration configuration)
            : base(manager, configuration)
        {
        }
    }
}