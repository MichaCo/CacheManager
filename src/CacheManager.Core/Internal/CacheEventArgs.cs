using System;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core.Internal
{
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
        /// Gets the key.
        /// </summary>
        /// <value>The key.</value>
        public string Key { get; }

        /// <summary>
        /// Gets the region.
        /// </summary>
        /// <value>The region.</value>
        public string Region { get; }
    }

    /// <summary>
    /// Event arguments for cache clear events.
    /// </summary>
    public sealed class CacheClearEventArgs : EventArgs
    {
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
        /// <exception cref="System.ArgumentNullException">If region is null.</exception>
        public CacheClearRegionEventArgs(string region)
        {
            NotNullOrWhiteSpace(region, nameof(region));

            this.Region = region;
        }

        /// <summary>
        /// Gets the region.
        /// </summary>
        /// <value>The region.</value>
        public string Region { get; }
    }

    /// <summary>
    /// Event arguments for cache update actions.
    /// </summary>
    /// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
    public sealed class CacheUpdateEventArgs<TCacheValue> : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CacheUpdateEventArgs{TCacheValue}" /> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        /// <param name="maxRetries">The number of retries configured.</param>
        /// <param name="result">The result.</param>
        public CacheUpdateEventArgs(string key, string region, int maxRetries, UpdateItemResult<TCacheValue> result)
        {
            this.Key = key;
            this.Region = region;
            this.Result = result;
            this.MaxRetries = maxRetries;
        }

        /// <summary>
        /// Gets the number of tries which were configured for the update operation.
        /// </summary>
        /// <value>The number of tries.</value>
        public int MaxRetries { get; }

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
        /// Gets the result.
        /// </summary>
        /// <value>The result.</value>
        public UpdateItemResult<TCacheValue> Result { get; }
    }
}