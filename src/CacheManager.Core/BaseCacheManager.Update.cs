using System;
using System.Globalization;
using System.Linq;
using CacheManager.Core.Internal;
using CacheManager.Core.Logging;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core
{
    public partial class BaseCacheManager<TCacheValue>
    {
        /// <inheritdoc />
        public TCacheValue AddOrUpdate(string key, TCacheValue addValue, Func<TCacheValue, TCacheValue> updateValue) =>
            AddOrUpdate(key, addValue, updateValue, Configuration.MaxRetries);

        /// <inheritdoc />
        public TCacheValue AddOrUpdate(string key, string region, TCacheValue addValue, Func<TCacheValue, TCacheValue> updateValue) =>
            AddOrUpdate(key, region, addValue, updateValue, Configuration.MaxRetries);

        /// <inheritdoc />
        public TCacheValue AddOrUpdate(string key, TCacheValue addValue, Func<TCacheValue, TCacheValue> updateValue, int maxRetries) =>
            AddOrUpdate(new CacheItem<TCacheValue>(key, addValue), updateValue, maxRetries);

        /// <inheritdoc />
        public TCacheValue AddOrUpdate(string key, string region, TCacheValue addValue, Func<TCacheValue, TCacheValue> updateValue, int maxRetries) =>
            AddOrUpdate(new CacheItem<TCacheValue>(key, region, addValue), updateValue, maxRetries);

        /// <inheritdoc />
        public TCacheValue AddOrUpdate(CacheItem<TCacheValue> addItem, Func<TCacheValue, TCacheValue> updateValue) =>
            AddOrUpdate(addItem, updateValue, Configuration.MaxRetries);

        /// <inheritdoc />
        public TCacheValue AddOrUpdate(CacheItem<TCacheValue> addItem, Func<TCacheValue, TCacheValue> updateValue, int maxRetries)
        {
            NotNull(addItem, nameof(addItem));
            NotNull(updateValue, nameof(updateValue));
            Ensure(maxRetries >= 0, "Maximum number of retries must be greater than or equal to zero.");

            return AddOrUpdateInternal(addItem, updateValue, maxRetries);
        }

        private TCacheValue AddOrUpdateInternal(CacheItem<TCacheValue> item, Func<TCacheValue, TCacheValue> updateValue, int maxRetries)
        {
            CheckDisposed();
            if (_logTrace)
            {
                Logger.LogTrace("Add or update: {0} {1}.", item.Key, item.Region);
            }

            var tries = 0;
            do
            {
                tries++;

                if (AddInternal(item))
                {
                    if (_logTrace)
                    {
                        Logger.LogTrace("Add or update: {0} {1}: successfully added the item.", item.Key, item.Region);
                    }

                    return item.Value;
                }

                if (_logTrace)
                {
                    Logger.LogTrace(
                        "Add or update: {0} {1}: add failed, trying to update...",
                        item.Key,
                        item.Region);
                }

                TCacheValue returnValue;
                var updated = string.IsNullOrWhiteSpace(item.Region) ?
                    TryUpdate(item.Key, updateValue, maxRetries, out returnValue) :
                    TryUpdate(item.Key, item.Region, updateValue, maxRetries, out returnValue);

                if (updated)
                {
                    if (_logTrace)
                    {
                        Logger.LogTrace("Add or update: {0} {1}: successfully updated.", item.Key, item.Region);
                    }

                    return returnValue;
                }

                if (_logTrace)
                {
                    Logger.LogTrace(
                        "Add or update: {0} {1}: update FAILED, retrying [{2}/{3}].",
                        item.Key,
                        item.Region,
                        tries,
                        Configuration.MaxRetries);
                }
            }
            while (tries <= maxRetries);

            // exceeded max retries, failing the operation... (should not happen in 99,99% of the cases though, better throw?)
            throw new InvalidOperationException(
                string.Format(CultureInfo.InvariantCulture, "Could not add or update the item {0} {1}.", item.Key, item.Region));
        }

        /// <inheritdoc />
        public bool TryUpdate(string key, Func<TCacheValue, TCacheValue> updateValue, out TCacheValue value) =>
            TryUpdate(key, updateValue, Configuration.MaxRetries, out value);

        /// <inheritdoc />
        public bool TryUpdate(string key, string region, Func<TCacheValue, TCacheValue> updateValue, out TCacheValue value) =>
            TryUpdate(key, region, updateValue, Configuration.MaxRetries, out value);

        /// <inheritdoc />
        public bool TryUpdate(string key, Func<TCacheValue, TCacheValue> updateValue, int maxRetries, out TCacheValue value)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNull(updateValue, nameof(updateValue));
            Ensure(maxRetries >= 0, "Maximum number of retries must be greater than or equal to zero.");

            return UpdateInternal(_cacheHandles, key, updateValue, maxRetries, false, out value);
        }

        /// <inheritdoc />
        public bool TryUpdate(string key, string region, Func<TCacheValue, TCacheValue> updateValue, int maxRetries, out TCacheValue value)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNullOrWhiteSpace(region, nameof(region));
            NotNull(updateValue, nameof(updateValue));
            Ensure(maxRetries >= 0, "Maximum number of retries must be greater than or equal to zero.");

            return UpdateInternal(_cacheHandles, key, region, updateValue, maxRetries, false, out value);
        }

        /// <inheritdoc />
        public TCacheValue Update(string key, Func<TCacheValue, TCacheValue> updateValue) =>
            Update(key, updateValue, Configuration.MaxRetries);

        /// <inheritdoc />
        public TCacheValue Update(string key, string region, Func<TCacheValue, TCacheValue> updateValue) =>
            Update(key, region, updateValue, Configuration.MaxRetries);

        /// <inheritdoc />
        public TCacheValue Update(string key, Func<TCacheValue, TCacheValue> updateValue, int maxRetries)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNull(updateValue, nameof(updateValue));
            Ensure(maxRetries >= 0, "Maximum number of retries must be greater than or equal to zero.");

            if (!UpdateInternal(_cacheHandles, key, updateValue, maxRetries, true, out var value))
            {
                throw new InvalidOperationException($"Update failed for key '{key}'.");
            }

            return value;
        }

        /// <inheritdoc />
        public TCacheValue Update(string key, string region, Func<TCacheValue, TCacheValue> updateValue, int maxRetries)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNullOrWhiteSpace(region, nameof(region));
            NotNull(updateValue, nameof(updateValue));
            Ensure(maxRetries >= 0, "Maximum number of retries must be greater than or equal to zero.");

            if (!UpdateInternal(_cacheHandles, key, region, updateValue, maxRetries, true, out var value))
            {
                throw new InvalidOperationException($"Update failed for key '{region}:{key}'.");
            }

            return value;
        }

        private bool UpdateInternal(
            BaseCacheHandle<TCacheValue>[] handles,
            string key,
            Func<TCacheValue, TCacheValue> updateValue,
            int maxRetries,
            bool throwOnFailure,
            out TCacheValue value) =>
            UpdateInternal(handles, key, null, updateValue, maxRetries, throwOnFailure, out value);

        private bool UpdateInternal(
            BaseCacheHandle<TCacheValue>[] handles,
            string key,
            string region,
            Func<TCacheValue, TCacheValue> updateValue,
            int maxRetries,
            bool throwOnFailure,
            out TCacheValue value)
        {
            CheckDisposed();

            // assign null
            value = default(TCacheValue);

            if (handles.Length == 0)
            {
                return false;
            }

            if (_logTrace)
            {
                Logger.LogTrace("Update: {0} {1}.", key, region);
            }

            // lowest level
            // todo: maybe check for only run on the backplate if configured (could potentially be not the last one).
            var handleIndex = handles.Length - 1;
            var handle = handles[handleIndex];

            var result = string.IsNullOrWhiteSpace(region) ?
                handle.Update(key, updateValue, maxRetries) :
                handle.Update(key, region, updateValue, maxRetries);

            if (_logTrace)
            {
                Logger.LogTrace(
                    "Update: {0} {1}: tried on handle {2}: result: {3}.",
                    key,
                    region,
                    handle.Configuration.Name,
                    result.UpdateState);
            }

            if (result.UpdateState == UpdateItemResultState.Success)
            {
                // only on success, the returned value will not be null
                value = result.Value.Value;
                handle.Stats.OnUpdate(key, region, result);

                // evict others, we don't know if the update on other handles could actually
                // succeed... There is a risk the update on other handles could create a
                // different version than we created with the first successful update... we can
                // safely add the item to handles below us though.
                EvictFromHandlesAbove(key, region, handleIndex);

                // optimizing - not getting the item again from cache. We already have it
                // var item = string.IsNullOrWhiteSpace(region) ? handle.GetCacheItem(key) : handle.GetCacheItem(key, region);
                AddToHandlesBelow(result.Value, handleIndex);
                TriggerOnUpdate(key, region);
            }
            else if (result.UpdateState == UpdateItemResultState.FactoryReturnedNull)
            {
                Logger.LogWarn($"Update failed on '{region}:{key}' because value factory returned null.");

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
                Logger.LogWarn($"Update failed on '{region}:{key}' because of too many retries.");

                EvictFromOtherHandles(key, region, handleIndex);

                if (throwOnFailure)
                {
                    throw new InvalidOperationException($"Update failed on '{region}:{key}' because of too many retries: {result.NumberOfTriesNeeded}.");
                }
            }
            else if (result.UpdateState == UpdateItemResultState.ItemDidNotExist)
            {
                // If update fails because item doesn't exist AND the current handle is backplane source or the lowest cache handle level,
                // remove the item from other handles (if exists).
                // Otherwise, if we do not exit here, calling update on the next handle might succeed and would return a misleading result.
                Logger.LogInfo($"Update failed on '{region}:{key}' because the region/key did not exist.");

                EvictFromOtherHandles(key, region, handleIndex);

                if (throwOnFailure)
                {
                    throw new InvalidOperationException($"Update failed on '{region}:{key}' because the region/key did not exist.");
                }
            }

            // update backplane
            if (result.UpdateState == UpdateItemResultState.Success && _cacheBackplane != null)
            {
                if (_logTrace)
                {
                    Logger.LogTrace("Update: {0} {1}: notifies backplane [change].", key, region);
                }

                if (string.IsNullOrWhiteSpace(region))
                {
                    _cacheBackplane.NotifyChange(key, CacheItemChangedEventAction.Update);
                }
                else
                {
                    _cacheBackplane.NotifyChange(key, region, CacheItemChangedEventAction.Update);
                }
            }

            return result.UpdateState == UpdateItemResultState.Success;
        }
    }
}
