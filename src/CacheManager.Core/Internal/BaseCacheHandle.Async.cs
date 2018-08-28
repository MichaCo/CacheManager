using System;
using System.Threading.Tasks;
using CacheManager.Core.Logging;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core.Internal
{
#if !NET40
    public abstract partial class BaseCacheHandle<TCacheValue>
    {
        /// <summary>
        /// Adds a value to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
        /// </returns>
        protected internal override Task<bool> AddInternalAsync(CacheItem<TCacheValue> item)
        {
            CheckDisposed();
            item = GetItemExpiration(item);
            return AddInternalPreparedAsync(item);
        }

        /// <summary>
        /// Adds a value to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
        /// </returns>
        protected virtual Task<bool> AddInternalPreparedAsync(CacheItem<TCacheValue> item)
        {
            var result = AddInternalPrepared(item);
            return Task.FromResult(result);
        }

        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        protected override Task<bool> RemoveInternalAsync(string key)
        { 
            var result = RemoveInternal(key);
            return Task.FromResult(result);
        }

        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        protected override Task<bool> RemoveInternalAsync(string key, string region)
        {
            var result = RemoveInternal(key, region);
            return Task.FromResult(result);
        }
    }
#endif
}
