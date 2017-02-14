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
    public sealed partial class BaseCacheManager<TCacheValue> : BaseCache<TCacheValue>, ICacheManager<TCacheValue>, IDisposable
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
        public BaseCacheManager(ICacheManagerConfiguration configuration)
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
        /// <exception cref="ArgumentNullException">
        /// When <paramref name="name"/> or <paramref name="configuration"/> is null.
        /// </exception>
        /// <see cref="CacheFactory"/>
        /// <see cref="ConfigurationBuilder"/>
        /// <see cref="BaseCacheHandle{TCacheValue}"/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2200:RethrowToPreserveStackDetails", Justification = "fine for now")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "nope")]
        private BaseCacheManager(string name, ICacheManagerConfiguration configuration)
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
                throw ex.InnerException ?? ex;
            }
        }

        /// <inheritdoc />
        public event EventHandler<CacheActionEventArgs> OnAdd;

        /// <inheritdoc />
        public event EventHandler<CacheClearEventArgs> OnClear;

        /// <inheritdoc />
        public event EventHandler<CacheClearRegionEventArgs> OnClearRegion;

        /// <inheritdoc />
        public event EventHandler<CacheActionEventArgs> OnGet;

        /// <inheritdoc />
        public event EventHandler<CacheActionEventArgs> OnPut;

        /// <inheritdoc />
        public event EventHandler<CacheActionEventArgs> OnRemove;

        /// <inheritdoc />
        public event EventHandler<CacheActionEventArgs> OnUpdate;

        /// <inheritdoc />
        public IReadOnlyCacheManagerConfiguration Configuration { get; }

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public override void Expire(string key, ExpirationMode mode, TimeSpan timeout)
        {
            this.CheckDisposed();
            if (this.logTrace)
            {
                this.Logger.LogTrace("Expire [{0}] stareted.", key);
            }

            foreach (var handle in this.cacheHandles)
            {
                if (this.logTrace)
                {
                    this.Logger.LogTrace("Update expiration of [{0}] on handle {1}.", key, handle.Configuration.Name);
                }

                handle.Expire(key, mode, timeout);
            }
        }

        /// <inheritdoc />
        public override void Expire(string key, string region, ExpirationMode mode, TimeSpan timeout)
        {
            this.CheckDisposed();
            if (this.logTrace)
            {
                this.Logger.LogTrace("Expire [{0}:{1}] stareted.", region, key);
            }

            foreach (var handle in this.cacheHandles)
            {
                if (this.logTrace)
                {
                    this.Logger.LogTrace("Update expiration of [{0}:{1}] on handle {2}.", region, key, handle.Configuration.Name);
                }

                handle.Expire(key, region, mode, timeout);
            }
        }

        /// <inheritdoc />
        public override bool Exists(string key)
        {
            foreach (var handle in this.cacheHandles)
            {
                if (this.logTrace)
                {
                    this.Logger.LogTrace("Checking if [{0}] exists on handle '{1}'.", key, handle.Configuration.Name);
                }

                if (handle.Exists(key))
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public override bool Exists(string key, string region)
        {
            foreach (var handle in this.cacheHandles)
            {
                if (this.logTrace)
                {
                    this.Logger.LogTrace("Checking if [{0}:{1}] exists on handle '{2}'.", region, key, handle.Configuration.Name);
                }

                if (handle.Exists(key, region))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString() =>
            string.Format(CultureInfo.InvariantCulture, "Name: {0}, Handles: [{1}]", this.Name, string.Join(",", this.cacheHandles.Select(p => p.GetType().Name)));

        /// <inheritdoc />
        protected internal override bool AddInternal(CacheItem<TCacheValue> item)
        {
            NotNull(item, nameof(item));

            this.CheckDisposed();
            if (this.logTrace)
            {
                this.Logger.LogTrace("Add [{0}] started.", item);
            }

            var handleIndex = this.cacheHandles.Length - 1;

            var result = AddItemToHandle(item, this.cacheHandles[handleIndex]);

            // evict from other handles in any case because if it exists, it might be a different version
            // if not exist, its just a sanity check to invalidate other versions in upper layers.
            this.EvictFromOtherHandles(item.Key, item.Region, handleIndex);

            if (result)
            {
                // update backplane
                if (this.cacheBackplane != null)
                {
                    if (string.IsNullOrWhiteSpace(item.Region))
                    {
                        this.cacheBackplane.NotifyChange(item.Key, CacheItemChangedEventAction.Add);
                    }
                    else
                    {
                        this.cacheBackplane.NotifyChange(item.Key, item.Region, CacheItemChangedEventAction.Add);
                    }

                    if (this.logTrace)
                    {
                        this.Logger.LogTrace("Notified backplane 'change' because [{0}] was added.", item);
                    }
                }

                // trigger only once and not per handle and only if the item was added!
                this.TriggerOnAdd(item.Key, item.Region);
            }

            return result;
        }

        /// <inheritdoc />
        protected internal override void PutInternal(CacheItem<TCacheValue> item)
        {
            NotNull(item, nameof(item));

            this.CheckDisposed();
            if (this.logTrace)
            {
                this.Logger.LogTrace("Put [{0}] started.", item);
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
                        "Put [{0}:{1}] successfully to handle '{2}'.",
                        item.Region,
                        item.Key,
                        handle.Configuration.Name);
                }

                handle.Put(item);
            }

            // update backplane
            if (this.cacheBackplane != null)
            {
                if (this.logTrace)
                {
                    this.Logger.LogTrace("Put [{0}:{1}] was scuccessful. Notifying backplane [change].", item.Region, item.Key);
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key) =>
            this.GetCacheItemInternal(key, null);

        /// <inheritdoc />
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key, string region)
        {
            this.CheckDisposed();

            CacheItem<TCacheValue> cacheItem = null;

            if (this.logTrace)
            {
                this.Logger.LogTrace("Get [{0}:{1}] started.", region, key);
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
                        this.Logger.LogTrace("Get [{0}:{1}], found in handle[{2}] '{3}'.", region, key, handleIndex, handle.Configuration.Name);
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
                        this.Logger.LogTrace("Get [{0}:{1}], item NOT found in handle[{2}] '{3}'.", region, key, handleIndex, handle.Configuration.Name);
                    }

                    handle.Stats.OnMiss(region);
                }
            }

            return cacheItem;
        }

        /// <inheritdoc />
        protected override bool RemoveInternal(string key) =>
            this.RemoveInternal(key, null);

        /// <inheritdoc />
        protected override bool RemoveInternal(string key, string region)
        {
            this.CheckDisposed();

            var result = false;

            if (this.logTrace)
            {
                this.Logger.LogTrace("Removing [{0}:{1}].", region, key);
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
                            "Remove [{0}:{1}], successfully removed from handle '{2}'.",
                            region,
                            key,
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
                        this.Logger.LogTrace("Removed [{0}:{1}], notifying backplane [remove].", region, key);
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
                    "Evict [{0}:{1}] from handle '{2}'.",
                    region,
                    key,
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

        private void AddToHandles(CacheItem<TCacheValue> item, int foundIndex)
        {
            if (this.logTrace)
            {
                this.Logger.LogTrace(
                    "Add [{0}] to handles with update mode '{1}'.",
                    item,
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
                                this.Logger.LogTrace("Add [{0}:{1}] to handles, handle '{2}'.", item.Region, item.Key, handleIndex);
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
                                this.Logger.LogTrace("Add [{0}:{1}] to handles, handle '{2}'.", item.Region, item.Key, handleIndex);
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
                this.Logger.LogTrace("Add [{0}] to handles below handle '{1}'.", item, foundIndex);
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
                this.Logger.LogTrace("Evict [{0}:{1}] from other handles excluding handle '{2}'.", region, key, excludeIndex);
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
    }
}