using System;
using System.IO;
using System.Linq;
using CacheManager.Core;
using CacheManager.Core.Internal;
using pb = ProtoBuf;

namespace CacheManager.Serialization.ProtoBuf
{
    public class ProtoBufSerializer : ICacheSerializer
    {
        private static readonly Type cacheItemType = typeof(ProtoBufCacheItem);

        public ProtoBufSerializer()
        {
        }

        public object Deserialize(byte[] data, Type target)
        {
            byte[] destination;
            if (data.Length == 0)
            {
                destination = data;
            }
            else
            {
                destination = new byte[data.Length - 1];
                Array.Copy(data, 1, destination, 0, data.Length - 1);
            }

            using (var stream = new MemoryStream(destination))
            {
                return pb.Serializer.Deserialize(target, stream);
            }
        }

        public CacheItem<T> DeserializeCacheItem<T>(byte[] value, Type valueType)
        {
            var item = (ProtoBufCacheItem)Deserialize(value, cacheItemType);
            if (item == null)
            {
                throw new Exception("Unable to deserialize the CacheItem");
            }

            var cachedValue = Deserialize(item.Value, valueType);
            return item.ToCacheItem<T>(cachedValue);
        }

        public byte[] Serialize<T>(T value)
        {
            byte[] output = null;
            using (var stream = new MemoryStream())
            {
                pb.Serializer.Serialize(stream, value);
                output = stream.ToArray();
            }

            // Protobuf returns an empty byte array {} which would be treated as Null value in redis
            // this is not allowed in cache manager and would cause issues (would look like the item does not exist)
            // we'll simply add a prefix byte and remove it before deserialization.
            var prefix = new byte[] { 1 };
            return prefix.Concat(output).ToArray();
        }

        public byte[] SerializeCacheItem<T>(CacheItem<T> value)
        {
            var cachedValue = Serialize(value.Value);
            var pbCacheItem = ProtoBufCacheItem.FromCacheItem(value, cachedValue);

            return Serialize(pbCacheItem);
        }
    }
}