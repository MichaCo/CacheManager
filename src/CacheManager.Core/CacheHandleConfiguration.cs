using System;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core
{
    /// <summary>
    /// Defines all settings the cache handle should respect.
    /// </summary>
    public sealed class CacheHandleConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CacheHandleConfiguration"/> class.
        /// </summary>
        public CacheHandleConfiguration()
        {
            this.Name = this.Key = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheHandleConfiguration"/> class.
        /// </summary>
        /// <param name="handleName">Name of the handle. This value will also be used for the <see cref="Key"/>.</param>
        /// <exception cref="System.ArgumentNullException">If <paramref name="handleName"/> is null.</exception>
        public CacheHandleConfiguration(string handleName)
        {
            NotNullOrWhiteSpace(handleName, nameof(handleName));

            this.Name = this.Key = handleName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheHandleConfiguration"/> class.
        /// </summary>
        /// <param name="handleName">Name of the handle.</param>
        /// <param name="configurationKey">The key which can be used to identify another part of the configuration which the handle might need.</param>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="handleName"/> or <paramref name="configurationKey"/> is null.
        /// </exception>
        public CacheHandleConfiguration(string handleName, string configurationKey)
        {
            NotNullOrWhiteSpace(handleName, nameof(handleName));
            NotNullOrWhiteSpace(configurationKey, nameof(configurationKey));

            this.Name = handleName;
            this.Key = configurationKey;
        }

        /// <summary>
        /// Gets or sets a value indicating whether performance counters should be enabled or not.
        /// <para>
        /// If enabled, and the initialization of performance counters doesn't work, for example
        /// because of security reasons. The counters will get disabled silently.
        /// </para>
        /// </summary>
        /// <value><c>true</c> if performance counters should be enable; otherwise, <c>false</c>.</value>
        public bool EnablePerformanceCounters { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether statistics should be enabled.
        /// </summary>
        /// <value><c>true</c> if statistics should be enabled; otherwise, <c>false</c>.</value>
        public bool EnableStatistics { get; set; }

        /// <summary>
        /// Gets or sets the expiration mode.
        /// </summary>
        /// <value>The expiration mode.</value>
        public ExpirationMode ExpirationMode { get; set; }

        /// <summary>
        /// Gets or sets the expiration timeout.
        /// </summary>
        /// <value>The expiration timeout.</value>
        public TimeSpan ExpirationTimeout { get; set; }

        /// <summary>
        /// Gets or sets the name for the cache handle which is also the identifier of the configuration.
        /// </summary>
        /// <value>The name of the handle.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the configuration key.
        /// Some cache handles require to reference another part of the configuration by name.
        /// If not specified, the <see cref="Name"/> will be used instead.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is backplane source.
        /// <para>
        /// Only one cache handle inside one cache manager can be backplane source. Usually this is
        /// a distributed cache. It might not make any sense to define an in process cache as backplane source.
        /// </para>
        /// <para>If no backplane is configured for the cache, this setting will have no effect.</para>
        /// </summary>
        /// <value><c>true</c> if this instance should be backplane source; otherwise, <c>false</c>.</value>
        public bool IsBackplaneSource { get; set; }

        /// <summary>
        /// Gets or sets the type of the handle.
        /// </summary>
        /// <value>The type of the handle.</value>
        public Type HandleType { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{HandleType}";
        }

        internal object[] ConfigurationTypes { get; set; } = new object[0];
    }
}