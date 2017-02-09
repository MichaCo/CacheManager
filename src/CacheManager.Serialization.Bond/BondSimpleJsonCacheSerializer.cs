using System;
using System.IO;
using System.Linq;
using System.Text;
using Bond;
#if !NOUNSAFE
using Bond.IO.Unsafe;
#else
using Bond.IO.Safe;
#endif
using Bond.Protocols;
using CacheManager.Core.Internal;

namespace CacheManager.Serialization.Bond
{
    /// <summary>
    /// Implements the <see cref="ICacheSerializer"/> contract using <c>Microsoft.Bond</c>.
    /// </summary>
    public class BondSimpleJsonCacheSerializer : BondSerializerBase, ICacheSerializer
    {
        private readonly SimpleJsonSerializerCache _cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="BondCompactBinaryCacheSerializer"/> class.
        /// </summary>
        public BondSimpleJsonCacheSerializer(int defaultWriteBufferSize = 1024) : base(defaultWriteBufferSize)
        {
            _cache = new SimpleJsonSerializerCache();
        }

        /// <inheritdoc/>
        protected override byte[] Serialize(object value, Type type)
        {
            var serializer = _cache.GetSerializer(value.GetType());
            var buffer = StringBuilderPool.Lease();

            using (var stringWriter = new StringWriter(buffer))
            {
                var writer = new SimpleJsonWriter(stringWriter);
                serializer.Serialize(value, writer);

                var bytes = Encoding.UTF8.GetBytes(buffer.ToString());
                StringBuilderPool.Return(buffer);
                return bytes;
            }
        }

        //var length = Encoding.UTF8.GetByteCount(chars, 0, charLength);
        //var bytes = ByteArrayPool.Rent(length);
        //Encoding.UTF8.GetBytes(chars, 0, charLength, bytes, 0);
        //Buffer = new ArraySegment<byte>(bytes, 0, length);

        /// <inheritdoc/>
        public override object Deserialize(byte[] data, Type target)
        {
            var deserializer = _cache.GetDeserializer(target);

            string value = Encoding.UTF8.GetString(data, 0, data.Length);
            using (var reader = new StringReader(value))
            {
                var jsonReader = new SimpleJsonReader(reader);
                return deserializer.Deserialize(jsonReader);
            }
        }

        private class SimpleJsonSerializerCache : SerializerCache<SimpleJsonWriter, SimpleJsonReader>
        {
            public override SimpleJsonReader CreateReader(InputBuffer buffer)
            {
                throw new NotImplementedException();
            }

            public override SimpleJsonWriter CreateWriter(OutputBuffer buffer)
            {
                throw new NotImplementedException();
            }

            protected override Deserializer<SimpleJsonReader> CreateDeserializer(Type type)
            {
                return new Deserializer<SimpleJsonReader>(type);
            }

            protected override Serializer<SimpleJsonWriter> CreateSerializer(Type type)
            {
                return new Serializer<SimpleJsonWriter>(type);
            }
        }
    }
}