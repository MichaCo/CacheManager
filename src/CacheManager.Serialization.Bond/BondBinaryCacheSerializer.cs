using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bond;
#if !NOUNSAFE
using Bond.IO.Unsafe;
#else
using Bond.IO.Safe;
#endif
using Bond.Protocols;
using CacheManager.Core;
using CacheManager.Core.Internal;

namespace CacheManager.Serialization.Bond
{
    /// <summary>
    /// Implements the <see cref="ICacheSerializer"/> contract using <c>Microsoft.Bond</c>.
    /// </summary>
    public class BondBinaryCacheSerializer : BondSerializerBase, ICacheSerializer
    {
        private readonly BinarySerializerCache _cache;
        private readonly Dictionary<string, Type> _types = new Dictionary<string, Type>();
        private readonly object _typesLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="BondBinaryCacheSerializer"/> class.
        /// </summary>
        public BondBinaryCacheSerializer(int defaultWriteBufferSize = 1024) : base(defaultWriteBufferSize)
        {
            _cache = new BinarySerializerCache();
        }

        /// <inheritdoc/>
        public byte[] Serialize<T>(T value)
        {
            return Serialize(value, value.GetType());
        }

        private byte[] Serialize(object value, Type type)
        {
            var serializer = _cache.GetSerializer(value.GetType());
            var buffer = LeaseOutputBuffer();
            var writer = _cache.CreateWriter(buffer);

            serializer.Serialize(value, writer);

            var bytes = new byte[buffer.Data.Count];
            Buffer.BlockCopy(buffer.Data.Array, 0, bytes, 0, buffer.Data.Count);
            ReturnOutputBuffer(buffer);
            return bytes;
        }

        /// <inheritdoc/>
        public object Deserialize(byte[] data, Type target)
        {
            var deserializer = _cache.GetDeserializer(target);
            var buffer = new InputBuffer(data);
            var reader = _cache.CreateReader(buffer);

            return deserializer.Deserialize(reader);
        }

        private static readonly Type _tObject = typeof(object);

        /// <inheritdoc/>
        public byte[] SerializeCacheItem<T>(CacheItem<T> value)
        {
            if (typeof(T) == _tObject)
            {
                var valueBytes = this.Serialize(value.Value, value.ValueType);
                var itemType = BondCacheItem<object>.OpenItemType.MakeGenericType(value.ValueType);
                var item = Activator.CreateInstance(itemType, value);

                var wrapper = new BondCacheItemWrapper()
                {
                    ValueType = value.ValueType.AssemblyQualifiedName,
                    Data = this.Serialize(item, itemType)
                };

                return this.Serialize(wrapper);
            }
            else
            {
                var bondCacheItem = new BondCacheItem<T>(value);
                return this.Serialize(bondCacheItem);
            }
        }

        /// <inheritdoc/>
        public CacheItem<T> DeserializeCacheItem<T>(byte[] value, Type valueType = null)
        {
            if (typeof(T) == _tObject)
            {
                var wrapper = (BondCacheItemWrapper)this.Deserialize(value, typeof(BondCacheItemWrapper));
                var targetValueType = valueType ?? GetType(wrapper.ValueType);
                var itemType = BondCacheItem<object>.OpenItemType.MakeGenericType(targetValueType);
                var obj = Deserialize(wrapper.Data, itemType);

#if NET40
                var methodInfo = itemType.GetMethod("ToObjectCacheItem");
#else
                var methodInfo = itemType.GetTypeInfo().GetDeclaredMethod("ToObjectCacheItem");
#endif

                var cacheItem = (CacheItem<T>)methodInfo.Invoke(obj, new object[0]);
                return cacheItem;
            }
            else
            {
                var bondCacheItem = (BondCacheItem<T>)Deserialize(value, typeof(BondCacheItem<T>));

                return bondCacheItem.ToCacheItem();
            }
        }

        private class BinarySerializerCache : SerializerCache<CompactBinaryWriter<OutputBuffer>, CompactBinaryReader<InputBuffer>>
        {
            public override CompactBinaryReader<InputBuffer> CreateReader(InputBuffer buffer)
            {
                return new CompactBinaryReader<InputBuffer>(buffer);
            }

            public override CompactBinaryWriter<OutputBuffer> CreateWriter(OutputBuffer buffer)
            {
                return new CompactBinaryWriter<OutputBuffer>(buffer);
            }

            protected override Deserializer<CompactBinaryReader<InputBuffer>> CreateDeserializer(Type type)
            {
                return new Deserializer<CompactBinaryReader<InputBuffer>>(type);
            }

            protected override Serializer<CompactBinaryWriter<OutputBuffer>> CreateSerializer(Type type)
            {
                return new Serializer<CompactBinaryWriter<OutputBuffer>>(type);
            }
        }

        private class FastSerializerCache : SerializerCache<FastBinaryWriter<OutputBuffer>, FastBinaryReader<InputBuffer>>
        {
            public override FastBinaryReader<InputBuffer> CreateReader(InputBuffer buffer)
            {
                return new FastBinaryReader<InputBuffer>(buffer);
            }

            public override FastBinaryWriter<OutputBuffer> CreateWriter(OutputBuffer buffer)
            {
                return new FastBinaryWriter<OutputBuffer>(buffer);
            }

            protected override Deserializer<FastBinaryReader<InputBuffer>> CreateDeserializer(Type type)
            {
                return new Deserializer<FastBinaryReader<InputBuffer>>(type);
            }

            protected override Serializer<FastBinaryWriter<OutputBuffer>> CreateSerializer(Type type)
            {
                return new Serializer<FastBinaryWriter<OutputBuffer>>(type);
            }
        }

        private Type GetType(string type)
        {
            if (!this._types.ContainsKey(type))
            {
                lock (this._typesLock)
                {
                    if (!this._types.ContainsKey(type))
                    {
                        var typeResult = Type.GetType(type, false);
                        if (typeResult == null)
                        {
                            // fixing an issue for corlib types if mixing net core clr and full clr calls
                            // (e.g. typeof(string) is different for those two, either System.String, System.Private.CoreLib or System.String, mscorlib)
                            var typeName = type.Split(',').FirstOrDefault();
                            typeResult = Type.GetType(typeName, true);
                        }

                        this._types.Add(type, typeResult);
                    }
                }
            }

            return (Type)this._types[type];
        }
    }
}