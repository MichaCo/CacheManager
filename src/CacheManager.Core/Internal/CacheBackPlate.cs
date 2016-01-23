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

        /// <summary>
        /// Called when another client changed a cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <exception cref="System.ArgumentNullException">If key is null.</exception>
        public void OnChange(string key)
        {
            NotNullOrWhiteSpace(key, nameof(key));

            this.onChangeKey(key);
        }

        /// <summary>
        /// Called when another client changed a cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        /// <exception cref="System.ArgumentNullException">If key or region are null.</exception>
        public void OnChange(string key, string region)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNullOrWhiteSpace(region, nameof(region));

            this.onChangeKeyRegion(key, region);
        }

        /// <summary>
        /// Called when another client cleared the cache.
        /// </summary>
        public void OnClear() => this.onClear();

        /// <summary>
        /// Called when another client cleared a region.
        /// </summary>
        /// <param name="region">The region.</param>
        /// <exception cref="System.ArgumentNullException">If region is null.</exception>
        public void OnClearRegion(string region)
        {
            NotNullOrWhiteSpace(region, nameof(region));

            this.onClearRegion(region);
        }

        /// <summary>
        /// Called when another client removed a cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <exception cref="System.ArgumentNullException">If key is null.</exception>
        public void OnRemove(string key)
        {
            NotNullOrWhiteSpace(key, nameof(key));

            this.onRemoveKey(key);
        }

        /// <summary>
        /// Called when another client removed a cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        /// <exception cref="System.ArgumentNullException">If key or region are null.</exception>
        public void OnRemove(string key, string region)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNullOrWhiteSpace(region, nameof(region));

            this.onRemoveKeyRegion(key, region);
        }

        /// <summary>
        /// The cache manager will subscribe to the back plate to get triggered whenever another
        /// client changed a cache key.
        /// </summary>
        /// <param name="change">The change.</param>
        /// <exception cref="System.ArgumentNullException">Id change is null.</exception>
        internal void SubscribeChanged(Action<string> change)
        {
            NotNull(change, nameof(change));

            this.onChangeKey = change;
        }

        /// <summary>
        /// The cache manager will subscribe to the back plate to get triggered whenever another
        /// client changed a cache key.
        /// </summary>
        /// <param name="change">The change.</param>
        /// <exception cref="System.ArgumentNullException">If change is null.</exception>
        internal void SubscribeChanged(Action<string, string> change)
        {
            NotNull(change, nameof(change));

            this.onChangeKeyRegion = change;
        }

        /// <summary>
        /// Subscribes the clear.The cache manager will subscribe to the back plate to get triggered
        /// whenever another client cleared the cache.
        /// </summary>
        /// <param name="clear">The clear.</param>
        /// <exception cref="System.ArgumentNullException">If clear is null.</exception>
        internal void SubscribeClear(Action clear)
        {
            NotNull(clear, nameof(clear));

            this.onClear = clear;
        }

        /// <summary>
        /// Subscribes the clear region.The cache manager will subscribe to the back plate to get
        /// triggered whenever another client cleared a region.
        /// </summary>
        /// <param name="clearRegion">The clear region.</param>
        /// <exception cref="System.ArgumentNullException">If clearRegion is null.</exception>
        internal void SubscribeClearRegion(Action<string> clearRegion)
        {
            NotNull(clearRegion, nameof(clearRegion));

            this.onClearRegion = clearRegion;
        }

        /// <summary>
        /// The cache manager will subscribe to the back plate to get triggered whenever another
        /// client removed a cache item.
        /// </summary>
        /// <param name="remove">The remove.</param>
        /// <exception cref="System.ArgumentNullException">If remove is null.</exception>
        internal void SubscribeRemove(Action<string> remove)
        {
            NotNull(remove, nameof(remove));

            this.onRemoveKey = remove;
        }

        /// <summary>
        /// The cache manager will subscribe to the back plate to get triggered whenever another
        /// client removed a cache item.
        /// </summary>
        /// <param name="remove">The remove.</param>
        /// <exception cref="System.ArgumentNullException">If remove is null.</exception>
        internal void SubscribeRemove(Action<string, string> remove)
        {
            NotNull(remove, nameof(remove));

            this.onRemoveKeyRegion = remove;
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
}