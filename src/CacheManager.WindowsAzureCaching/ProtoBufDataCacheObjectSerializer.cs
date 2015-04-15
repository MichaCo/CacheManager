using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using CacheManager.Core;
using Microsoft.ApplicationServer.Caching;
using ProtoBuf;

namespace CacheManager.WindowsAzureCaching
{
    /// <summary>
    /// Serializer implementation based on Protobuf.
    /// </summary>
    /// <typeparam name="T">The object type.</typeparam>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Buf", Justification = "Library name")]
    public class ProtoBufDataCacheObjectSerializer<T> : IDataCacheObjectSerializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProtoBufDataCacheObjectSerializer{T}"/> class.
        /// </summary>
        public ProtoBufDataCacheObjectSerializer()
        {
        }

        /// <summary>
        /// Deserializes a memory stream to an object.
        /// </summary>
        /// <param name="stream">The memory stream returned from the cache.</param>
        /// <returns>
        /// Returns <see cref="T:System.Object" />.
        /// </returns>
        public object Deserialize(Stream stream)
        {
            return Serializer.Deserialize<CacheItem<T>>(stream);
        }

        /// <summary>
        /// Serializes an object to a memory stream.
        /// </summary>
        /// <param name="stream">A memory stream to use to store the serialized object.</param>
        /// <param name="value">The object to serialize.</param>
        /// <exception cref="System.InvalidOperationException">
        /// If value is null or of wrong type
        /// or
        /// we cannot serialize the object with Protobuf.net
        /// or
        /// protobuf.net doesn't support T.
        /// </exception>
        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "Protobuf", Justification = "Library name")]
        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "Protobuf", Justification = "Library name")]
        public void Serialize(Stream stream, object value)
        {
            var item = value as CacheItem<T>;
            if (item == null)
            {
                throw new InvalidOperationException("Value is null or of wrong type.");
            }

            if (!Serializer.NonGeneric.CanSerialize(typeof(CacheItem<T>)))
            {
                throw new InvalidOperationException("Cannot serialize the object with Protobuf.net. " + typeof(CacheItem<T>));
            }

            try
            {
                Serializer.Serialize<CacheItem<T>>(stream, item);
            }
            catch (InvalidOperationException ex)
            {
                // TODO: error msg could chane by the lib, anyways this is just for better understanding the issue...
                if (ex.Message.Equals("No serializer defined for type: System.Object"))
                {
                    throw new InvalidOperationException("Protobuf.net doesn't support T:object. Maybe specify a concrete type or use a different serializer.");
                }

                throw;
            }
        }
    }
}