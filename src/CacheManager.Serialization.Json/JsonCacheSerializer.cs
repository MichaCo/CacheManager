using System;
using System.Text;
using CacheManager.Core;
using CacheManager.Core.Internal;
using Newtonsoft.Json;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Serialization.Json
{
    /// <summary>
    /// Implements the <see cref="ICacheSerializer"/> contract using <c>Newtonsoft.Json</c>.
    /// </summary>
    public class JsonCacheSerializer : ICacheSerializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonCacheSerializer"/> class.
        /// </summary>
        public JsonCacheSerializer()
            : this(new JsonSerializerSettings(), new JsonSerializerSettings())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonCacheSerializer"/> class.
        /// With this overload the settings for de-/serialization can be set independently.
        /// </summary>
        /// <param name="serializationSettings">The settings which should be used during serialization.</param>
        /// <param name="deserializationSettings">The settings which should be used during deserialization.</param>
        public JsonCacheSerializer(JsonSerializerSettings serializationSettings, JsonSerializerSettings deserializationSettings)
        {
            NotNull(serializationSettings, nameof(serializationSettings));
            NotNull(deserializationSettings, nameof(deserializationSettings));

            this.SerializationSettings = serializationSettings;
            this.DeserializationSettings = deserializationSettings;
        }

        /// <summary>
        /// Gets the settings which should be used during deserialization.
        /// If nothing is specified the default <see cref="JsonSerializerSettings"/> will be used.
        /// </summary>
        /// <value>The deserialization settings.</value>
        public JsonSerializerSettings DeserializationSettings { get; }

        /// <summary>
        /// Gets the settings which should be used during serialization.
        /// If nothing is specified the default <see cref="JsonSerializerSettings"/> will be used.
        /// </summary>
        /// <value>The serialization settings.</value>
        public JsonSerializerSettings SerializationSettings { get; }

        /// <inheritdoc/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Is checked by GetString")]
        public virtual object Deserialize(byte[] data, Type target)
        {
            var stringValue = Encoding.UTF8.GetString(data, 0, data.Length);
            return JsonConvert.DeserializeObject(stringValue, target, this.DeserializationSettings);
        }

        /// <inheritdoc/>
        public CacheItem<T> DeserializeCacheItem<T>(byte[] value, Type valueType)
        {
            var jsonItem = (JsonCacheItem)this.Deserialize(value, typeof(JsonCacheItem));
            EnsureNotNull(jsonItem, "Could not deserialize cache item");

            var deserializedValue = this.Deserialize(jsonItem.Value, valueType);

            return jsonItem.ToCacheItem<T>(deserializedValue);
        }

        /// <inheritdoc/>
        public virtual byte[] Serialize<T>(T value)
        {
            var stringValue = JsonConvert.SerializeObject(value, this.SerializationSettings);
            return Encoding.UTF8.GetBytes(stringValue);
        }

        /// <inheritdoc/>
        public byte[] SerializeCacheItem<T>(CacheItem<T> value)
        {
            NotNull(value, nameof(value));
            var jsonValue = this.Serialize(value.Value);
            var jsonItem = JsonCacheItem.FromCacheItem(value, jsonValue);

            return this.Serialize(jsonItem);
        }
    }
}