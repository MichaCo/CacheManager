using System;
using System.Linq;
using CacheManager.Core.Utility;

namespace CacheManager.Core.Internal
{
    /// <summary>
    /// Base implementation for cache serializers.
    /// </summary>
    public abstract class CacheSerializer : ICacheSerializer
    {
        /// <summary>
        /// Returns the open generic type of this class.
        /// </summary>
        /// <returns>The type.</returns>
        protected abstract Type GetOpenGeneric();

        /// <summary>
        /// Creates a new instance of the serializer specific cache item.
        /// Items should implement <see cref="SerializerCacheItem{T}"/> and the implementation should call
        /// the second constructor taking exactly these two arguments.
        /// </summary>
        /// <param name="properties">The item properties to copy from.</param>
        /// <param name="value">The actual cache item value.</param>
        /// <returns>The serializer specific cache item instance.</returns>
        /// <typeparam name="TCacheValue">The cache value type.</typeparam>
        protected abstract object CreateNewItem<TCacheValue>(ICacheItemProperties properties, object value);

        /// <inheritdoc/>
        public abstract byte[] Serialize<T>(T value);

        /// <inheritdoc/>
        public abstract object Deserialize(byte[] data, Type target);

        /// <inheritdoc/>
        public virtual byte[] SerializeCacheItem<T>(CacheItem<T> value)
        {
            Guard.NotNull(value, nameof(value));
            var item = CreateFromCacheItem(value);
            return Serialize(item);
        }

        /// <inheritdoc/>
        public virtual CacheItem<T> DeserializeCacheItem<T>(byte[] value, Type valueType)
        {
            var targetType = GetOpenGeneric().MakeGenericType(valueType);
            var item = (ICacheItemConverter)Deserialize(value, targetType);

            return item.ToCacheItem<T>();
        }

        private object CreateFromCacheItem<TCacheValue>(CacheItem<TCacheValue> source)
        {
            var tType = typeof(TCacheValue);

            if (tType != source.ValueType || tType == TypeCache.ObjectType)
            {
                var targetType = GetOpenGeneric().MakeGenericType(source.ValueType);
                return Activator.CreateInstance(targetType, (ICacheItemProperties)source, source.Value);
            }
            else
            {
                return CreateNewItem<TCacheValue>((ICacheItemProperties)source, source.Value);
            }
        }
    }
}