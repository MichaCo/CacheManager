using System;

namespace CacheManager.Core
{
    /// <summary>
    /// Contract which exposes only the properties of the <see cref="CacheItem{T}"/> without T value.
    /// </summary>
    public interface ICacheItemProperties
    {
        /// <summary>
        /// Gets the creation date of the cache item.
        /// </summary>
        /// <value>The creation date.</value>
        DateTime CreatedUtc { get; }

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
        /// Gets the cache key.
        /// </summary>
        /// <value>The cache key.</value>
        string Key { get; }

        /// <summary>
        /// Gets or sets the last accessed date of the cache item.
        /// </summary>
        /// <value>The last accessed date.</value>
        DateTime LastAccessedUtc { get; set; }

        /// <summary>
        /// Gets the cache region.
        /// </summary>
        /// <value>The cache region.</value>
        string Region { get; }

        /// <summary>
        /// Gets a value indicating whether the cache item uses the cache handle's configured expiration.
        /// </summary>
        bool UsesExpirationDefaults { get; }

        /// <summary>
        /// Gets the type of the cache value.
        /// <para>This might be used for serialization and deserialization.</para>
        /// </summary>
        /// <value>The type of the cache value.</value>
        Type ValueType { get; }
    }
}