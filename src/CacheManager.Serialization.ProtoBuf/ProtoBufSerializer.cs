using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CacheManager.Core;
using CacheManager.Core.Internal;
using ProtoBuf;

namespace CacheManager.Serialization.ProtoBuf
{
    /// <summary>
    /// Implements the <see cref="ICacheSerializer"/> contract using <c>ProtoBuf</c>.
    /// </summary>
    public class ProtoBufSerializer : ICacheSerializer
    {
        private static readonly Type cacheItemType = typeof(ProtoBufCacheItem);
        private readonly Dictionary<string, Type> types = new Dictionary<string, Type>();
        private readonly object typesLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="ProtoBufSerializer"/> class.
        /// </summary>
        public ProtoBufSerializer()
        {
        }

        /// <inheritdoc/>
        public object Deserialize(byte[] data, Type target)
        {
            byte[] destination;
            if (data.Length == 0)
            {
                destination = data;
            }
            else
            {
                destination = new byte[data.Length - 1];
                Array.Copy(data, 1, destination, 0, data.Length - 1);
            }

            using (var stream = new MemoryStream(destination))
            {
                return Serializer.Deserialize(target, stream);
            }
        }

        /// <inheritdoc/>
        public CacheItem<T> DeserializeCacheItem<T>(byte[] value, Type valueType = null)
        {
            var item = (ProtoBufCacheItem)Deserialize(value, cacheItemType);
            if (item == null)
            {
                throw new Exception("Unable to deserialize the CacheItem");
            }

            var cachedValue = Deserialize(item.Value, valueType ?? this.GetType(item.ValueType));
            return item.ToCacheItem<T>(cachedValue);
        }

        /// <inheritdoc/>
        public byte[] Serialize<T>(T value)
        {
            byte[] output = null;
            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, value);
                output = stream.ToArray();
            }

            // Protobuf returns an empty byte array {} which would be treated as Null value in redis
            // this is not allowed in cache manager and would cause issues (would look like the item does not exist)
            // we'll simply add a prefix byte and remove it before deserialization.
            var prefix = new byte[] { 1 };
            return prefix.Concat(output).ToArray();
        }

        /// <inheritdoc/>
        public byte[] SerializeCacheItem<T>(CacheItem<T> value)
        {
            var cachedValue = Serialize(value.Value);
            var pbCacheItem = ProtoBufCacheItem.FromCacheItem(value, cachedValue);

            return Serialize(pbCacheItem);
        }

        private Type GetType(string type)
        {
            if (!this.types.ContainsKey(type))
            {
                lock (this.typesLock)
                {
                    if (!this.types.ContainsKey(type))
                    {
                        var typeResult = Type.GetType(type, false);
                        if (typeResult == null)
                        {
                            // fixing an issue for corlib types if mixing net core clr and full clr calls
                            // (e.g. typeof(string) is different for those two, either System.String, System.Private.CoreLib or System.String, mscorlib)
                            var typeName = type.Split(',').FirstOrDefault();
                            typeResult = Type.GetType(typeName, true);
                        }

                        this.types.Add(type, typeResult);
                    }
                }
            }

            return this.types[type];
        }
    }
}