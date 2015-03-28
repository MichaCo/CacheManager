using System;
using CacheManager.Core.Configuration;

namespace CacheManager.Core.Cache
{
    public abstract class BaseCacheHandle<TCacheValue> : BaseCache<TCacheValue>, ICacheHandle<TCacheValue>
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        protected BaseCacheHandle(ICacheManager<TCacheValue> manager, ICacheHandleConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }

            if (string.IsNullOrWhiteSpace(configuration.HandleName))
            {
                throw new ArgumentException("Configuration name cannot be empty.");
            }

            this.Configuration = configuration;

            this.Manager = manager;

            this.Stats = new CacheStats<TCacheValue>(
                this.Configuration.CacheName,
                this.Configuration.HandleName,
                this.Configuration.EnableStatistics, 
                this.Configuration.EnablePerformanceCounters);
        }

        public abstract int Count { get; }

        public ICacheHandleConfiguration Configuration { get; private set; }

        public ICacheManager<TCacheValue> Manager { get; private set; }

        public CacheStats<TCacheValue> Stats { get; private set; }

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                Stats.Dispose();
            }

            base.Dispose(disposeManaged);
        }

        protected internal override bool AddInternal(CacheItem<TCacheValue> item)
        {
            GetItemExpiration(item);
            return this.AddInternalPrepared(item);
        }

        protected abstract bool AddInternalPrepared(CacheItem<TCacheValue> item);

        protected internal override void PutInternal(CacheItem<TCacheValue> item)
        {
            GetItemExpiration(item);
            this.PutInternalPrepared(item);
        }

        protected abstract void PutInternalPrepared(CacheItem<TCacheValue> item);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#")]
        protected void GetItemExpiration(CacheItem<TCacheValue> item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            // logic should be that the item setting overrules the handle setting
            // if the item doesn't define a mode (value is None) it should use the handle's setting.
            // if the handle also doesn't define a mode (value is None), we use None.
            var expirationMode = ExpirationMode.None;
            var expirationTimeout = TimeSpan.Zero;

            if (item.ExpirationMode != ExpirationMode.None || this.Configuration.ExpirationMode != ExpirationMode.None)
            {
                expirationMode = item.ExpirationMode != ExpirationMode.None ? item.ExpirationMode : this.Configuration.ExpirationMode;

                // if a mode is defined, the item or the fallback (handle config) must have a timeout defined.
                // ToDo: this check is pretty late, but the user can configure the CacheItem explicitly, so we have to catch it at this point.
                if (item.ExpirationTimeout == TimeSpan.Zero && this.Configuration.ExpirationTimeout == TimeSpan.Zero)
                {
                    throw new InvalidOperationException("Expiration mode defined without timeout.");
                }

                expirationTimeout = item.ExpirationTimeout != TimeSpan.Zero ? item.ExpirationTimeout : this.Configuration.ExpirationTimeout;
            }

            // Fix issue 2: updating the item exp timeout and mode:
            item.ExpirationMode = expirationMode;
            item.ExpirationTimeout = expirationTimeout;
        }

        public virtual UpdateItemResult Update(string key, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config)
        {
            if (updateValue == null)
            {
                throw new ArgumentNullException("updateValue");
            }

            var original = this.Get(key);
            if (original == null)
            {
                return new UpdateItemResult(false, false, 1);
            }

            var value = updateValue(original);
            this.Put(key, value);
            return new UpdateItemResult(false, true, 1);
        }

        public virtual UpdateItemResult Update(string key, string region, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config)
        {
            if (updateValue == null)
            {
                throw new ArgumentNullException("updateValue");
            }
            var original = this.Get(key, region);
            if (original == null)
            {
                return new UpdateItemResult(false, false, 1);
            }

            var value = updateValue(original);
            this.Put(key, value, region);
            return new UpdateItemResult(false, true, 1);
        }
    }
}