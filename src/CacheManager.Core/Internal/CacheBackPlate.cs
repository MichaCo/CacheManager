using System;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core.Internal
{
    /// <summary>
    /// In CacheManager, a cache back plate is used to keep in process and distributed caches in
    /// sync. <br/> If the cache manager runs inside multiple nodes or applications accessing the
    /// same distributed cache, and an in process cache is configured to be in front of the
    /// distributed cache handle. All Get calls will hit the in process cache. <br/> Now when an
    /// item gets removed for example by one client, all other clients still have that cache item
    /// available in the in process cache. <br/> This could lead to errors and unexpected behavior,
    /// therefore a cache back plate will send a message to all other cache clients to also remove
    /// that item.
    /// <para>
    /// The same mechanism will apply to any Update, Put, Remove, Clear or ClearRegion call of the cache.
    /// </para>
    /// </summary>
    public abstract class CacheBackPlate : IDisposable
    {
        private Action<string> onChangeKey;
        private Action<string, string> onChangeKeyRegion;
        private Action onClear;
        private Action<string> onClearRegion;
        private Action<string> onRemoveKey;
        private Action<string, string> onRemoveKeyRegion;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheBackPlate" /> class.
        /// </summary>
        /// <param name="configuration">The cache manager configuration.</param>
        /// <exception cref="System.ArgumentNullException">If configuration is null.</exception>
        protected CacheBackPlate(CacheManagerConfiguration configuration)
        {
            NotNull(configuration, nameof(configuration));
            this.CacheConfiguration = configuration;
            this.ConfigurationKey = configuration.BackPlateConfigurationKey;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="CacheBackPlate"/> class.
        /// </summary>
        ~CacheBackPlate()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Gets the cache configuration.
        /// </summary>
        /// <value>
        /// The cache configuration.
        /// </value>
        public CacheManagerConfiguration CacheConfiguration { get; }

        /// <summary>
        /// Gets the name of the configuration to be used.
        /// <para>The key might be used to find cache vendor specific configuration.</para>
        /// </summary>
        /// <value>The configuration key.</value>
        public string ConfigurationKey { get; }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting
        /// unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Notifies other cache clients about a changed cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        public abstract void NotifyChange(string key);

        /// <summary>
        /// Notifies other cache clients about a changed cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        public abstract void NotifyChange(string key, string region);

        /// <summary>
        /// Notifies other cache clients about a cache clear.
        /// </summary>
        public abstract void NotifyClear();

        /// <summary>
        /// Notifies other cache clients about a cache clear region call.
        /// </summary>
        /// <param name="region">The region.</param>
        public abstract void NotifyClearRegion(string region);

        /// <summary>
        /// Notifies other cache clients about a removed cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        public abstract void NotifyRemove(string key);

        /// <summary>
        /// Notifies other cache clients about a removed cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        public abstract void NotifyRemove(string key, string region);

        public event EventHandler<CacheItemEventArgs> Changed;

        public event EventHandler<EventArgs> Cleared;

        public event EventHandler<RegionEventArgs> ClearedRegion;
        
        public event EventHandler<CacheItemEventArgs> Removed;

        protected void TriggerChanged(string key)
        {
            this.Changed?.Invoke(this, new CacheItemEventArgs(key));
        }

        protected void TriggerChanged(string key, string region)
        {
            this.Changed?.Invoke(this, new CacheItemEventArgs(key, region));
        }

        protected void TriggerCleared()
        {
            this.Cleared?.Invoke(this, new EventArgs());
        }

        protected void TriggerClearedRegion(string region)
        {
            this.ClearedRegion?.Invoke(this, new RegionEventArgs(region));
        }

        protected void TriggerRemoved(string key)
        {
            this.Removed?.Invoke(this, new CacheItemEventArgs(key));
        }

        protected void TriggerRemoved(string key, string region)
        {
            this.Removed?.Invoke(this, new CacheItemEventArgs(key, region));
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="managed">
        /// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release
        /// only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool managed)
        {
        }
    }

    public class RegionEventArgs : EventArgs
    {
        public RegionEventArgs(string region)
        {
            NotNull(region, nameof(region));
            this.Region = region;
        }

        public string Region { get; }
    }

    public class CacheItemEventArgs : EventArgs
    {
        public CacheItemEventArgs(string key)
        {
            NotNull(key, nameof(key));
            this.Key = key;
        }

        public CacheItemEventArgs(string key, string region)
            : this(key)
        {
            NotNull(region, nameof(region));
            this.Region = region;
        }

        public string Key { get; }

        public string Region { get; }
    }
}