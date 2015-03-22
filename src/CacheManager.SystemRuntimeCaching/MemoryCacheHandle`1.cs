using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Runtime.Caching;
using System.Text.RegularExpressions;
using CacheManager.Core;
using CacheManager.Core.Cache;
using CacheManager.Core.Configuration;

namespace CacheManager.SystemRuntimeCaching
{
    /// <summary>
    /// Simple implementation for the <see cref="System.Runtime.Caching.MemoryCache"/>.
    ///
    /// </summary>
    /// <remarks>
    /// Although the MemoryCache doesn't support regions nor a RemoveAll/Clear method, we will implement it
    /// via cache dependencies.
    /// </remarks>
    /// <typeparam name="TCacheValue"></typeparam>
    public class MemoryCacheHandle<TCacheValue> : BaseCacheHandle<TCacheValue>
    {
        private const string DefaultName = "default";

        private volatile MemoryCache cache = null;

        // can be default or any other name
        private readonly string cacheName = string.Empty;

        private string instanceKey = null;

        public MemoryCacheHandle(ICacheManager<TCacheValue> manager, ICacheHandleConfiguration configuration)
            : base(manager, configuration)
        {
            cacheName = this.Configuration.HandleName;

            if (cacheName.ToUpper(CultureInfo.InvariantCulture).Equals(DefaultName.ToUpper(CultureInfo.InvariantCulture)))
            {
                this.cache = System.Runtime.Caching.MemoryCache.Default;
            }
            else
            {
                this.cache = new System.Runtime.Caching.MemoryCache(cacheName);
            }

            instanceKey = Guid.NewGuid().ToString();

            CreateInstanceToken();
        }

        public override void Clear()
        {
            this.cache.Remove(this.instanceKey);
            CreateInstanceToken();
        }

        public override void ClearRegion(string region)
        {
            cache.Remove(GetRegionTokenKey(region));
        }

        public override int Count
        {
            get { return (int)this.cache.GetCount(); }
        }

        protected override bool AddInternalPrepared(CacheItem<TCacheValue> item)
        {
            var key = GetItemKey(item);

            if (this.cache.Contains(key))
            {
                return false;
            }

            CacheItemPolicy policy = GetPolicy(item);
            return cache.Add(key, item, policy);
        }

        protected override void PutInternalPrepared(CacheItem<TCacheValue> item)
        {
            var key = GetItemKey(item);
            CacheItemPolicy policy = GetPolicy(item);
            cache.Set(key, item, policy);
        }

        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key)
        {
            return GetCacheItemInternal(key, null);
        }

        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key, string region)
        {
            string fullKey = GetItemKey(key, region);
            var item = cache.Get(fullKey) as CacheItem<TCacheValue>;

            if (item == null)
            {
                return null;
            }

            // maybe the item is already expired
            // because MemoryCache implements a default interval of 20 seconds! to check for
            // expired items on each store, we do it on access to also reflect smaller time frames
            // especially for sliding expiration...
            if (IsExpired(item))
            {
                // if so remove it
                this.RemoveInternal(item.Key, item.Region);
                return null;
            }

            if (item.ExpirationMode == ExpirationMode.Sliding)
            {
                // because we don't use UpdateCallback because of some multithreading issues
                // lets try to simply reset the item by setting it again.
                this.GetItemExpiration(item);
                cache.Set(fullKey, item, GetPolicy(item));
            }

            return item;
        }

        protected override bool RemoveInternal(string key)
        {
            return RemoveInternal(key, null);
        }

        protected override bool RemoveInternal(string key, string region)
        {
            var fullKey = this.GetItemKey(key, region);
            var obj = cache.Remove(fullKey);
            
            return obj != null;
        }
        
        public NameValueCollection CacheSettings
        {
            get
            {
                return GetSettings(this.cache);
            }
        }

        private CacheItemPolicy GetPolicy(CacheItem<TCacheValue> item)
        {
            var monitorKeys = new[] { instanceKey };

            if (!string.IsNullOrWhiteSpace(item.Region))
            {
                // this should be the only place to create the region token if it doesn't exist
                // it might got removed by clearRegion but next time put or add gets called, the region should be re added...
                var key = GetRegionTokenKey(item.Region);
                if (!this.cache.Contains(key))
                {
                    CreateRegionToken(item.Region);
                }
                monitorKeys = new[] { instanceKey, key };
            }

            var policy = new CacheItemPolicy()
            {
                Priority = CacheItemPriority.Default,
                ChangeMonitors = { cache.CreateCacheEntryChangeMonitor(monitorKeys) },
                AbsoluteExpiration = System.Runtime.Caching.ObjectCache.InfiniteAbsoluteExpiration,
                SlidingExpiration = System.Runtime.Caching.ObjectCache.NoSlidingExpiration,
            };

            if (item.ExpirationMode == ExpirationMode.Absolute)
            {
                policy.AbsoluteExpiration = new DateTimeOffset(DateTime.UtcNow.Add(item.ExpirationTimeout));
                policy.RemovedCallback = new CacheEntryRemovedCallback(ItemRemoved);
            }

            if (item.ExpirationMode == ExpirationMode.Sliding)
            {
                policy.SlidingExpiration = item.ExpirationTimeout;
                policy.RemovedCallback = new CacheEntryRemovedCallback(ItemRemoved);
                // for some reason, we'll get issues with multithreading if we set this...
                //policy.UpdateCallback = new CacheEntryUpdateCallback(ItemUpdated); // must be set, otherwise sliding doesn't work at all.
            }

            item.LastAccessedUtc = DateTime.UtcNow;

            return policy;
        }

        private void ItemRemoved(CacheEntryRemovedArguments arguments)
        {
            var key = arguments.CacheItem.Key;
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            // only handle auto removes
            if (arguments.RemovedReason != CacheEntryRemovedReason.Expired)
            {
                return;
            }

            // identify item keys and ignore region or instance key
            if (key.Contains(":"))
            {
                if (key.Contains("@"))
                {
                    // example instanceKey@region:itemkey , instanceKey:itemKey
                    var region = Regex.Match(key, "@(.+?):").Groups[1].Value;
                    this.Stats.OnRemove(region);
                }
                else
                {
                    this.Stats.OnRemove();
                }
            }
        }

        private string GetItemKey(CacheItem<TCacheValue> item)
        {
            return GetItemKey(item.Key, item.Region);
        }

        private string GetItemKey(string key, string region = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }

            if (string.IsNullOrWhiteSpace(region))
            {
                return instanceKey + ":" + key;
            }

            key = key.Replace("@", "!!").Replace(":", "!!");
            return string.Concat(instanceKey, "@", region, ":", key);
        }

        private void CreateInstanceToken()
        {
            // don't add a new key while we are disposing our instance
            if (!this.Disposing)
            {
                var instanceItem = new CacheItem<string>(instanceKey, instanceKey);
                CacheItemPolicy policy = new CacheItemPolicy()
                {
                    Priority = CacheItemPriority.Default,
                    RemovedCallback = new CacheEntryRemovedCallback(InstanceTokenRemoved),
                    AbsoluteExpiration = System.Runtime.Caching.ObjectCache.InfiniteAbsoluteExpiration,
                    SlidingExpiration = System.Runtime.Caching.ObjectCache.NoSlidingExpiration,
                };

                this.cache.Add(instanceItem.Key, instanceItem, policy);
            }
        }

        private void InstanceTokenRemoved(CacheEntryRemovedArguments arguments)
        {
            instanceKey = Guid.NewGuid().ToString();
        }

        private void CreateRegionToken(string region)
        {
            var key = GetRegionTokenKey(region);
            // add region token with dependency on our instance token, so that all regions get removed
            // whenever the instance gets cleared.
            CacheItemPolicy policy = new CacheItemPolicy()
            {
                Priority = CacheItemPriority.Default,
                AbsoluteExpiration = System.Runtime.Caching.ObjectCache.InfiniteAbsoluteExpiration,
                SlidingExpiration = System.Runtime.Caching.ObjectCache.NoSlidingExpiration,
                ChangeMonitors = { cache.CreateCacheEntryChangeMonitor(new[] { instanceKey }) },
            };
            this.cache.Add(key, region, policy);
        }

        private string GetRegionTokenKey(string region)
        {
            var key = string.Concat(this.instanceKey, "@", region);
            return key;
        }

        private static NameValueCollection GetSettings(MemoryCache instance)
        {
            var cacheCfg = new NameValueCollection();

            cacheCfg.Add("CacheMemoryLimitMegabytes", (instance.CacheMemoryLimit / 1024 / 1024).ToString(CultureInfo.InvariantCulture));
            cacheCfg.Add("PhysicalMemoryLimitPercentage", (instance.PhysicalMemoryLimit).ToString(CultureInfo.InvariantCulture));
            cacheCfg.Add("PollingInterval", (instance.PollingInterval).ToString());

            return cacheCfg;
        }

        private static bool IsExpired(CacheItem<TCacheValue> item)
        {
            var now = DateTime.UtcNow;
            if (item.ExpirationMode == ExpirationMode.Absolute
                && item.CreatedUtc.Add(item.ExpirationTimeout) < now)
            {
                return true;
            }
            else if (item.ExpirationMode == ExpirationMode.Sliding
                && item.LastAccessedUtc.Add(item.ExpirationTimeout) < now)
            {
                return true;
            }

            return false;
        }
    }
}