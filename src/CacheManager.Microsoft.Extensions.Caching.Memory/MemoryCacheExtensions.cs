using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;

namespace CacheManager.MicrosoftCachingMemory
{
    public static class MemoryCacheExtensions
    {
        public static bool Contains(this MemoryCache cache, object key)
        {
            object temp;
            return cache.TryGetValue(key, out temp);
        }

        internal static void RegisterChild(this MemoryCache cache, object parentKey, object childKey)
        {
            object temp;
            if (cache.TryGetValue(parentKey, out temp))
            {
                var set = (HashSet<object>)temp;
                set.Add(childKey);
            }
        }

        internal static void RemoveChilds(this MemoryCache cache, object key)
        {
            object temp;
            if (cache.TryGetValue(key, out temp))
            {
                var set = (HashSet<object>)temp;
                foreach (var item in set)
                    cache.Remove(item);
            }
        }
    }
}