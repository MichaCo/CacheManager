using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.Core.Logging;
using CacheManager.Core.Utility;
using Enyim.Caching;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached;
using Enyim.Caching.Memcached.Results;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Memcached
{
    /// <summary>
    /// Cache handle implementation based on the Enyim memcached client.
    /// </summary>
    /// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
    [RequiresSerializer]
    public class MemcachedCacheHandle<TCacheValue> : BaseCacheHandle<TCacheValue>
    {
        private static readonly string _defaultEnyimSectionName = "enyim.com/memcached";
        private static readonly string _defaultSectionName = "default";
        private static readonly TimeSpan _maximumTimeout = TimeSpan.FromDays(30);
        private readonly ICacheManagerConfiguration _managerConfiguration;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemcachedCacheHandle{TCacheValue}"/> class.
        /// </summary>
        /// <param name="managerConfiguration">The manager configuration.</param>
        /// <param name="configuration">The cache handle configuration.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="serializer">The serializer.</param>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="configuration"/> or <paramref name="loggerFactory"/> is null.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// The cache value type is not serializable or if the enyim configuration section could not
        /// be initialized.
        /// </exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Cache gets disposed correctly when the owner gets disposed.")]
        public MemcachedCacheHandle(
            ICacheManagerConfiguration managerConfiguration,
            CacheHandleConfiguration configuration,
            ILoggerFactory loggerFactory,
            ICacheSerializer serializer)
            : this(configuration, managerConfiguration, loggerFactory)
        {
            try
            {
                NotNull(configuration, nameof(configuration));
                var sectionName = GetEnyimSectionName(configuration.Key);
                var section = GetSection(sectionName);

                Cache = new MemcachedClient(section);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new InvalidOperationException("Failed to initialize " + GetType().Name + ". " + ex.BareMessage, ex);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemcachedCacheHandle{TCacheValue}"/> class.
        /// </summary>
        /// <param name="managerConfiguration">The manager configuration.</param>
        /// <param name="configuration">The cache handle configuration.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="client">The <see cref="MemcachedClient"/> to use.</param>
        public MemcachedCacheHandle(ICacheManagerConfiguration managerConfiguration, CacheHandleConfiguration configuration, ILoggerFactory loggerFactory, ICacheSerializer serializer, MemcachedClient client)
            : this(configuration, managerConfiguration, loggerFactory)
        {
            // serializer gets ignored, just added to the ctor to satisfy the ctor finder in our custom DI to actually hit this ctor if the client is specified.
            NotNull(client, nameof(client));
            Cache = client;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemcachedCacheHandle{TCacheValue}"/> class.
        /// </summary>
        /// <param name="managerConfiguration">The manager configuration.</param>
        /// <param name="configuration">The cache handle configuration.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="clientConfiguration">The <see cref="MemcachedClientConfiguration"/> to use to create the <see cref="MemcachedClient"/>.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Cache gets disposed correctly when the owner gets disposed.")]
        public MemcachedCacheHandle(
            ICacheManagerConfiguration managerConfiguration,
            CacheHandleConfiguration configuration,
            ILoggerFactory loggerFactory,
            ICacheSerializer serializer,
            MemcachedClientConfiguration clientConfiguration)
            : this(configuration, managerConfiguration, loggerFactory)
        {
            NotNull(clientConfiguration, nameof(clientConfiguration));
            if (clientConfiguration.Transcoder.GetType() == typeof(DefaultTranscoder))
            {
                clientConfiguration.Transcoder = new CacheManagerTanscoder<TCacheValue>(serializer);

                // default is 10, that might be too long as it can take up to 10sec to recover during retries
                clientConfiguration.SocketPool.DeadTimeout = TimeSpan.FromSeconds(2);
            }

            Cache = new MemcachedClient(clientConfiguration);
        }

        private MemcachedCacheHandle(
                    CacheHandleConfiguration configuration,
                    ICacheManagerConfiguration managerConfiguration,
                    ILoggerFactory loggerFactory)
                    : base(managerConfiguration, configuration)
        {
            NotNull(configuration, nameof(configuration));
            NotNull(loggerFactory, nameof(loggerFactory));
            NotNull(managerConfiguration, nameof(managerConfiguration));

            _managerConfiguration = managerConfiguration;
            Logger = loggerFactory.CreateLogger(this);
        }

        /// <summary>
        /// Gets the number of items the cache handle currently maintains.
        /// </summary>
        /// <value>The count.</value>
        public override int Count => int.Parse(Cache.Stats().GetRaw("curr_items").First().Value);

        /// <summary>
        /// Gets the get memcached client configuration.
        /// </summary>
        /// <value>The get memcached client configuration.</value>
        public IMemcachedClientConfiguration GetMemcachedClientConfiguration => GetSection();

        /// <summary>
        /// Gets the total number of items per server.
        /// </summary>
        /// <value>The total number of items per server.</value>
        public IEnumerable<long> ServerItemCount
        {
            get
            {
                foreach (var count in Cache.Stats().GetRaw("total_items"))
                {
                    yield return long.Parse(count.Value, CultureInfo.InvariantCulture);
                }
            }
        }

        /// <summary>
        /// Gets the servers.
        /// </summary>
        /// <value>The servers.</value>
        public IList<IPEndPoint> Servers => GetServers();

        /// <summary>
        /// Gets the cache.
        /// </summary>
        /// <value>The cache.</value>
        public MemcachedClient Cache { get; }

        /// <inheritdoc />
        protected override ILogger Logger { get; }

        /// <summary>
        /// Clears this cache, removing all items in the base cache and all regions.
        /// </summary>
        public override void Clear() => Cache.FlushAll();

        /// <summary>
        /// Clears the cache region, removing all items from the specified <paramref name="region"/> only.
        /// </summary>
        /// <remarks>Not supported for memcached.</remarks>
        /// <param name="region">The cache region.</param>
        public override void ClearRegion(string region)
        {
            var regionPrefix = StoreNewRegionPrefix(region);

            if (Logger.IsEnabled(LogLevel.Debug))
            {
                Logger.LogDebug("Cleared region {0}, new region key is {1}.", region, regionPrefix);
            }
        }

        /// <inheritdoc />
        public override bool Exists(string key)
        {
            var result = Cache.ExecuteAppend(GetKey(key, null, false), default(ArraySegment<byte>));
            return result.StatusCode.HasValue && result.StatusCode.Value != (int)StatusCode.KeyNotFound;
        }

        /// <inheritdoc />
        public override bool Exists(string key, string region)
        {
            NotNullOrWhiteSpace(region, nameof(region));

            var result = Cache.ExecuteAppend(GetKey(key, region, false), default(ArraySegment<byte>));
            return result.StatusCode.HasValue && result.StatusCode.Value != (int)StatusCode.KeyNotFound;
        }

        /// <inheritdoc />
        public override UpdateItemResult<TCacheValue> Update(string key, Func<TCacheValue, TCacheValue> updateValue, int maxRetries) =>
            Update(key, null, updateValue, maxRetries);

        /// <inheritdoc />
        public override UpdateItemResult<TCacheValue> Update(string key, string region, Func<TCacheValue, TCacheValue> updateValue, int maxRetries) =>
            Set(key, region, updateValue, maxRetries);

        /// <summary>
        /// Adds a value to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        /// <returns>
        /// <c>true</c> if the key was not already added to the cache, <c>false</c> otherwise.
        /// </returns>
        protected override bool AddInternalPrepared(CacheItem<TCacheValue> item)
        {
            IOperationResult result;
            var shouldRetry = false;
            var tries = 0;
            do
            {
                tries++;
                result = Store(StoreMode.Add, item, out shouldRetry);
                if (!shouldRetry)
                {
                    return result.Success;
                }

                WaitRetry(tries);
            }
            while (shouldRetry && tries < _managerConfiguration.MaxRetries);

            throw new InvalidOperationException($"Add failed after {tries} tries for [{item.ToString()}]. {result.InnerResult?.Message ?? result.Message}");
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
                Cache.Dispose();
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
            var item = Cache.Get(GetKey(key, region)) as CacheItem<TCacheValue>;
            if (item != null)
            {
                if (item.IsExpired)
                {
                    return null;
                }
                else if (item.ExpirationMode == ExpirationMode.Sliding)
                {
                    // the only way I see to update sliding expiration for keys
                    // is to store them again with updated TTL... What a b...t
                    item.LastAccessedUtc = DateTime.UtcNow;
                    bool shouldRetry;
                    Store(StoreMode.Set, item, out shouldRetry);
                }
            }

            return item;
        }

        /// <summary>
        /// Puts the <paramref name="item"/> into the cache. If the item exists it will get updated
        /// with the new value. If the item doesn't exist, the item will be added to the cache.
        /// </summary>
        /// <param name="item">The <c>CacheItem</c> to be added to the cache.</param>
        protected override void PutInternalPrepared(CacheItem<TCacheValue> item)
        {
            IOperationResult result;
            var shouldRetry = false;
            var tries = 0;
            do
            {
                tries++;
                result = Store(StoreMode.Set, item, out shouldRetry);
                if (!shouldRetry)
                {
                    return;
                }

                WaitRetry(tries);
            }
            while (shouldRetry && tries < _managerConfiguration.MaxRetries);

            throw new InvalidOperationException($"Put failed after {tries} tries for [{item.ToString()}]. {result.InnerResult?.Message ?? result.Message}");
        }

        private void WaitRetry(int currentTry)
        {
            var delay = _managerConfiguration.RetryTimeout == 0 ? 10 : _managerConfiguration.RetryTimeout;

            var adjusted = delay * currentTry;
            if (adjusted > 10000)
            {
                adjusted = 10000;
            }

#if !NET40
            Task.Delay(adjusted).ConfigureAwait(false).GetAwaiter().GetResult();
#else
            Thread.Sleep(adjusted);
#endif
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
            var result = Cache.ExecuteRemove(GetKey(key, region, false));
            var statusCode = result.StatusCode ?? result.InnerResult?.StatusCode ?? -1;
            if (result.Success && statusCode != (int)StatusCode.KeyNotFound)
            {
                LogOperationResult(LogLevel.Debug, result, "Removed {0} {1}", region, key);
            }
            else
            {
                if (statusCode == (int)StatusCode.KeyNotFound)
                {
                    LogOperationResult(LogLevel.Information, result, "Remove Failed, key not found: {0} {1}", region, key);
                }
                else
                {
                    LogOperationResult(LogLevel.Error, result, "Remove Failed for {0} {1}", region, key);
                    //// throw new InvalidOperationException($"Remove failed for {region} {key}; statusCode:{statusCode}, message:{result.InnerResult?.Message ?? result.Message}");
                }
            }

            return result.Success;
        }

        /// <summary>
        /// Stores the item with the specified mode.
        /// </summary>
        /// <remarks>
        /// Memcached only supports expiration of seconds, meaning anything less than one second will mean zero, which means it expires right after adding it.
        /// </remarks>
        /// <param name="mode">The mode.</param>
        /// <param name="item">The item.</param>
        /// <param name="shouldRetry">Flag indicating if the operation should be retried. Returnd succssess code will be false anyways.</param>
        /// <returns>The result.</returns>
        protected virtual IStoreOperationResult Store(StoreMode mode, CacheItem<TCacheValue> item, out bool shouldRetry)
        {
            NotNull(item, nameof(item));
            shouldRetry = false;

            var key = GetKey(item.Key, item.Region);
            IStoreOperationResult result;

            if (item.ExpirationMode == ExpirationMode.Absolute || item.ExpirationMode == ExpirationMode.Sliding)
            {
                if (item.ExpirationTimeout > _maximumTimeout)
                {
                    throw new InvalidOperationException($"Timeout must not exceed {_maximumTimeout.TotalDays} days.");
                }

                result = Cache.ExecuteStore(mode, key, item, item.ExpirationTimeout);
            }
            else
            {
                result = Cache.ExecuteStore(mode, key, item);
            }

            if (mode == StoreMode.Add && result.StatusCode == (int?)StatusCode.KeyExists)
            {
                LogOperationResult(LogLevel.Information, result, "Failed to add item [{0}] because it exists.", item);
            }
            else if (result.Success)
            {
                LogOperationResult(LogLevel.Debug, result, "Stored [{0}]", item);
            }
            else
            {
                shouldRetry = true;
                LogOperationResult(LogLevel.Error, result, "Store failed for [{0}]", item);
            }

            return result;
        }

        private void LogOperationResult(LogLevel level, IOperationResult result, string message, params object[] args)
        {
            if (Logger.IsEnabled(level))
            {
                var msg = $"{string.Format(message, args)}; Result Success:'{result.Success}' Code:'{result.InnerResult?.StatusCode ?? result.StatusCode}' Message:'{result.InnerResult?.Message ?? result.Message}'.";
                Logger.Log(level, 0, msg, result.Exception);
            }
        }

        private string StoreNewRegionPrefix(string region)
        {
            var regionKey = ComputeRegionLookupKey(region);
            var oldRegionPredix = GetRegionPrefix(region);
            var tries = 0;
            var created = false;
            while (!created && tries <= _managerConfiguration.MaxRetries)
            {
                var timestamp = Clock.GetUnixTimestampMillis();
                if (timestamp.ToString() == oldRegionPredix)
                {
                    // we are too fast in creating new keys it seems, try again...
                    continue;
                }

                tries++;

                if (Logger.IsEnabled(LogLevel.Debug))
                {
                    Logger.LogDebug("Trying to store new region prefix '{0}', for region key '{1}'.", timestamp, regionKey);
                }

                created = Cache.Store(StoreMode.Set, regionKey, timestamp);

                if (created)
                {
                    if (Logger.IsEnabled(LogLevel.Debug))
                    {
                        Logger.LogDebug("Successfully stored new region prefix '{0}', for region key '{1}'.", timestamp, regionKey);
                    }

                    return timestamp.ToString();
                }
            }

            Logger.LogError("Failed to store prefix for region key '{0}'", regionKey);

            throw new InvalidOperationException($"Could not store new cache region prefix.");
        }

        private string GetRegionPrefix(string region)
        {
            var regionKey = ComputeRegionLookupKey(region);

            object val;
            if (Cache.ExecuteTryGet(regionKey, out val).Success)
            {
                return val?.ToString();
            }

            return null;
        }

        private static string ComputeRegionLookupKey(string region)
        {
            return GetPerhapsHashedKey("__" + region + "_rk__");
        }

        /// <summary>
        /// Gets the name of the enyim section.
        /// </summary>
        /// <param name="handleName">Name of the handle.</param>
        /// <returns>The section name.</returns>
        private static string GetEnyimSectionName(string handleName)
        {
            if (handleName.Equals(_defaultSectionName, StringComparison.OrdinalIgnoreCase))
            {
                return _defaultEnyimSectionName;
            }
            else
            {
                return handleName;
            }
        }

        private string GetKey(string key, string region = null, bool eventuallyCreateNewRegion = true)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            var fullKey = key;

            if (!string.IsNullOrWhiteSpace(region))
            {
                var regionKey = GetRegionPrefix(region);
                if (regionKey == null)
                {
                    if (eventuallyCreateNewRegion)
                    {
                        if (Logger.IsEnabled(LogLevel.Debug))
                        {
                            Logger.LogDebug("Region key for region '{0}' not present, creating a new one...", region);
                        }

                        regionKey = StoreNewRegionPrefix(region);
                    }
                    else
                    {
                        // create a fake one (e.g. on Remove key, we don't have to create a region if it doesn't exist at all)
                        regionKey = (Clock.GetUnixTimestampMillis() * -1).ToString();
                    }
                }

                fullKey = string.Concat(regionKey, ":", key);
            }

            return GetPerhapsHashedKey(fullKey);
        }

        private static string GetPerhapsHashedKey(string key)
        {
            // Memcached still has a 250 character limit
            if (key.Length >= 250)
            {
                return GetSHA256Key(key);
            }

            return key;
        }

        // TODO: test the config section ctor now with this
        private static IMemcachedClientConfiguration GetSection(string sectionName)
        {
            var section = (MemcachedClientSection)ConfigurationManager.GetSection(sectionName);
            if (section == null)
            {
                throw new ConfigurationErrorsException("Section " + sectionName + " is not found.");
            }

            return section;
        }

        private static string GetSHA256Key(string key)
        {
            using (var sha = SHA256Managed.Create())
            {
                var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
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
            var sectionName = GetEnyimSectionName(Configuration.Name);
            var section = (MemcachedClientSection)ConfigurationManager.GetSection(sectionName);

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
            var section = GetSection();
            return section.Servers;
        }

        private UpdateItemResult<TCacheValue> Set(string key, string region, Func<TCacheValue, TCacheValue> updateValue, int maxRetries)
        {
            var fullyKey = GetKey(key, region);
            var tries = 0;
            IStoreOperationResult result;

            do
            {
                tries++;
                var getTries = 0;
                StatusCode getStatus;
                IGetOperationResult<CacheItem<TCacheValue>> getResult;
                CacheItem<TCacheValue> item;
                do
                {
                    getTries++;
                    getResult = Cache.ExecuteGet<CacheItem<TCacheValue>>(fullyKey);

                    item = getResult.Value;
                    getStatus = (StatusCode)(getResult.StatusCode ?? getResult.InnerResult?.StatusCode ?? -1);
                }
                while (ShouldRetry(getStatus) && getTries <= maxRetries);

                // break operation if we cannot retrieve the object (maybe it has expired already).
                if (!getResult.Success || item == null)
                {
                    LogOperationResult(LogLevel.Warning, getResult, "Get item during update failed for '{0}'.", fullyKey);
                    return UpdateItemResult.ForItemDidNotExist<TCacheValue>();
                }

                if (item.IsExpired)
                {
                    return UpdateItemResult.ForItemDidNotExist<TCacheValue>();
                }

                var newValue = updateValue(item.Value);

                // added null check, throw explicit to me more consistent. Otherwise it would throw later eventually
                if (newValue == null)
                {
                    return UpdateItemResult.ForFactoryReturnedNull<TCacheValue>();
                }

                item = item.WithValue(newValue);
                item.LastAccessedUtc = DateTime.UtcNow;

                if (item.ExpirationMode == ExpirationMode.Absolute || item.ExpirationMode == ExpirationMode.Sliding)
                {
                    if (item.ExpirationTimeout > _maximumTimeout)
                    {
                        throw new InvalidOperationException($"Timeout must not exceed {_maximumTimeout.TotalDays} days.");
                    }

                    result = Cache.ExecuteCas(StoreMode.Set, fullyKey, item, item.ExpirationTimeout, getResult.Cas);
                }
                else
                {
                    result = Cache.ExecuteCas(StoreMode.Set, fullyKey, item, getResult.Cas);
                }

                if (result.Success)
                {
                    return UpdateItemResult.ForSuccess(item, tries > 1, tries);
                }
                else
                {
                    LogOperationResult(LogLevel.Warning, result, "Update failed for '{0}'.", fullyKey);
                }

                WaitRetry(tries);
            }
            while (tries < maxRetries);

            return UpdateItemResult.ForTooManyRetries<TCacheValue>(tries);
        }

        private class CacheManagerTanscoder<T> : DefaultTranscoder
        {
            private readonly ICacheSerializer _serializer;

            public CacheManagerTanscoder(ICacheSerializer serializer)
            {
                NotNull(serializer, nameof(serializer));
                _serializer = serializer;
            }

            protected override object DeserializeObject(ArraySegment<byte> value)
            {
                var position = value.Offset;
                var typeNameLen = BitConverter.ToUInt16(value.Array, position);
                position += 2;

                var typeName = Encoding.UTF8.GetString(value.Array, position, typeNameLen);
                position += typeNameLen;
                if (value.Array[position++] != 0)
                {
                    throw new InvalidOperationException("Invalid data, stop bit not found in type name encoding.");
                }

                var data = new byte[value.Count - position + value.Offset];
                Buffer.BlockCopy(value.Array, position, data, 0, data.Length);
                return _serializer.DeserializeCacheItem<T>(data, TypeCache.GetType(typeName));
            }

            protected override ArraySegment<byte> SerializeObject(object value)
            {
                var cacheItem = value as CacheItem<T>;
                if (cacheItem == null)
                {
                    throw new ArgumentException($"Value is not {nameof(CacheItem<T>)}.", nameof(value));
                }

                var typeName = cacheItem.Value.GetType().AssemblyQualifiedName;
                var typeNameBytes = Encoding.UTF8.GetBytes(typeName);
                var typeBytesLength = BitConverter.GetBytes((ushort)typeNameBytes.Length);
                var data = _serializer.SerializeCacheItem(cacheItem);

                var result = new byte[typeNameBytes.Length + typeBytesLength.Length + data.Length + 1];

                /* Encoding the actual item value Type into the cached item
                 *
                 * | 0 - 1 | 2 - len | len + 1 | ...
                 * |  len  |TypeName |0 - stop | data
                 */
                Buffer.BlockCopy(typeBytesLength, 0, result, 0, typeBytesLength.Length);
                Buffer.BlockCopy(typeNameBytes, 0, result, typeBytesLength.Length, typeNameBytes.Length);
                Buffer.BlockCopy(data, 0, result, typeBytesLength.Length + typeNameBytes.Length + 1, data.Length);

                return new ArraySegment<byte>(result);
            }
        }
    }
}