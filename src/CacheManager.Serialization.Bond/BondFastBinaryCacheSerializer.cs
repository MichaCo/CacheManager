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
    public class BondFastBinaryCacheSerializer : BondSerializerBase
    {
        private readonly FastBinarySerializerCache _cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="BondFastBinaryCacheSerializer"/> class.
        /// </summary>
        public BondFastBinaryCacheSerializer() : base()
        {
            _cache = new FastBinarySerializerCache();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BondFastBinaryCacheSerializer"/> class.
        /// </summary>
        /// <param name="defaultWriteBufferSize">The default buffer size.</param>
        public BondFastBinaryCacheSerializer(int defaultWriteBufferSize) : base(defaultWriteBufferSize)
        {
            _cache = new FastBinarySerializerCache();
        }

        /// <inheritdoc/>
        public override byte[] Serialize<T>(T value)
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

        private class FastBinarySerializerCache : SerializerCache<FastBinaryWriter<OutputBuffer>, FastBinaryReader<InputBuffer>>
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
    }
}