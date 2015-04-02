using System;
using CacheManager.Core.Configuration;

namespace CacheManager.Core.Cache
{
    /// <summary>
    /// The <c>BaseCacheHandle</c> implements all the logic which might be common for all the cache
    /// handles. It abstracts the <see cref="ICache{T}"/> and <see cref="ICacheHandle{T}"/>
    /// interface and defines new properties and methods the implementer must use.
    /// <para>
    /// Actually it is not advisable to not use <see cref="BaseCacheHandle{T}"/> and directly
    /// implement <see cref="ICacheHandle{T}"/>.
    /// </para>
    /// </summary>
    /// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
    public abstract class BaseCacheHandle<TCacheValue> : BaseCache<TCacheValue>, ICacheHandle<TCacheValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseCacheHandle{TCacheValue}"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="configuration">The configuration.</param>
        /// <exception cref="System.ArgumentNullException">
        /// If configuration or manager are null.
        /// </exception>
        /// <exception cref="System.ArgumentException">If configuration name is empty.</exception>
        protected BaseCacheHandle(ICacheManager<TCacheValue> manager, ICacheHandleConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }

            if (string.IsNullOrWhiteSpace(configuration.HandleName))
            {
                throw new ArgumentException("Configuration name cannot be empty.");
            }

            this.Configuration = configuration;

            this.Manager = manager;

            this.Stats = new CacheStats<TCacheValue>(
                this.Configuration.CacheName,
                this.Configuration.HandleName,
                this.Configuration.EnableStatistics,
                this.Configuration.EnablePerformanceCounters);
        }

        /// <summary>
        /// Gets the cache handle configuration.
        /// </summary>
        /// <value>The configuration.</value>
        public ICacheHandleConfiguration Configuration { get; private set; }

        /// <summary>
        /// Gets the number of items the cache handle currently maintains.
        /// </summary>
        /// <value>The count.</value>
        public abstract int Count { get; }

        /// <summary>
        /// Gets the cache manager the cache handle was added to.
        /// </summary>
        /// <value>The manager.</value>
        public ICacheManager<TCacheValue> Manager { get; private set; }

        /// <summary>
        /// Gets the cache stats object.
        /// </summary>
        /// <value>The stats.</value>
        public CacheStats<TCacheValue> Stats { get; private set; }

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
        /// <param name="config">The cache configuration used to specify the update behavior.</param>
        /// <returns>The update result which is interpreted by the cache manager.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// If key, updateValue or config are null.
        /// </exception>
        /// <remarks>
        /// If the cache does not use a distributed cache system. Update is doing exactly the same
        /// as Get plus Put.
        /// </remarks>
        public virtual UpdateItemResult Update(string key, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config)
        {
            if (updateValue == null)
            {
                throw new ArgumentNullException("updateValue");
            }

            var original = this.Get(key);
            if (original == null)
            {
                return new UpdateItemResult(false, false, 1);
            }

            var value = updateValue(original);
            this.Put(key, value);
            return new UpdateItemResult(false, true, 1);
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
        /// <param name="config">The cache configuration used to specify the update behavior.</param>
        /// <returns>The update result which is interpreted by the cache manager.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// If key, region, updateValue or config are null.
        /// </exception>
        /// <remarks>
        /// If the cache does not use a distributed cache system. Update is doing exactly the same
        /// as Get plus Put.
        /// </remarks>
        public virtual UpdateItemResult Update(string key, string region, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config)
        {
            if (updateValue == null)
            {
                throw new ArgumentNullException("updateValue");
            }
            var original = this.Get(key, region);
            if (original == null)
            {
                return new UpdateItemResult(false, false, 1);
            }

            var value = updateValue(original);
            this.Put(key, value, region);
            return new UpdateItemResult(false, true, 1);
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
            this.GetItemExpiration(item);
            return this.AddInternalPrepared(item);
        }

        /// <summary>
        /// Puts the <paramref name="item"/> into the cache. If the item exists it will get updated
        /// with the new value. If the item doesn't exist, the item will be added to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        protected internal override void PutInternal(CacheItem<TCacheValue> item)
        {
            this.GetItemExpiration(item);
            this.PutInternalPrepared(item);
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
                this.Stats.Dispose();
            }

            base.Dispose(disposeManaged);
        }

        /// <summary>
        /// Gets the item expiration.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <exception cref="System.ArgumentNullException">If item is null.</exception>
        /// <exception cref="System.InvalidOperationException">
        /// If expiration mode is defined without timeout.
        /// </exception>
        protected void GetItemExpiration(CacheItem<TCacheValue> item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            // logic should be that the item setting overrules the handle setting if the item
            // doesn't define a mode (value is None) it should use the handle's setting. if the
            // handle also doesn't define a mode (value is None), we use None.
            var expirationMode = ExpirationMode.None;
            var expirationTimeout = TimeSpan.Zero;

            if (item.ExpirationMode != ExpirationMode.None || this.Configuration.ExpirationMode != ExpirationMode.None)
            {
                expirationMode = item.ExpirationMode != ExpirationMode.None ? item.ExpirationMode : this.Configuration.ExpirationMode;

                // if a mode is defined, the item or the fallback (handle config) must have a
                // timeout defined.
                // ToDo: this check is pretty late, but the user can configure the CacheItem
                //       explicitly, so we have to catch it at this point.
                if (item.ExpirationTimeout == TimeSpan.Zero && this.Configuration.ExpirationTimeout == TimeSpan.Zero)
                {
                    throw new InvalidOperationException("Expiration mode is defined without timeout.");
                }

                expirationTimeout = item.ExpirationTimeout != TimeSpan.Zero ? item.ExpirationTimeout : this.Configuration.ExpirationTimeout;
            }

            // Fix issue 2: updating the item exp timeout and mode:
            item.ExpirationMode = expirationMode;
            item.ExpirationTimeout = expirationTimeout;
        }

        /// <summary>
        /// Puts the <paramref name="item"/> into the cache. If the item exists it will get updated
        /// with the new value. If the item doesn't exist, the item will be added to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        protected abstract void PutInternalPrepared(CacheItem<TCacheValue> item);
    }
}