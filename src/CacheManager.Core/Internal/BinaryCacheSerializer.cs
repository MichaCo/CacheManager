#if !PORTABLE && !DOTNET5_2
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace CacheManager.Core.Internal
{
    public class BinaryCacheSerializer : ICacheSerializer
    {
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

        public CacheItem<T> DeserializeCacheItem<T>(byte[] value, Type valueType)
            => (CacheItem<T>)Deserialize(value, valueType);

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

        public byte[] SerializeCacheItem<T>(CacheItem<T> value)
            => Serialize(value);
    }
}
#endif