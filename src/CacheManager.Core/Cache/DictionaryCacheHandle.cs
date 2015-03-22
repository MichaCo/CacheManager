using CacheManager.Core.Configuration;

namespace CacheManager.Core.Cache
{
    public class DictionaryCacheHandle : DictionaryCacheHandle<object>
    {
        public DictionaryCacheHandle(ICacheManager<object> manager, ICacheHandleConfiguration configuration)
            : base(manager, configuration)
        {
        }
    }
}