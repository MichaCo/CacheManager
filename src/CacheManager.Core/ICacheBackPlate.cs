using System;
using CacheManager.Core.Configuration;

namespace CacheManager.Core
{
    /// <summary>
    /// Defines the contract for a cache back plate.
    /// <para>
    /// In CacheManager, a cache back plate is used to keep in process and distributed caches in
    /// sync. <br/> If the cache manager runs inside multiple nodes or applications accessing the
    /// same distributed cache, and an in process cache is configured to be in front of the
    /// distributed cache handle. All Get calls will hit the in process cache. <br/> Now when an
    /// item gets removed for example by one client, all other clients still have that cache item
    /// available in the in process cache. <br/> This could lead to errors and unexpected behavior,
    /// therefore a cache back plate will send a message to all other cache clients to also remove
    /// that item.
    /// </para>
    /// <para>
    /// The same mechanism will apply to any Update, Put, Remove, Clear or ClearRegion call of the cache.
    /// </para>
    /// </summary>
    public interface ICacheBackPlate : IDisposable
    {
        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>The configuration.</value>
        ICacheManagerConfiguration Configuration { get; }

        /// <summary>
        /// Gets the name.
        /// <para>The name might be used to find cache vendor specific configuration.</para>
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Notifies other cache clients about a changed cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        void NotifyChange(string key);

        /// <summary>
        /// Notifies other cache clients about a changed cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        void NotifyChange(string key, string region);

        /// <summary>
        /// Notifies other cache clients about a cache clear.
        /// </summary>
        void NotifyClear();

        /// <summary>
        /// Notifies other cache clients about a cache clear region call.
        /// </summary>
        /// <param name="region">The region.</param>
        void NotifyClearRegion(string region);

        /// <summary>
        /// Notifies other cache clients about a removed cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        void NotifyRemove(string key);

        /// <summary>
        /// Notifies other cache clients about a removed cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        void NotifyRemove(string key, string region);

        /// <summary>
        /// Called when another client changed a cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        void OnChange(string key);

        /// <summary>
        /// Called when another client changed a cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        void OnChange(string key, string region);

        /// <summary>
        /// Called when another client cleared the cache.
        /// </summary>
        void OnClear();

        /// <summary>
        /// Called when another client cleared a region.
        /// </summary>
        /// <param name="region">The region.</param>
        void OnClearRegion(string region);

        /// <summary>
        /// Called when another client removed a cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        void OnRemove(string key);

        /// <summary>
        /// Called when another client removed a cache key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        void OnRemove(string key, string region);

        /// <summary>
        /// The cache manager will subscribe to the back plate to get triggered whenever another
        /// client changed a cache key.
        /// </summary>
        /// <param name="change">The change.</param>
        void SubscribeChanged(Action<string> change);

        /// <summary>
        /// The cache manager will subscribe to the back plate to get triggered whenever another
        /// client changed a cache key.
        /// </summary>
        /// <param name="change">The change.</param>
        void SubscribeChanged(Action<string, string> change);

        /// <summary>
        /// Subscribes the clear.The cache manager will subscribe to the back plate to get triggered
        /// whenever another client cleared the cache.
        /// </summary>
        /// <param name="clear">The clear.</param>
        void SubscribeClear(Action clear);

        /// <summary>
        /// Subscribes the clear region.The cache manager will subscribe to the back plate to get
        /// triggered whenever another client cleared a region.
        /// </summary>
        /// <param name="clearRegion">The clear region.</param>
        void SubscribeClearRegion(Action<string> clearRegion);

        /// <summary>
        /// The cache manager will subscribe to the back plate to get triggered whenever another
        /// client removed a cache item.
        /// </summary>
        /// <param name="remove">The remove.</param>
        void SubscribeRemove(Action<string> remove);

        /// <summary>
        /// The cache manager will subscribe to the back plate to get triggered whenever another
        /// client removed a cache item.
        /// </summary>
        /// <param name="remove">The remove.</param>
        void SubscribeRemove(Action<string, string> remove);
    }
}