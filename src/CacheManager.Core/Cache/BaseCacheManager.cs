using System;
using System.Collections.Generic;
using System.Linq;
using CacheManager.Core.Configuration;

namespace CacheManager.Core.Cache
{
    public sealed class BaseCacheManager<TCacheValue> : BaseCache<TCacheValue>, ICacheManager<TCacheValue>
    {
        private IDictionary<int, ICacheHandle<TCacheValue>> cacheHandles;

        public event EventHandler<CacheActionEventArgs> OnRemove;

        public event EventHandler<CacheActionEventArgs> OnAdd;

        public event EventHandler<CacheActionEventArgs> OnPut;

        public event EventHandler<CacheUpdateEventArgs> OnUpdate;

        public event EventHandler<CacheActionEventArgs> OnGet;

        public event EventHandler<CacheClearEventArgs> OnClear;

        public event EventHandler<CacheClearRegionEventArgs> OnClearRegion;

        public ICacheManagerConfiguration Configuration
        {
            get;
            private set;
        }

        public IList<ICacheHandle<TCacheValue>> CacheHandles
        {
            get { return cacheHandles.OrderBy(p => p.Key).Select(p => p.Value).ToList(); }
        }

        private BaseCacheManager(ICacheManagerConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            this.cacheHandles = new Dictionary<int, ICacheHandle<TCacheValue>>();
            this.Configuration = configuration;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseCacheManager{TCacheValue}"/> class using the specified configuration and list of handles.
        /// <para>
        /// In this case, the <paramref name="configuration"/> will not get interpreted, only the handles passed in by 
        /// <paramref name="handles"/> will be added to the cache.
        /// The <paramref name="configuration"/> only defines the name of this instance.
        /// </para>
        /// </summary>
        /// <remarks>
        /// This constructor is primarily used for unit testing. To construct a cache manager, use the <see cref="CacheFactory"/>!
        /// </remarks>
        /// <param name="configuration">The configuration which defines the name of the manager.</param>
        /// <param name="handles">The list of cache handles.</param>
        public BaseCacheManager(ICacheManagerConfiguration configuration, params ICacheHandle<TCacheValue>[] handles)
            : this(configuration)
        {
            if (handles == null)
            {
                throw new ArgumentNullException("handles");
            }

            foreach (var handle in handles)
            {
                this.AddCacheHandle(handle);
            }
        }

        /// <summary>
        /// Adds a cache handle to the cache manager instance.
        /// </summary>
        /// <param name="handle">The cache handle.</param>
        public void AddCacheHandle(ICacheHandle<TCacheValue> handle)
        {
            if (handle == null)
            {
                throw new ArgumentNullException("handle");
            }

            var currentIndex = this.cacheHandles.Count;
            
            var backPlate = handle.BackPlate;
            if (backPlate != null)
            {
                backPlate.SubscribeChanged((key) =>
                {
                    this.UpdateEvictFromOtherHandles(key, null, currentIndex);
                });

                backPlate.SubscribeChanged((key, region) =>
                {
                    this.UpdateEvictFromOtherHandles(key, region, currentIndex);
                });

                backPlate.SubscribeRemove((key) =>
                {
                    this.UpdateEvictFromOtherHandles(key, null, currentIndex);
                });

                backPlate.SubscribeRemove((key, region) =>
                {
                    this.UpdateEvictFromOtherHandles(key, region, currentIndex);
                });

                backPlate.SubscribeClear(() =>
                {
                    this.ClearOtherHandles(currentIndex);
                });

                backPlate.SubscribeClearRegion((region) =>
                {
                    this.ClearRegionOtherHandles(region, currentIndex);
                });
            }

            this.cacheHandles.Add(currentIndex, handle);
        }

        protected internal override bool AddInternal(CacheItem<TCacheValue> item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            var result = false;
            foreach (var handle in cacheHandles.Values)
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

        protected internal override void PutInternal(CacheItem<TCacheValue> item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            foreach (var handle in cacheHandles.Values)
            {
                if (handle.Configuration.EnableStatistics)
                {
                    // check if it is really a new item otherwise the items count is crap
                    // because we count it every time, but use only the current handle to 
                    // retrieve the item, otherwise we would trigger gets and find it in another handle maybe
                    var oldItem = string.IsNullOrWhiteSpace(item.Region) ?
                        handle.GetCacheItem(item.Key) :
                        handle.GetCacheItem(item.Key, item.Region);

                    handle.Stats.OnPut(item, oldItem == null);
                }

                handle.Put(item);
            }

            this.TriggerOnPut(item.Key, item.Region);
        }

        protected override bool RemoveInternal(string key)
        {
            return this.RemoveInternal(key, null);
        }

        protected override bool RemoveInternal(string key, string region)
        {
            var result = false;

            foreach (var handle in cacheHandles)
            {
                var handleResult = false;
                if (!string.IsNullOrWhiteSpace(region))
                {
                    handleResult = handle.Value.Remove(key, region);
                }
                else
                {
                    handleResult = handle.Value.Remove(key);
                }

                if (handleResult)
                {
                    result = true;
                    handle.Value.Stats.OnRemove(region);
                }
            }

            // trigger only once and not per handle
            if (result)
            {
                this.TriggerOnRemove(key, region);
            }

            return result;
        }

        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key)
        {
            return this.GetCacheItemInternal(key, null);
        }

        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key, string region)
        {
            CacheItem<TCacheValue> cacheItem = null;

            foreach (var handle in cacheHandles)
            {
                if (string.IsNullOrWhiteSpace(region))
                {
                    cacheItem = handle.Value.GetCacheItem(key);
                }
                else
                {
                    cacheItem = handle.Value.GetCacheItem(key, region);
                }

                handle.Value.Stats.OnGet(region);

                if (cacheItem != null)
                {
                    // update last accessed, might be used for custom sliding implementations
                    cacheItem.LastAccessedUtc = DateTime.UtcNow;

                    // update other handles if needed
                    this.AddToHandles(cacheItem, handle.Key);
                    handle.Value.Stats.OnHit(region);
                    this.TriggerOnGet(key, region);
                    break;
                }
                else
                {
                    handle.Value.Stats.OnMiss(region);
                }
            }

            return cacheItem;
        }
        public bool Update(string key, Func<TCacheValue, TCacheValue> updateValue)
        {
            return Update(key, updateValue, new UpdateItemConfig());
        }

        public bool Update(string key, string region, Func<TCacheValue, TCacheValue> updateValue)
        {
            return Update(key, region, updateValue, new UpdateItemConfig());
        }

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

            return UpdateInternal(key, updateValue, config);
        }

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

        private bool UpdateInternal(string key, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config)
        {
            return UpdateInternal(key, null, updateValue, config);
        }

        private bool UpdateInternal(string key, string region, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config)
        {
            bool overallResult = false;
            bool overallVersionConflictOccurred = false;
            int overallTries = 1;

            foreach (var handle in cacheHandles)
            {
                UpdateItemResult result;
                if (string.IsNullOrWhiteSpace(region))
                {
                    result = handle.Value.Update(key, updateValue, config);
                }
                else
                {
                    result = handle.Value.Update(key, region, updateValue, config);
                }

                if (result.Success)
                {
                    overallResult = true;
                    handle.Value.Stats.OnUpdate(key, region, result);
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
                        TriggerOnUpdate(key, region, config, new UpdateItemResult(overallVersionConflictOccurred, false, overallTries));
                        return false;
                    }

                    switch (config.VersionConflictOperation)
                    {
                        case VersionConflictHandling.EvictItemFromOtherCaches:
                            this.UpdateEvictFromOtherHandles(key, region, handle.Key);
                            break;
                        case VersionConflictHandling.UpdateOtherCaches:
                            CacheItem<TCacheValue> item;
                            if (string.IsNullOrWhiteSpace(region))
                            {
                                item = handle.Value.GetCacheItem(key);
                            }
                            else
                            {
                                item = handle.Value.GetCacheItem(key, region);
                            }

                            this.UpdateOtherHandles(item, handle.Key);
                            break;
                    }

                    // stop loop because we already handled everything.
                    break;
                }
            }

            TriggerOnUpdate(key, region, config, new UpdateItemResult(overallVersionConflictOccurred, overallResult, overallTries));

            return overallResult;
        }
        
        public override void Clear()
        {
            foreach (var handleItem in cacheHandles)
            {
                ICacheHandle<TCacheValue> handle = handleItem.Value;

                handle.Clear();
                handle.Stats.OnClear();
            }

            this.TriggerOnClear();
        }

        private void ClearOtherHandles(int excludeIndex)
        {
            if (excludeIndex < 0 || excludeIndex >= this.cacheHandles.Count)
            {
                throw new ArgumentOutOfRangeException("excludeIndex");
            }

            foreach (var handle in cacheHandles.Where(p => p.Key != excludeIndex).Select(p => p.Value))
            {
                handle.Clear();
                handle.Stats.OnClear();
            }

            this.TriggerOnClear();
        }

        public override void ClearRegion(string region)
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                throw new ArgumentNullException("region");
            }

            foreach (var handle in this.cacheHandles)
            {
                handle.Value.ClearRegion(region);
                handle.Value.Stats.OnClearRegion(region);
            }

            this.TriggerOnClearRegion(region);
        }

        private void ClearRegionOtherHandles(string region, int excludeIndex)
        {
            if (excludeIndex < 0 || excludeIndex >= this.cacheHandles.Count)
            {
                throw new ArgumentOutOfRangeException("excludeIndex");
            }

            foreach (var handle in cacheHandles.Where(p => p.Key != excludeIndex).Select(p => p.Value))
            {
                handle.ClearRegion(region);
                handle.Stats.OnClearRegion(region);
            }

            this.TriggerOnClearRegion(region);
        }

        private void TriggerOnRemove(string key, string region)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }

            if (OnRemove != null)
            {
                this.OnRemove(this, new CacheActionEventArgs(key, region));
            }
        }

        private void TriggerOnAdd(string key, string region)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }

            if (OnAdd != null)
            {
                this.OnAdd(this, new CacheActionEventArgs(key, region));
            }
        }

        private void TriggerOnPut(string key, string region)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }

            if (OnPut != null)
            {
                this.OnPut(this, new CacheActionEventArgs(key, region));
            }
        }

        private void TriggerOnUpdate(string key, string region, UpdateItemConfig config, UpdateItemResult result)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }

            if (OnUpdate != null)
            {
                this.OnUpdate(this, new CacheUpdateEventArgs(key, region, config, result));
            }
        }

        private void TriggerOnGet(string key, string region)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }

            if (OnGet != null)
            {
                this.OnGet(this, new CacheActionEventArgs(key, region));
            }
        }

        private void TriggerOnClear()
        {
            if (OnClear != null)
            {
                this.OnClear(this, new CacheClearEventArgs());
            }
        }

        private void TriggerOnClearRegion(string region)
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                throw new ArgumentNullException("region");
            }

            if (OnClearRegion != null)
            {
                this.OnClearRegion(this, new CacheClearRegionEventArgs(region));
            }
        }

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                foreach (var handle in cacheHandles)
                {
                    handle.Value.Dispose();
                }
            }

            base.Dispose(disposeManaged);
        }

        private void UpdateOtherHandles(CacheItem<TCacheValue> item, int excludeIndex)
        {
            if (item == null)
            {
                return;
            }

            foreach (var handle in cacheHandles.Where(p => p.Key != excludeIndex).Select(p => p.Value))
            {
                handle.Put(item);
                //// handle.Stats.OnPut(item); don't update, 
                //// we expect the item to be in the cache already at this point, so we should not increase the count...

                this.TriggerOnPut(item.Key, item.Region);
            }
        }

        private void UpdateEvictFromOtherHandles(string key, string region, int excludeIndex)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }
            if (excludeIndex < 0 || excludeIndex >= this.cacheHandles.Count)
            {
                throw new ArgumentOutOfRangeException("excludeIndex");
            }

            foreach (var handle in cacheHandles.Where(p => p.Key != excludeIndex).Select(p => p.Value))
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
                    //this.TriggerOnRemove(key, region);
                }
            }
        }
        
        private void AddToHandles(CacheItem<TCacheValue> item, int foundIndex)
        {
            switch (this.Configuration.CacheUpdateMode)
            {
                case CacheUpdateMode.None:
                    // do basically nothing
                    break;

                case CacheUpdateMode.Full:
                    // update all cache handles except the one where we found the item
                    AddToHandles(cacheHandles
                        .Where(p => p.Key != foundIndex)
                        .Select(p => p.Value),
                        item);

                    break;

                case CacheUpdateMode.Up:
                    // update all cache handles with lower order, up the list
                    AddToHandles(cacheHandles
                        .OrderBy(p => p.Key)
                        .Where(p => p.Key < foundIndex)
                        .Select(p => p.Value),
                        item);

                    break;
            }
        }

        private static void AddToHandles(IEnumerable<ICacheHandle<TCacheValue>> handles, CacheItem<TCacheValue> item)
        {
            foreach (var handle in handles)
            {
                // use Add because we expect the handle should be updated cuz it doesn't contain the item.
                handle.Add(item);
                // stats update should happen in Add method handle.Stats.OnAdd(item);
            }
        }
    }
}