using System;
using System.Collections.Generic;
using CacheManager.Core.Internal;

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
        /// Occurs when an item was successfully added to the cache.
        /// <para>The event will not get triggered if <c>Add</c> would return false.</para>
        /// </summary>
        event EventHandler<CacheActionEventArgs> OnAdd;

        /// <summary>
        /// Occurs when <c>Clear</c> gets called, after the cache has been cleared.
        /// </summary>
        event EventHandler<CacheClearEventArgs> OnClear;

        /// <summary>
        /// Occurs when <c>ClearRegion</c> gets called, after the cache region has been cleared.
        /// </summary>
        event EventHandler<CacheClearRegionEventArgs> OnClearRegion;

        /// <summary>
        /// Occurs when an item was retrieved from the cache.
        /// <para>The event will only get triggered on cache hit. Misses do not trigger!</para>
        /// </summary>
        event EventHandler<CacheActionEventArgs> OnGet;

        /// <summary>
        /// Occurs when an item was put into the cache.
        /// </summary>
        event EventHandler<CacheActionEventArgs> OnPut;

        /// <summary>
        /// Occurs when an item was successfully removed from the cache.
        /// </summary>
        event EventHandler<CacheActionEventArgs> OnRemove;

        /// <summary>
        /// Occurs when an item was removed by the cache handle due to expiration or e.g. memory pressure eviction.
        /// The <see cref="CacheItemRemovedEventArgs.Reason"/> property indicates the reason while the <see cref="CacheItemRemovedEventArgs.Level"/> indicates 
        /// which handle triggered the event.
        /// </summary>
        event EventHandler<CacheItemRemovedEventArgs> OnRemoveByHandle;

        /// <summary>
        /// Occurs when an item was successfully updated.
        /// </summary>
        event EventHandler<CacheActionEventArgs> OnUpdate;

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>The configuration.</value>
        IReadOnlyCacheManagerConfiguration Configuration { get; }

        /// <summary>
        /// Gets the cache name.
        /// </summary>
        /// <value>The cache name.</value>
        string Name { get; }

        /// <summary>
        /// Gets a list of cache handles currently registered within the cache manager.
        /// </summary>
        /// <value>The cache handles.</value>
        /// <remarks>
        /// This list is read only, any changes to the returned list instance will not affect the
        /// state of the cache manager instance.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "Will usually not be used in public API anyways.")]
        IEnumerable<BaseCacheHandle<TCacheValue>> CacheHandles { get; }

        /// <summary>
        /// Adds an item to the cache or, if the item already exists, updates the item using the
        /// <paramref name="updateValue"/> function.
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
        /// <param name="key">The key to update.</param>
        /// <param name="addValue">
        /// The value which should be added in case the item doesn't already exist.
        /// </param>
        /// <param name="updateValue">
        /// The function to perform the update in case the item does already exist.
        /// </param>
        /// <returns>
        /// The value which has been added or updated, or null, if the update was not successful.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="key"/> or <paramref name="updateValue"/> are null.
        /// </exception>
        /// <remarks>
        /// If the cache does not use a distributed cache system. Update is doing exactly the same
        /// as Get plus Put.
        /// </remarks>
        TCacheValue AddOrUpdate(string key, TCacheValue addValue, Func<TCacheValue, TCacheValue> updateValue);

        /// <summary>
        /// Adds an item to the cache or, if the item already exists, updates the item using the
        /// <paramref name="updateValue"/> function.
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
        /// <param name="key">The key to update.</param>
        /// <param name="region">The region of the key to update.</param>
        /// <param name="addValue">
        /// The value which should be added in case the item doesn't already exist.
        /// </param>
        /// <param name="updateValue">
        /// The function to perform the update in case the item does already exist.
        /// </param>
        /// <returns>
        /// The value which has been added or updated, or null, if the update was not successful.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="key"/> or <paramref name="region"/> or <paramref name="updateValue"/>
        /// are null.
        /// </exception>
        /// <remarks>
        /// If the cache does not use a distributed cache system. Update is doing exactly the same
        /// as Get plus Put.
        /// </remarks>
        TCacheValue AddOrUpdate(string key, string region, TCacheValue addValue, Func<TCacheValue, TCacheValue> updateValue);

        /// <summary>
        /// Adds an item to the cache or, if the item already exists, updates the item using the
        /// <paramref name="updateValue"/> function.
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
        /// <param name="key">The key to update.</param>
        /// <param name="addValue">
        /// The value which should be added in case the item doesn't already exist.
        /// </param>
        /// <param name="updateValue">
        /// The function to perform the update in case the item does already exist.
        /// </param>
        /// <param name="maxRetries">
        /// The number of tries which should be performed in case of version conflicts.
        /// If the cache cannot perform an update within the number of <paramref name="maxRetries"/>,
        /// this method will return <c>Null</c>.
        /// </param>
        /// <returns>
        /// The value which has been added or updated, or null, if the update was not successful.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="key"/> or <paramref name="updateValue"/> is null.
        /// </exception>
        /// <remarks>
        /// If the cache does not use a distributed cache system. Update is doing exactly the same
        /// as Get plus Put.
        /// </remarks>
        TCacheValue AddOrUpdate(string key, TCacheValue addValue, Func<TCacheValue, TCacheValue> updateValue, int maxRetries);

        /// <summary>
        /// Adds an item to the cache or, if the item already exists, updates the item using the
        /// <paramref name="updateValue"/> function.
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
        /// <param name="key">The key to update.</param>
        /// <param name="region">The region of the key to update.</param>
        /// <param name="addValue">
        /// The value which should be added in case the item doesn't already exist.
        /// </param>
        /// <param name="updateValue">
        /// The function to perform the update in case the item does already exist.
        /// </param>
        /// <param name="maxRetries">
        /// The number of tries which should be performed in case of version conflicts.
        /// If the cache cannot perform an update within the number of <paramref name="maxRetries"/>,
        /// this method will return <c>Null</c>.
        /// </param>
        /// <returns>
        /// The value which has been added or updated, or null, if the update was not successful.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="key"/> or <paramref name="region"/> or <paramref name="updateValue"/> is null.
        /// </exception>
        /// <remarks>
        /// If the cache does not use a distributed cache system. Update is doing exactly the same
        /// as Get plus Put.
        /// </remarks>
        TCacheValue AddOrUpdate(string key, string region, TCacheValue addValue, Func<TCacheValue, TCacheValue> updateValue, int maxRetries);

        /// <summary>
        /// Adds an item to the cache or, if the item already exists, updates the item using the
        /// <paramref name="updateValue"/> function.
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
        /// <param name="addItem">The item which should be added or updated.</param>
        /// <param name="updateValue">The function to perform the update, if the item does exist.</param>
        /// <returns>
        /// The value which has been added or updated, or null, if the update was not successful.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="addItem"/> or <paramref name="updateValue"/> are null.
        /// </exception>
        TCacheValue AddOrUpdate(CacheItem<TCacheValue> addItem, Func<TCacheValue, TCacheValue> updateValue);

        /// <summary>
        /// Adds an item to the cache or, if the item already exists, updates the item using the
        /// <paramref name="updateValue"/> function.
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
        /// <param name="addItem">The item which should be added or updated.</param>
        /// <param name="updateValue">The function to perform the update, if the item does exist.</param>
        /// <param name="maxRetries">
        /// The number of tries which should be performed in case of version conflicts.
        /// If the cache cannot perform an update within the number of <paramref name="maxRetries"/>,
        /// this method will return <c>Null</c>.
        /// </param>
        /// <returns>
        /// The value which has been added or updated, or null, if the update was not successful.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="addItem"/> or <paramref name="updateValue"/> is null.
        /// </exception>
        TCacheValue AddOrUpdate(CacheItem<TCacheValue> addItem, Func<TCacheValue, TCacheValue> updateValue, int maxRetries);

        /// <summary>
        /// Returns an existing item or adds the item to the cache if it does not exist.
        /// The method returns either the existing item's value or the newly added <paramref name="value"/>.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value which should be added.</param>
        /// <returns>Either the added or the existing value.</returns>
        /// <exception cref="ArgumentException">
        /// If either <paramref name="key"/> or <paramref name="value"/> is null.
        /// </exception>
        TCacheValue GetOrAdd(string key, TCacheValue value);

        /// <summary>
        /// Returns an existing item or adds the item to the cache if it does not exist.
        /// The method returns either the existing item's value or the newly added <paramref name="value"/>.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="region">The cache region.</param>
        /// <param name="value">The value which should be added.</param>
        /// <returns>Either the added or the existing value.</returns>
        /// <exception cref="ArgumentException">
        /// If either <paramref name="key"/>, <paramref name="region"/> or <paramref name="value"/> is null.
        /// </exception>
        TCacheValue GetOrAdd(string key, string region, TCacheValue value);

        /// <summary>
        /// Returns an existing item or adds the item to the cache if it does not exist.
        /// The <paramref name="valueFactory"/> will be evaluated only if the item does not exist.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="valueFactory">The method which creates the value which should be added.</param>
        /// <returns>Either the added or the existing value.</returns>
        /// <exception cref="ArgumentException">
        /// If either <paramref name="key"/> or <paramref name="valueFactory"/> is null.
        /// </exception>
        TCacheValue GetOrAdd(string key, Func<string, TCacheValue> valueFactory);

        /// <summary>
        /// Returns an existing item or adds the item to the cache if it does not exist.
        /// The <paramref name="valueFactory"/> will be evaluated only if the item does not exist.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="region">The cache region.</param>
        /// <param name="valueFactory">The method which creates the value which should be added.</param>
        /// <returns>Either the added or the existing value.</returns>
        /// <exception cref="ArgumentException">
        /// If either <paramref name="key"/> or <paramref name="valueFactory"/> is null.
        /// </exception>
        TCacheValue GetOrAdd(string key, string region, Func<string, string, TCacheValue> valueFactory);

        /// <summary>
        /// Tries to either retrieve an existing item or add the item to the cache if it does not exist.
        /// The <paramref name="valueFactory"/> will be evaluated only if the item does not exist.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="valueFactory">The method which creates the value which should be added.</param>
        /// <param name="value">The cache value.</param>
        /// <returns><c>True</c> if the operation succeeds, <c>False</c> in case there are too many retries or the <paramref name="valueFactory"/> returns null.</returns>
        /// <exception cref="ArgumentException">
        /// If either <paramref name="key"/> or <paramref name="valueFactory"/> is null.
        /// </exception>
        bool TryGetOrAdd(string key, Func<string, TCacheValue> valueFactory, out TCacheValue value);

        /// <summary>
        /// Tries to either retrieve an existing item or add the item to the cache if it does not exist.
        /// The <paramref name="valueFactory"/> will be evaluated only if the item does not exist.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="region">The cache region.</param>
        /// <param name="valueFactory">The method which creates the value which should be added.</param>
        /// <param name="value">The cache value.</param>
        /// <returns><c>True</c> if the operation succeeds, <c>False</c> in case there are too many retries or the <paramref name="valueFactory"/> returns null.</returns>
        /// <exception cref="ArgumentException">
        /// If either <paramref name="key"/> or <paramref name="valueFactory"/> is null.
        /// </exception>
        bool TryGetOrAdd(string key, string region, Func<string, string, TCacheValue> valueFactory, out TCacheValue value);

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
        /// <returns>The updated value, or null, if the update was not successful.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="key"/> or <paramref name="updateValue"/> are null.
        /// </exception>
        TCacheValue Update(string key, Func<TCacheValue, TCacheValue> updateValue);

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
        /// <returns>The updated value, or null, if the update was not successful.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="key"/> or <paramref name="region"/> or <paramref name="updateValue"/>
        /// are null.
        /// </exception>
        TCacheValue Update(string key, string region, Func<TCacheValue, TCacheValue> updateValue);

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
        /// <param name="maxRetries">
        /// The number of tries which should be performed in case of version conflicts.
        /// If the cache cannot perform an update within the number of <paramref name="maxRetries"/>,
        /// this method will return <c>Null</c>.
        /// </param>
        /// <returns>The updated value, or null, if the update was not successful.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="key"/> or <paramref name="updateValue"/> is null.
        /// </exception>
        TCacheValue Update(string key, Func<TCacheValue, TCacheValue> updateValue, int maxRetries);

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
        /// <param name="maxRetries">
        /// The number of tries which should be performed in case of version conflicts.
        /// If the cache cannot perform an update within the number of <paramref name="maxRetries"/>,
        /// this method will return <c>Null</c>.
        /// </param>
        /// <returns>The updated value, or null, if the update was not successful.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="key"/> or <paramref name="region"/> or <paramref name="updateValue"/> is null.
        /// </exception>
        TCacheValue Update(string key, string region, Func<TCacheValue, TCacheValue> updateValue, int maxRetries);

        /// <summary>
        /// Tries to update an existing key in the cache.
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
        /// <param name="key">The key to update.</param>
        /// <param name="updateValue">The function to perform the update.</param>
        /// <param name="value">The updated value, or null, if the update was not successful.</param>
        /// <returns><c>True</c> if the update operation was successful, <c>False</c> otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="key"/> or <paramref name="updateValue"/> are null.
        /// </exception>
        /// <remarks>
        /// If the cache does not use a distributed cache system. Update is doing exactly the same
        /// as Get plus Put.
        /// </remarks>
        bool TryUpdate(string key, Func<TCacheValue, TCacheValue> updateValue, out TCacheValue value);

        /// <summary>
        /// Tries to update an existing key in the cache.
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
        /// <param name="key">The key to update.</param>
        /// <param name="region">The region of the key to update.</param>
        /// <param name="updateValue">The function to perform the update.</param>
        /// <param name="value">The updated value, or null, if the update was not successful.</param>
        /// <returns><c>True</c> if the update operation was successful, <c>False</c> otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="key"/> or <paramref name="region"/> or <paramref name="updateValue"/>
        /// are null.
        /// </exception>
        /// <remarks>
        /// If the cache does not use a distributed cache system. Update is doing exactly the same
        /// as Get plus Put.
        /// </remarks>
        bool TryUpdate(string key, string region, Func<TCacheValue, TCacheValue> updateValue, out TCacheValue value);

        /// <summary>
        /// Tries to update an existing key in the cache.
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
        /// <param name="key">The key to update.</param>
        /// <param name="updateValue">The function to perform the update.</param>
        /// <param name="maxRetries">
        /// The number of tries which should be performed in case of version conflicts.
        /// If the cache cannot perform an update within the number of <paramref name="maxRetries"/>,
        /// this method will return <c>False</c>.
        /// </param>
        /// <param name="value">The updated value, or null, if the update was not successful.</param>
        /// <returns><c>True</c> if the update operation was successful, <c>False</c> otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="key"/> or <paramref name="updateValue"/> is null.
        /// </exception>
        /// <remarks>
        /// If the cache does not use a distributed cache system. Update is doing exactly the same
        /// as Get plus Put.
        /// </remarks>
        bool TryUpdate(string key, Func<TCacheValue, TCacheValue> updateValue, int maxRetries, out TCacheValue value);

        /// <summary>
        /// Tries to update an existing key in the cache.
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
        /// <param name="key">The key to update.</param>
        /// <param name="region">The region of the key to update.</param>
        /// <param name="updateValue">The function to perform the update.</param>
        /// <param name="maxRetries">
        /// The number of tries which should be performed in case of version conflicts.
        /// If the cache cannot perform an update within the number of <paramref name="maxRetries"/>,
        /// this method will return <c>False</c>.
        /// </param>
        /// <param name="value">The updated value, or null, if the update was not successful.</param>
        /// <returns><c>True</c> if the update operation was successful, <c>False</c> otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="key"/> or <paramref name="region"/> or <paramref name="updateValue"/> is null.
        /// </exception>
        /// <remarks>
        /// If the cache does not use a distributed cache system. Update is doing exactly the same
        /// as Get plus Put.
        /// </remarks>
        bool TryUpdate(string key, string region, Func<TCacheValue, TCacheValue> updateValue, int maxRetries, out TCacheValue value);
    }
}