using System;
using System.Text;
using CacheManager.Core;
using CacheManager.Core.Internal;
using Newtonsoft.Json;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Serialization.Json
{
    // This project can output the Class library as a NuGet Package.
    // To enable this option, right-click on the project and select the Properties menu item. In the Build tab select "Produce outputs on build".
    public class JsonCacheSerializer : ICacheSerializer
    {
        public JsonCacheSerializer()
            : this(new JsonSerializerSettings(), new JsonSerializerSettings())
        {
        }

        public JsonCacheSerializer(JsonSerializerSettings serializationSettings, JsonSerializerSettings deserializationSettings)
        {
            NotNull(serializationSettings, nameof(serializationSettings));
            NotNull(deserializationSettings, nameof(deserializationSettings));

            this.SerializationSettings = serializationSettings;
            this.DeserializationSettings = deserializationSettings;
        }

        public JsonSerializerSettings DeserializationSettings { get; }

        public JsonSerializerSettings SerializationSettings { get; }

        public object Deserialize(byte[] data, Type target)
        {
            var stringValue = Encoding.UTF8.GetString(data);
            return JsonConvert.DeserializeObject(stringValue, target, this.DeserializationSettings);
        }

        public CacheItem<T> DeserializeCacheItem<T>(byte[] value, Type valueType)
        {
            var valueItemType = typeof(JsonCacheItem<>);
            var closedItemType = valueItemType.MakeGenericType(valueType);
            var jsonItem = Deserialize(value, closedItemType) as JsonCacheItem<T>;
            EnsureNotNull(jsonItem, "Could not deserialize cache item");

            return jsonItem.ToCacheItem();
        }

        public byte[] Serialize<T>(T value)
        {
            var stringValue = JsonConvert.SerializeObject(value, this.SerializationSettings);
            return Encoding.UTF8.GetBytes(stringValue);
        }

        public byte[] SerializeCacheItem<T>(CacheItem<T> value)
        {
            var jsonItem = JsonCacheItem<T>.FromCacheItem(value);

            return this.Serialize(jsonItem);
        }
    }
}
