namespace CacheManager.Core.Cache
{
    using System;
    using CacheManager.Core.Configuration;

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
    public abstract class CacheBackPlate : ICacheBackPlate
    {
        private Action<string> onChangeKey;
        private Action<string, string> onChangeKeyRegion;
        private Action onClear;
        private Action<string> onClearRegion;
        private Action<string> onRemoveKey;
        private Action<string, string> onRemoveKeyRegion;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheBackPlate"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="configuration">The configuration.</param>
        public CacheBackPlate(string name, ICacheManagerConfiguration configuration)
        {
            this.Name = name;
            this.Configuration = configuration;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="CacheBackPlate"/> class.
        /// </summary>
        ~CacheBackPlate()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>The configuration.</value>
        public ICacheManagerConfiguration Configuration { get; private set; }

        /// <summary>
        /// Gets the name.
        /// <para>The name might be used to find cache vendor specific configuration.</para>
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; private set; }

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
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="managed">
        /// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release
        /// only unmanaged resources.
        /// </param>
        public virtual void Dispose(bool managed)
        {
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
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }

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
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }
            if (string.IsNullOrWhiteSpace(region))
            {
                throw new ArgumentNullException("region");
            }

            this.onChangeKeyRegion(key, region);
        }

        /// <summary>
        /// Called when another client cleared the cache.
        /// </summary>
        public void OnClear()
        {
            this.onClear();
        }

        /// <summary>
        /// Called when another client cleared a region.
        /// </summary>
        /// <param name="region">The region.</param>
        /// <exception cref="System.ArgumentNullException">If region is null.</exception>
        public void OnClearRegion(string region)
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                throw new ArgumentNullException("region");
            }

            this.onClearRegion(region);
        }

        /// <summary>
        /// Called when another client removed a cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <exception cref="System.ArgumentNullException">If key is null.</exception>
        public void OnRemove(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }

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
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }
            if (string.IsNullOrWhiteSpace(region))
            {
                throw new ArgumentNullException("region");
            }

            this.onRemoveKeyRegion(key, region);
        }

        /// <summary>
        /// The cache manager will subscribe to the back plate to get triggered whenever another
        /// client changed a cache key.
        /// </summary>
        /// <param name="change">The change.</param>
        /// <exception cref="System.ArgumentNullException">Id change is null.</exception>
        public void SubscribeChanged(Action<string> change)
        {
            if (change == null)
            {
                throw new ArgumentNullException("change");
            }

            this.onChangeKey = change;
        }

        /// <summary>
        /// The cache manager will subscribe to the back plate to get triggered whenever another
        /// client changed a cache key.
        /// </summary>
        /// <param name="change">The change.</param>
        /// <exception cref="System.ArgumentNullException">If change is null.</exception>
        public void SubscribeChanged(Action<string, string> change)
        {
            if (change == null)
            {
                throw new ArgumentNullException("change");
            }

            this.onChangeKeyRegion = change;
        }

        /// <summary>
        /// Subscribes the clear.The cache manager will subscribe to the back plate to get triggered
        /// whenever another client cleared the cache.
        /// </summary>
        /// <param name="clear">The clear.</param>
        /// <exception cref="System.ArgumentNullException">If clear is null.</exception>
        public void SubscribeClear(Action clear)
        {
            if (clear == null)
            {
                throw new ArgumentNullException("clear");
            }

            this.onClear = clear;
        }

        /// <summary>
        /// Subscribes the clear region.The cache manager will subscribe to the back plate to get
        /// triggered whenever another client cleared a region.
        /// </summary>
        /// <param name="clearRegion">The clear region.</param>
        /// <exception cref="System.ArgumentNullException">If clearRegion is null.</exception>
        public void SubscribeClearRegion(Action<string> clearRegion)
        {
            if (clearRegion == null)
            {
                throw new ArgumentNullException("clearRegion");
            }

            this.onClearRegion = clearRegion;
        }

        /// <summary>
        /// The cache manager will subscribe to the back plate to get triggered whenever another
        /// client removed a cache item.
        /// </summary>
        /// <param name="remove">The remove.</param>
        /// <exception cref="System.ArgumentNullException">If remove is null.</exception>
        public void SubscribeRemove(Action<string> remove)
        {
            if (remove == null)
            {
                throw new ArgumentNullException("remove");
            }

            this.onRemoveKey = remove;
        }

        /// <summary>
        /// The cache manager will subscribe to the back plate to get triggered whenever another
        /// client removed a cache item.
        /// </summary>
        /// <param name="remove">The remove.</param>
        /// <exception cref="System.ArgumentNullException">If remove is null.</exception>
        public void SubscribeRemove(Action<string, string> remove)
        {
            if (remove == null)
            {
                throw new ArgumentNullException("remove");
            }

            this.onRemoveKeyRegion = remove;
        }
    }
}