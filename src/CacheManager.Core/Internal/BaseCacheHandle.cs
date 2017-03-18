using System;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core.Internal
{
    /// <summary>
    /// The <c>BaseCacheHandle</c> implements all the logic which might be common for all the cache
    /// handles. It abstracts the <see cref="ICache{T}"/> interface and defines new properties and
    /// methods the implementer must use.
    /// <para>Actually it is not advisable to not use <see cref="BaseCacheHandle{T}"/>.</para>
    /// </summary>
    /// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
    public abstract class BaseCacheHandle<TCacheValue> : BaseCache<TCacheValue>, IDisposable
    {
        private readonly object _updateLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseCacheHandle{TCacheValue}"/> class.
        /// </summary>
        /// <param name="managerConfiguration">The manager's configuration.</param>
        /// <param name="configuration">The configuration.</param>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="managerConfiguration"/> or <paramref name="configuration"/> are null.
        /// </exception>
        /// <exception cref="System.ArgumentException">If <paramref name="configuration"/> name is empty.</exception>
        protected BaseCacheHandle(ICacheManagerConfiguration managerConfiguration, CacheHandleConfiguration configuration)
        {
            NotNull(configuration, nameof(configuration));
            NotNull(managerConfiguration, nameof(managerConfiguration));
            NotNullOrWhiteSpace(configuration.Name, nameof(configuration.Name));

            Configuration = configuration;

            Stats = new CacheStats<TCacheValue>(
                managerConfiguration.Name,
                Configuration.Name,
                Configuration.EnableStatistics,
                Configuration.EnablePerformanceCounters);
        }

        internal event EventHandler<CacheItemRemovedEventArgs> OnCacheSpecificRemove;

        /// <summary>
        /// Gets the cache handle configuration.
        /// </summary>
        /// <value>The configuration.</value>
        public CacheHandleConfiguration Configuration { get; }

        /// <summary>
        /// Gets the number of items the cache handle currently maintains.
        /// </summary>
        /// <value>The count.</value>
        public abstract int Count { get; }

        /// <summary>
        /// Gets the cache stats object.
        /// </summary>
        /// <value>The stats.</value>
        public virtual CacheStats<TCacheValue> Stats { get; }

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
        /// <param name="key">The key to update.</param>
        /// <param name="updateValue">The function to perform the update.</param>
        /// <param name="maxRetries">The number of tries.</param>
        /// <returns>The update result which is interpreted by the cache manager.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="key"/> or <paramref name="updateValue"/> is null.
        /// </exception>
        /// <remarks>
        /// If the cache does not use a distributed cache system. Update is doing exactly the same
        /// as Get plus Put.
        /// </remarks>
        public virtual UpdateItemResult<TCacheValue> Update(string key, Func<TCacheValue, TCacheValue> updateValue, int maxRetries)
        {
            NotNull(updateValue, nameof(updateValue));
            CheckDisposed();

            lock (_updateLock)
            {
                var original = GetCacheItem(key);
                if (original == null)
                {
                    return UpdateItemResult.ForItemDidNotExist<TCacheValue>();
                }

                var newValue = updateValue(original.Value);

                if (newValue == null)
                {
                    return UpdateItemResult.ForFactoryReturnedNull<TCacheValue>();
                }

                var newItem = original.WithValue(newValue);
                newItem.LastAccessedUtc = DateTime.UtcNow;
                Put(newItem);
                return UpdateItemResult.ForSuccess(newItem);
            }
        }

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
        /// <param name="key">The key to update.</param>
        /// <param name="region">The cache region.</param>
        /// <param name="updateValue">The function to perform the update.</param>
        /// <param name="maxRetries">The number of tries.</param>
        /// <returns>The update result which is interpreted by the cache manager.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="key"/>, <paramref name="region"/> or <paramref name="updateValue"/> is null.
        /// </exception>
        /// <remarks>
        /// If the cache does not use a distributed cache system. Update is doing exactly the same
        /// as Get plus Put.
        /// </remarks>
        public virtual UpdateItemResult<TCacheValue> Update(string key, string region, Func<TCacheValue, TCacheValue> updateValue, int maxRetries)
        {
            NotNull(updateValue, nameof(updateValue));
            CheckDisposed();

            lock (_updateLock)
            {
                var original = GetCacheItem(key, region);
                if (original == null)
                {
                    return UpdateItemResult.ForItemDidNotExist<TCacheValue>();
                }

                var newValue = updateValue(original.Value);
                if (newValue == null)
                {
                    return UpdateItemResult.ForFactoryReturnedNull<TCacheValue>();
                }

                var newItem = original.WithValue(newValue);

                newItem.LastAccessedUtc = DateTime.UtcNow;
                Put(newItem);
                return UpdateItemResult.ForSuccess(newItem);
            }
        }

        /// <summary>
        /// Adds a value to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
        /// </returns>
        protected internal override bool AddInternal(CacheItem<TCacheValue> item)
        {
            CheckDisposed();
            item = GetItemExpiration(item);
            return AddInternalPrepared(item);
        }

        /// <summary>
        /// Puts the <paramref name="item"/> into the cache. If the item exists it will get updated
        /// with the new value. If the item doesn't exist, the item will be added to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        protected internal override void PutInternal(CacheItem<TCacheValue> item)
        {
            CheckDisposed();
            item = GetItemExpiration(item);
            PutInternalPrepared(item);
        }

        /// <summary>
        /// Can be used to signal a remove event to the <see cref="ICacheManager{TCacheValue}"/> in case the underlying cache supports this and the implementation
        /// can react on evictions and expiration of cache items.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="region">The cache region. Can be null.</param>
        /// <param name="reason">The reason.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="key"/> is null.</exception>
        protected void TriggerCacheSpecificRemove(string key, string region, CacheItemRemovedReason reason)
        {
            NotNullOrWhiteSpace(key, nameof(key));

            OnCacheSpecificRemove?.Invoke(this, new CacheItemRemovedEventArgs(key, region, reason));
        }

        /// <summary>
        /// Adds a value to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
        /// </returns>
        protected abstract bool AddInternalPrepared(CacheItem<TCacheValue> item);

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        /// <param name="disposeManaged">Indicator if managed resources should be released.</param>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                Stats.Dispose();
            }

            base.Dispose(disposeManaged);
        }

        /// <summary>
        /// Gets the item expiration.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>Returns the updated cache item.</returns>
        /// <exception cref="System.ArgumentNullException">If item is null.</exception>
        /// <exception cref="System.InvalidOperationException">
        /// If expiration mode is defined without timeout.
        /// </exception>
        protected virtual CacheItem<TCacheValue> GetItemExpiration(CacheItem<TCacheValue> item)
        {
            NotNull(item, nameof(item));

            // logic should be that the item setting overrules the handle setting if the item
            // doesn't define a mode (value is Default) it should use the handle's setting. if the
            // handle also doesn't define a mode (value is None|Default), we use None.
            var expirationMode = ExpirationMode.Default;
            var expirationTimeout = TimeSpan.Zero;
            var useItemExpiration = item.ExpirationMode != ExpirationMode.Default && !item.UsesExpirationDefaults;

            if (useItemExpiration)
            {
                expirationMode = item.ExpirationMode;
                expirationTimeout = item.ExpirationTimeout;
            }
            else if (Configuration.ExpirationMode != ExpirationMode.Default)
            {
                expirationMode = Configuration.ExpirationMode;
                expirationTimeout = Configuration.ExpirationTimeout;
            }

            if (expirationMode == ExpirationMode.Default || expirationMode == ExpirationMode.None)
            {
                expirationMode = ExpirationMode.None;
                expirationTimeout = TimeSpan.Zero;
            }
            else if (expirationTimeout == TimeSpan.Zero)
            {
                throw new InvalidOperationException("Expiration mode is defined without timeout.");
            }

            return item.WithExpiration(expirationMode, expirationTimeout, !useItemExpiration);
        }

        /// <summary>
        /// Puts the <paramref name="item"/> into the cache. If the item exists it will get updated
        /// with the new value. If the item doesn't exist, the item will be added to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        protected abstract void PutInternalPrepared(CacheItem<TCacheValue> item);
    }
}