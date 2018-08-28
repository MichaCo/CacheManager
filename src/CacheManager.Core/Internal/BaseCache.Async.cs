using System;
using System.Threading.Tasks;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core.Internal
{
#if !NET40
    public partial class BaseCache<TCacheValue>
    {
        /// <summary>
        /// Adds the specified <c>CacheItem</c> to the cache.
        /// <para>
        /// Use this overload to overrule the configured expiration settings of the cache and to
        /// define a custom expiration for this <paramref name="item"/> only.
        /// </para>
        /// <para>
        /// The <c>Add</c> method will <b>not</b> be successful if the specified
        /// <paramref name="item"/> already exists within the cache!
        /// </para>
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If the <paramref name="item"/> or the item's key or value is null.
        /// </exception>
        public virtual Task<bool> AddAsync(CacheItem<TCacheValue> item)
        {
            NotNull(item, nameof(item));

            return AddInternalAsync(item);
        }
        
        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="key"/> is null.</exception>
        public virtual Task<bool> RemoveAsync(string key)
        {
            NotNullOrWhiteSpace(key, nameof(key));

            return RemoveInternalAsync(key);
        }

        /// <summary>
        /// Removes a value from the cache for the specified key and region.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If the <paramref name="key"/> or <paramref name="region"/> is null.
        /// </exception>
        public virtual Task<bool> RemoveAsync(string key, string region)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNullOrWhiteSpace(region, nameof(region));

            return RemoveInternalAsync(key, region);
        }

        
        /// <summary>
        /// Adds a value to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
        /// </returns>
        protected internal abstract Task<bool> AddInternalAsync(CacheItem<TCacheValue> item);
        
        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        protected abstract Task<bool> RemoveInternalAsync(string key);

        /// <summary>
        /// Removes a value from the cache for the specified key and region.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        protected abstract Task<bool> RemoveInternalAsync(string key, string region);

    }
#endif
}
