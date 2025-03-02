﻿using System;
using System.IO;
using System.Text;
using CacheManager.Core;
using CacheManager.Core.Internal;
using Microsoft.Extensions.ObjectPool;
using Newtonsoft.Json;

namespace CacheManager.Serialization.Json
{
    /// <summary>
    /// Implements the <c>ICacheSerializer</c> contract using <c>Newtonsoft.Json</c>.
    /// </summary>
    public class JsonCacheSerializer : CacheSerializer
    {
        private static readonly Type _openGenericItemType = typeof(JsonCacheItem<>);
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
            if (serializationSettings is null)
            {
                throw new ArgumentNullException(nameof(serializationSettings));
            }

            if (deserializationSettings is null)
            {
                throw new ArgumentNullException(nameof(deserializationSettings));
            }

            _serializer = JsonSerializer.Create(serializationSettings);
            _deserializer = JsonSerializer.Create(deserializationSettings);
            _stringBuilderPool = new DefaultObjectPool<StringBuilder>(new StringBuilderPooledObjectPolicy());
            SerializationSettings = serializationSettings;
            DeserializationSettings = deserializationSettings;
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
            var value = Encoding.UTF8.GetString(data, 0, data.Length);
            using (var reader = new StringReader(value))
            using (var jsonReader = new JsonTextReader(reader))
            {
                return _deserializer.Deserialize(jsonReader, target);
            }
        }

        /// <inheritdoc/>
        public override byte[] Serialize<T>(T value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var buffer = _stringBuilderPool.Get();

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
            return _openGenericItemType;
        }

        /// <inheritdoc/>
        protected override object CreateNewItem<TCacheValue>(ICacheItemProperties properties, object value)
        {
            return new JsonCacheItem<TCacheValue>(properties, value);
        }
    }
}
