using System;
using System.Collections.Generic;

namespace CacheManager.Core.Configuration
{
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
        /// Gets the name which serves as Identifier and can be passed in to construct a CacheManager instance.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the <see cref="CacheUpdateMode"/> for the cache manager instance.
        /// <para>
        /// Drives the behavior of the cache manager how it should update the different cache handles it manages.
        /// </para>
        /// </summary>
        public CacheUpdateMode CacheUpdateMode { get; internal set; }

        /// <summary>
        /// Gets the list of cache handle configurations.
        /// <para>Internally used only.</para>
        /// </summary>
        internal IList<CacheHandleConfiguration<TCacheValue>> CacheHandles { get; private set; }

        /// <summary>
        /// Gets or sets the limit of the number of retry operations per action.
        /// <para>Default is <see cref="int.MaxValue"/>.</para>
        /// </summary>
        public int MaxRetries { get; set; }

        /// <summary>
        /// Gets or sets the number of milliseconds the cache should wait before it 
        /// will retry an action.
        /// <para>Default is 10.</para>
        /// </summary>
        public int RetryTimeout { get; set; }

        internal Type BackPlateType { get; set; }

        public string BackPlateName { get; internal set; }
    }
}