using System;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core.Internal
{
    /// <summary>
    /// The origin enum indicates if the cache event was triggered locally or through the backplane.
    /// </summary>
    public enum CacheActionEventArgOrigin
    {
        /// <summary>
        /// Locally triggered action.
        /// </summary>
        Local,

        /// <summary>
        /// Remote, through the backplane triggered action.
        /// </summary>
        Remote
    }

    /// <summary>
    /// A flag indicating the reason when an item got removed from the cache.
    /// </summary>
    public enum CacheItemRemovedReason
    {
        /////// <summary>
        /////// A <see cref="CacheItem{T}"/> was removed using the <see cref="ICache{TCacheValue}.Remove(string)"/> or
        /////// <see cref="ICache{TCacheValue}.Remove(string, string)"/> method.
        /////// </summary>
        ////Removed = 0,

        /// <summary>
        /// A <see cref="CacheItem{T}"/> was removed because it expired.
        /// </summary>
        Expired = 0,

        /// <summary>
        /// A <see cref="CacheItem{T}"/> was removed because the underlying cache decided to remove it.
        /// This can happen if cache-specific memory limits are reached for example.
        /// </summary>
        Evicted = 1
    }

    /// <summary>
    /// Event arguments for cache actions.
    /// </summary>
    public sealed class CacheItemRemovedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CacheItemRemovedEventArgs"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        /// <param name="reason">The reason.</param>
        /// <param name="level">The cache level the event got triggered by.</param>
        /// <exception cref="System.ArgumentNullException">If key is null.</exception>
        public CacheItemRemovedEventArgs(string key, string region, CacheItemRemovedReason reason, int level = 0)
        {
            NotNullOrWhiteSpace(key, nameof(key));

            Reason = reason;
            Key = key;
            Region = region;
            Level = level;
        }

        /// <summary>
        /// Gets the key.
        /// </summary>
        /// <value>The key.</value>
        public string Key { get; }

        /// <summary>
        /// Gets the region.
        /// </summary>
        /// <value>The region.</value>
        public string Region { get; }

        /// <summary>
        /// Gets the reason flag indicating details why the <see cref="CacheItem{T}"/> has been removed.
        /// </summary>
        public CacheItemRemovedReason Reason { get; }

        /// <summary>
        /// Gets a value indicating the cache level the event got triggered by.
        /// </summary>
        public int Level { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"CacheItemRemovedEventArgs {Region}:{Key} - {Reason} {Level}";
        }
    }

    /// <summary>
    /// Event arguments for cache actions.
    /// </summary>
    public sealed class CacheActionEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CacheActionEventArgs"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        /// <exception cref="System.ArgumentNullException">If key is null.</exception>
        public CacheActionEventArgs(string key, string region)
        {
            NotNullOrWhiteSpace(key, nameof(key));

            Key = key;
            Region = region;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheActionEventArgs"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        /// <param name="origin">The origin the event ocured. If remote, the event got triggered by the backplane and was not actually excecuted locally.</param>
        /// <exception cref="System.ArgumentNullException">If key is null.</exception>
        public CacheActionEventArgs(string key, string region, CacheActionEventArgOrigin origin)
            : this(key, region)
        {
            Origin = origin;
        }

        /// <summary>
        /// Gets the key.
        /// </summary>
        /// <value>The key.</value>
        public string Key { get; }

        /// <summary>
        /// Gets the region.
        /// </summary>
        /// <value>The region.</value>
        public string Region { get; }

        /// <summary>
        /// Gets the event origin indicating if the event was triggered by a local action or remotly, through the backplane.
        /// </summary>
        public CacheActionEventArgOrigin Origin { get; } = CacheActionEventArgOrigin.Local;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"CacheActionEventArgs {Region}:{Key} - {Origin}";
        }
    }

    /// <summary>
    /// Event arguments for cache clear events.
    /// </summary>
    public sealed class CacheClearEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CacheClearEventArgs"/> class.
        /// </summary>
        /// <param name="origin">The origin the event ocured. If remote, the event got triggered by the backplane and was not actually excecuted locally.</param>
        public CacheClearEventArgs(CacheActionEventArgOrigin origin = CacheActionEventArgOrigin.Local)
        {
            Origin = origin;
        }

        /// <summary>
        /// Gets the event origin indicating if the event was triggered by a local action or remotly, through the backplane.
        /// </summary>
        public CacheActionEventArgOrigin Origin { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"CacheClearEventArgs {Origin}";
        }
    }

    /// <summary>
    /// Event arguments for clear region events.
    /// </summary>
    public sealed class CacheClearRegionEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CacheClearRegionEventArgs"/> class.
        /// </summary>
        /// <param name="region">The region.</param>
        /// <param name="origin">The origin the event ocured. If remote, the event got triggered by the backplane and was not actually excecuted locally.</param>
        /// <exception cref="System.ArgumentNullException">If region is null.</exception>
        public CacheClearRegionEventArgs(string region, CacheActionEventArgOrigin origin = CacheActionEventArgOrigin.Local)
        {
            NotNullOrWhiteSpace(region, nameof(region));

            Region = region;
            Origin = origin;
        }

        /// <summary>
        /// Gets the region.
        /// </summary>
        /// <value>The region.</value>
        public string Region { get; }

        /// <summary>
        /// Gets the event origin indicating if the event was triggered by a local action or remotly, through the backplane.
        /// </summary>
        public CacheActionEventArgOrigin Origin { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"CacheClearRegionEventArgs {Region} - {Origin}";
        }
    }
}