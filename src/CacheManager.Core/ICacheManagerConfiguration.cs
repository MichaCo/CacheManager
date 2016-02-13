using System;
using System.Collections.Generic;

namespace CacheManager.Core
{
    /// <summary>
    /// The writable configuration contract used primarrily internal only
    /// </summary>
    public interface ICacheManagerConfiguration : IReadOnlyCacheManagerConfiguration
    {
        /// <summary>
        /// Gets the list of cache handle configurations.
        /// </summary>
        /// <value>The list of cache handle configurations.</value>
        IList<CacheHandleConfiguration> CacheHandleConfigurations { get; }

        /// <summary>
        /// Gets a <see cref="ConfigurationBuilder"/> for the current <see cref="CacheManagerConfiguration"/> instance
        /// to manipulate the configuration fluently.
        /// </summary>
        /// <returns>The <see cref="ConfigurationBuilder"/>.</returns>
        ConfigurationBuilder Builder { get; }
    }

    /// <summary>
    /// The readonly configuration contract for cache managers.
    /// </summary>
    public interface IReadOnlyCacheManagerConfiguration
    {
        /// <summary>
        /// Gets the back plate channel name.
        /// </summary>
        /// <value>The channel name.</value>
        string BackPlateChannelName { get; }

        /// <summary>
        /// Gets the configuration key the back plate might use.
        /// </summary>
        /// <value>The key of the back plate configuration.</value>
        string BackPlateConfigurationKey { get; }

        /// <summary>
        /// Gets the factory method for a cache back plate.
        /// </summary>
        /// <value>The back plate activator.</value>
        Type BackPlateType { get; }

        /// <summary>
        /// Gets additional arguments which should be used instantiating the back-plate.
        /// </summary>
        /// <value>The list of arguments.</value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "nope")]
        object[] BackPlateTypeArguments { get; }

        /// <summary>
        /// Gets a value indicating whether this instance has a back plate defined.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has cache back plate; otherwise, <c>false</c>.
        /// </value>
        bool HasBackPlate { get; }

        /// <summary>
        /// Gets the factory method for a logger factory.
        /// </summary>
        /// <value>
        /// The logger factory activator.
        /// </value>
        Type LoggerFactoryType { get; }

        /// <summary>
        /// Gets additional arguments which should be used instantiating the logger factory.
        /// </summary>
        /// <value>The list of arguments.</value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "nope")]
        object[] LoggerFactoryTypeArguments { get; }

        /// <summary>
        /// Gets the limit of the number of retry operations per action.
        /// <para>Default is 50.</para>
        /// </summary>
        /// <value>The maximum retries.</value>
        int MaxRetries { get; }

        /// <summary>
        /// Gets the name of the cache.
        /// </summary>
        /// <value>The name of the cache.</value>
        string Name { get; }

        /// <summary>
        /// Gets the number of milliseconds the cache should wait before it will retry an action.
        /// <para>Default is 100.</para>
        /// </summary>
        /// <value>The retry timeout.</value>
        int RetryTimeout { get; }

        /// <summary>
        /// Gets the factory method for a cache serializer.
        /// </summary>
        /// <value>The serializer activator.</value>
        Type SerializerType { get; }

        /// <summary>
        /// Gets additional arguments which should be used instantiating the serializer.
        /// </summary>
        /// <value>The list of arguments.</value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "nope")]
        object[] SerializerTypeArguments { get; }

        /// <summary>
        /// Gets the <see cref="UpdateMode"/> for the cache manager instance.
        /// <para>
        /// Drives the behavior of the cache manager how it should update the different cache
        /// handles it manages.
        /// </para>
        /// </summary>
        /// <value>The cache update mode.</value>
        /// <see cref="UpdateMode"/>
        CacheUpdateMode UpdateMode { get; }
    }
}