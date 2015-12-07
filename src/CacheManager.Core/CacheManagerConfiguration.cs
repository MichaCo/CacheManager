using System;
using System.Collections.Generic;
using CacheManager.Core.Internal;
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
        /// <param name="backPlateName">The name of the cache back plate.</param>
        /// <param name="backPlateType">The type of the cache back plate implementation.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "We use it for configuration only.")]
        public CacheManagerConfiguration(
            CacheUpdateMode mode = CacheUpdateMode.None, 
            int maxRetries = int.MaxValue, 
            int retryTimeout = 10, 
            Type backPlateType = null, 
            string backPlateName = null, 
            ICacheSerializer serializer = null)
            : this()
        {
            this.CacheUpdateMode = mode;
            this.MaxRetries = maxRetries;
            this.RetryTimeout = retryTimeout;
            this.BackPlateType = backPlateType;
            this.BackPlateName = backPlateName;
#if !PORTABLE && !DOTNET5_2
            // default to binary serialization if available
            this.CacheSerializer = serializer ?? new BinaryCacheSerializer();
#else
            this.CacheSerializer = serializer;
#endif
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
        /// Gets the name of the back plate.
        /// </summary>
        /// <value>The name of the back plate.</value>
        public string BackPlateName { get; private set; }

        /// <summary>
        /// Gets the type of the back plate.
        /// </summary>
        /// <value>The type of the back plate.</value>
        public Type BackPlateType { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance has a back plate defined.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has cache back plate; otherwise, <c>false</c>.
        /// </value>
        public bool HasBackPlate => this.BackPlateType != null;

        /// <summary>
        /// Gets the list of cache handle configurations.
        /// <para>Internally used only.</para>
        /// </summary>
        /// <value>
        /// The cache handle configurations.
        /// </value>
        internal IList<CacheHandleConfiguration> CacheHandleConfigurations { get; } = new List<CacheHandleConfiguration>();

        internal void WithBackPlate(Type backPlateType, string backPlateName)
        {
            NotNull(backPlateType, nameof(backPlateType));
            NotNullOrWhiteSpace(backPlateName, nameof(backPlateName));

            this.BackPlateName = backPlateName;
            this.BackPlateType = backPlateType;
        }

        internal void WithSerializer(ICacheSerializer instance)
        {
            NotNull(instance, nameof(instance));

            this.CacheSerializer = instance; ;
        }
    }
}