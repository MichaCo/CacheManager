using System;
using System.Collections.Generic;

namespace CacheManager.Core.Configuration
{
    /// <summary>
    /// The basic cache manager configuration class.
    /// </summary>
    /// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
    public sealed class CacheManagerConfiguration<TCacheValue> : ICacheManagerConfiguration
    {
        internal CacheManagerConfiguration(string cacheName)
        {
            if (string.IsNullOrWhiteSpace(cacheName))
            {
                throw new ArgumentNullException("cacheName");
            }

            this.Name = cacheName;
            this.CacheHandles = new List<CacheHandleConfiguration<TCacheValue>>();
            this.MaxRetries = int.MaxValue;
            this.RetryTimeout = 10;
            this.CacheUpdateMode = CacheUpdateMode.Up;
        }

        internal CacheManagerConfiguration(string cacheName, int maxRetries = int.MaxValue, int retryTimeout = 10)
            : this(cacheName)
        {
            this.MaxRetries = maxRetries;
            this.RetryTimeout = retryTimeout;
        }

        /// <summary>
        /// Gets the name of the back plate.
        /// </summary>
        /// <value>The name of the back plate.</value>
        public string BackPlateName { get; internal set; }

        /// <summary>
        /// Gets the <see cref="CacheUpdateMode"/> for the cache manager instance.
        /// <para>
        /// Drives the behavior of the cache manager how it should update the different cache
        /// handles it manages.
        /// </para>
        /// </summary>
        /// <value>The cache update mode.</value>
        /// <see cref="CacheUpdateMode"/>
        public CacheUpdateMode CacheUpdateMode { get; internal set; }

        /// <summary>
        /// Gets or sets the limit of the number of retry operations per action.
        /// <para>Default is <see cref="int.MaxValue"/>.</para>
        /// </summary>
        /// <value>The maximum retries.</value>
        public int MaxRetries { get; set; }

        /// <summary>
        /// Gets the name which serves as Identifier and can be passed in to construct a
        /// CacheManager instance.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets or sets the number of milliseconds the cache should wait before it will retry an action.
        /// <para>Default is 10.</para>
        /// </summary>
        /// <value>The retry timeout.</value>
        public int RetryTimeout { get; set; }

        /// <summary>
        /// Gets or sets the type of the back plate.
        /// </summary>
        /// <value>The type of the back plate.</value>
        internal Type BackPlateType { get; set; }

        /// <summary>
        /// Gets the list of cache handle configurations.
        /// <para>Internally used only.</para>
        /// </summary>
        internal IList<CacheHandleConfiguration<TCacheValue>> CacheHandles { get; private set; }
    }
}