using System;

namespace CacheManager.Core.Configuration
{
    /// <summary>
    /// Defines the contract for the cache handle configuration.
    /// </summary>
    public interface ICacheHandleConfiguration
    {
        /// <summary>
        /// Gets the name of the cache.
        /// </summary>
        /// <value>The name of the cache.</value>
        string CacheName { get; }

        /// <summary>
        /// Gets a value indicating whether performance counters should be enabled or not.
        /// <para>
        /// If enabled, and the initialization of performance counters doesn't work, for example
        /// because of security reasons. The counters will get disabled silently.
        /// </para>
        /// </summary>
        /// <value><c>true</c> if performance counters should be enable; otherwise, <c>false</c>.</value>
        bool EnablePerformanceCounters { get; }

        /// <summary>
        /// Gets a value indicating whether statistics should be enabled.
        /// </summary>
        /// <value><c>true</c> if statistics should be enabled; otherwise, <c>false</c>.</value>
        bool EnableStatistics { get; }

        /// <summary>
        /// Gets the expiration mode.
        /// </summary>
        /// <value>The expiration mode.</value>
        ExpirationMode ExpirationMode { get; }

        /// <summary>
        /// Gets the expiration timeout.
        /// </summary>
        /// <value>The expiration timeout.</value>
        TimeSpan ExpirationTimeout { get; }

        /// <summary>
        /// Gets the name of the handle.
        /// <para>
        /// The handle's name might be used by the cache handle to find configuration sections or values.
        /// </para>
        /// </summary>
        /// <value>The name of the handle.</value>
        string HandleName { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is back plate source.
        /// <para>
        /// Only one cache handle inside one cache manager can be back plate source. Usually this is
        /// a distributed cache. It might not make any sense to define an in process cache as back
        /// plate source.
        /// </para>
        /// <para>If no back plate is configured for the cache, this setting will have no effect.</para>
        /// </summary>
        /// <value><c>true</c> if this instance should be back plate source; otherwise, <c>false</c>.</value>
        bool IsBackPlateSource { get; }
    }
}