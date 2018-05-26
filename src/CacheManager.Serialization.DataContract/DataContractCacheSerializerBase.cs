using System;
using System.IO;
using System.Runtime.Serialization;
using CacheManager.Core.Internal;

namespace CacheManager.Serialization.DataContract
{
    /// <summary>
    /// Implementing the <see cref="ICacheSerializer"/> contract using <c>System.Runtime.Serialization</c> as a base class.
    /// </summary>
    /// <typeparam name="TSettings">Type of settings for the serializers in the namespace <c>System.Runtime.Serialization</c></typeparam>
    public abstract class DataContractCacheSerializerBase<TSettings> : CacheSerializer
    {
        private static readonly Type _openItemType = typeof(DataContractCacheItem<>);

        /// <summary>
        /// Gets the settings which should be used during deserialization/serialization.
        /// </summary>
        /// <value>The deserialization/serialization settings.</value>
        public TSettings SerializerSettings { get; private set; }

        /// <summary>
        /// Base constructor for the objects that inherit DataContractCacheSerializerBase.
        /// </summary>
        /// <param name="serializerSettings">Serializer's settings</param>
        protected DataContractCacheSerializerBase(TSettings serializerSettings)
        {
            SerializerSettings = serializerSettings;
        }

        /// <inheritdoc/>
        protected override object CreateNewItem<TCacheValue>(ICacheItemProperties properties, object value)
        {
            return new DataContractCacheItem<TCacheValue>(properties, value);
        }

        /// <inheritdoc/>
        protected override Type GetOpenGeneric()
        {
            return _openItemType;
        }

        /// <inheritdoc/>
        public override byte[] Serialize<T>(T value)
        {
            if (value == null)
            {
                return null;
            }

            var serializer = GetSerializer(value.GetType());
            using (var stream = new MemoryStream())
            {
                WriteObject(serializer, stream, value);
                return stream.ToArray();
            }
        }

        /// <inheritdoc/>
        public override object Deserialize(byte[] data, Type target)
        {
            if (data == null)
            {
                return null;
            }

            var serializer = GetSerializer(target);
            using (var stream = new MemoryStream(data))
            {
                return ReadObject(serializer, stream);
            }
        }

        /// <summary>
        /// Deserializes the given <paramref name="stream"/> with the given <paramref name="serializer"/>.
        /// </summary>
        /// <param name="serializer">The serializer that's going to be used for deserialization.</param>
        /// <param name="stream">The stream that's including the serialized data.</param>
        /// <returns>The deserialized object.</returns>
        protected virtual object ReadObject(XmlObjectSerializer serializer, Stream stream)
        {
            return serializer.ReadObject(stream);
        }

        /// <summary>
        /// Serializes the given <paramref name="graph"/> into <paramref name="stream"/> with the given <paramref name="serializer"/>.
        /// </summary>
        /// <param name="serializer">The serializer that's going to be used for serialization.</param>
        /// <param name="stream">The stream that the <paramref name="graph"/> will be serialized into.</param>
        /// <param name="graph">The object that will be serialized.</param>
        protected virtual void WriteObject(XmlObjectSerializer serializer, Stream stream, object graph)
        {
            serializer.WriteObject(stream, graph);
        }

        /// <summary>
        /// Gets the DataContract serializer for the target type.
        /// </summary>
        /// <param name="target">The target type for serializer.</param>
        /// <returns>Returns the serializer.</returns>
        protected abstract XmlObjectSerializer GetSerializer(Type target);
    }
}
