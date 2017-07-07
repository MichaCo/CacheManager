﻿using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.Core.Logging;
using Newtonsoft.Json.Linq;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Couchbase
{
    /// <summary>
    /// Definition for what bucket should be used and optionally a bucket password.
    /// </summary>
    /// <seealso cref="CacheManager.Core.Internal.BaseCacheHandle{TCacheValue}" />
    public class BucketCacheHandleAdditionalConfiguration
    {
        /// <summary>
        /// Gets or sets the name of the bucket.
        /// </summary>
        /// <value>
        /// The name of the bucket.
        /// </value>
        public string BucketName { get; set; } = CouchbaseConfigurationManager.DefaultBucketName;

        /// <summary>
        /// Gets or sets the bucket password.
        /// </summary>
        /// <value>
        /// The bucket password.
        /// </value>
        public string BucketPassword { get; set; }
    }

    /// <summary>
    /// Cache handle implementation based on the couchbase .net client.
    /// </summary>
    /// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
    public class BucketCacheHandle<TCacheValue> : BaseCacheHandle<TCacheValue>
    {
        private readonly CouchbaseConfigurationManager _bucketManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="BucketCacheHandle{TCacheValue}" /> class.
        /// </summary>
        /// <param name="managerConfiguration">The manager configuration.</param>
        /// <param name="configuration">The cache handle configuration.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <exception cref="System.InvalidOperationException">If <c>configuration.HandleName</c> is not valid.</exception>
        public BucketCacheHandle(ICacheManagerConfiguration managerConfiguration, CacheHandleConfiguration configuration, ILoggerFactory loggerFactory)
            : this(managerConfiguration, configuration, loggerFactory, new BucketCacheHandleAdditionalConfiguration())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BucketCacheHandle{TCacheValue}" /> class.
        /// </summary>
        /// <param name="managerConfiguration">The manager configuration.</param>
        /// <param name="configuration">The cache handle configuration.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="additionalSettings">The additional settings.</param>
        /// <exception cref="System.InvalidOperationException">If <c>configuration.HandleName</c> is not valid.</exception>
        public BucketCacheHandle(ICacheManagerConfiguration managerConfiguration, CacheHandleConfiguration configuration, ILoggerFactory loggerFactory, BucketCacheHandleAdditionalConfiguration additionalSettings)
            : base(managerConfiguration, configuration)
        {
            NotNull(configuration, nameof(configuration));
            NotNull(loggerFactory, nameof(loggerFactory));

            Logger = loggerFactory.CreateLogger(this);

            if (additionalSettings == null)
            {
                additionalSettings = new BucketCacheHandleAdditionalConfiguration();
            }

            // we can configure the bucket name by having "<configKey>:<bucketName>" as handle's
            // this should only be used in 100% by app/web.config based configuration
            var nameParts = configuration.Key.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);

            var configurationName = nameParts.Length > 0 ? nameParts[0] : Guid.NewGuid().ToString();

            if (nameParts.Length == 2)
            {
                additionalSettings.BucketName = nameParts[1];
            }

            _bucketManager = new CouchbaseConfigurationManager(configurationName, additionalSettings.BucketName, additionalSettings.BucketPassword);
        }

        /// <inheritdoc />
        public override bool IsDistributedCache => true;

        /// <summary>
        /// Gets the number of items the cache handle currently maintains.
        /// </summary>
        /// <value>The count.</value>
        public override int Count => (int)Stats.GetStatistic(CacheStatsCounterType.Items);

        /// <inheritdoc />
        protected override ILogger Logger { get; }

        /// <summary>
        /// Clears this cache, removing all items in the base cache and all regions.
        /// </summary>
        public override void Clear()
        {
            // warning: takes ~20seconds to flush the bucket...
            var manager = _bucketManager.GetManager();
            if (manager != null)
            {
                using (manager)
                {
                    manager.Flush();
                }
            }
        }

        /// <summary>
        /// Clears the cache region, removing all items from the specified <paramref name="region"/> only.
        /// </summary>
        /// <param name="region">The cache region.</param>
        /// <exception cref="System.NotImplementedException">Not supported in this version.</exception>
        public override void ClearRegion(string region)
        {
            // TODO: not supported?
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override bool Exists(string key)
        {
            var fullKey = GetKey(key);
            return _bucketManager.Bucket.Exists(fullKey);
        }

        /// <inheritdoc />
        public override bool Exists(string key, string region)
        {
            NotNullOrWhiteSpace(region, nameof(region));

            var fullKey = GetKey(key, region);
            return _bucketManager.Bucket.Exists(fullKey);
        }

        /// <inheritdoc />
        /// <exception cref="System.NotImplementedException">Not supported in this version.</exception>
        public override IEnumerable<string> Keys(string pattern, string region)
        {
            throw new NotImplementedException();
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
            NotNull(item, nameof(item));

            var fullKey = GetKey(item.Key, item.Region);
            if (item.ExpirationMode != ExpirationMode.None)
            {
                return _bucketManager.Bucket.Insert(fullKey, item, item.ExpirationTimeout).Success;
            }

            return _bucketManager.Bucket.Insert(fullKey, item).Success;
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
            }

            base.Dispose(disposeManaged);
        }

        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key) =>
            GetCacheItemInternal(key, null);

        /// <summary>
        /// Gets a <c>CacheItem</c> for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <param name="region">The cache region.</param>
        /// <returns>The <c>CacheItem</c>.</returns>
        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key, string region)
        {
            var fullkey = GetKey(key, region);
            var result = _bucketManager.Bucket.Get<CacheItem<TCacheValue>>(fullkey);

            if (!result.Success)
            {
                return null;
            }

            var cacheItem = result.Value;
            if (cacheItem.Value is JToken)
            {
                var value = cacheItem.Value as JToken;
                cacheItem = cacheItem.WithValue((TCacheValue)value.ToObject(cacheItem.ValueType));
            }

            if (cacheItem.IsExpired)
            {
                return null;
            }
            
            // extend sliding expiration
            if (cacheItem.ExpirationMode == ExpirationMode.Sliding)
            {
                cacheItem.LastAccessedUtc = DateTime.UtcNow;
                PutInternalPrepared(cacheItem);
            }

            return cacheItem;
        }

        /// <summary>
        /// Puts the <paramref name="item"/> into the cache. If the item exists it will get updated
        /// with the new value. If the item doesn't exist, the item will be added to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        protected override void PutInternalPrepared(CacheItem<TCacheValue> item)
        {
            NotNull(item, nameof(item));

            var fullKey = GetKey(item.Key, item.Region);
            if (item.ExpirationMode == ExpirationMode.Absolute || item.ExpirationMode == ExpirationMode.Sliding)
            {
                _bucketManager.Bucket.Upsert(fullKey, item, item.ExpirationTimeout);
            }
            else
            {
                _bucketManager.Bucket.Upsert(fullKey, item);
            }
        }

        /// <summary>
        /// Removes a value from the cache for the specified key.
        /// </summary>
        /// <param name="key">The key being used to identify the item within the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was found and removed from the cache, <c>false</c> otherwise.
        /// </returns>
        protected override bool RemoveInternal(string key) => RemoveInternal(key, null);

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
            var fullKey = GetKey(key, region);
            var result = _bucketManager.Bucket.Remove(fullKey);
            return result.Success;
        }

        private static string GetSHA256Key(string key)
        {
            using (var sha = SHA256.Create())
            {
                var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
                return Convert.ToBase64String(hashBytes);
            }
        }

        private static string GetKey(string key, string region = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

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
    }
}