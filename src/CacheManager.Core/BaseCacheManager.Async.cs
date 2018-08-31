using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CacheManager.Core.Internal;
using CacheManager.Core.Logging;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core
{
#if !NET40
    public partial class BaseCacheManager<TCacheValue>
    {
        /// <inheritdoc />
        protected internal override async ValueTask<bool> AddInternalAsync(CacheItem<TCacheValue> item)
        {
            NotNull(item, nameof(item));

            CheckDisposed();
            if (_logTrace)
            {
                Logger.LogTrace("Add [{0}] started.", item);
            }

            var handleIndex = _cacheHandles.Length - 1;

            var result = await AddItemToHandleAsync(item, _cacheHandles[handleIndex]);

            // evict from other handles in any case because if it exists, it might be a different version
            // if not exist, its just a sanity check to invalidate other versions in upper layers.
            await EvictFromOtherHandlesAsync(item.Key, item.Region, handleIndex);

            if (result)
            {
                // update backplane
                if (_cacheBackplane != null)
                {
                    if (string.IsNullOrWhiteSpace(item.Region))
                    {
                        _cacheBackplane.NotifyChange(item.Key, CacheItemChangedEventAction.Add);
                    }
                    else
                    {
                        _cacheBackplane.NotifyChange(item.Key, item.Region, CacheItemChangedEventAction.Add);
                    }

                    if (_logTrace)
                    {
                        Logger.LogTrace("Notified backplane 'change' because [{0}] was added.", item);
                    }
                }

                // trigger only once and not per handle and only if the item was added!
                TriggerOnAdd(item.Key, item.Region);
            }

            return result;
        }
        
        /// <inheritdoc />
        public override async ValueTask ClearAsync()
        {
            CheckDisposed();
            if (_logTrace)
            {
                Logger.LogTrace("Clear: flushing cache...");
            }

            foreach (var handle in _cacheHandles)
            {
                if (_logTrace)
                {
                    Logger.LogTrace("Clear: clearing handle {0}.", handle.Configuration.Name);
                }

                await handle.ClearAsync();
                handle.Stats.OnClear();
            }

            if (_cacheBackplane != null)
            {
                if (_logTrace)
                {
                    Logger.LogTrace("Clear: notifies backplane.");
                }

                _cacheBackplane.NotifyClear();
            }

            TriggerOnClear();
        }

        /// <inheritdoc />
        public override async ValueTask ClearRegionAsync(string region)
        {
            NotNullOrWhiteSpace(region, nameof(region));

            CheckDisposed();
            if (_logTrace)
            {
                Logger.LogTrace("Clear region: {0}.", region);
            }

            foreach (var handle in _cacheHandles)
            {
                if (_logTrace)
                {
                    Logger.LogTrace("Clear region: {0} in handle {1}.", region, handle.Configuration.Name);
                }

                await handle.ClearRegionAsync(region);
                handle.Stats.OnClearRegion(region);
            }

            if (_cacheBackplane != null)
            {
                if (_logTrace)
                {
                    Logger.LogTrace("Clear region: {0}: notifies backplane [clear region].", region);
                }

                _cacheBackplane.NotifyClearRegion(region);
            }

            TriggerOnClearRegion(region);
        }
        
        /// <inheritdoc />
        public override async ValueTask<bool> ExistsAsync(string key)
        {
            foreach (var handle in _cacheHandles)
            {
                if (_logTrace)
                {
                    Logger.LogTrace("Checking if [{0}] exists on handle '{1}'.", key, handle.Configuration.Name);
                }

                if (await handle.ExistsAsync(key))
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public override async ValueTask<bool> ExistsAsync(string key, string region)
        {
            foreach (var handle in _cacheHandles)
            {
                if (_logTrace)
                {
                    Logger.LogTrace("Checking if [{0}:{1}] exists on handle '{2}'.", region, key, handle.Configuration.Name);
                }

                if (await handle.ExistsAsync(key, region))
                {
                    return true;
                }
            }

            return false;
        }
        
        /// <inheritdoc />
        protected override ValueTask<CacheItem<TCacheValue>> GetCacheItemInternalAsync(string key) =>
            GetCacheItemInternalAsync(key, null);
        
        /// <inheritdoc />
        protected override async ValueTask<CacheItem<TCacheValue>> GetCacheItemInternalAsync(string key, string region)
        {
            CheckDisposed();

            CacheItem<TCacheValue> cacheItem = null;

            if (_logTrace)
            {
                Logger.LogTrace("Get [{0}:{1}] started.", region, key);
            }

            for (var handleIndex = 0; handleIndex < _cacheHandles.Length; handleIndex++)
            {
                var handle = _cacheHandles[handleIndex];
                if (string.IsNullOrWhiteSpace(region))
                {
                    cacheItem = handle.GetCacheItem(key);
                }
                else
                {
                    cacheItem = await handle.GetCacheItemAsync(key, region);
                }

                handle.Stats.OnGet(region);

                if (cacheItem != null)
                {
                    if (_logTrace)
                    {
                        Logger.LogTrace("Get [{0}:{1}], found in handle[{2}] '{3}'.", region, key, handleIndex, handle.Configuration.Name);
                    }

                    // update last accessed, might be used for custom sliding implementations
                    cacheItem.LastAccessedUtc = DateTime.UtcNow;

                    // update other handles if needed
                    AddToHandles(cacheItem, handleIndex);
                    handle.Stats.OnHit(region);
                    TriggerOnGet(key, region);
                    break;
                }
                else
                {
                    if (_logTrace)
                    {
                        Logger.LogTrace("Get [{0}:{1}], item NOT found in handle[{2}] '{3}'.", region, key, handleIndex, handle.Configuration.Name);
                    }

                    handle.Stats.OnMiss(region);
                }
            }

            return cacheItem;
        }
        
        /// <inheritdoc />
        protected internal override async ValueTask PutInternalAsync(CacheItem<TCacheValue> item)
        {
            NotNull(item, nameof(item));

            CheckDisposed();
            if (_logTrace)
            {
                Logger.LogTrace("Put [{0}] started.", item);
            }

            foreach (var handle in _cacheHandles)
            {
                if (handle.Configuration.EnableStatistics)
                {
                    // check if it is really a new item otherwise the items count is crap because we
                    // count it every time, but use only the current handle to retrieve the item,
                    // otherwise we would trigger gets and find it in another handle maybe
                    var oldItem = string.IsNullOrWhiteSpace(item.Region) ?
                        await handle.GetCacheItemAsync(item.Key) :
                        await handle.GetCacheItemAsync(item.Key, item.Region);

                    handle.Stats.OnPut(item, oldItem == null);
                }

                if (_logTrace)
                {
                    Logger.LogTrace(
                        "Put [{0}:{1}] successfully to handle '{2}'.",
                        item.Region,
                        item.Key,
                        handle.Configuration.Name);
                }

                await handle.PutAsync(item);
            }

            // update backplane
            if (_cacheBackplane != null)
            {
                if (_logTrace)
                {
                    Logger.LogTrace("Put [{0}:{1}] was scuccessful. Notifying backplane [change].", item.Region, item.Key);
                }

                if (string.IsNullOrWhiteSpace(item.Region))
                {
                    _cacheBackplane.NotifyChange(item.Key, CacheItemChangedEventAction.Put);
                }
                else
                {
                    _cacheBackplane.NotifyChange(item.Key, item.Region, CacheItemChangedEventAction.Put);
                }
            }

            TriggerOnPut(item.Key, item.Region);
        }
        
        /// <inheritdoc />
        protected override ValueTask<bool> RemoveInternalAsync(string key) =>
            RemoveInternalAsync(key, null);
        
        /// <inheritdoc />
        protected override async ValueTask<bool> RemoveInternalAsync(string key, string region)
        {
            CheckDisposed();

            var result = false;

            if (_logTrace)
            {
                Logger.LogTrace("Removing [{0}:{1}].", region, key);
            }

            foreach (var handle in _cacheHandles)
            {
                var handleResult = false;
                if (!string.IsNullOrWhiteSpace(region))
                {
                    handleResult = await handle.RemoveAsync(key, region);
                }
                else
                {
                    handleResult = await handle.RemoveAsync(key);
                }

                if (handleResult)
                {
                    if (_logTrace)
                    {
                        Logger.LogTrace(
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
                if (_cacheBackplane != null)
                {
                    if (_logTrace)
                    {
                        Logger.LogTrace("Removed [{0}:{1}], notifying backplane [remove].", region, key);
                    }

                    if (string.IsNullOrWhiteSpace(region))
                    {
                        _cacheBackplane.NotifyRemove(key);
                    }
                    else
                    {
                        _cacheBackplane.NotifyRemove(key, region);
                    }
                }

                // trigger only once and not per handle
                TriggerOnRemove(key, region);
            }

            return result;
        }

        
        private async ValueTask EvictFromOtherHandlesAsync(string key, string region, int excludeIndex)
        {
            if (excludeIndex < 0 || excludeIndex >= _cacheHandles.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(excludeIndex));
            }

            if (_logTrace)
            {
                Logger.LogTrace("Evict [{0}:{1}] from other handles excluding handle '{2}'.", region, key, excludeIndex);
            }

            for (var handleIndex = 0; handleIndex < _cacheHandles.Length; handleIndex++)
            {
                if (handleIndex != excludeIndex)
                {
                    await EvictFromHandleAsync(key, region, _cacheHandles[handleIndex]);
                }
            }
        }

        private async ValueTask EvictFromHandleAsync(string key, string region, BaseCacheHandle<TCacheValue> handle)
        {
            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug(
                    "Evicting '{0}:{1}' from handle '{2}'.",
                    region,
                    key,
                    handle.Configuration.Name);
            }

            bool result;
            if (string.IsNullOrWhiteSpace(region))
            {
                result = await handle.RemoveAsync(key);
            }
            else
            {
                result = await handle.RemoveAsync(key, region);
            }

            if (result)
            {
                handle.Stats.OnRemove(region);
            }
        }
        
        private static async ValueTask<bool> AddItemToHandleAsync(CacheItem<TCacheValue> item, BaseCacheHandle<TCacheValue> handle)
        {
            if (await handle.AddAsync(item))
            {
                handle.Stats.OnAdd(item);
                return true;
            }

            return false;
        }

    }

#endif
}
