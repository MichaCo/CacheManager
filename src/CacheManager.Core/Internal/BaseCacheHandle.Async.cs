using System;
using System.Threading.Tasks;
using CacheManager.Core.Logging;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core.Internal
{
#if !NET40
    public abstract partial class BaseCacheHandle<TCacheValue>
    {
        /// <inheritdoc />
        protected internal override ValueTask<bool> AddInternalAsync(CacheItem<TCacheValue> item)
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
        protected virtual ValueTask<bool> AddInternalPreparedAsync(CacheItem<TCacheValue> item)
        {
            var result = AddInternalPrepared(item);
            return new ValueTask<bool>(result);
        }

        /// <inheritdoc />
        protected override ValueTask<CacheItem<TCacheValue>> GetCacheItemInternalAsync(string key)
        {
            var result = GetCacheItemInternal(key);
            return new ValueTask<CacheItem<TCacheValue>>(result);
        }

        /// <inheritdoc />
        protected override ValueTask<CacheItem<TCacheValue>> GetCacheItemInternalAsync(string key, string region)
        {
            var result = GetCacheItemInternal(key, region);
            return new ValueTask<CacheItem<TCacheValue>>(result);
        }
        
        /// <inheritdoc />
        protected override ValueTask<bool> RemoveInternalAsync(string key)
        { 
            var result = RemoveInternal(key);
            return new ValueTask<bool>(result);
        }

        /// <inheritdoc />
        protected override ValueTask<bool> RemoveInternalAsync(string key, string region)
        {
            var result = RemoveInternal(key, region);
            return new ValueTask<bool>(result);
        }
    }
#endif
}
