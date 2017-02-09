using System;

namespace CacheManager.Core.Internal
{
    /// <summary>
    /// Defines the contract for serialization of the cache value and cache items.
    /// The cache item serialization should be separated in case the serialization
    /// technology does not support immutable objects; in that case <see cref="CacheItem{T}"/> might not
    /// be serializable directly and the implementation has to wrap the cache item.
    /// </summary>
    public interface ICacheSerializer
    {
        /// <summary>
        /// Serializes the given <paramref name="value"/> and returns the serialized data as byte array.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="value">The value to serialize.</param>
        /// <returns>The serialization result</returns>
        byte[] Serialize<T>(T value);

        /// <summary>
        /// Deserializes the <paramref name="data"/> into the given <paramref name="target"/> <c>Type</c>.
        /// </summary>
        /// <param name="data">The data which should be deserialized.</param>
        /// <param name="target">The type of the object to deserialize into.</param>
        /// <returns>The deserialized object.</returns>
        object Deserialize(byte[] data, Type target);

        /// <summary>
        /// Serializes the given <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="T">The type of the cache value.</typeparam>
        /// <param name="value">The value to serialize.</param>
        /// <returns>The serialized result.</returns>
        byte[] SerializeCacheItem<T>(CacheItem<T> value);

        /// <summary>
        /// Deserializes the <paramref name="value"/> into a <see cref="CacheItem{T}"/>.
        /// The <paramref name="valueType"/> must not match the <typeparamref name="T"/> in case
        /// <typeparamref name="T"/> is <c>object</c> for example, the <paramref name="valueType"/>
        /// might be the real type of the value. This is needed to properly deserialize in some cases.
        /// </summary>
        /// <typeparam name="T">The type of the cache value.</typeparam>
        /// <param name="value">The data to deserialize from.</param>
        /// <param name="valueType">The type of the actual serialized cache value.</param>
        /// <returns>The deserialized cache item.</returns>
        CacheItem<T> DeserializeCacheItem<T>(byte[] value, Type valueType);
    }
}