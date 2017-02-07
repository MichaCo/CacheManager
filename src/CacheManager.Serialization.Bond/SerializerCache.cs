using System;
using System.Collections.Concurrent;
using System.Linq;
using Bond;
#if !NOUNSAFE
using Bond.IO.Unsafe;
#else
using Bond.IO.Safe;
#endif

namespace CacheManager.Serialization.Bond
{
    internal abstract class SerializerCache<TWriter, TReader>
    {
        private static readonly ConcurrentDictionary<Type, Serializer<TWriter>> _serializers = new ConcurrentDictionary<Type, Serializer<TWriter>>();
        private static readonly ConcurrentDictionary<Type, Deserializer<TReader>> _deserializers = new ConcurrentDictionary<Type, Deserializer<TReader>>();

        public Serializer<TWriter> GetSerializer(Type type)
        {
            Serializer<TWriter> serializer;
            if (!_serializers.TryGetValue(type, out serializer))
            {
                serializer = CreateSerializer(type);
                _serializers.TryAdd(type, serializer);
            }

            return serializer;
        }

        public Deserializer<TReader> GetDeserializer(Type type)
        {
            Deserializer<TReader> deserializer;
            if (!_deserializers.TryGetValue(type, out deserializer))
            {
                deserializer = CreateDeserializer(type);
                _deserializers.TryAdd(type, deserializer);
            }

            return deserializer;
        }

        public abstract TWriter CreateWriter(OutputBuffer buffer);

        public abstract TReader CreateReader(InputBuffer buffer);

        protected abstract Serializer<TWriter> CreateSerializer(Type type);

        protected abstract Deserializer<TReader> CreateDeserializer(Type type);
    }
}