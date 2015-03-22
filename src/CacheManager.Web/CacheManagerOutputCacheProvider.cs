using System;
using System.Collections.Specialized;
using System.Reflection;
using System.Web.Caching;
using CacheManager.Core;
using CacheManager.Core.Configuration;

namespace CacheManager.Web
{
    public class CacheManagerOutputCacheProvider : OutputCacheProvider
    {
        private static readonly object configLock = new object();
        private static bool isInitialized = false;
        private static ICacheManager<object> cacheInstance;

        public static ICacheManager<object> Cache
        {
            get
            {
                if (!isInitialized)
                {
                    throw new InvalidOperationException("Output cache provider has not yet been initialized.");
                }

                return cacheInstance;
            }
        }

        public override object Add(string key, object entry, DateTime utcExpiry)
        {
            if (!cacheInstance.Add(GetCacheItem(key, entry, utcExpiry)))
            {
                return Cache.Get(key);
            }

            return null;
        }

        public override object Get(string key)
        {
            return Cache.Get(key);
        }

        public override void Remove(string key)
        {
            Cache.Remove(key);
        }

        public override void Set(string key, object entry, DateTime utcExpiry)
        {
            Cache.Put(GetCacheItem(key, entry, utcExpiry));
        }

        private static CacheItem<object> GetCacheItem(string key, object entry, DateTime utcExpiry)
        {
            CacheItem<object> newItem;
            if (utcExpiry != null && utcExpiry != DateTime.MaxValue)
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

        public override void Initialize(string name, NameValueCollection config)
        {
            try
            {
                if (!isInitialized)
                {
                    lock (configLock)
                    {
                        if (!isInitialized)
                        {
                            var cacheName = config["cacheName"];
                            if (string.IsNullOrWhiteSpace(cacheName))
                            {
                                cacheName = "default";
                            }

                            InitializeStaticCache(cacheName);
                            isInitialized = true;
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

        private static void InitializeStaticCache(string cacheName)
        {
            cacheInstance = CacheFactory.FromConfiguration<object>(cacheName);
        }
    }
}