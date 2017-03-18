#if !NETSTANDARD
using System;
using System.IO;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using CacheManager.Core.Utility;

namespace CacheManager.Core.Internal
{
    /// <summary>
    /// Basic binary serialization implementation of the <see cref="ICacheSerializer"/>.
    /// This implementation will be used in case no other serializer is configured for the cache manager
    /// and serialization is needed (only distributed caches will have to serialize the cache value).
    /// Binary serialization will not be available in some environments.
    /// </summary>
    public class BinaryCacheSerializer : ICacheSerializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryCacheSerializer"/> class.
        /// </summary>
        /// <param name="serializationFormatter">The formatter to use to do the serialization.</param>
        /// <param name="deserializationFormatter">The formatter to use to do the deserialization.</param>
        public BinaryCacheSerializer(BinaryFormatter serializationFormatter, BinaryFormatter deserializationFormatter)
        {
            Guard.NotNull(serializationFormatter, nameof(serializationFormatter));
            Guard.NotNull(deserializationFormatter, nameof(deserializationFormatter));

            SerializationFormatter = serializationFormatter;
            DeserializationFormatter = deserializationFormatter;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryCacheSerializer"/> class.
        /// </summary>
        public BinaryCacheSerializer()
        {
            DeserializationFormatter = SerializationFormatter = new BinaryFormatter()
            {
                AssemblyFormat = FormatterAssemblyStyle.Simple
            };
        }

        /// <summary>
        /// Gets the formatter which should be used during deserialization.
        /// If nothing is specified the default <see cref="BinaryFormatter"/> will be used.
        /// </summary>
        /// <value>The deserialization formatter.</value>
        public BinaryFormatter DeserializationFormatter { get; }

        /// <summary>
        /// Gets the formatter which should be used during serialization.
        /// If nothing is specified the default <see cref="BinaryFormatter"/> will be used.
        /// </summary>
        /// <value>The serialization formatter.</value>
        public BinaryFormatter SerializationFormatter { get; }

        /// <inheritdoc/>
        public object Deserialize(byte[] data, Type target)
        {
            if (data == null)
            {
                return null;
            }

            using (var memoryStream = new MemoryStream(data))
            {
                return DeserializationFormatter.Deserialize(memoryStream);
            }
        }

        /// <inheritdoc/>
        public CacheItem<T> DeserializeCacheItem<T>(byte[] value, Type valueType)
            => (CacheItem<T>)Deserialize(value, valueType);

        /// <inheritdoc/>
        public byte[] Serialize<T>(T value)
        {
            if (value == null)
            {
                return null;
            }

            using (var memoryStream = new MemoryStream())
            {
                SerializationFormatter.Serialize(memoryStream, value);
                var objectDataAsStream = memoryStream.ToArray();
                return objectDataAsStream;
            }
        }

        /// <inheritdoc/>
        public byte[] SerializeCacheItem<T>(CacheItem<T> value)
            => Serialize(value);
    }
}
#endif