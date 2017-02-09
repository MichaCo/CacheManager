using System;
using System.Linq;

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
        /// <typeparam name="TTarget"></typeparam>
        /// <returns>The cache item.</returns>
        CacheItem<TTarget> ToCacheItem<TTarget>();
    }

    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SerializerCacheItem<T> : ICacheItemConverter
    {
        /// <summary>
        /// Gets or sets the created utc date in ticks.
        /// Can be converted from and to <see cref="DateTime"/>.
        /// </summary>
        public abstract long CreatedUtc { get; set; }

        /// <inheritdoc/>
        public abstract ExpirationMode ExpirationMode { get; set; }

        /// <summary>
        /// Gets or set the expiration timeout in milliseconds.
        /// Can be coverted from and to <see cref="TimeSpan"/>.
        /// </summary>
        public abstract int ExpirationTimeout { get; set; }

        /// <inheritdoc/>
        public abstract string Key { get; set; }

        /// <summary>
        /// Gets or sets the last accessed utc date in ticks.
        /// Can be converted from and to <see cref="DateTime"/>.
        /// </summary>
        public abstract long LastAccessedUtc { get; set; }

        /// <inheritdoc/>
        public abstract string Region { get; set; }

        /// <inheritdoc/>
        public abstract bool UsesExpirationDefaults { get; set; }

        /// <inheritdoc/>
        public abstract string ValueType { get; set; }

        /// <summary>
        /// The value.
        /// </summary>
        public abstract T Value { get; set; }

        /// <summary>
        ///
        /// </summary>
        public SerializerCacheItem()
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="value"></param>
        public SerializerCacheItem(ICacheItemProperties properties, object value) : this()
        {
            this.CreatedUtc = properties.CreatedUtc.Ticks;
            this.ExpirationMode = properties.ExpirationMode;
            this.ExpirationTimeout = (int)properties.ExpirationTimeout.TotalMilliseconds;
            this.Key = properties.Key;
            this.LastAccessedUtc = properties.LastAccessedUtc.Ticks;
            this.Region = properties.Region;
            this.UsesExpirationDefaults = properties.UsesExpirationDefaults;
            this.ValueType = properties.ValueType.AssemblyQualifiedName;
            this.Value = (T)value;
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TCacheItem"></typeparam>
        /// <param name="source"></param>
        /// <param name="factory"></param>
        /// <param name="openGenericSerializerCacheItemType"></param>
        /// <returns></returns>
        public static object CreateFromCacheItem<TCacheItem>(
            CacheItem<TCacheItem> source,
            Func<ICacheItemProperties, object, SerializerCacheItem<T>> factory,
            Type openGenericSerializerCacheItemType)
        {
            Type tType = typeof(T);

            if (tType != source.ValueType || tType == TypeCache.ObjectType)
            {
                var targetType = openGenericSerializerCacheItemType.MakeGenericType(source.ValueType);
                return Activator.CreateInstance(targetType, (ICacheItemProperties)source, source.Value);
            }
            else
            {
                return factory((ICacheItemProperties)source, source.Value);
            }
        }

        /// <inheritdoc/>
        public CacheItem<TTarget> ToCacheItem<TTarget>()
        {
            var item = string.IsNullOrWhiteSpace(this.Region) ?
                new CacheItem<TTarget>(this.Key, (TTarget)(object)Value) :
                new CacheItem<TTarget>(this.Key, this.Region, (TTarget)(object)Value);

            // resetting expiration in case the serializer actually stores serialization properties (Redis does for example).
            if (!this.UsesExpirationDefaults)
            {
                if (this.ExpirationMode == ExpirationMode.Sliding)
                {
                    item = item.WithSlidingExpiration(TimeSpan.FromMilliseconds(this.ExpirationTimeout));
                }
                else if (this.ExpirationMode == ExpirationMode.Absolute)
                {
                    item = item.WithAbsoluteExpiration(TimeSpan.FromMilliseconds(this.ExpirationTimeout));
                }
                else if (this.ExpirationMode == ExpirationMode.None)
                {
                    item = item.WithNoExpiration();
                }
            }
            else
            {
                item = item.WithDefaultExpiration();
            }

            item.LastAccessedUtc = new DateTime(this.LastAccessedUtc, DateTimeKind.Utc);

            return item.WithCreated(new DateTime(this.CreatedUtc, DateTimeKind.Utc));
        }
    }
}