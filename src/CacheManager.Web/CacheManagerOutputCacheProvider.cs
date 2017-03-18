using System;
using System.Collections.Specialized;
using System.Reflection;
using System.Web.Caching;
using CacheManager.Core;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Web
{
    /// <summary>
    /// Implements a simple System.Web.Caching.OutputCacheProvider which uses a cache manager
    /// configured via web.config.
    /// </summary>
    public class CacheManagerOutputCacheProvider : OutputCacheProvider
    {
        private static readonly object _configLock = new object();
        private static ICacheManager<object> _cacheInstance;
        private static bool _isInitialized = false;

        /// <summary>
        /// Gets the cache.
        /// </summary>
        /// <value>The cache.</value>
        /// <exception cref="System.InvalidOperationException">
        /// Output cache provider has not yet been initialized.
        /// </exception>
        public static ICacheManager<object> Cache
        {
            get
            {
                Ensure(_isInitialized, "Output cache provider has not yet been initialized.");

                return _cacheInstance;
            }
        }

        /// <summary>
        /// Inserts the specified entry into the output cache.
        /// </summary>
        /// <param name="key">A unique identifier for <paramref name="entry"/>.</param>
        /// <param name="entry">The content to add to the output cache.</param>
        /// <param name="utcExpiry">The time and date on which the cached entry expires.</param>
        /// <returns>A reference to the specified provider.</returns>
        public override object Add(string key, object entry, DateTime utcExpiry)
        {
            if (!_cacheInstance.Add(GetCacheItem(key, entry, utcExpiry)))
            {
                return Cache.Get(key);
            }

            return null;
        }

        /// <summary>
        /// Returns a reference to the specified entry in the output cache.
        /// </summary>
        /// <param name="key">A unique identifier for a cached entry in the output cache.</param>
        /// <returns>
        /// The <paramref name="key"/> value that identifies the specified entry in the cache, or
        /// null if the specified entry is not in the cache.
        /// </returns>
        public override object Get(string key) => Cache.Get(key);

        /// <summary>
        /// Initializes the provider.
        /// </summary>
        /// <param name="name">The friendly name of the provider.</param>
        /// <param name="config">
        /// A collection of the name/value pairs representing the provider-specific attributes
        /// specified in the configuration for this provider.
        /// </param>
        /// <exception cref="System.InvalidOperationException">Might be re thrown.</exception>
        public override void Initialize(string name, NameValueCollection config)
        {
            NotNull(config, nameof(config));

            try
            {
                if (!_isInitialized)
                {
                    lock (_configLock)
                    {
                        if (!_isInitialized)
                        {
                            var cacheName = config["cacheName"];
                            if (string.IsNullOrWhiteSpace(cacheName))
                            {
                                cacheName = "default";
                            }

                            InitializeStaticCache(cacheName);
                            _isInitialized = true;
                        }
                    }
                }

                base.Initialize(name, config);
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException != null)
                {
                    throw new InvalidOperationException(ex.InnerException.Message, ex.InnerException);
                }

                throw;
            }
        }

        /// <summary>
        /// Removes the specified entry from the output cache.
        /// </summary>
        /// <param name="key">The unique identifier for the entry to remove from the output cache.</param>
        public override void Remove(string key)
        {
            Cache.Remove(key);
        }

        /// <summary>
        /// Inserts the specified entry into the output cache, overwriting the entry if it is
        /// already cached.
        /// </summary>
        /// <param name="key">A unique identifier for <paramref name="entry"/>.</param>
        /// <param name="entry">The content to add to the output cache.</param>
        /// <param name="utcExpiry">
        /// The time and date on which the cached <paramref name="entry"/> expires.
        /// </param>
        public override void Set(string key, object entry, DateTime utcExpiry)
        {
            Cache.Put(GetCacheItem(key, entry, utcExpiry));
        }

        private static CacheItem<object> GetCacheItem(string key, object entry, DateTime utcExpiry)
        {
            CacheItem<object> newItem;
            if (utcExpiry != default(DateTime) && utcExpiry != DateTime.MaxValue)
            {
                var timeout = TimeSpan.FromTicks(utcExpiry.Ticks - DateTime.UtcNow.Ticks);
                newItem = new CacheItem<object>(key, entry, ExpirationMode.Absolute, timeout);
            }
            else
            {
                newItem = new CacheItem<object>(key, entry);
            }

            return newItem;
        }

        private static void InitializeStaticCache(string cacheName)
        {
            _cacheInstance = CacheFactory.FromConfiguration<object>(cacheName);
        }
    }
}