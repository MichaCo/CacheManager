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

            this.Key = key;
            this.Region = region;
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
            this.Origin = origin;
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
            this.Origin = origin;
        }

        /// <summary>
        /// Gets the event origin indicating if the event was triggered by a local action or remotly, through the backplane.
        /// </summary>
        public CacheActionEventArgOrigin Origin { get; }
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

            this.Region = region;
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
    }

    /////// <summary>
    /////// Event arguments for cache update actions.
    /////// </summary>
    /////// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
    ////public sealed class CacheUpdateEventArgs<TCacheValue> : EventArgs
    ////{
    ////    /// <summary>
    ////    /// Initializes a new instance of the <see cref="CacheUpdateEventArgs{TCacheValue}" /> class.
    ////    /// </summary>
    ////    /// <param name="key">The key.</param>
    ////    /// <param name="region">The region.</param>
    ////    /// <param name="maxRetries">The number of retries configured.</param>
    ////    /// <param name="result">The result.</param>
    ////    public CacheUpdateEventArgs(string key, string region, int maxRetries, UpdateItemResult<TCacheValue> result)
    ////    {
    ////        this.Key = key;
    ////        this.Region = region;
    ////        this.Result = result;
    ////        this.MaxRetries = maxRetries;
    ////    }

    ////    /// <summary>
    ////    /// Gets the number of tries which were configured for the update operation.
    ////    /// </summary>
    ////    /// <value>The number of tries.</value>
    ////    public int MaxRetries { get; }

    ////    /// <summary>
    ////    /// Gets the key.
    ////    /// </summary>
    ////    /// <value>The key.</value>
    ////    public string Key { get; }

    ////    /// <summary>
    ////    /// Gets the region.
    ////    /// </summary>
    ////    /// <value>The region.</value>
    ////    public string Region { get; }

    ////    /// <summary>
    ////    /// Gets the result.
    ////    /// </summary>
    ////    /// <value>The result.</value>
    ////    public UpdateItemResult<TCacheValue> Result { get; }
    ////}
}