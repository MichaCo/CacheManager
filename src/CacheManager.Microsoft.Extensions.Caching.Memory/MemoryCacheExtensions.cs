using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;

namespace CacheManager.MicrosoftCachingMemory
{
    /// <summary>
    /// Extensions for the configuration builder specific to Microsoft.Extensions.Caching.Memory cache handle.
    /// </summary>
    internal static class MemoryCacheExtensions
    {
        /// <summary>
        /// Extension method to check if a key exists in the given <paramref name="cache"/> instance.
        /// </summary>
        /// <param name="cache">The cache instance.</param>
        /// <param name="key">The key.</param>
        /// <returns><c>True</c> if the key exists.</returns>
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

        internal static void RemoveChilds(this MemoryCache cache, object region)
        {
            object keys;
            if (cache.TryGetValue(region, out keys))
            {
                var keySet = (HashSet<object>)keys;
                foreach (var key in keySet)
                {
                    cache.Remove(key);
                }
            }
        }
    }
}