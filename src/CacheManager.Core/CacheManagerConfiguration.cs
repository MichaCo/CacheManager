using System;
using System.Collections.Generic;

namespace CacheManager.Core
{
    /// <summary>
    /// The basic cache manager configuration class.
    /// </summary>
    public sealed class CacheManagerConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CacheManagerConfiguration"/> class.
        /// </summary>
        public CacheManagerConfiguration()
        {
            this.CacheHandleConfigurations = new List<CacheHandleConfiguration>();
            this.MaxRetries = int.MaxValue;
            this.RetryTimeout = 10;
            this.CacheUpdateMode = CacheUpdateMode.Up;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheManagerConfiguration"/> class.
        /// </summary>
        /// <param name="maxRetries">The maximum retries.</param>
        /// <param name="retryTimeout">The retry timeout.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "We use it for configuration only.")]
        public CacheManagerConfiguration(int maxRetries = int.MaxValue, int retryTimeout = 10)
            : this()
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
        /// Gets or sets the <see cref="CacheUpdateMode"/> for the cache manager instance.
        /// <para>
        /// Drives the behavior of the cache manager how it should update the different cache
        /// handles it manages.
        /// </para>
        /// </summary>
        /// <value>The cache update mode.</value>
        /// <see cref="CacheUpdateMode"/>
        public CacheUpdateMode CacheUpdateMode { get; set; }

        /// <summary>
        /// Gets or sets the limit of the number of retry operations per action.
        /// <para>Default is <see cref="int.MaxValue"/>.</para>
        /// </summary>
        /// <value>The maximum retries.</value>
        public int MaxRetries { get; set; }

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
        public Type BackPlateType { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance has a back plate defined.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has cache back plate; otherwise, <c>false</c>.
        /// </value>
        public bool HasBackPlate
        {
            get
            {
                return this.BackPlateType != null;
            }
        }

        /// <summary>
        /// Gets the list of cache handle configurations.
        /// <para>Internally used only.</para>
        /// </summary>
        /// <value>
        /// The cache handle configurations.
        /// </value>
        public IList<CacheHandleConfiguration> CacheHandleConfigurations { get; private set; }
    }
}