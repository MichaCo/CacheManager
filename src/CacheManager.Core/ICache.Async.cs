using System;
using System.Threading.Tasks;

namespace CacheManager.Core
{
    public partial interface ICache<TCacheValue>
    {
#if !NET40

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
        ValueTask<bool> AddAsync(string key, TCacheValue value);

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
        ValueTask<bool> AddAsync(string key, TCacheValue value, string region);
        
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
        ValueTask<bool> AddAsync(CacheItem<TCacheValue> item);

        /// <summary>
        /// Clears this cache, removing all items in the base cache and all regions.
        /// </summary>
        ValueTask ClearAsync();

        /// <summary>
        /// Clears the cache region, removing all items from the specified <paramref name="region"/> only.
        /// </summary>
        /// <param name="region">The cache region.</param>
        /// <exception cref="ArgumentNullException">If the <paramref name="region"/> is null.</exception>
        ValueTask ClearRegionAsync(string region);
        
        /// <summary>
        /// Returns a value indicating if the <paramref name="key"/> exists in at least one cache layer
        /// configured in CacheManger, without actually retrieving it from the cache.
        /// </summary>
        /// <param name="key">The cache key to check.</param>
        /// <returns><c>True</c> if the <paramref name="key"/> exists, <c>False</c> otherwise.</returns>
        ValueTask<bool> ExistsAsync(string key);

        /// <summary>
        /// Returns a value indicating if the <paramref name="key"/> in <paramref name="region"/> exists in at least one cache layer
        /// configured in CacheManger, without actually retrieving it from the cache (if supported).
        /// </summary>
        /// <param name="key">The cache key to check.</param>
        /// <param name="region">The cache region.</param>
        /// <returns><c>True</c> if the <paramref name="key"/> exists, <c>False</c> otherwise.</returns>
        ValueTask<bool> ExistsAsync(string key, string region);
        
        /// <summary>
        /// Gets a value for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The value being stored in the cache for the given <paramref name="key"/>.</returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="key"/> is null.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Get", Justification = "Maybe at some point.")]
        ValueTask<TCacheValue> GetAsync(string key);

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
        ValueTask<TCacheValue> GetAsync(string key, string region);

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
        ValueTask<TOut> GetAsync<TOut>(string key);

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
        ValueTask<TOut> GetAsync<TOut>(string key, string region);
        
        /// <summary>
        /// Gets the <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="key"/> is null.</exception>
        ValueTask<CacheItem<TCacheValue>> GetCacheItemAsync(string key);
        
        /// <summary>
        /// Gets the <c>CacheItem</c> for the specified key and region.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// If the <paramref name="key"/> or <paramref name="region"/> is null.
        /// </exception>
        ValueTask<CacheItem<TCacheValue>> GetCacheItemAsync(string key, string region);
        
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
        ValueTask PutAsync(string key, TCacheValue value);

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
        ValueTask PutAsync(string key, TCacheValue value, string region);
        
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
        ValueTask PutAsync(CacheItem<TCacheValue> item);
        
        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="key"/> is null.</exception>
        ValueTask<bool> RemoveAsync(string key);

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
        ValueTask<bool> RemoveAsync(string key, string region);

#endif
    }
}
