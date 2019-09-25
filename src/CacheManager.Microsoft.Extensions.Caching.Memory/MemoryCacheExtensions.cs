using System;
using System.Collections.Concurrent;
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
            if (cache.TryGetValue(parentKey, out var keys))
            {
                var keySet = (ConcurrentDictionary<object, bool>)keys;
                keySet.TryAdd(childKey, true);
            }
        }

        internal static void RemoveChilds(this MemoryCache cache, object region)
        {
            if (cache.TryGetValue(region, out var keys))
            {
                var keySet = (ConcurrentDictionary<object, bool>)keys;
                foreach (var key in keySet.Keys)
                {
                    cache.Remove(key);
                }
            }
        }
    }
}