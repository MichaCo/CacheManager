#if !NETSTANDARD
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

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
        /// <inheritdoc/>
        public object Deserialize(byte[] data, Type target)
        {
            if (data == null)
            {
                return null;
            }

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                return binaryFormatter.Deserialize(memoryStream);
            }
        }

        /// <inheritdoc/>
        public CacheItem<T> DeserializeCacheItem<T>(byte[] value, Type valueType)
            => (CacheItem<T>)this.Deserialize(value, valueType);

        /// <inheritdoc/>
        public byte[] Serialize<T>(T value)
        {
            if (value == null)
            {
                return null;
            }

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                binaryFormatter.Serialize(memoryStream, value);
                byte[] objectDataAsStream = memoryStream.ToArray();
                return objectDataAsStream;
            }
        }

        /// <inheritdoc/>
        public byte[] SerializeCacheItem<T>(CacheItem<T> value)
            => this.Serialize(value);
    }
}
#endif