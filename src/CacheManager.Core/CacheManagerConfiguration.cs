using System;
using System.Collections.Generic;
using CacheManager.Core.Internal;
using CacheManager.Core.Logging;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core
{
    /// <summary>
    /// The basic cache manager configuration class.
    /// </summary>
    public class CacheManagerConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CacheManagerConfiguration"/> class.
        /// </summary>
        public CacheManagerConfiguration()
        {
        }

        /// <summary>
        /// Gets or sets the name of the cache.
        /// </summary>
        /// <value>The name of the cache.</value>
        public string Name { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the <see cref="CacheUpdateMode"/> for the cache manager instance.
        /// <para>
        /// Drives the behavior of the cache manager how it should update the different cache
        /// handles it manages.
        /// </para>
        /// </summary>
        /// <value>The cache update mode.</value>
        /// <see cref="CacheUpdateMode"/>
        public CacheUpdateMode CacheUpdateMode { get; set; } = CacheUpdateMode.Up;

        /// <summary>
        /// Gets or sets the limit of the number of retry operations per action.
        /// <para>Default is 50.</para>
        /// </summary>
        /// <value>The maximum retries.</value>
        public int MaxRetries { get; set; } = 50;

        /// <summary>
        /// Gets or sets the number of milliseconds the cache should wait before it will retry an action.
        /// <para>Default is 100.</para>
        /// </summary>
        /// <value>The retry timeout.</value>
        public int RetryTimeout { get; set; } = 100;

        /// <summary>
        /// Gets or sets the configuration key the back plate might use.
        /// </summary>
        /// <value>The key of the back plate configuration.</value>
        public string BackPlateConfigurationKey { get; set; }

        /// <summary>
        /// Gets or sets the back plate channel name.
        /// </summary>
        /// <value>The channel name.</value>
        public string BackPlateChannelName { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance has a back plate defined.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has cache back plate; otherwise, <c>false</c>.
        /// </value>
        public bool HasBackPlate => this.BackPlateType != null;

        /// <summary>
        /// Gets or sets the factory method for a cache back plate.
        /// </summary>
        /// <value>The back plate activator.</value>
        public Type BackPlateType { get; set; }

        /// <summary>
        /// Gets or sets additional arguments which should be used instantiating the back-plate.
        /// </summary>
        /// <value>The list of arguments.</value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "nope")]
        public object[] BackPlateTypeArguments { get; set; }

        /// <summary>
        /// Gets or sets the factory method for a cache serializer.
        /// </summary>
        /// <value>The serializer activator.</value>
        public Type SerializerType { get; set; }

        /// <summary>
        /// Gets or sets additional arguments which should be used instantiating the serializer.
        /// </summary>
        /// <value>The list of arguments.</value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "nope")]
        public object[] SerializerTypeArguments { get; set; }

        /// <summary>
        /// Gets or sets the factory method for a logger factory.
        /// </summary>
        /// <value>
        /// The logger factory activator.
        /// </value>
        public Type LoggerFactoryType { get; set; }

        /// <summary>
        /// Gets or sets additional arguments which should be used instantiating the logger factory.
        /// </summary>
        /// <value>The list of arguments.</value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "nope")]
        public object[] LoggerFactoryTypeArguments { get; set; }

        /// <summary>
        /// Gets the list of cache handle configurations.
        /// </summary>
        /// <value>The list of cache handle configurations.</value>
        public IList<CacheHandleConfiguration> CacheHandleConfigurations { get; } = new List<CacheHandleConfiguration>();
    }
}