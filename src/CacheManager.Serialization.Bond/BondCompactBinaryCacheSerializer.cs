using System;
using System.Linq;
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
    public class BondCompactBinaryCacheSerializer : BondSerializerBase, ICacheSerializer
    {
        private readonly BinarySerializerCache _cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="BondCompactBinaryCacheSerializer"/> class.
        /// </summary>
        public BondCompactBinaryCacheSerializer(int defaultWriteBufferSize = 1024) : base(defaultWriteBufferSize)
        {
            _cache = new BinarySerializerCache();
        }

        /// <inheritdoc/>
        protected override byte[] Serialize(object value, Type type)
        {
            var serializer = _cache.GetSerializer(value.GetType());
            var buffer = OutputBufferPool.Lease();
            var writer = _cache.CreateWriter(buffer);

            serializer.Serialize(value, writer);

            var bytes = new byte[buffer.Data.Count];
            Buffer.BlockCopy(buffer.Data.Array, 0, bytes, 0, buffer.Data.Count);
            OutputBufferPool.Return(buffer);
            return bytes;
        }

        /// <inheritdoc/>
        public override object Deserialize(byte[] data, Type target)
        {
            var deserializer = _cache.GetDeserializer(target);
            var buffer = new InputBuffer(data);
            var reader = _cache.CreateReader(buffer);

            return deserializer.Deserialize(reader);
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
    }
}