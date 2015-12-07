using System;

namespace CacheManager.Core.Internal
{
    public interface ICacheSerializer
    {
        byte[] Serialize<T>(T value);
        object Deserialize(byte[] data, Type target);
        byte[] SerializeCacheItem<T>(CacheItem<T> value);
        CacheItem<T> DeserializeCacheItem<T>(byte[] value, Type valueType);
    }
}