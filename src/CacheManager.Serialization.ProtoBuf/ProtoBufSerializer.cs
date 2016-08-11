using System;
using System.IO;

using CacheManager.Core;
using CacheManager.Core.Internal;
using pb = ProtoBuf;

namespace CacheManager.Serialization.ProtoBuf
{
    public class ProtoBufSerializer : ICacheSerializer
    {
        public ProtoBufSerializer()
        { }

        public object Deserialize(byte[] data, Type target)
        {
            var stream = new MemoryStream(data);
            return pb.Serializer.Deserialize(target, stream);
        }

        public CacheItem<T> DeserializeCacheItem<T>(byte[] value, Type valueType)
        {
            var item = (ProtoBufCacheItem)Deserialize(value, typeof(ProtoBufCacheItem));
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
            return output;
        }

        public byte[] SerializeCacheItem<T>(CacheItem<T> value)
        {
            var cachedValue = Serialize(value.Value);
            var pbCacheItem = ProtoBufCacheItem.FromCacheItem(value, cachedValue);

            return Serialize(pbCacheItem);
        }
    }
}
