using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using CacheManager.Core;
using Microsoft.ApplicationServer.Caching;
using ProtoBuf;

namespace CacheManager.WindowsAzureCaching
{
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Buf", Justification = "Library name")]
    public class ProtoBufDataCacheObjectSerializer<T> : IDataCacheObjectSerializer
    {
        public ProtoBufDataCacheObjectSerializer()
        {
        }

        public object Deserialize(Stream stream)
        {
            return Serializer.Deserialize<CacheItem<T>>(stream);
        }

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