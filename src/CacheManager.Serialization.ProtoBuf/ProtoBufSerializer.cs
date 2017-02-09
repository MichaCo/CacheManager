using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CacheManager.Core;
using CacheManager.Core.Internal;
using ProtoBuf;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Serialization.ProtoBuf
{
    /// <summary>
    /// Implements the <see cref="ICacheSerializer"/> contract using <c>ProtoBuf</c>.
    /// </summary>
    public class ProtoBufSerializer : ICacheSerializer
    {
        private static readonly Type cacheItemType = typeof(ProtoBufCacheItem);

        /// <summary>
        /// Initializes a new instance of the <see cref="ProtoBufSerializer"/> class.
        /// </summary>
        public ProtoBufSerializer()
        {
        }

        /// <inheritdoc/>
        public object Deserialize(byte[] data, Type target)
        {
            int offset = 0;
            if (data.Length > 0)
            {
                offset = 1;
            }

            using (var stream = new MemoryStream(data, offset, data.Length - offset))
            {
                return Serializer.Deserialize(target, stream);
            }
        }

        /// <inheritdoc/>
        public CacheItem<T> DeserializeCacheItem<T>(byte[] value, Type valueType = null)
        {
            var targetType = ProtoBufCacheItem.GetGenericJsonCacheItemType(valueType);
            var item = (ICacheItemConverter)this.Deserialize(value, targetType);

            return item.ToCacheItem<T>();
        }

        /// <inheritdoc/>
        public byte[] Serialize<T>(T value)
        {
            using (var stream = new MemoryStream())
            {
                // Protobuf returns an empty byte array {} which would be treated as Null value in redis
                // this is not allowed in cache manager and would cause issues (would look like the item does not exist)
                // we'll simply add a prefix byte and remove it before deserialization.
                stream.WriteByte(0);
                Serializer.Serialize(stream, value);
                return stream.ToArray();
            }
        }

        /// <inheritdoc/>
        public byte[] SerializeCacheItem<T>(CacheItem<T> value)
        {
            NotNull(value, nameof(value));
            var jsonItem = ProtoBufCacheItem.CreateFromCacheItem(value);

            return this.Serialize(jsonItem);
        }
    }
}