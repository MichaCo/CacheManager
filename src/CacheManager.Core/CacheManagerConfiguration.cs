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
    public sealed class CacheManagerConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CacheManagerConfiguration"/> class.
        /// </summary>
        public CacheManagerConfiguration()
        {
            this.LoggerFactory = new NullLoggerFactory();
#if !PORTABLE && !DOTNET5_2
            // default to binary serialization if available
            this.CacheSerializer = new BinaryCacheSerializer();
#endif
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheManagerConfiguration"/> class.
        /// </summary>
        /// <param name="maxRetries">The maximum retries.</param>
        /// <param name="retryTimeout">The retry timeout.</param>
        /// <param name="mode">The cache update mode.</param>
        /// <param name="backPlateConfigurationKey">The name of the cache back plate's configuration.</param>
        /// <param name="backPlateType">The type of the cache back plate implementation.</param>
        /// <param name="serializer">The serializer to be used to serialize the cache item.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "We use it for configuration only.")]
        public CacheManagerConfiguration(
            CacheUpdateMode mode = CacheUpdateMode.None,
            int maxRetries = int.MaxValue,
            int retryTimeout = 10,
            Type backPlateType = null,
            string backPlateConfigurationKey = null,
            ICacheSerializer serializer = null,
            ILoggerFactory loggerFactory = null)
            : this()
        {
            this.CacheUpdateMode = mode;
            this.MaxRetries = maxRetries;
            this.RetryTimeout = retryTimeout;
            this.BackPlateType = backPlateType;
            this.BackPlateConfigurationKey = backPlateConfigurationKey;
#if !PORTABLE && !DOTNET5_2
            // default to binary serialization if available
            this.CacheSerializer = serializer ?? new BinaryCacheSerializer();
#else
            this.CacheSerializer = serializer;
#endif
            this.LoggerFactory = loggerFactory ?? new NullLoggerFactory();
        }

        /// <summary>
        /// Gets the serializer which should be used to serialize the cache item's value.
        /// </summary>
        /// <value>The serializer.</value>
        public ICacheSerializer CacheSerializer { get; private set; }

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
        /// <para>Default is <see cref="int.MaxValue"/>.</para>
        /// </summary>
        /// <value>The maximum retries.</value>
        public int MaxRetries { get; set; } = int.MaxValue;

        /// <summary>
        /// Gets or sets the number of milliseconds the cache should wait before it will retry an action.
        /// <para>Default is 10.</para>
        /// </summary>
        /// <value>The retry timeout.</value>
        public int RetryTimeout { get; set; } = 10;

        /// <summary>
        /// Gets the configuration key the back plate might use.
        /// </summary>
        /// <value>The key of the back plate configuration.</value>
        public string BackPlateConfigurationKey { get; private set; }

        /// <summary>
        /// Gets the type of the back plate.
        /// </summary>
        /// <value>The type of the back plate.</value>
        public Type BackPlateType { get; private set; }

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
        /// Gets the logger factory.
        /// </summary>
        /// <value>
        /// The logger factory.
        /// </value>
        public ILoggerFactory LoggerFactory { get; private set; }

        internal IList<CacheHandleConfiguration> CacheHandleConfigurations { get; } = new List<CacheHandleConfiguration>();

        internal void WithBackPlate(Type backPlateType, string backPlateName, string channelName = null)
        {
            NotNull(backPlateType, nameof(backPlateType));
            NotNullOrWhiteSpace(backPlateName, nameof(backPlateName));

            this.BackPlateConfigurationKey = backPlateName;
            this.BackPlateType = backPlateType;
            this.BackPlateChannelName = channelName;
        }

        internal void WithSerializer(ICacheSerializer instance)
        {
            NotNull(instance, nameof(instance));

            this.CacheSerializer = instance;
        }

        internal void WithLoggerFactory(ILoggerFactory loggerFactory)
        {
            NotNull(loggerFactory, nameof(loggerFactory));

            this.LoggerFactory = loggerFactory;
        }
    }
}