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

        /// <summary>
        /// Clears this cache, removing all items in the base cache and all regions.
        /// </summary>
        public override ValueTask ClearAsync()
        {
            Clear();
            return new ValueTask();
        }

        /// <summary>
        /// Clears the cache region, removing all items from the specified <paramref name="region"/> only.
        /// </summary>
        /// <param name="region">The cache region.</param>
        /// <exception cref="ArgumentNullException">If the <paramref name="region"/> is null.</exception>
        public override ValueTask ClearRegionAsync(string region)
        {
            ClearRegion(region);
            return new ValueTask();
        }

        /// <inheritdoc />
        public override ValueTask<bool> ExistsAsync(string key)
        {
            var result = Exists(key);
            return new ValueTask<bool>(result);
        }

        /// <inheritdoc />
        public override ValueTask<bool> ExistsAsync(string key, string region)
        {
            var result = Exists(key, region);
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
        
        /// <summary>
        /// Puts the <paramref name="item"/> into the cache. If the item exists it will get updated
        /// with the new value. If the item doesn't exist, the item will be added to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        protected internal override ValueTask PutInternalAsync(CacheItem<TCacheValue> item)
        {
            CheckDisposed();
            item = GetItemExpiration(item);
            return PutInternalPreparedAsync(item);
        }

        /// <summary>
        /// Puts the <paramref name="item"/> into the cache. If the item exists it will get updated
        /// with the new value. If the item doesn't exist, the item will be added to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        protected virtual ValueTask PutInternalPreparedAsync(CacheItem<TCacheValue> item)
        {
            PutInternalPrepared(item);
            return new ValueTask();
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
