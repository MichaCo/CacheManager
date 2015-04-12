using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CacheManager.Core.Configuration;

namespace CacheManager.Core.Cache
{
    /// <summary>
    /// The BaseCacheManager implements <see cref="ICacheManager{T}"/> and is the main class which
    /// gets constructed by <see cref="CacheFactory"/>.
    /// <para>
    /// The cache manager manages the list of <see cref="BaseCacheHandle{T}"/>'s which have been added. 
    /// It will keep them in sync depending on the configuration.
    /// </para>
    /// </summary>
    /// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
    public sealed class BaseCacheManager<TCacheValue> : BaseCache<TCacheValue>, ICacheManager<TCacheValue>
    {
        /// <summary>
        /// The cache back plate.
        /// </summary>
        private CacheBackPlate cacheBackPlate = null;

        /// <summary>
        /// The cache handles collection.
        /// </summary>
        private BaseCacheHandle<TCacheValue>[] cacheHandles = new BaseCacheHandle<TCacheValue>[] { };

        /// <summary>
        /// Gets the cache name.
        /// </summary>
        /// <value>
        /// The name of the cache.
        /// </value>
        public string Name { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseCacheManager{TCacheValue}"/> class
        /// using the specified configuration and list of handles.
        /// <para>
        /// In this case, the <paramref name="configuration"/> will not get interpreted, only the
        /// handles passed in by <paramref name="handles"/> will be added to the cache. The
        /// <paramref name="configuration"/> only defines the name of this instance.
        /// </para>
        /// </summary>
        /// <param name="cacheName">The name of the cache. Can be used for scoping the cache.</param>
        /// <param name="configuration">The configuration which defines the name of the manager.</param>
        /// <param name="handles">The list of cache handles.</param>
        /// <exception cref="System.ArgumentNullException">When handles is null.</exception>
        /// <remarks>
        /// This constructor is primarily used for unit testing. To construct a cache manager, use
        /// the <see cref="CacheFactory"/>.
        /// </remarks>
        public BaseCacheManager(string cacheName, ICacheManagerConfiguration configuration, params BaseCacheHandle<TCacheValue>[] handles)
            : this(configuration)
        {
            if (string.IsNullOrEmpty(cacheName))
            {
                throw new ArgumentNullException("cacheName");
            }

            if (handles == null)
            {
                throw new ArgumentNullException("handles");
            }

            this.Name = cacheName;

            foreach (var handle in handles)
            {
                this.AddCacheHandle(handle);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseCacheManager{TCacheValue}"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <exception cref="System.ArgumentNullException">If configuration is null.</exception>
        private BaseCacheManager(ICacheManagerConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            this.Configuration = configuration;
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
        public event EventHandler<CacheUpdateEventArgs> OnUpdate;

        /// <summary>
        /// Gets a list of cache handles currently registered within the cache manager.
        /// </summary>
        /// <value>The cache handles.</value>
        /// <remarks>
        /// This list is read only, any changes to the returned list instance will not affect the
        /// state of the cache manager instance.
        /// </remarks>
#if NET40
        public ICollection<BaseCacheHandle<TCacheValue>> CacheHandles
#else
        public IReadOnlyCollection<BaseCacheHandle<TCacheValue>> CacheHandles
#endif
        {
            get
            {
                return new ReadOnlyCollection<BaseCacheHandle<TCacheValue>>(
                    new List<BaseCacheHandle<TCacheValue>>(
                        this.cacheHandles));
            }
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>The configuration.</value>
        public ICacheManagerConfiguration Configuration
        {
            get;
            private set;
        }

        /// <summary>
        /// Adds a cache handle to the cache manager instance.
        /// </summary>
        /// <param name="handle">The cache handle.</param>
        public void AddCacheHandle(BaseCacheHandle<TCacheValue> handle)
        {
            if (handle == null)
            {
                throw new ArgumentNullException("handle");
            }

            var handleList = new List<BaseCacheHandle<TCacheValue>>(this.cacheHandles);
            handleList.Add(handle);
            this.cacheHandles = handleList.ToArray();
        }

        /// <summary>
        /// Clears this cache, removing all items in the base cache and all regions.
        /// </summary>
        public override void Clear()
        {
            foreach (var handle in this.cacheHandles)
            {
                handle.Clear();
                handle.Stats.OnClear();
            }

            if (this.cacheBackPlate != null)
            {
                this.cacheBackPlate.NotifyClear();
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
            if (string.IsNullOrWhiteSpace(region))
            {
                throw new ArgumentNullException("region");
            }

            foreach (var handle in this.cacheHandles)
            {
                handle.ClearRegion(region);
                handle.Stats.OnClearRegion(region);
            }

            if (this.cacheBackPlate != null)
            {
                this.cacheBackPlate.NotifyClearRegion(region);
            }

            this.TriggerOnClearRegion(region);
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
        public bool Update(string key, Func<TCacheValue, TCacheValue> updateValue)
        {
            return this.Update(key, updateValue, new UpdateItemConfig());
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
        /// <param name="region">The region of the key to update.</param>
        /// <param name="updateValue">The function to perform the update.</param>
        /// <returns><c>True</c> if the update operation was successfully, <c>False</c> otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="key"/> or <paramref name="region"/> or <paramref name="updateValue"/>
        /// is null.
        /// </exception>
        public bool Update(string key, string region, Func<TCacheValue, TCacheValue> updateValue)
        {
            return this.Update(key, region, updateValue, new UpdateItemConfig());
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
        /// <param name="config">The cache configuration used to specify the update behavior.</param>
        /// <returns><c>True</c> if the update operation was successfully, <c>False</c> otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="key"/> or <paramref name="updateValue"/> or <paramref name="config"/>
        /// is null.
        /// </exception>
        public bool Update(string key, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }
            if (updateValue == null)
            {
                throw new ArgumentNullException("updateValue");
            }
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            return this.UpdateInternal(key, updateValue, config);
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
        /// <param name="region">The region of the key to update.</param>
        /// <param name="updateValue">The function to perform the update.</param>
        /// <param name="config">The cache configuration used to specify the update behavior.</param>
        /// <returns><c>True</c> if the update operation was successfully, <c>False</c> otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="key"/> or <paramref name="region"/> or <paramref name="updateValue"/>
        /// or <paramref name="config"/> is null.
        /// </exception>
        public bool Update(string key, string region, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }

            if (string.IsNullOrWhiteSpace(region))
            {
                throw new ArgumentNullException("region");
            }
            if (updateValue == null)
            {
                throw new ArgumentNullException("updateValue");
            }
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            return this.UpdateInternal(key, region, updateValue, config);
        }

        /// <summary>
        /// Sets the cache back plate and subscribes to it.
        /// </summary>
        /// <param name="backPlate">The back plate.</param>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="backPlate"/> is null.
        /// </exception>
        internal void SetCacheBackPlate(CacheBackPlate backPlate)
        {
            if (backPlate == null)
            {
                throw new ArgumentNullException("backPlate");
            }

            this.cacheBackPlate = backPlate;

            var handles = new Func<BaseCacheHandle<TCacheValue>[]>(() =>
            {
                var handleList = new List<BaseCacheHandle<TCacheValue>>();
                foreach (var handle in this.cacheHandles)
                {
                    if (!handle.Configuration.IsBackPlateSource)
                    {
                        handleList.Add(handle);
                    }
                }
                return handleList.ToArray();
            });

            backPlate.SubscribeChanged((key) =>
            {
                this.EvictFromHandles(key, null, handles());
            });

            backPlate.SubscribeChanged((key, region) =>
            {
                this.EvictFromHandles(key, region, handles());
            });

            backPlate.SubscribeRemove((key) =>
            {
                this.EvictFromHandles(key, null, handles());
            });

            backPlate.SubscribeRemove((key, region) =>
            {
                this.EvictFromHandles(key, region, handles());
            });

            backPlate.SubscribeClear(() =>
            {
                this.ClearHandles(handles());
            });

            backPlate.SubscribeClearRegion((region) =>
            {
                this.ClearRegionHandles(region, handles());
            });
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
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            var result = false;
            foreach (var handle in this.cacheHandles)
            {
                if (handle.Add(item))
                {
                    result = true;
                    handle.Stats.OnAdd(item);
                }
            }

            // trigger only once and not per handle and only if the item was added!
            if (result)
            {
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
            if (item == null)
            {
                throw new ArgumentNullException("item");
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

                handle.Put(item);
            }

            // update back plate
            if (this.cacheBackPlate != null)
            {
                if (string.IsNullOrWhiteSpace(item.Region))
                {
                    this.cacheBackPlate.NotifyChange(item.Key);
                }
                else
                {
                    this.cacheBackPlate.NotifyChange(item.Key, item.Region);
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
            }

            base.Dispose(disposeManaged);
        }

        /// <summary>
        /// Gets the <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        /// <exception cref="ArgumentNullException">If the <paramref name="key"/> is null.</exception>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key)
        {
            return this.GetCacheItemInternal(key, null);
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
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key, string region)
        {
            CacheItem<TCacheValue> cacheItem = null;

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
        protected override bool RemoveInternal(string key)
        {
            return this.RemoveInternal(key, null);
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
        protected override bool RemoveInternal(string key, string region)
        {
            var result = false;

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
                    result = true;
                    handle.Stats.OnRemove(region);
                }
            }

            // update back plate
            if (this.cacheBackPlate != null)
            {
                if (string.IsNullOrWhiteSpace(region))
                {
                    this.cacheBackPlate.NotifyRemove(key);
                }
                else
                {
                    this.cacheBackPlate.NotifyRemove(key, region);
                }
            }

            // trigger only once and not per handle
            if (result)
            {
                this.TriggerOnRemove(key, region);
            }

            return result;
        }

        /// <summary>
        /// Adds an item to handles depending on the update mode configuration.
        /// </summary>
        /// <param name="item">The item to be added.</param>
        /// <param name="foundIndex">The index of the cache handle the item was found in.</param>
        private void AddToHandles(CacheItem<TCacheValue> item, int foundIndex)
        {
            switch (this.Configuration.CacheUpdateMode)
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
                            this.cacheHandles[handleIndex].Add(item);
                        }
                    }

                    break;
            }
        }

        /// <summary>
        /// Clears the cache handles provided.
        /// </summary>
        /// <param name="handles">The handles.</param>
        private void ClearHandles(BaseCacheHandle<TCacheValue>[] handles)
        {
            foreach (var handle in handles)
            {
                handle.Clear();
                handle.Stats.OnClear();
            }

            this.TriggerOnClear();
        }

        /// <summary>
        /// Invokes ClearRegion on the <paramref name="handles"/>.
        /// </summary>
        /// <param name="region">The region.</param>
        /// <param name="handles">The handles.</param>
        private void ClearRegionHandles(string region, BaseCacheHandle<TCacheValue>[] handles)
        {
            foreach (var handle in handles)
            {
                handle.ClearRegion(region);
                handle.Stats.OnClearRegion(region);
            }

            this.TriggerOnClearRegion(region);
        }

        /// <summary>
        /// Evicts a cache item from <paramref name="handles"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        /// <param name="handles">The handles.</param>
        private void EvictFromHandles(string key, string region, BaseCacheHandle<TCacheValue>[] handles)
        {
            foreach (var handle in handles)
            {
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
        }

        /// <summary>
        /// Evicts a cache item from all cache handles except the one at <paramref name="excludeIndex"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        /// <param name="excludeIndex">Index of the exclude.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">If excludeIndex is not valid.</exception>
        private void EvictFromOtherHandles(string key, string region, int excludeIndex)
        {
            if (excludeIndex < 0 || excludeIndex >= this.cacheHandles.Length)
            {
                throw new ArgumentOutOfRangeException("excludeIndex");
            }

            for (int handleIndex = 0; handleIndex < this.cacheHandles.Length; handleIndex++)
            {
                if (handleIndex != excludeIndex)
                {
                    var handle = this.cacheHandles[handleIndex];
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
            }
        }

        /// <summary>
        /// Triggers OnAdd.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        private void TriggerOnAdd(string key, string region)
        {
            if (this.OnAdd != null)
            {
                this.OnAdd(this, new CacheActionEventArgs(key, region));
            }
        }

        /// <summary>
        /// Triggers OnClear.
        /// </summary>
        private void TriggerOnClear()
        {
            if (this.OnClear != null)
            {
                this.OnClear(this, new CacheClearEventArgs());
            }
        }

        /// <summary>
        /// Triggers OnClearRegion.
        /// </summary>
        /// <param name="region">The region.</param>
        private void TriggerOnClearRegion(string region)
        {
            if (this.OnClearRegion != null)
            {
                this.OnClearRegion(this, new CacheClearRegionEventArgs(region));
            }
        }

        /// <summary>
        /// Triggers OnGet.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        private void TriggerOnGet(string key, string region)
        {
            if (this.OnGet != null)
            {
                this.OnGet(this, new CacheActionEventArgs(key, region));
            }
        }

        /// <summary>
        /// Triggers TriggerOnPut.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        private void TriggerOnPut(string key, string region)
        {
            if (this.OnPut != null)
            {
                this.OnPut(this, new CacheActionEventArgs(key, region));
            }
        }

        /// <summary>
        /// Triggers TriggerOnRemove.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        /// <exception cref="System.ArgumentNullException">If key is null.</exception>
        private void TriggerOnRemove(string key, string region)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }

            if (this.OnRemove != null)
            {
                this.OnRemove(this, new CacheActionEventArgs(key, region));
            }
        }

        /// <summary>
        /// Triggers OnUpdate.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        /// <param name="config">The configuration.</param>
        /// <param name="result">The result.</param>
        private void TriggerOnUpdate(string key, string region, UpdateItemConfig config, UpdateItemResult result)
        {
            if (this.OnUpdate != null)
            {
                this.OnUpdate(this, new CacheUpdateEventArgs(key, region, config, result));
            }
        }

        /// <summary>
        /// Private implementation of Update.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="updateValue">The update value.</param>
        /// <param name="config">The configuration.</param>
        /// <returns><c>True</c> if the item has been updated.</returns>
        private bool UpdateInternal(string key, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config)
        {
            return this.UpdateInternal(key, null, updateValue, config);
        }

        /// <summary>
        /// Private implementation of Update.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        /// <param name="updateValue">The update value.</param>
        /// <param name="config">The configuration.</param>
        /// <returns><c>True</c> if the item has been updated.</returns>
        private bool UpdateInternal(string key, string region, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config)
        {
            bool overallResult = false;
            bool overallVersionConflictOccurred = false;
            int overallTries = 1;

            for (int handleIndex = 0; handleIndex < this.cacheHandles.Length; handleIndex++)
            {
                var handle = this.cacheHandles[handleIndex];
                UpdateItemResult result;
                if (string.IsNullOrWhiteSpace(region))
                {
                    result = handle.Update(key, updateValue, config);
                }
                else
                {
                    result = handle.Update(key, region, updateValue, config);
                }

                if (result.Success)
                {
                    overallResult = true;
                    handle.Stats.OnUpdate(key, region, result);
                }

                if (result.VersionConflictOccurred)
                {
                    overallVersionConflictOccurred = true;
                }

                overallTries += result.NumberOfRetriesNeeded > 1 ? result.NumberOfRetriesNeeded - 1 : 0;

                if (result.VersionConflictOccurred && config.VersionConflictOperation != VersionConflictHandling.Ignore)
                {
                    if (!result.Success)
                    {
                        this.TriggerOnUpdate(key, region, config, new UpdateItemResult(overallVersionConflictOccurred, false, overallTries));
                        return false;
                    }

                    switch (config.VersionConflictOperation)
                    {
                        case VersionConflictHandling.EvictItemFromOtherCaches:
                            this.EvictFromOtherHandles(key, region, handleIndex);
                            break;

                        case VersionConflictHandling.UpdateOtherCaches:
                            CacheItem<TCacheValue> item;
                            if (string.IsNullOrWhiteSpace(region))
                            {
                                item = handle.GetCacheItem(key);
                            }
                            else
                            {
                                item = handle.GetCacheItem(key, region);
                            }

                            this.UpdateOtherHandles(item, handleIndex);
                            break;
                    }

                    // stop loop because we already handled everything.
                    break;
                }
            }

            // update back plate
            if (overallResult && this.cacheBackPlate != null)
            {
                if (string.IsNullOrWhiteSpace(region))
                {
                    this.cacheBackPlate.NotifyChange(key);
                }
                else
                {
                    this.cacheBackPlate.NotifyChange(key, region);
                }
            }

            this.TriggerOnUpdate(key, region, config, new UpdateItemResult(overallVersionConflictOccurred, overallResult, overallTries));

            return overallResult;
        }

        /// <summary>
        /// Updates all cache handles except the one at <paramref name="excludeIndex"/>.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="excludeIndex">Index of the exclude.</param>
        private void UpdateOtherHandles(CacheItem<TCacheValue> item, int excludeIndex)
        {
            if (item == null)
            {
                return;
            }

            // .Where(p => p.Key != excludeIndex).Select(p => p.Value)
            for (int handleIndex = 0; handleIndex < this.cacheHandles.Length; handleIndex++)
            {
                if (handleIndex != excludeIndex)
                {
                    this.cacheHandles[handleIndex].Put(item);
                    //// handle.Stats.OnPut(item); don't update,
                    //// we expect the item to be in the cache already at this point, so we should not increase the count...

                    this.TriggerOnPut(item.Key, item.Region);
                }
            }
        }
    }
}