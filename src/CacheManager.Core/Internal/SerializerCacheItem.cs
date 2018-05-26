using System;
using System.Linq;
using CacheManager.Core.Utility;

namespace CacheManager.Core.Internal
{
    /// <summary>
    /// Simple converter contract used by the serializer cache item. Serializers will use that to convert back to
    /// The <see cref="CacheItem{T}"/>.
    /// </summary>
    public interface ICacheItemConverter
    {
        /// <summary>
        /// Converts the current instance to a <see cref="CacheItem{T}"/>.
        /// The returned item must return the orignial created and last accessed date!
        /// </summary>
        /// <typeparam name="TTarget">The type.</typeparam>
        /// <returns>The cache item.</returns>
        CacheItem<TTarget> ToCacheItem<TTarget>();
    }

    /// <summary>
    /// Basic abstraction for serializers to work with cache items.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
#if !NETSTANDARD1
    [Serializable]
    [System.Runtime.Serialization.DataContract]
#endif
    public abstract class SerializerCacheItem<T> : ICacheItemConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SerializerCacheItem{T}"/> class.
        /// </summary>
        public SerializerCacheItem()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializerCacheItem{T}"/> class.
        /// </summary>
        /// <param name="properties">The actual properties.</param>
        /// <param name="value">The cache value.</param>
        public SerializerCacheItem(ICacheItemProperties properties, object value)
            : this()
        {
            Guard.NotNull(properties, nameof(properties));
            Guard.NotNull(value, nameof(value));

            CreatedUtc = properties.CreatedUtc.Ticks;
            ExpirationMode = properties.ExpirationMode;
            ExpirationTimeout = properties.ExpirationTimeout.TotalMilliseconds;
            Key = properties.Key;
            LastAccessedUtc = properties.LastAccessedUtc.Ticks;
            Region = properties.Region;
            UsesExpirationDefaults = properties.UsesExpirationDefaults;
            ValueType = properties.ValueType.AssemblyQualifiedName;
            Value = (T)value;
        }

        /// <summary>
        /// Gets or sets the created utc date in ticks.
        /// Can be converted from and to <see cref="DateTime"/>.
        /// </summary>
#if !NETSTANDARD1
        [System.Runtime.Serialization.DataMember]
#endif
        public abstract long CreatedUtc { get; set; }

        /// <summary>
        /// Gets or sets the expiration mode.
        /// </summary>
#if !NETSTANDARD1
        [System.Runtime.Serialization.DataMember]
#endif
        public abstract ExpirationMode ExpirationMode { get; set; }

        /// <summary>
        /// Gets or sets the expiration timeout in milliseconds.
        /// Can be coverted from and to <see cref="TimeSpan"/>.
        /// </summary>
#if !NETSTANDARD1
        [System.Runtime.Serialization.DataMember]
#endif
        public abstract double ExpirationTimeout { get; set; }

        /// <summary>
        /// Gets or sets the key.
        /// </summary>
#if !NETSTANDARD1
        [System.Runtime.Serialization.DataMember]
#endif
        public abstract string Key { get; set; }

        /// <summary>
        /// Gets or sets the last accessed utc date in ticks.
        /// Can be converted from and to <see cref="DateTime"/>.
        /// </summary>
#if !NETSTANDARD1
        [System.Runtime.Serialization.DataMember]
#endif
        public abstract long LastAccessedUtc { get; set; }

        /// <summary>
        /// Gets or sets the region.
        /// </summary>
#if !NETSTANDARD1
        [System.Runtime.Serialization.DataMember]
#endif
        public abstract string Region { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the default expiration should be used.
        /// </summary>
#if !NETSTANDARD1
        [System.Runtime.Serialization.DataMember]
#endif
        public abstract bool UsesExpirationDefaults { get; set; }

        /// <summary>
        /// Gets or sets the value type.
        /// </summary>
#if !NETSTANDARD1
        [System.Runtime.Serialization.DataMember]
#endif
        public abstract string ValueType { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
#if !NETSTANDARD1
        [System.Runtime.Serialization.DataMember]
#endif
        public abstract T Value { get; set; }

        /// <inheritdoc/>
        public CacheItem<TTarget> ToCacheItem<TTarget>()
        {
            var item = string.IsNullOrWhiteSpace(Region) ?
                new CacheItem<TTarget>(Key, (TTarget)(object)Value) :
                new CacheItem<TTarget>(Key, Region, (TTarget)(object)Value);

            // resetting expiration in case the serializer actually stores serialization properties (Redis does for example).
            if (!UsesExpirationDefaults)
            {
                if (ExpirationMode == ExpirationMode.Sliding)
                {
                    item = item.WithSlidingExpiration(TimeSpan.FromMilliseconds(ExpirationTimeout));
                }
                else if (ExpirationMode == ExpirationMode.Absolute)
                {
                    item = item.WithAbsoluteExpiration(TimeSpan.FromMilliseconds(ExpirationTimeout));
                }
                else if (ExpirationMode == ExpirationMode.None)
                {
                    item = item.WithNoExpiration();
                }
            }
            else
            {
                item = item.WithDefaultExpiration();
            }

            item.LastAccessedUtc = new DateTime(LastAccessedUtc, DateTimeKind.Utc);

            return item.WithCreated(new DateTime(CreatedUtc, DateTimeKind.Utc));
        }
    }
}
