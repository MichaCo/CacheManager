using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using CacheManager.Core;
using CacheManager.Core.Internal;
using Enyim.Caching;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached;
using Enyim.Caching.Memcached.Results;

namespace CacheManager.Memcached
{
    /// <summary>
    /// Cache handle implementation based on the Enyim memcached client.
    /// </summary>
    public class MemcachedCacheHandle : MemcachedCacheHandle<object>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemcachedCacheHandle"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="configuration">The configuration.</param>
        public MemcachedCacheHandle(ICacheManager<object> manager, CacheHandleConfiguration configuration)
            : base(manager, configuration)
        {
        }
    }

    /// <summary>
    /// Cache handle implementation based on the Enyim memcached client.
    /// </summary>
    /// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
    public class MemcachedCacheHandle<TCacheValue> : BaseCacheHandle<TCacheValue>
    {
        private static readonly string DefaultEnyimSectionName = "enyim.com/memcached";
        private static readonly string DefaultSectionName = "default";

        /// <summary>
        /// Initializes a new instance of the <see cref="MemcachedCacheHandle{TCacheValue}"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="configuration">The configuration.</param>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="configuration"/> is null.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// The cache value type is not serializable or if the enyim configuration section could not
        /// be initialized.
        /// </exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Cache gets disposed correctly when the owner gets disposed.")]
        public MemcachedCacheHandle(ICacheManager<TCacheValue> manager, CacheHandleConfiguration configuration)
            : base(manager, configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (!typeof(TCacheValue).IsSerializable)
            {
                throw new InvalidOperationException("The cache value type must be serializable but " + typeof(TCacheValue).ToString() + " is not.");
            }

            // initialize memcached client with section name which must be equal to handle name...
            // Default is "enyim.com/memcached"
            try
            {
                var sectionName = GetEnyimSectionName(configuration.HandleName);
                this.Cache = new MemcachedClient(sectionName);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new InvalidOperationException("Failed to initialize " + this.GetType().Name + ". " + ex.BareMessage, ex);
            }
        }

        /// <summary>
        /// Gets the number of items the cache handle currently maintains.
        /// </summary>
        /// <value>The count.</value>
        public override int Count
        {
            get
            {
                return (int)this.Stats.GetStatistic(CacheStatsCounterType.Items);
            }
        }

        /// <summary>
        /// Gets the get memcached client configuration.
        /// </summary>
        /// <value>The get memcached client configuration.</value>
        public IMemcachedClientConfiguration GetMemcachedClientConfiguration
        {
            get
            {
                return this.GetSection();
            }
        }

        /// <summary>
        /// Gets the total number of items per server.
        /// </summary>
        /// <value>The total number of items per server.</value>
        public IEnumerable<long> ServerItemCount
        {
            get
            {
                foreach (var count in this.Cache.Stats().GetRaw("total_items"))
                {
                    yield return long.Parse(count.Value, CultureInfo.InvariantCulture);
                }
            }
        }

        /// <summary>
        /// Gets the servers.
        /// </summary>
        /// <value>The servers.</value>
        public IList<IPEndPoint> Servers
        {
            get
            {
                return this.GetServers();
            }
        }

        /// <summary>
        /// Gets or sets the cache.
        /// </summary>
        /// <value>The cache.</value>
        protected MemcachedClient Cache { get; set; }

        /// <summary>
        /// Clears this cache, removing all items in the base cache and all regions.
        /// </summary>
        public override void Clear()
        {
            this.Cache.FlushAll();
        }

        /// <summary>
        /// Clears the cache region, removing all items from the specified <paramref name="region"/> only.
        /// </summary>
        /// <param name="region">The cache region.</param>
        public override void ClearRegion(string region)
        {
            // not supported, clearing all instead
            // TODO: find workaround this.Clear();
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
        public override UpdateItemResult<TCacheValue> Update(string key, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config)
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
        /// <remarks>
        /// If the cache does not use a distributed cache system. Update is doing exactly the same
        /// as Get plus Put.
        /// </remarks>
        public override UpdateItemResult<TCacheValue> Update(string key, string region, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config)
        {
            return this.Set(key, region, updateValue, config);
        }

        /// <summary>
        /// Adds a value to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
        /// </returns>
        protected override bool AddInternalPrepared(CacheItem<TCacheValue> item)
        {
            return this.Store(StoreMode.Add, item).Success;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        /// <param name="disposeManaged">Indicator if managed resources should be released.</param>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                this.Cache.Dispose();
            }

            base.Dispose(disposeManaged);
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
            var item = this.Cache.Get(GetKey(key, region));
            return item as CacheItem<TCacheValue>;
        }

        /// <summary>
        /// Puts the <paramref name="item"/> into the cache. If the item exists it will get updated
        /// with the new value. If the item doesn't exist, the item will be added to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        protected override void PutInternalPrepared(CacheItem<TCacheValue> item)
        {
            this.Store(StoreMode.Set, item);
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
            return this.RemoveInternal(key, null);
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
            return this.Cache.Remove(GetKey(key, region));
        }

        /// <summary>
        /// Stores the item with the specified mode.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="item">The item.</param>
        /// <returns>The result.</returns>
        protected virtual IStoreOperationResult Store(StoreMode mode, CacheItem<TCacheValue> item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var key = GetKey(item.Key, item.Region);

            if (item.ExpirationMode == ExpirationMode.Absolute)
            {
                // the implementation internally transforms it to UTC, we have to work with local time
                var timeoutDate = DateTime.Now.Add(item.ExpirationTimeout);
                var result = this.Cache.ExecuteStore(mode, key, item, timeoutDate);
                return result;
            }
            else if (item.ExpirationMode == ExpirationMode.Sliding)
            {
                var result = this.Cache.ExecuteStore(mode, key, item, item.ExpirationTimeout);
                return result;
            }
            else
            {
                var result = this.Cache.ExecuteStore(mode, key, item);
                return result;
            }
        }

        /// <summary>
        /// Gets the name of the enyim section.
        /// </summary>
        /// <param name="handleName">Name of the handle.</param>
        /// <returns>The section name.</returns>
        private static string GetEnyimSectionName(string handleName)
        {
            if (handleName.Equals(DefaultSectionName, StringComparison.OrdinalIgnoreCase))
            {
                return DefaultEnyimSectionName;
            }
            else
            {
                return handleName;
            }
        }

        private static string GetKey(string key, string region = null)
        {
            var fullKey = key;

            if (!string.IsNullOrWhiteSpace(region))
            {
                fullKey = string.Concat(region, ":", key);
            }

            // Memcached still has a 250 character limit
            if (fullKey.Length >= 250)
            {
                return GetSHA256Key(fullKey);
            }

            return fullKey;
        }

        private static string GetSHA256Key(string key)
        {
            using (var sha = SHA256Managed.Create())
            {
                byte[] hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
                return Convert.ToBase64String(hashBytes);
            }
        }

        private static bool ShouldRetry(StatusCode statusCode)
        {
            switch (statusCode)
            {
                case StatusCode.NodeShutdown:
                case StatusCode.OperationTimeout:
                case StatusCode.OutOfMemory:
                case StatusCode.Busy:
                case StatusCode.SocketPoolTimeout:
                case StatusCode.UnableToLocateNode:
                case StatusCode.VBucketBelongsToAnotherServer:
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the section.
        /// </summary>
        /// <returns>The client configuration.</returns>
        /// <exception cref="System.Configuration.ConfigurationErrorsException">
        /// If memcached client section was not found or there are no servers defined for memcached.
        /// </exception>
        private IMemcachedClientConfiguration GetSection()
        {
            string sectionName = GetEnyimSectionName(this.Configuration.HandleName);
            MemcachedClientSection section = (MemcachedClientSection)ConfigurationManager.GetSection(sectionName);

            if (section == null)
            {
                throw new ConfigurationErrorsException("Client section " + sectionName + " is not found.");
            }

            // validate
            if (section.Servers.Count <= 0)
            {
                throw new ConfigurationErrorsException("There are no servers defined.");
            }

            return section;
        }

        private IList<IPEndPoint> GetServers()
        {
            var section = this.GetSection();
            return section.Servers;
        }

        private UpdateItemResult<TCacheValue> Set(string key, string region, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config)
        {
            var fullyKey = GetKey(key, region);
            var tries = 0;
            IStoreOperationResult result;

            do
            {
                tries++;
                var getTries = 0;
                StatusCode getStatus;
                CacheItem<TCacheValue> item;
                CasResult<CacheItem<TCacheValue>> cas;
                do
                {
                    getTries++;
                    cas = this.Cache.GetWithCas<CacheItem<TCacheValue>>(fullyKey);

                    item = cas.Result;
                    getStatus = (StatusCode)cas.StatusCode;
                }
                while (ShouldRetry(getStatus) && getTries <= config.MaxRetries);

                // break operation if we cannot retrieve the object (maybe it has expired already).
                if (getStatus != StatusCode.Success || item == null)
                {
                    return UpdateItemResult.ForItemDidNotExist<TCacheValue>();
                }

                item = item.WithValue(updateValue(item.Value));

                if (item.ExpirationMode == ExpirationMode.Absolute)
                {
                    var timeoutDate = item.ExpirationTimeout;
                    result = this.Cache.ExecuteCas(StoreMode.Set, fullyKey, item, timeoutDate, cas.Cas);
                }
                else if (item.ExpirationMode == ExpirationMode.Sliding)
                {
                    result = this.Cache.ExecuteCas(StoreMode.Set, fullyKey, item, item.ExpirationTimeout, cas.Cas);
                }
                else
                {
                    result = this.Cache.ExecuteCas(StoreMode.Set, fullyKey, item, cas.Cas);
                }

                if (result.Success)
                {
                    return UpdateItemResult.ForSuccess<TCacheValue>(item.Value, tries > 1, tries);
                }
            }
            while (!result.Success && result.StatusCode.HasValue && result.StatusCode.Value == 2 && tries <= config.MaxRetries);

            return UpdateItemResult.ForTooManyRetries<TCacheValue>(tries);
        }
    }
}