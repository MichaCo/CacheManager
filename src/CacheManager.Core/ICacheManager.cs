using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CacheManager.Core.Cache;
using CacheManager.Core.Configuration;

namespace CacheManager.Core
{
    /// <summary>
    /// This interface extends the <c>ICache</c> interface by some cache manager specific methods 
    /// and also defines the events someone can register with.
    /// </summary>
    /// <typeparam name="TCacheValue">The type of the cache item value.</typeparam>
    public interface ICacheManager<TCacheValue> : ICache<TCacheValue>
    {
        /// <summary>
        /// Gets a list of cache handles currently registered within the cache manager.
        /// </summary>
        /// <remarks>
        /// This list is read only, any changes to the returned list instance will not affect the 
        /// state of the cache manager instance!
        /// </remarks>
        /// <value>The cache handles.</value>
        IReadOnlyCollection<ICacheHandle<TCacheValue>> CacheHandles { get; }

        /// <summary>
        /// Adds a cache handle to the cache manager instance.
        /// </summary>
        /// <param name="handle">The cache handle.</param>
        void AddCacheHandle(ICacheHandle<TCacheValue> handle);

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>The configuration.</value>
        ICacheManagerConfiguration Configuration { get; }

        /// <summary>
        /// Updates an existing key in the cache.
        /// <para>
        /// The cache manager will make sure the update will always happen on the most recent version.
        /// </para>
        /// <para>
        /// If version conflicts occur, if for example multiple cache clients try to write the same 
        /// key, and during the update process, someone else changed the value for the key,
        /// the cache manager will retry the operation.
        /// </para>
        /// <para>
        /// The <paramref name="updateValue"/> function will get invoked on each retry with the 
        /// most recent value which is stored in cache.
        /// </para>
        /// </summary>
        /// <remarks>
        /// If the cache does not use a distributed cache system. Update is doing exactly the same as Get plus Put.
        /// </remarks>
        /// <param name="key">The key to update.</param>
        /// <param name="updateValue">The function to perform the update.</param>
        /// <returns><c>True</c> if the update operation was successfully, <c>False</c> otherwise.</returns>
        bool Update(string key, Func<TCacheValue, TCacheValue> updateValue);

        /// <summary>
        /// Updates an existing key in the cache.
        /// <para>
        /// The cache manager will make sure the update will always happen on the most recent version.
        /// </para>
        /// <para>
        /// If version conflicts occur, if for example multiple cache clients try to write the same 
        /// key, and during the update process, someone else changed the value for the key,
        /// the cache manager will retry the operation.
        /// </para>
        /// <para>
        /// The <paramref name="updateValue"/> function will get invoked on each retry with the 
        /// most recent value which is stored in cache.
        /// </para>
        /// </summary>
        /// <remarks>
        /// If the cache does not use a distributed cache system. Update is doing exactly the same as Get plus Put.
        /// </remarks>
        /// <param name="key">The key to update.</param>
        /// <param name="region">The region of the key to update.</param>
        /// <param name="updateValue">The function to perform the update.</param>
        /// <returns><c>True</c> if the update operation was successfully, <c>False</c> otherwise.</returns>
        bool Update(string key, string region, Func<TCacheValue, TCacheValue> updateValue);

        /// <summary>
        /// Updates an existing key in the cache.
        /// <para>
        /// The cache manager will make sure the update will always happen on the most recent version.
        /// </para>
        /// <para>
        /// If version conflicts occur, if for example multiple cache clients try to write the same 
        /// key, and during the update process, someone else changed the value for the key,
        /// the cache manager will retry the operation.
        /// </para>
        /// <para>
        /// The <paramref name="updateValue"/> function will get invoked on each retry with the 
        /// most recent value which is stored in cache.
        /// </para>
        /// </summary>
        /// <remarks>
        /// If the cache does not use a distributed cache system. Update is doing exactly the same as Get plus Put.
        /// </remarks>
        /// <param name="key">The key to update.</param>
        /// <param name="updateValue">The function to perform the update.</param>
        /// <param name="config">The cache configuration used to specify the update behavior.</param>
        /// <returns><c>True</c> if the update operation was successfully, <c>False</c> otherwise.</returns>
        bool Update(string key, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config);

        /// <summary>
        /// Updates an existing key in the cache.
        /// <para>
        /// The cache manager will make sure the update will always happen on the most recent version.
        /// </para>
        /// <para>
        /// If version conflicts occur, if for example multiple cache clients try to write the same 
        /// key, and during the update process, someone else changed the value for the key,
        /// the cache manager will retry the operation.
        /// </para>
        /// <para>
        /// The <paramref name="updateValue"/> function will get invoked on each retry with the 
        /// most recent value which is stored in cache.
        /// </para>
        /// </summary>
        /// <remarks>
        /// If the cache does not use a distributed cache system. Update is doing exactly the same as Get plus Put.
        /// </remarks>
        /// <param name="key">The key to update.</param>
        /// <param name="region">The region of the key to update.</param>
        /// <param name="updateValue">The function to perform the update.</param>
        /// <param name="config">The cache configuration used to specify the update behavior.</param>
        /// <returns><c>True</c> if the update operation was successfully, <c>False</c> otherwise.</returns>
        bool Update(string key, string region, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config);

        /// <summary>
        /// Occurs when an item was successfully removed from the cache.
        /// </summary>
        event EventHandler<CacheActionEventArgs> OnRemove;

        /// <summary>
        /// Occurs when an item was successfully added to the cache.
        /// <para>
        /// The event will not get triggered if <c>Add</c> would return false.
        /// </para>
        /// </summary>
        event EventHandler<CacheActionEventArgs> OnAdd;

        /// <summary>
        /// Occurs when an item was put into the cache.
        /// </summary>
        event EventHandler<CacheActionEventArgs> OnPut;

        /// <summary>
        /// Occurs when an item was successfully updated.
        /// </summary>
        event EventHandler<CacheUpdateEventArgs> OnUpdate;

        /// <summary>
        /// Occurs when an item was retrieved from the cache.
        /// <para>
        /// The event will only get triggered on cache hit. Misses do not trigger!
        /// </para>
        /// </summary>
        event EventHandler<CacheActionEventArgs> OnGet;

        /// <summary>
        /// Occurs when <c>Clear</c> gets called, after the cache has been cleared.
        /// </summary>
        event EventHandler<CacheClearEventArgs> OnClear;

        /// <summary>
        /// Occurs when <c>ClearRegion</c> gets called, after the cache region has been cleared.
        /// </summary>
        event EventHandler<CacheClearRegionEventArgs> OnClearRegion;
    }
}