using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CacheManager.Core;
using CacheManager.Core.Cache;
using CacheManager.Core.Configuration;
using Microsoft.ApplicationServer.Caching;

namespace CacheManager.AppFabricCache
{
    /// <summary>
    /// A cache handle implementing AppFabricCache.
    /// </summary>
    /// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
    public class AppFabricCacheHandle<TCacheValue> : BaseCacheHandle<TCacheValue>
    {
        // TODO: make this configurable...
        private const int DefaultMaxRetryCount = 50;

        private const string DefaultName = "DEFAULT";

        // TODO: make this configurable...
        private const int DefaultRetryWaitTimeout = 0;

        private static List<string> customRegions = new List<string>();

        private static object regionLock = new object();

        private static int[] transientErrorCodes = new int[]
            {
                DataCacheErrorCode.ConnectionTerminated,
                DataCacheErrorCode.RetryLater,
                DataCacheErrorCode.Timeout,
                DataCacheErrorCode.ServiceAccessError,
            };

        private DataCache cache = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppFabricCacheHandle{TCacheValue}"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="configuration">The configuration.</param>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "not possible")]
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Gets validated in base.")]
        public AppFabricCacheHandle(ICacheManager<TCacheValue> manager, ICacheHandleConfiguration configuration)
            : base(manager, configuration)
        {
            DataCacheFactoryConfiguration cfg = null;

            cfg = new DataCacheFactoryConfiguration(configuration.HandleName);

            var factory = cfg == null ? new DataCacheFactory() : new DataCacheFactory(cfg);

            RunRetry(() =>
            {
                if (configuration.HandleName.ToUpper(CultureInfo.InvariantCulture).Equals(DefaultName))
                {
                    this.cache = factory.GetDefaultCache();
                }
                else
                {
                    this.cache = factory.GetCache(configuration.HandleName);
                }
            });
        }

        /// <summary>
        /// Gets the number of items the cache handle currently maintains.
        /// </summary>
        /// <value>The count.</value>
        public override int Count
        {
            get { return (int)this.Stats.GetStatistic(CacheStatsCounterType.Items); }
        }

        /// <summary>
        /// Clears this cache, removing all items in the base cache and all regions.
        /// </summary>
        public override void Clear()
        {
            RunRetry(() =>
            {
                foreach (var region in this.cache.GetSystemRegions())
                {
                    this.cache.ClearRegion(region);
                }
            });

            if (customRegions.Count > 0)
            {
                lock (regionLock)
                {
                    foreach (var region in customRegions)
                    {
                        RunRetry(() =>
                        {
                            this.cache.RemoveRegion(region);
                        });
                    }

                    customRegions.Clear();
                }
            }
        }

        /// <summary>
        /// Clears the cache region, removing all items from the specified <paramref name="region"/> only.
        /// </summary>
        /// <param name="region">The cache region.</param>
        public override void ClearRegion(string region)
        {
            RunRetry(() =>
            {
                try
                {
                    if (customRegions.Contains(region))
                    {
                        lock (regionLock)
                        {
                            if (customRegions.Contains(region))
                            {
                                customRegions.Remove(region);
                                this.cache.RemoveRegion(region);
                            }
                        }
                    }
                    else
                    {
                        this.cache.ClearRegion(region);
                    }
                }
                catch (DataCacheException ex)
                {
                    if (ex.ErrorCode == DataCacheErrorCode.RegionDoesNotExist)
                    {
                        // do nothing, if the region doesn't exist, all items are already gone.
                    }
                    else
                    {
                        throw;
                    }
                }
            });
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
        /// <param name="updateValue">The function to perform the update.</param>
        /// <param name="config">The cache configuration used to specify the update behavior.</param>
        /// <returns>The update result which is interpreted by the cache manager.</returns>
        /// <remarks>
        /// If the cache does not use a distributed cache system. Update is doing exactly the same
        /// as Get plus Put.
        /// </remarks>
        public override UpdateItemResult Update(string key, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config)
        {
            return this.Update(key, null, updateValue, config);
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
        /// <exception cref="System.ArgumentNullException">If updateValue or config are null.</exception>
        /// <remarks>
        /// If the cache does not use a distributed cache system. Update is doing exactly the same
        /// as Get plus Put.
        /// </remarks>
        public override UpdateItemResult Update(string key, string region, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config)
        {
            if (updateValue == null)
            {
                throw new ArgumentNullException("updateValue");
            }
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            DataCacheLockHandle lockHandle = null;
            CacheItem<TCacheValue> item = null;
            var retry = false;  // indicate lock = version conflict
            var hasVersionConflict = false;
            var tries = 0;
            do
            {
                tries++;
                retry = false;
                try
                {
                    if (string.IsNullOrWhiteSpace(region))
                    {
                        item = this.cache.GetAndLock(key, TimeSpan.FromMilliseconds(100), out lockHandle) as CacheItem<TCacheValue>;
                    }
                    else
                    {
                        RegisterRegion(region);
                        item = this.cache.GetAndLock(key, TimeSpan.FromMilliseconds(100), out lockHandle, region) as CacheItem<TCacheValue>;
                    }
                }
                catch (DataCacheException ex)
                {
                    if (ex.ErrorCode == DataCacheErrorCode.ObjectLocked)
                    {
                        // object seems to be locked so we have a version conflict and we'll retry...
                        retry = true;
                        hasVersionConflict = true;
                    }
                    else if (IsTransientError(ex))
                    {
                        retry = true;
                        Task.Delay(DefaultRetryWaitTimeout).Wait();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            while (retry && tries <= config.MaxRetries);

            if (item == null)
            {
                return new UpdateItemResult(hasVersionConflict, false, tries);
            }

            item = item.WithValue(updateValue(item.Value));

            do
            {
                tries++;
                retry = false;
                try
                {
                    if (string.IsNullOrWhiteSpace(item.Region))
                    {
                        if (item.ExpirationTimeout != TimeSpan.Zero)
                        {
                            this.cache.PutAndUnlock(item.Key, item, lockHandle, item.ExpirationTimeout);
                        }
                        else
                        {
                            this.cache.PutAndUnlock(item.Key, item, lockHandle);
                        }
                    }
                    else
                    {
                        if (item.ExpirationTimeout != TimeSpan.Zero)
                        {
                            this.cache.PutAndUnlock(item.Key, item, lockHandle, item.ExpirationTimeout, item.Region);
                        }
                        else
                        {
                            this.cache.PutAndUnlock(item.Key, item, lockHandle, item.Region);
                        }
                    }
                }
                catch (DataCacheException ex)
                {
                    if (ex.ErrorCode == DataCacheErrorCode.ObjectNotLocked
                        || ex.ErrorCode == DataCacheErrorCode.InvalidCacheLockHandle
                        || ex.ErrorCode == DataCacheErrorCode.KeyDoesNotExist)
                    {
                        return new UpdateItemResult(hasVersionConflict, false, tries);
                    }
                    else if (IsTransientError(ex))
                    {
                        Task.Delay(DefaultRetryWaitTimeout).Wait();
                        retry = true;
                    }
                    else
                    {
                        // return new UpdateItemResult(hasVersionConflict, false, tries);
                        throw; // shell we throw the exception? Usually we just return false...
                    }
                }
            }
            while (retry && tries <= config.MaxRetries);

            // if retry is still true, we exceeded max tries: returning false indicating that the
            // item has not been updated
            if (retry)
            {
                return new UpdateItemResult(hasVersionConflict, false, tries);
            }

            return new UpdateItemResult(hasVersionConflict, true, tries);
        }

        /// <summary>
        /// Adds a value to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">If item is null.</exception>
        protected override bool AddInternalPrepared(CacheItem<TCacheValue> item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            try
            {
                RunRetry(() =>
                {
                    if (string.IsNullOrWhiteSpace(item.Region))
                    {
                        if (item.ExpirationTimeout != TimeSpan.Zero)
                        {
                            this.cache.Add(item.Key, item, item.ExpirationTimeout);
                        }
                        else
                        {
                            this.cache.Add(item.Key, item);
                        }
                    }
                    else
                    {
                        RegisterRegion(item.Region);
                        try
                        {
                            if (item.ExpirationTimeout != TimeSpan.Zero)
                            {
                                this.cache.Add(item.Key, item, item.ExpirationTimeout, item.Region);
                            }
                            else
                            {
                                this.cache.Add(item.Key, item, item.Region);
                            }
                        }
                        catch (DataCacheException ex)
                        {
                            // create non existing region and retry... this can occur if the cache
                            // cluster got restarted or what not.
                            if (ex.ErrorCode == DataCacheErrorCode.RegionDoesNotExist)
                            {
                                this.CreateRegion(item.Region);
                                this.AddInternal(item);
                            }
                            else
                            {
                                throw;
                            }
                        }
                    }
                });

                return true;
            }
            catch (DataCacheException ex)
            {
                // according to documentation
                // http: //msdn.microsoft.com/en-us/library/windowsazure/ff424901.aspx this is the
                // only way to determine if the object was added successfully...
                if (ex.ErrorCode == DataCacheErrorCode.KeyAlreadyExists)
                {
                    return false;
                }

                throw;
            }
        }

        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key)
        {
            return this.GetCacheItemInternal(key, null);
        }

        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key, string region)
        {
            try
            {
                return RunRetry(() =>
                {
                    if (string.IsNullOrWhiteSpace(region))
                    {
                        return this.cache.Get(key) as CacheItem<TCacheValue>;
                    }
                    else
                    {
                        RegisterRegion(region);
                        return this.cache.Get(key, region) as CacheItem<TCacheValue>;
                    }
                });
            }
            catch (DataCacheException)
            {
                return null;
            }
        }

        /// <summary>
        /// Puts the <paramref name="item"/> into the cache. If the item exists it will get updated
        /// with the new value. If the item doesn't exist, the item will be added to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        protected override void PutInternalPrepared(CacheItem<TCacheValue> item)
        {
            RunRetry(() =>
            {
                PutNoLock(item, item.ExpirationTimeout);
            });
        }

        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        protected override bool RemoveInternal(string key)
        {
            return RunRetry(() =>
            {
                return RemoveInternal(key, null);
            });
        }

        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        protected override bool RemoveInternal(string key, string region)
        {
            return RunRetry(() =>
            {
                if (string.IsNullOrWhiteSpace(region))
                {
                    return this.cache.Remove(key);
                }
                else
                {
                    RegisterRegion(region);
                    return this.cache.Remove(key, region);
                }
            });
        }

        private static bool IsTransientError(DataCacheException ex)
        {
            return transientErrorCodes.Contains(ex.ErrorCode);
        }

        private static void RegisterRegion(string region)
        {
            if (!customRegions.Contains(region))
            {
                lock (regionLock)
                {
                    if (!customRegions.Contains(region))
                    {
                        customRegions.Add(region);
                    }
                }
            }
        }

        private static T RunRetry<T>(Func<T> retryFunc, int maxRetries = DefaultMaxRetryCount)
        {
            int currentRetry = 0;

            while (currentRetry < maxRetries)
            {
                try
                {
                    return retryFunc();
                }
                catch (DataCacheException ex)
                {
                    if (!IsTransientError(ex))
                    {
                        throw;
                    }
                }

                Trace.TraceWarning("Retrying action...");
                Task.Delay(DefaultRetryWaitTimeout).Wait();
                currentRetry++;
            }

            return default(T);
        }

        private static void RunRetry(Action retryFunc, int maxRetries = DefaultMaxRetryCount)
        {
            RunRetry(
                () =>
                {
                    retryFunc();
                    return true;
                },
                maxRetries);
        }

        private void CreateRegion(string region)
        {
            // Log.Info(string.Concat("Creation region [", region, "]"));
            RunRetry(() =>
            {
                this.cache.CreateRegion(region);
            });

            RegisterRegion(region);
        }

        private void PutNoLock(CacheItem<TCacheValue> item, TimeSpan expirationTimeout)
        {
            if (string.IsNullOrWhiteSpace(item.Region))
            {
                if (expirationTimeout != TimeSpan.Zero)
                {
                    this.cache.Put(item.Key, item, expirationTimeout);
                }
                else
                {
                    this.cache.Put(item.Key, item);
                }
            }
            else
            {
                try
                {
                    RegisterRegion(item.Region);
                    if (expirationTimeout != TimeSpan.Zero)
                    {
                        this.cache.Put(item.Key, item, expirationTimeout, item.Region);
                    }
                    else
                    {
                        this.cache.Put(item.Key, item, item.Region);
                    }
                }
                catch (DataCacheException ex)
                {
                    // create non existing region and retry... this can occur if the cache cluster
                    // got restarted or what not.
                    if (ex.ErrorCode == DataCacheErrorCode.RegionDoesNotExist)
                    {
                        this.CreateRegion(item.Region);
                        this.AddInternal(item);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
    }
}