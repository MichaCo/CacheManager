using System;
using CacheManager.Core.Cache;
using CacheManager.Core.Configuration;

namespace CacheManager.Core
{
    /// <summary>
    /// Defines the contract of a cache handle.
    /// <para>For each supported cache system, there will be one implementation of this interface.</para>
    /// <para>
    /// The cache manager implementation in core assumes that every cache handle actually extends
    /// <see cref="BaseCacheHandle{TCacheValue}"/> and does not implement this interface directly.
    /// </para>
    /// </summary>
    /// <typeparam name="TCacheValue">The cache value type.</typeparam>
    public interface ICacheHandle<TCacheValue> : ICache<TCacheValue>
    {
        /// <summary>
        /// Gets the cache handle configuration.
        /// </summary>
        ICacheHandleConfiguration Configuration { get; }

        /// <summary>
        /// Gets the number of items the cache handle currently maintains.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the cache stats object.
        /// </summary>
        CacheStats<TCacheValue> Stats { get; }

        /// <summary>
        /// Updates an existing key in the cache.
        /// <para>
        /// The cache manager will make sure the update will always happen on the most recent version.
        /// </para>
        /// <para>
        /// If version conflicts occur, if for example multiple cache clients try to write the same
        /// key, and during the update process, someone else changed the value for the key, the
        /// cache manager will retry the operation.
        /// </para>
        /// <para>
        /// The <paramref name="updateValue"/> function will get invoked on each retry with the most
        /// recent value which is stored in cache.
        /// </para>
        /// </summary>
        /// <remarks>
        /// If the cache does not use a distributed cache system. Update is doing exactly the same
        /// as Get plus Put.
        /// </remarks>
        /// <param name="key">The key to update.</param>
        /// <param name="updateValue">The function to perform the update.</param>
        /// <param name="config">The cache configuration used to specify the update behavior.</param>
        /// <returns>The update result which is interpreted by the cache manager.</returns>
        UpdateItemResult Update(string key, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config);

        /// <summary>
        /// Updates an existing key in the cache.
        /// <para>
        /// The cache manager will make sure the update will always happen on the most recent version.
        /// </para>
        /// <para>
        /// If version conflicts occur, if for example multiple cache clients try to write the same
        /// key, and during the update process, someone else changed the value for the key, the
        /// cache manager will retry the operation.
        /// </para>
        /// <para>
        /// The <paramref name="updateValue"/> function will get invoked on each retry with the most
        /// recent value which is stored in cache.
        /// </para>
        /// </summary>
        /// <remarks>
        /// If the cache does not use a distributed cache system. Update is doing exactly the same
        /// as Get plus Put.
        /// </remarks>
        /// <param name="key">The key to update.</param>
        /// <param name="region">The region of the key to update.</param>
        /// <param name="updateValue">The function to perform the update.</param>
        /// <param name="config">The cache configuration used to specify the update behavior.</param>
        /// <returns>The update result which is interpreted by the cache manager.</returns>
        UpdateItemResult Update(string key, string region, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config);
    }
}