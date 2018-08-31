using System;
using System.Threading.Tasks;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core.Internal
{
#if !NET40
    public partial class BaseCache<TCacheValue>
    {
        /// <summary>
        /// Adds a value for the specified key to the cache.
        /// <para>
        /// The <c>Add</c> method will <b>not</b> be successful if the specified
        /// <paramref name="key"/> already exists within the cache!
        /// </para>
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="value">The value which should be cached.</param>
        /// <returns>
        /// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If the <paramref name="key"/> or <paramref name="value"/> is null.
        /// </exception>
        public virtual ValueTask<bool> AddAsync(string key, TCacheValue value)
        {
            // null checks are done within ctor of the item
            var item = new CacheItem<TCacheValue>(key, value);
            return AddAsync(item);
        }

        /// <summary>
        /// Adds a value for the specified key and region to the cache.
        /// <para>
        /// The <c>Add</c> method will <b>not</b> be successful if the specified
        /// <paramref name="key"/> already exists within the cache!
        /// </para>
        /// <para>
        /// With <paramref name="region"/> specified, the key will <b>not</b> be found in the global cache.
        /// </para>
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="value">The value which should be cached.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>
        /// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If the <paramref name="key"/>, <paramref name="value"/> or <paramref name="region"/> is null.
        /// </exception>
        public virtual ValueTask<bool> AddAsync(string key, TCacheValue value, string region)
        {
            // null checks are done within ctor of the item
            var item = new CacheItem<TCacheValue>(key, region, value);
            return AddAsync(item);
        }
        
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
        public virtual ValueTask<bool> AddAsync(CacheItem<TCacheValue> item)
        {
            NotNull(item, nameof(item));

            return AddInternalAsync(item);
        }
        
        /// <summary>
        /// Clears this cache, removing all items in the base cache and all regions.
        /// </summary>
        public abstract ValueTask ClearAsync();

        /// <summary>
        /// Clears the cache region, removing all items from the specified <paramref name="region"/> only.
        /// </summary>
        /// <param name="region">The cache region.</param>
        /// <exception cref="ArgumentNullException">If the <paramref name="region"/> is null.</exception>
        public abstract ValueTask ClearRegionAsync(string region);
        
        /// <inheritdoc />
        public abstract ValueTask<bool> ExistsAsync(string key);

        /// <inheritdoc />
        public abstract ValueTask<bool> ExistsAsync(string key, string region);
        
        /// <summary>
        /// Gets a value for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The value being stored in the cache for the given <paramref name="key"/>.</returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="key"/> is null.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Get", Justification = "Maybe at some point.")]
        public virtual async ValueTask<TCacheValue> GetAsync(string key)
        {
            var item = await GetCacheItemAsync(key);

            if (item != null && item.Key.Equals(key))
            {
                return item.Value;
            }

            return default(TCacheValue);
        }

        /// <summary>
        /// Gets a value for the specified key and region.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>
        /// The value being stored in the cache for the given <paramref name="key"/> and <paramref name="region"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If the <paramref name="key"/> or <paramref name="region"/> is null.
        /// </exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Get", Justification = "Maybe at some point.")]
        public virtual async ValueTask<TCacheValue> GetAsync(string key, string region)
        {
            var item = await GetCacheItemAsync(key, region);

            if (item != null && item.Key.Equals(key) && item.Region != null && item.Region.Equals(region))
            {
                return item.Value;
            }

            return default(TCacheValue);
        }

        /// <summary>
        /// Gets a value for the specified key and will cast it to the specified type.
        /// </summary>
        /// <typeparam name="TOut">The type the value is converted and returned.</typeparam>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The value being stored in the cache for the given <paramref name="key"/>.</returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="key"/> is null.</exception>
        /// <exception cref="InvalidCastException">
        /// If no explicit cast is defined from <c>TCacheValue</c> to <c>TOut</c>.
        /// </exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Get", Justification = "Maybe at some point.")]
        public virtual async ValueTask<TOut> GetAsync<TOut>(string key)
        {
            object value = await GetAsync(key);
            return GetCasted<TOut>(value);
        }

        /// <summary>
        /// Gets a value for the specified key and region and will cast it to the specified type.
        /// </summary>
        /// <typeparam name="TOut">The type the cached value should be converted to.</typeparam>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>
        /// The value being stored in the cache for the given <paramref name="key"/> and <paramref name="region"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If the <paramref name="key"/> or <paramref name="region"/> is null.
        /// </exception>
        /// <exception cref="InvalidCastException">
        /// If no explicit cast is defined from <c>TCacheValue</c> to <c>TOut</c>.
        /// </exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Get", Justification = "Maybe at some point.")]
        public virtual async ValueTask<TOut> GetAsync<TOut>(string key, string region)
        {
            object value = await GetAsync(key, region);
            return GetCasted<TOut>(value);
        }
        
        /// <summary>
        /// Gets the <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="key"/> is null.</exception>
        public virtual ValueTask<CacheItem<TCacheValue>> GetCacheItemAsync(string key)
        {
            NotNullOrWhiteSpace(key, nameof(key));

            return GetCacheItemInternalAsync(key);
        }
        
        /// <summary>
        /// Gets the <c>CacheItem</c> for the specified key and region.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// If the <paramref name="key"/> or <paramref name="region"/> is null.
        /// </exception>
        public virtual ValueTask<CacheItem<TCacheValue>> GetCacheItemAsync(string key, string region)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNullOrWhiteSpace(region, nameof(region));

            return GetCacheItemInternalAsync(key, region);
        }

        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected abstract ValueTask<CacheItem<TCacheValue>> GetCacheItemInternalAsync(string key);
        
        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key and region.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected abstract ValueTask<CacheItem<TCacheValue>> GetCacheItemInternalAsync(string key, string region);
        
        /// <summary>
        /// Puts a value for the specified key into the cache.
        /// <para>
        /// If the <paramref name="key"/> already exists within the cache, the existing value will
        /// be replaced with the new <paramref name="value"/>.
        /// </para>
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="value">The value which should be cached.</param>
        /// <exception cref="ArgumentNullException">
        /// If the <paramref name="key"/> or <paramref name="value"/> is null.
        /// </exception>
        public virtual ValueTask PutAsync(string key, TCacheValue value)
        {
            var item = new CacheItem<TCacheValue>(key, value);
            return PutAsync(item);
        }

        /// <summary>
        /// Puts a value for the specified key and region into the cache.
        /// <para>
        /// If the <paramref name="key"/> already exists within the cache, the existing value will
        /// be replaced with the new <paramref name="value"/>.
        /// </para>
        /// <para>
        /// With <paramref name="region"/> specified, the key will <b>not</b> be found in the global cache.
        /// </para>
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="value">The value which should be cached.</param>
        /// <param name="region">The cache region.</param>
        /// <exception cref="ArgumentNullException">
        /// If the <paramref name="key"/>, <paramref name="value"/> or <paramref name="region"/> is null.
        /// </exception>
        public virtual ValueTask PutAsync(string key, TCacheValue value, string region)
        {
            var item = new CacheItem<TCacheValue>(key, region, value);
            return PutAsync(item);
        }
        
        /// <summary>
        /// Puts the specified <c>CacheItem</c> into the cache.
        /// <para>
        /// If the <paramref name="item"/> already exists within the cache, the existing item will
        /// be replaced with the new <paramref name="item"/>.
        /// </para>
        /// <para>
        /// Use this overload to overrule the configured expiration settings of the cache and to
        /// define a custom expiration for this <paramref name="item"/> only.
        /// </para>
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be cached.</param>
        /// <exception cref="ArgumentNullException">
        /// If the <paramref name="item"/> or the item's key or value is null.
        /// </exception>
        public virtual ValueTask PutAsync(CacheItem<TCacheValue> item)
        {
            NotNull(item, nameof(item));

            return PutInternalAsync(item);
        }
        
        /// <summary>
        /// Puts a value into the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        protected internal abstract ValueTask PutInternalAsync(CacheItem<TCacheValue> item);
        
        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="key"/> is null.</exception>
        public virtual ValueTask<bool> RemoveAsync(string key)
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
        public virtual ValueTask<bool> RemoveAsync(string key, string region)
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
        protected internal abstract ValueTask<bool> AddInternalAsync(CacheItem<TCacheValue> item);
        
        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        protected abstract ValueTask<bool> RemoveInternalAsync(string key);

        /// <summary>
        /// Removes a value from the cache for the specified key and region.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        protected abstract ValueTask<bool> RemoveInternalAsync(string key, string region);

    }
#endif
}
