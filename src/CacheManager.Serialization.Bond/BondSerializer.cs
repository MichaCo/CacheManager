using System;
using Bond.IO.Safe;
using Bond.Protocols;
using CacheManager.Core;
using CacheManager.Core.Internal;
using MSBond = Bond;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Serialization.Bond
{
    public class BondSerializer : ICacheSerializer
    {
        public object Deserialize(byte[] data, Type target)
        {
            var input = new InputBuffer(data);
            var reader = new CompactBinaryReader<InputBuffer>(input);
            return new MSBond.Deserializer<CompactBinaryReader<InputBuffer>>(target).Deserialize(reader);
        }

        public CacheItem<T> DeserializeCacheItem<T>(byte[] value, Type valueType)
        {
            var bondItem = (BondCacheItem)this.Deserialize(value, typeof(BondCacheItem));
            EnsureNotNull(bondItem, "Could not deserialize cache item");
            var deserializedValue = this.Deserialize(bondItem.Value, valueType);
            return bondItem.ToCacheItem<T>(deserializedValue);
        }

        public byte[] Serialize<T>(T value)
        {
            var output = new OutputBuffer();
            var writer = new CompactBinaryWriter<OutputBuffer>(output);
            MSBond.Serialize.To(writer, value);
            return output.Data.Array;
        }

        public byte[] SerializeCacheItem<T>(CacheItem<T> value)
        {
            NotNull(value, nameof(value));
            var jsonValue = this.Serialize(value.Value);
            var jsonItem = BondCacheItem.FromCacheItem(value, jsonValue);
            return this.Serialize(jsonItem);
        }
    }
 
}
