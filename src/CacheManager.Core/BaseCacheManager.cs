using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using CacheManager.Core.Internal;
using CacheManager.Core.Logging;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core
{
    /// <summary>
    /// The <see cref="BaseCacheManager{TCacheValue}"/> implements <see cref="ICacheManager{TCacheValue}"/> and is the main class
    /// of this library.
    /// The cache manager delegates all cache operations to the list of <see cref="BaseCacheHandle{T}"/>'s which have been
    /// added. It will keep them in sync according to rules and depending on the configuration.
    /// </summary>
    /// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
    public sealed class BaseCacheManager<TCacheValue> : BaseCache<TCacheValue>, ICacheManager<TCacheValue>, IDisposable
    {
        private readonly bool logTrace = false;
        private readonly BaseCacheHandle<TCacheValue>[] cacheHandles;
        private readonly CacheBackplane cacheBackplane;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseCacheManager{TCacheValue}"/> class
        /// using the specified <paramref name="configuration"/>.
        /// If the name of the <paramref name="configuration"/> is defined, the cache manager will
        /// use it. Otherwise a random string will be generated.
        /// </summary>
        /// <param name="configuration">
        /// The configuration which defines the structure and complexity of the cache manager.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// When <paramref name="configuration"/> is null.
        /// </exception>
        /// <see cref="CacheFactory"/>
        /// <see cref="ConfigurationBuilder"/>
        /// <see cref="BaseCacheHandle{TCacheValue}"/>
        public BaseCacheManager(CacheManagerConfiguration configuration)
            : this(configuration?.Name ?? Guid.NewGuid().ToString(), configuration)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseCacheManager{TCacheValue}"/> class
        /// using the specified <paramref name="name"/> and <paramref name="configuration"/>.
        /// </summary>
        /// <param name="name">The cache name.</param>
        /// <param name="configuration">
        /// The configuration which defines the structure and complexity of the cache manager.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// When <paramref name="name"/> or <paramref name="configuration"/> is null.
        /// </exception>
        /// <see cref="CacheFactory"/>
        /// <see cref="ConfigurationBuilder"/>
        /// <see cref="BaseCacheHandle{TCacheValue}"/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "nope")]
        private BaseCacheManager(string name, CacheManagerConfiguration configuration)
        {
            NotNullOrWhiteSpace(name, nameof(name));
            NotNull(configuration, nameof(configuration));

            this.Name = name;
            this.Configuration = configuration;

            var loggerFactory = CacheReflectionHelper.CreateLoggerFactory(configuration);
            var serializer = CacheReflectionHelper.CreateSerializer(configuration, loggerFactory);

            this.Logger = loggerFactory.CreateLogger(this);
            this.logTrace = this.Logger.IsEnabled(LogLevel.Trace);
            this.Logger.LogInfo("Cache manager: adding cache handles...");
            try
            {
                this.cacheHandles = CacheReflectionHelper.CreateCacheHandles(this, loggerFactory, serializer).ToArray();

                this.cacheBackplane = CacheReflectionHelper.CreateBackplane(configuration, loggerFactory);
                if (this.cacheBackplane != null)
                {
                    this.RegisterCacheBackplane(this.cacheBackplane);
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Error occurred while creating the cache manager.");
                throw;
            }
        }

        /// <summary>
        /// Occurs when an item was successfully added to the cache.
        /// <para>The event will not get triggered if <c>Add</c> would return false.</para>
        /// </summary>
        public event EventHandler<CacheActionEventArgs> OnAdd;

        /// <summary>
        /// Occurs when <c>Clear</c> gets called, after the cache has been cleared.
        /// </summary>
        public event EventHandler<CacheClearEventArgs> OnClear;

        /// <summary>
        /// Occurs when <c>ClearRegion</c> gets called, after the cache region has been cleared.
        /// </summary>
        public event EventHandler<CacheClearRegionEventArgs> OnClearRegion;

        /// <summary>
        /// Occurs when an item was retrieved from the cache.
        /// <para>The event will only get triggered on cache hit. Misses do not trigger!</para>
        /// </summary>
        public event EventHandler<CacheActionEventArgs> OnGet;

        /// <summary>
        /// Occurs when an item was put into the cache.
        /// </summary>
        public event EventHandler<CacheActionEventArgs> OnPut;

        /// <summary>
        /// Occurs when an item was successfully removed from the cache.
        /// </summary>
        public event EventHandler<CacheActionEventArgs> OnRemove;

        /// <summary>
        /// Occurs when an item was successfully updated.
        /// </summary>
        public event EventHandler<CacheActionEventArgs> OnUpdate;

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>The configuration.</value>
        public IReadOnlyCacheManagerConfiguration Configuration { get; }

        /// <summary>
        /// Gets a list of cache handles currently registered within the cache manager.
        /// </summary>
        /// <value>The cache handles.</value>
        /// <remarks>
        /// This list is read only, any changes to the returned list instance will not affect the
        /// state of the cache manager instance.
        /// </remarks>
        public IEnumerable<BaseCacheHandle<TCacheValue>> CacheHandles
            => new ReadOnlyCollection<BaseCacheHandle<TCacheValue>>(
                new List<BaseCacheHandle<TCacheValue>>(
                    this.cacheHandles));

        /// <summary>
        /// Gets the configured cache backplane.
        /// </summary>
        /// <value>The backplane.</value>
        public CacheBackplane Backplane => this.cacheBackplane;

        /// <summary>
        /// Gets the cache name.
        /// </summary>
        /// <value>The name of the cache.</value>
        public string Name { get; }

        /// <inheritdoc />
        protected override ILogger Logger { get; }

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
        public TCacheValue AddOrUpdate(string key, TCacheValue addValue, Func<TCacheValue, TCacheValue> updateValue) =>
            this.AddOrUpdate(key, addValue, updateValue, 50);

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
        public TCacheValue AddOrUpdate(string key, string region, TCacheValue addValue, Func<TCacheValue, TCacheValue> updateValue) =>
            this.AddOrUpdate(key, region, addValue, updateValue, 50);

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
        public TCacheValue AddOrUpdate(string key, TCacheValue addValue, Func<TCacheValue, TCacheValue> updateValue, int maxRetries) =>
            this.AddOrUpdate(new CacheItem<TCacheValue>(key, addValue), updateValue, maxRetries);

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
        public TCacheValue AddOrUpdate(string key, string region, TCacheValue addValue, Func<TCacheValue, TCacheValue> updateValue, int maxRetries) =>
            this.AddOrUpdate(new CacheItem<TCacheValue>(key, region, addValue), updateValue, maxRetries);

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
        public TCacheValue AddOrUpdate(CacheItem<TCacheValue> addItem, Func<TCacheValue, TCacheValue> updateValue) =>
            this.AddOrUpdate(addItem, updateValue, 50);

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
        public TCacheValue AddOrUpdate(CacheItem<TCacheValue> addItem, Func<TCacheValue, TCacheValue> updateValue, int maxRetries)
        {
            NotNull(addItem, nameof(addItem));
            NotNull(updateValue, nameof(updateValue));
            Ensure(maxRetries > 0, "Maximum number of retries must be greater than or equal to zero.");

            return this.AddOrUpdateInternal(addItem, updateValue, maxRetries);
        }

        /// <summary>
        /// Clears this cache, removing all items in the base cache and all regions.
        /// </summary>
        public override void Clear()
        {
            this.CheckDisposed();
            if (this.logTrace)
            {
                this.Logger.LogTrace("Clear: flushing cache...");
            }

            foreach (var handle in this.cacheHandles)
            {
                if (this.logTrace)
                {
                    this.Logger.LogTrace("Clear: clearing handle {0}.", handle.Configuration.Name);
                }

                handle.Clear();
                handle.Stats.OnClear();
            }

            if (this.cacheBackplane != null)
            {
                if (this.logTrace)
                {
                    this.Logger.LogTrace("Clear: notifies backplane.");
                }

                this.cacheBackplane.NotifyClear();
            }

            this.TriggerOnClear();
        }

        /// <summary>
        /// Clears the cache region, removing all items from the specified <paramref name="region"/> only.
        /// </summary>
        /// <param name="region">The cache region.</param>
        /// <exception cref="System.ArgumentNullException">If region is null.</exception>
        public override void ClearRegion(string region)
        {
            NotNullOrWhiteSpace(region, nameof(region));

            this.CheckDisposed();
            if (this.logTrace)
            {
                this.Logger.LogTrace("Clear region: {0}.", region);
            }

            foreach (var handle in this.cacheHandles)
            {
                if (this.logTrace)
                {
                    this.Logger.LogTrace("Clear region: {0} in handle {1}.", region, handle.Configuration.Name);
                }

                handle.ClearRegion(region);
                handle.Stats.OnClearRegion(region);
            }

            if (this.cacheBackplane != null)
            {
                if (this.logTrace)
                {
                    this.Logger.LogTrace("Clear region: {0}: notifies backplane [clear region].", region);
                }

                this.cacheBackplane.NotifyClearRegion(region);
            }

            this.TriggerOnClearRegion(region);
        }

        /// <summary>
        /// Changes the expiration <paramref name="mode" /> and <paramref name="timeout" /> for the
        /// given <paramref name="key" />.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="mode">The expiration mode.</param>
        /// <param name="timeout">The expiration timeout.</param>
        public override void Expire(string key, ExpirationMode mode, TimeSpan timeout)
        {
            this.CheckDisposed();
            if (this.logTrace)
            {
                this.Logger.LogTrace("Expire: {0}.", key);
            }

            foreach (var handle in this.cacheHandles)
            {
                if (this.logTrace)
                {
                    this.Logger.LogTrace("Expire: {0} on handle {1}.", key, handle.Configuration.Name);
                }

                handle.Expire(key, mode, timeout);
            }
        }

        /// <summary>
        /// Changes the expiration <paramref name="mode" /> and <paramref name="timeout" /> for the
        /// given <paramref name="key" />.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="region">The cache region.</param>
        /// <param name="mode">The expiration mode.</param>
        /// <param name="timeout">The expiration timeout.</param>
        public override void Expire(string key, string region, ExpirationMode mode, TimeSpan timeout)
        {
            this.CheckDisposed();
            if (this.logTrace)
            {
                this.Logger.LogTrace("Expire: {0} {1}.", key, region);
            }

            foreach (var handle in this.cacheHandles)
            {
                if (this.logTrace)
                {
                    this.Logger.LogTrace("Expire: {0} {1} on handle {2}.", key, region, handle.Configuration.Name);
                }

                handle.Expire(key, region, mode, timeout);
            }
        }

        /// <inheritdoc />
        public TCacheValue GetOrAdd(string key, TCacheValue value)
            => this.GetOrAdd(key, (k) => value);

        /// <inheritdoc />
        public TCacheValue GetOrAdd(string key, string region, TCacheValue value)
            => this.GetOrAdd(key, region, (k, r) => value);

        /// <inheritdoc />
        public TCacheValue GetOrAdd(string key, Func<string, TCacheValue> valueFactory)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNull(valueFactory, nameof(valueFactory));

            return this.GetOrAddInternal(key, null, (k, r) => valueFactory(k));
        }

        /// <inheritdoc />
        public TCacheValue GetOrAdd(string key, string region, Func<string, string, TCacheValue> valueFactory)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNullOrWhiteSpace(region, nameof(region));
            NotNull(valueFactory, nameof(valueFactory));

            return this.GetOrAddInternal(key, region, (k, r) => valueFactory(k, r));
        }

        /// <inheritdoc />
        public bool TryGetOrAdd(string key, Func<string, TCacheValue> valueFactory, out TCacheValue value)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNull(valueFactory, nameof(valueFactory));

            return this.TryGetOrAddInternal(key, null, (k, r) => valueFactory(k), out value);
        }

        /// <inheritdoc />
        public bool TryGetOrAdd(string key, string region, Func<string, string, TCacheValue> valueFactory, out TCacheValue value)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNullOrWhiteSpace(region, nameof(region));
            NotNull(valueFactory, nameof(valueFactory));

            return this.TryGetOrAddInternal(key, region, (k, r) => valueFactory(k, r), out value);
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() =>
            string.Format(CultureInfo.InvariantCulture, "Name: {0}, Handles: [{1}]", this.Name, string.Join(",", this.cacheHandles.Select(p => p.GetType().Name)));

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
        public bool TryUpdate(string key, Func<TCacheValue, TCacheValue> updateValue, out TCacheValue value) =>
            this.TryUpdate(key, updateValue, 50, out value);

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
        public bool TryUpdate(string key, string region, Func<TCacheValue, TCacheValue> updateValue, out TCacheValue value) =>
            this.TryUpdate(key, region, updateValue, 50, out value);

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
        public bool TryUpdate(string key, Func<TCacheValue, TCacheValue> updateValue, int maxRetries, out TCacheValue value)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNull(updateValue, nameof(updateValue));
            Ensure(maxRetries > 0, "Maximum number of retries must be greater than or equal to zero.");

            return this.UpdateInternal(this.cacheHandles, key, updateValue, maxRetries, false, out value);
        }

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
        public bool TryUpdate(string key, string region, Func<TCacheValue, TCacheValue> updateValue, int maxRetries, out TCacheValue value)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNullOrWhiteSpace(region, nameof(region));
            NotNull(updateValue, nameof(updateValue));
            Ensure(maxRetries > 0, "Maximum number of retries must be greater than or equal to zero.");

            return this.UpdateInternal(this.cacheHandles, key, region, updateValue, maxRetries, false, out value);
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
        /// <remarks>
        /// If the cache does not use a distributed cache system. Update is doing exactly the same
        /// as Get plus Put.
        /// </remarks>
        /// <param name="key">The key to update.</param>
        /// <param name="updateValue">The function to perform the update.</param>
        /// <returns><c>True</c> if the update operation was successfully, <c>False</c> otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="key"/> or <paramref name="updateValue"/> is null.
        /// </exception>
        public TCacheValue Update(string key, Func<TCacheValue, TCacheValue> updateValue) =>
            this.Update(key, updateValue, 50);

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
        /// <returns><c>True</c> if the update operation was successfully, <c>False</c> otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="key"/> or <paramref name="region"/> or <paramref name="updateValue"/>
        /// is null.
        /// </exception>
        public TCacheValue Update(string key, string region, Func<TCacheValue, TCacheValue> updateValue) =>
            this.Update(key, region, updateValue, 50);

        /// <summary>
        /// Updates an existing key in the cache.
        /// The cache manager will make sure the update will always happen on the most recent version.
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
        /// <returns>The updated value.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="key"/> or <paramref name="updateValue"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// If the key didn't exist prior to the update call or the max retries has been reached.
        /// </exception>
        public TCacheValue Update(string key, Func<TCacheValue, TCacheValue> updateValue, int maxRetries)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNull(updateValue, nameof(updateValue));
            Ensure(maxRetries > 0, "Maximum number of retries must be greater than or equal to zero.");

            TCacheValue value = default(TCacheValue);
            this.UpdateInternal(this.cacheHandles, key, updateValue, maxRetries, true, out value);

            return value;
        }

        /// <summary>
        /// Updates an existing key in the cache.
        /// The cache manager will make sure the update will always happen on the most recent version.
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
        /// <returns>The updated value.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="key"/> or <paramref name="region"/> or <paramref name="updateValue"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// If the key didn't exist prior to the update call or the max retries has been reached.
        /// </exception>
        public TCacheValue Update(string key, string region, Func<TCacheValue, TCacheValue> updateValue, int maxRetries)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNullOrWhiteSpace(region, nameof(region));
            NotNull(updateValue, nameof(updateValue));
            Ensure(maxRetries > 0, "Maximum number of retries must be greater than or equal to zero.");

            TCacheValue value = default(TCacheValue);
            this.UpdateInternal(this.cacheHandles, key, region, updateValue, maxRetries, true, out value);

            return value;
        }

        /// <summary>
        /// Adds a value to the cache handles. Triggers OnAdd if the key has been added.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">If item is null.</exception>
        protected internal override bool AddInternal(CacheItem<TCacheValue> item)
        {
            NotNull(item, nameof(item));

            this.CheckDisposed();
            if (this.logTrace)
            {
                this.Logger.LogTrace("Add: {0} {1}", item.Key, item.Region);
            }

            var result = false;

            // also inverse it, so that the lowest level gets invoked first
            for (int handleIndex = this.cacheHandles.Length - 1; handleIndex >= 0; handleIndex--)
            {
                var handle = this.cacheHandles[handleIndex];

                if (AddItemToHandle(item, handle))
                {
                    if (this.logTrace)
                    {
                        this.Logger.LogTrace(
                            "Add: successfully added {0} {1} to handle {2}",
                            item.Key,
                            item.Region,
                            handle.Configuration.Name);
                    }
                    result = true;
                }
                else
                {
                    // this means, the item exists already, maybe with a different value already
                    // lets evict the item from all other handles so that we might get a fresh copy
                    // whenever the item gets requested evict from other is more passive than adding
                    // the version which exists to all others lets have the user decide what to do
                    // when we return false...
                    // Note: we might also just have added the item to a cache handel a level below,
                    //       this will get removed, too!
                    if (this.logTrace)
                    {
                        this.Logger.LogTrace(
                            "Add: {0} {1} to handle {2} FAILED. Evicting items from other handles.",
                            item.Key,
                            item.Region,
                            handle.Configuration.Name);
                    }

                    this.EvictFromOtherHandles(item.Key, item.Region, handleIndex);
                    return false;
                }
            }

            // trigger only once and not per handle and only if the item was added!
            if (result)
            {
                // update backplane
                if (this.cacheBackplane != null)
                {
                    if (this.logTrace)
                    {
                        this.Logger.LogTrace("Put: {0} {1}: notifies backplane [change].", item.Key, item.Region);
                    }

                    if (string.IsNullOrWhiteSpace(item.Region))
                    {
                        this.cacheBackplane.NotifyChange(item.Key, CacheItemChangedEventAction.Add);
                    }
                    else
                    {
                        this.cacheBackplane.NotifyChange(item.Key, item.Region, CacheItemChangedEventAction.Add);
                    }
                }

                this.TriggerOnAdd(item.Key, item.Region);
            }

            return result;
        }

        /// <summary>
        /// Puts a value into all cache handles. Triggers OnPut.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        /// <exception cref="System.ArgumentNullException">If item is null.</exception>
        protected internal override void PutInternal(CacheItem<TCacheValue> item)
        {
            NotNull(item, nameof(item));

            this.CheckDisposed();
            if (this.logTrace)
            {
                this.Logger.LogTrace("Put: {0} {1}.", item.Key, item.Region);
            }

            foreach (var handle in this.cacheHandles)
            {
                if (handle.Configuration.EnableStatistics)
                {
                    // check if it is really a new item otherwise the items count is crap because we
                    // count it every time, but use only the current handle to retrieve the item,
                    // otherwise we would trigger gets and find it in another handle maybe
                    var oldItem = string.IsNullOrWhiteSpace(item.Region) ?
                        handle.GetCacheItem(item.Key) :
                        handle.GetCacheItem(item.Key, item.Region);

                    handle.Stats.OnPut(item, oldItem == null);
                }

                if (this.logTrace)
                {
                    this.Logger.LogTrace(
                        "Put: {0} {1} to handle {2}.",
                        item.Key,
                        item.Region,
                        handle.Configuration.Name);
                }

                handle.Put(item);
            }

            // update backplane
            if (this.cacheBackplane != null)
            {
                if (this.logTrace)
                {
                    this.Logger.LogTrace("Put: {0} {1}: notifies backplane [change].", item.Key, item.Region);
                }

                if (string.IsNullOrWhiteSpace(item.Region))
                {
                    this.cacheBackplane.NotifyChange(item.Key, CacheItemChangedEventAction.Put);
                }
                else
                {
                    this.cacheBackplane.NotifyChange(item.Key, item.Region, CacheItemChangedEventAction.Put);
                }
            }

            this.TriggerOnPut(item.Key, item.Region);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        /// <param name="disposeManaged">Indicates if the dispose should release managed resources.</param>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                foreach (var handle in this.cacheHandles)
                {
                    handle.Dispose();
                }

                if (this.cacheBackplane != null)
                {
                    this.cacheBackplane.Dispose();
                }
            }

            base.Dispose(disposeManaged);
        }

        /// <summary>
        /// Gets the <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="key"/> is null.</exception>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key) =>
            this.GetCacheItemInternal(key, null);

        /// <summary>
        /// Gets the <c>CacheItem</c> for the specified key and region.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// If the <paramref name="key"/> or <paramref name="region"/> is null.
        /// </exception>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key, string region)
        {
            this.CheckDisposed();

            CacheItem<TCacheValue> cacheItem = null;

            if (this.logTrace)
            {
                this.Logger.LogTrace("Get: {0} {1}.", key, region);
            }

            for (int handleIndex = 0; handleIndex < this.cacheHandles.Length; handleIndex++)
            {
                var handle = this.cacheHandles[handleIndex];
                if (string.IsNullOrWhiteSpace(region))
                {
                    cacheItem = handle.GetCacheItem(key);
                }
                else
                {
                    cacheItem = handle.GetCacheItem(key, region);
                }

                handle.Stats.OnGet(region);

                if (cacheItem != null)
                {
                    if (this.logTrace)
                    {
                        this.Logger.LogTrace("Get: {0} {1}: item found in handle {2}.", key, region, handle.Configuration.Name);
                    }

                    // update last accessed, might be used for custom sliding implementations
                    cacheItem.LastAccessedUtc = DateTime.UtcNow;

                    // update other handles if needed
                    this.AddToHandles(cacheItem, handleIndex);
                    handle.Stats.OnHit(region);
                    this.TriggerOnGet(key, region);
                    break;
                }
                else
                {
                    if (this.logTrace)
                    {
                        this.Logger.LogTrace("Get: {0} {1}: item NOT found in handle {2}.", key, region, handle.Configuration.Name);
                    }

                    handle.Stats.OnMiss(region);
                }
            }

            return cacheItem;
        }

        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="key"/> is null.</exception>
        protected override bool RemoveInternal(string key) =>
            this.RemoveInternal(key, null);

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
        protected override bool RemoveInternal(string key, string region)
        {
            this.CheckDisposed();

            var result = false;

            if (this.logTrace)
            {
                this.Logger.LogTrace("Remove: {0} {1}.", key, region);
            }

            foreach (var handle in this.cacheHandles)
            {
                var handleResult = false;
                if (!string.IsNullOrWhiteSpace(region))
                {
                    handleResult = handle.Remove(key, region);
                }
                else
                {
                    handleResult = handle.Remove(key);
                }

                if (handleResult)
                {
                    if (this.logTrace)
                    {
                        this.Logger.LogTrace(
                            "Remove: {0} {1}: removed from handle {2}.",
                            key,
                            region,
                            handle.Configuration.Name);
                    }
                    result = true;
                    handle.Stats.OnRemove(region);
                }
            }

            if (result)
            {
                // update backplane
                if (this.cacheBackplane != null)
                {
                    if (this.logTrace)
                    {
                        this.Logger.LogTrace("Remove: {0} {1}: notifies backplane [remove].", key, region);
                    }

                    if (string.IsNullOrWhiteSpace(region))
                    {
                        this.cacheBackplane.NotifyRemove(key);
                    }
                    else
                    {
                        this.cacheBackplane.NotifyRemove(key, region);
                    }
                }

                // trigger only once and not per handle
                this.TriggerOnRemove(key, region);
            }

            return result;
        }

        private static bool AddItemToHandle(CacheItem<TCacheValue> item, BaseCacheHandle<TCacheValue> handle)
        {
            if (handle.Add(item))
            {
                handle.Stats.OnAdd(item);
                return true;
            }

            return false;
        }

        private static void ClearHandles(BaseCacheHandle<TCacheValue>[] handles)
        {
            foreach (var handle in handles)
            {
                handle.Clear();
                handle.Stats.OnClear();
            }

            ////this.TriggerOnClear();
        }

        private static void ClearRegionHandles(string region, BaseCacheHandle<TCacheValue>[] handles)
        {
            foreach (var handle in handles)
            {
                handle.ClearRegion(region);
                handle.Stats.OnClearRegion(region);
            }

            ////this.TriggerOnClearRegion(region);
        }

        private void EvictFromHandles(string key, string region, BaseCacheHandle<TCacheValue>[] handles)
        {
            foreach (var handle in handles)
            {
                this.EvictFromHandle(key, region, handle);
            }
        }

        private void EvictFromHandle(string key, string region, BaseCacheHandle<TCacheValue> handle)
        {
            if (this.logTrace)
            {
                this.Logger.LogTrace(
                    "Evict from handle: {0} {1}: on handle {2}.",
                    key,
                    region,
                    handle.Configuration.Name);
            }

            bool result;
            if (string.IsNullOrWhiteSpace(region))
            {
                result = handle.Remove(key);
            }
            else
            {
                result = handle.Remove(key, region);
            }

            if (result)
            {
                handle.Stats.OnRemove(region);
            }
        }

        private TCacheValue AddOrUpdateInternal(CacheItem<TCacheValue> item, Func<TCacheValue, TCacheValue> updateValue, int maxRetries)
        {
            this.CheckDisposed();
            if (this.logTrace)
            {
                this.Logger.LogTrace("Add or update: {0} {1}.", item.Key, item.Region);
            }

            var tries = 0;
            do
            {
                tries++;

                if (this.AddInternal(item))
                {
                    if (this.logTrace)
                    {
                        this.Logger.LogTrace("Add or update: {0} {1}: successfully added the item.", item.Key, item.Region);
                    }

                    return item.Value;
                }

                if (this.logTrace)
                {
                    this.Logger.LogTrace(
                        "Add or update: {0} {1}: add failed, trying to update...",
                        item.Key,
                        item.Region);
                }

                TCacheValue returnValue;
                bool updated = string.IsNullOrWhiteSpace(item.Region) ?
                    this.TryUpdate(item.Key, updateValue, maxRetries, out returnValue) :
                    this.TryUpdate(item.Key, item.Region, updateValue, maxRetries, out returnValue);

                if (updated)
                {
                    if (this.logTrace)
                    {
                        this.Logger.LogTrace("Add or update: {0} {1}: successfully updated.", item.Key, item.Region);
                    }

                    return returnValue;
                }

                if (this.logTrace)
                {
                    this.Logger.LogTrace(
                        "Add or update: {0} {1}: update FAILED, retrying [{2}/{3}].",
                        item.Key,
                        item.Region,
                        tries,
                        this.Configuration.MaxRetries);
                }
            }
            while (tries <= maxRetries);

            // exceeded max retries, failing the operation... (should not happen in 99,99% of the cases though, better throw?)
            return default(TCacheValue);
        }

        private void AddToHandles(CacheItem<TCacheValue> item, int foundIndex)
        {
            if (this.logTrace)
            {
                this.Logger.LogTrace(
                    "Add to handles: {0} {1}: with update mode {2}.",
                    item.Key,
                    item.Region,
                    this.Configuration.UpdateMode);
            }

            switch (this.Configuration.UpdateMode)
            {
                case CacheUpdateMode.None:
                    // do basically nothing
                    break;

                case CacheUpdateMode.Full:
                    // update all cache handles except the one where we found the item
                    for (int handleIndex = 0; handleIndex < this.cacheHandles.Length; handleIndex++)
                    {
                        if (handleIndex != foundIndex)
                        {
                            if (this.logTrace)
                            {
                                this.Logger.LogTrace("Add to handles: {0} {1}: adding to handle {2}.", item.Key, item.Region, handleIndex);
                            }

                            this.cacheHandles[handleIndex].Add(item);
                        }
                    }

                    break;

                case CacheUpdateMode.Up:
                    // optimizing so we don't even have to iterate
                    if (foundIndex == 0)
                    {
                        break;
                    }

                    // update all cache handles with lower order, up the list
                    for (int handleIndex = 0; handleIndex < this.cacheHandles.Length; handleIndex++)
                    {
                        if (handleIndex < foundIndex)
                        {
                            if (this.logTrace)
                            {
                                this.Logger.LogTrace("Add to handles: {0} {1}: adding to handle {2}.", item.Key, item.Region, handleIndex);
                            }

                            this.cacheHandles[handleIndex].Add(item);
                        }
                    }

                    break;
            }
        }

        private void AddToHandlesBelow(CacheItem<TCacheValue> item, int foundIndex)
        {
            if (item == null)
            {
                return;
            }

            if (this.logTrace)
            {
                this.Logger.LogTrace("Add to handles below: {0} {1}: below handle {2}.", item.Key, item.Region, foundIndex);
            }

            for (int handleIndex = 0; handleIndex < this.cacheHandles.Length; handleIndex++)
            {
                if (handleIndex > foundIndex)
                {
                    if (this.cacheHandles[handleIndex].Add(item))
                    {
                        this.cacheHandles[handleIndex].Stats.OnAdd(item);
                    }
                }
            }
        }

        private void EvictFromOtherHandles(string key, string region, int excludeIndex)
        {
            if (excludeIndex < 0 || excludeIndex >= this.cacheHandles.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(excludeIndex));
            }

            if (this.logTrace)
            {
                this.Logger.LogTrace("Evict from other handles: {0} {1}: excluding handle {2}.", key, region, excludeIndex);
            }

            for (int handleIndex = 0; handleIndex < this.cacheHandles.Length; handleIndex++)
            {
                if (handleIndex != excludeIndex)
                {
                    this.EvictFromHandle(key, region, this.cacheHandles[handleIndex]);
                }
            }
        }

        private void EvictFromHandlesAbove(string key, string region, int excludeIndex)
        {
            if (this.logTrace)
            {
                this.Logger.LogTrace("Evict from handles above: {0} {1}: above handle {2}.", key, region, excludeIndex);
            }

            if (excludeIndex < 0 || excludeIndex >= this.cacheHandles.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(excludeIndex));
            }

            for (int handleIndex = 0; handleIndex < this.cacheHandles.Length; handleIndex++)
            {
                if (handleIndex < excludeIndex)
                {
                    this.EvictFromHandle(key, region, this.cacheHandles[handleIndex]);
                }
            }
        }

        private bool TryGetOrAddInternal(string key, string region, Func<string, string, TCacheValue> valueFactory, out TCacheValue value)
        {
            value = default(TCacheValue);
            var tries = 0;
            do
            {
                tries++;
                var item = this.GetCacheItemInternal(key, region);
                if (item != null)
                {
                    value = item.Value;
                    return true;
                }

                value = valueFactory(key, region);

                if (value == null)
                {
                    return false;
                }

                item = string.IsNullOrWhiteSpace(region) ? new CacheItem<TCacheValue>(key, value) : new CacheItem<TCacheValue>(key, region, value);
                if (this.AddInternal(item))
                {
                    return true;
                }
            }
            while (tries <= this.Configuration.MaxRetries);

            return false;
        }

        private TCacheValue GetOrAddInternal(string key, string region, Func<string, string, TCacheValue> valueFactory)
        {
            var tries = 0;
            do
            {
                tries++;
                var item = this.GetCacheItemInternal(key, region);
                if (item != null)
                {
                    return item.Value;
                }

                var newValue = valueFactory(key, region);

                // Throw explicit to me more consistent. Otherwise it would throw later eventually...
                if (newValue == null)
                {
                    throw new InvalidOperationException("The value which should be added must not be null.");
                }

                item = string.IsNullOrWhiteSpace(region) ? new CacheItem<TCacheValue>(key, newValue) : new CacheItem<TCacheValue>(key, region, newValue);
                if (this.AddInternal(item))
                {
                    return newValue;
                }
            }
            while (tries <= this.Configuration.MaxRetries);

            // should usually never occur, but could if e.g. max retries is 1 and an item gets added between the get and add.
            // pretty unusual, so keep the max tries at least around 50
            throw new InvalidOperationException(
                string.Format("Could not get nor add the item {0} {1}", key, region));
        }

        private void RegisterCacheBackplane(CacheBackplane backplane)
        {
            NotNull(backplane, nameof(backplane));

            // this should have been checked during activation already, just to be totally sure...
            if (this.cacheHandles.Any(p => p.Configuration.IsBackplaneSource))
            {
                var handles = new Func<BaseCacheHandle<TCacheValue>[]>(() =>
                {
                    var handleList = new List<BaseCacheHandle<TCacheValue>>();
                    foreach (var handle in this.cacheHandles)
                    {
                        if (!handle.Configuration.IsBackplaneSource)
                        {
                            handleList.Add(handle);
                        }
                    }
                    return handleList.ToArray();
                });

                backplane.Changed += (sender, args) =>
                {
                    if (this.logTrace)
                    {
                        this.Logger.LogTrace("Backplane event: [Changed] of {0} {1}.", args.Key, args.Region);
                    }

                    this.EvictFromHandles(args.Key, args.Region, handles());
                    switch (args.Action)
                    {
                        case CacheItemChangedEventAction.Add:
                            this.TriggerOnAdd(args.Key, args.Region, CacheActionEventArgOrigin.Remote);
                            break;
                        case CacheItemChangedEventAction.Put:
                            this.TriggerOnPut(args.Key, args.Region, CacheActionEventArgOrigin.Remote);
                            break;
                        case CacheItemChangedEventAction.Update:
                            this.TriggerOnUpdate(args.Key, args.Region, CacheActionEventArgOrigin.Remote);
                            break;
                    }
                };

                backplane.Removed += (sender, args) =>
                {
                    if (this.logTrace)
                    {
                        this.Logger.LogTrace("Backplane event: [Remove] of {0} {1}.", args.Key, args.Region);
                    }

                    this.EvictFromHandles(args.Key, args.Region, handles());
                    this.TriggerOnRemove(args.Key, args.Region, CacheActionEventArgOrigin.Remote);
                };

                backplane.Cleared += (sender, args) =>
                {
                    if (this.logTrace)
                    {
                        this.Logger.LogTrace("Backplane event: [Clear].");
                    }

                    ClearHandles(handles());
                    this.TriggerOnClear(CacheActionEventArgOrigin.Remote);
                };

                backplane.ClearedRegion += (sender, args) =>
                {
                    if (this.logTrace)
                    {
                        this.Logger.LogTrace("Backplane event: [Clear Region] region: {0}.", args.Region);
                    }

                    ClearRegionHandles(args.Region, handles());
                    this.TriggerOnClearRegion(args.Region, CacheActionEventArgOrigin.Remote);
                };
            }
        }

        private void TriggerOnAdd(string key, string region, CacheActionEventArgOrigin origin = CacheActionEventArgOrigin.Local)
        {
            this.OnAdd?.Invoke(this, new CacheActionEventArgs(key, region, origin));
        }

        private void TriggerOnClear(CacheActionEventArgOrigin origin = CacheActionEventArgOrigin.Local)
        {
            this.OnClear?.Invoke(this, new CacheClearEventArgs(origin));
        }

        private void TriggerOnClearRegion(string region, CacheActionEventArgOrigin origin = CacheActionEventArgOrigin.Local)
        {
            this.OnClearRegion?.Invoke(this, new CacheClearRegionEventArgs(region, origin));
        }

        private void TriggerOnGet(string key, string region, CacheActionEventArgOrigin origin = CacheActionEventArgOrigin.Local)
        {
            this.OnGet?.Invoke(this, new CacheActionEventArgs(key, region, origin));
        }

        private void TriggerOnPut(string key, string region, CacheActionEventArgOrigin origin = CacheActionEventArgOrigin.Local)
        {
            this.OnPut?.Invoke(this, new CacheActionEventArgs(key, region, origin));
        }

        private void TriggerOnRemove(string key, string region, CacheActionEventArgOrigin origin = CacheActionEventArgOrigin.Local)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            this.OnRemove?.Invoke(this, new CacheActionEventArgs(key, region, origin));
        }

        private void TriggerOnUpdate(string key, string region, CacheActionEventArgOrigin origin = CacheActionEventArgOrigin.Local)
        {
            this.OnUpdate?.Invoke(this, new CacheActionEventArgs(key, region, origin));
        }

        private bool UpdateInternal(
            BaseCacheHandle<TCacheValue>[] handles,
            string key,
            Func<TCacheValue, TCacheValue> updateValue,
            int maxRetries,
            bool throwOnFailure,
            out TCacheValue value) =>
            this.UpdateInternal(handles, key, null, updateValue, maxRetries, throwOnFailure, out value);

        private bool UpdateInternal(
            BaseCacheHandle<TCacheValue>[] handles,
            string key,
            string region,
            Func<TCacheValue, TCacheValue> updateValue,
            int maxRetries,
            bool throwOnFailure,
            out TCacheValue value)
        {
            this.CheckDisposed();

            // assign null
            value = default(TCacheValue);

            if (handles.Length == 0)
            {
                return false;
            }

            if (this.logTrace)
            {
                this.Logger.LogTrace("Update: {0} {1}.", key, region);
            }

            // lowest level
            // todo: maybe check for only run on the backplate if configured (could potentially be not the last one).
            var handleIndex = handles.Length - 1;
            var handle = handles[handleIndex];

            var result = string.IsNullOrWhiteSpace(region) ?
                handle.Update(key, updateValue, maxRetries) :
                handle.Update(key, region, updateValue, maxRetries);

            if (this.logTrace)
            {
                this.Logger.LogTrace(
                    "Update: {0} {1}: tried on handle {2}: result: {3}.",
                    key,
                    region,
                    handle.Configuration.Name,
                    result.UpdateState);
            }

            if (result.UpdateState == UpdateItemResultState.Success)
            {
                // only on success, the returned value will not be null
                value = result.Value;
                handle.Stats.OnUpdate(key, region, result);

                // evict others, we don't know if the update on other handles could actually
                // succeed... There is a risk the update on other handles could create a
                // different version than we created with the first successful update... we can
                // safely add the item to handles below us though.
                this.EvictFromHandlesAbove(key, region, handleIndex);

                var item = string.IsNullOrWhiteSpace(region) ? handle.GetCacheItem(key) : handle.GetCacheItem(key, region);
                this.AddToHandlesBelow(item, handleIndex);
                this.TriggerOnUpdate(key, region);
            }
            else if (result.UpdateState == UpdateItemResultState.FactoryReturnedNull)
            {
                this.Logger.LogWarn($"Update failed on '{region}:{key}' because value factory returned null.");

                if (throwOnFailure)
                {
                    throw new InvalidOperationException($"Update failed on '{region}:{key}' because value factory returned null.");
                }
            }
            else if (result.UpdateState == UpdateItemResultState.TooManyRetries)
            {
                // if we had too many retries, this basically indicates an
                // invalid state of the cache: The item is there, but we couldn't update it and
                // it most likely has a different version
                this.Logger.LogWarn($"Update failed on '{region}:{key}' because of too many retries.");

                this.EvictFromOtherHandles(key, region, handleIndex);

                if (throwOnFailure)
                {
                    throw new InvalidOperationException($"Update failed on '{region}:{key}' because of too many retries.");
                }
            }
            else if (result.UpdateState == UpdateItemResultState.ItemDidNotExist)
            {
                // If update fails because item doesn't exist AND the current handle is backplane source or the lowest cache handle level,
                // remove the item from other handles (if exists).
                // Otherwise, if we do not exit here, calling update on the next handle might succeed and would return a misleading result.
                this.Logger.LogWarn($"Update failed on '{region}:{key}' because the region/key did not exist.");

                this.EvictFromOtherHandles(key, region, handleIndex);

                if (throwOnFailure)
                {
                    throw new InvalidOperationException($"Update failed on '{region}:{key}' because the region/key did not exist.");
                }
            }

            // update backplane
            if (result.UpdateState == UpdateItemResultState.Success && this.cacheBackplane != null)
            {
                if (this.logTrace)
                {
                    this.Logger.LogTrace("Update: {0} {1}: notifies backplane [change].", key, region);
                }

                if (string.IsNullOrWhiteSpace(region))
                {
                    this.cacheBackplane.NotifyChange(key, CacheItemChangedEventAction.Update);
                }
                else
                {
                    this.cacheBackplane.NotifyChange(key, region, CacheItemChangedEventAction.Update);
                }
            }

            return result.UpdateState == UpdateItemResultState.Success;
        }
    }
}