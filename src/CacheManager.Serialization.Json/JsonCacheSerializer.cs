using System;
using System.IO;
using System.Linq;
using System.Text;
using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.Core.Utility;
using Newtonsoft.Json;

namespace CacheManager.Serialization.Json
{
    /// <summary>
    /// Implements the <see cref="ICacheSerializer"/> contract using <c>Newtonsoft.Json</c>.
    /// </summary>
    public class JsonCacheSerializer : CacheSerializer
    {
        private static readonly Type OpenGenericItemType = typeof(JsonCacheItem<>);
        private readonly ObjectPool<StringBuilder> _stringBuilderPool;
        private readonly JsonSerializer _deserializer;
        private readonly JsonSerializer _serializer;

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
            Guard.NotNull(serializationSettings, nameof(serializationSettings));
            Guard.NotNull(deserializationSettings, nameof(deserializationSettings));

            _serializer = JsonSerializer.Create(serializationSettings);
            _deserializer = JsonSerializer.Create(deserializationSettings);
            _stringBuilderPool = new ObjectPool<StringBuilder>(new StringBuilderPoolPolicy(100));
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
        public override object Deserialize(byte[] data, Type target)
        {
            string value = Encoding.UTF8.GetString(data, 0, data.Length);
            using (var reader = new StringReader(value))
            using (var jsonReader = new JsonTextReader(reader))
            {
                return _deserializer.Deserialize(jsonReader, target);
            }
        }

        /// <inheritdoc/>
        public override byte[] Serialize<T>(T value)
        {
            var buffer = _stringBuilderPool.Lease();

            using (var stringWriter = new JsonTextWriter(new StringWriter(buffer)))
            {
                _serializer.Serialize(stringWriter, value, value.GetType());

                var bytes = Encoding.UTF8.GetBytes(buffer.ToString());
                _stringBuilderPool.Return(buffer);
                return bytes;
            }
        }

        /// <inheritdoc/>
        protected override Type GetOpenGeneric()
        {
            return OpenGenericItemType;
        }

        /// <inheritdoc/>
        protected override object CreateNewItem<TCacheValue>(ICacheItemProperties properties, object value)
        {
            return new JsonCacheItem<TCacheValue>(properties, value);
        }

        private class StringBuilderPoolPolicy : IObjectPoolPolicy<StringBuilder>
        {
            private readonly int _defaultBufferSize;

            public StringBuilderPoolPolicy(int defaultBufferSize)
            {
                _defaultBufferSize = defaultBufferSize;
            }

            public StringBuilder CreateNew()
            {
                return new StringBuilder(_defaultBufferSize);
            }

            public bool Return(StringBuilder value)
            {
                //if (value.Data.Count > _defaultBufferSize * 1000)
                //{
                //    return false;
                //}

                value.Clear();
                return true;
            }
        }
    }
}