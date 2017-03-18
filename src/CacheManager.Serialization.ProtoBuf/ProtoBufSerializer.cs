using System;
using System.IO;
using System.Linq;
using CacheManager.Core;
using CacheManager.Core.Internal;
using ProtoBuf;

namespace CacheManager.Serialization.ProtoBuf
{
    /// <summary>
    /// Implements the <see cref="ICacheSerializer"/> contract using <c>ProtoBuf</c>.
    /// </summary>
    public class ProtoBufSerializer : CacheSerializer
    {
        private static readonly Type _openGenericItemType = typeof(ProtoBufCacheItem<>);

        /// <inheritdoc/>
        public override object Deserialize(byte[] data, Type target)
        {
            var offset = 0;
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
        public override byte[] Serialize<T>(T value)
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
        protected override object CreateNewItem<TCacheValue>(ICacheItemProperties properties, object value)
        {
            return new ProtoBufCacheItem<TCacheValue>(properties, value);
        }

        /// <inheritdoc/>
        protected override Type GetOpenGeneric()
        {
            return _openGenericItemType;
        }
    }
}