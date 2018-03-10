using System;
using System.Runtime.Serialization;
using CacheManager.Core;
using CacheManager.Core.Internal;
using System.IO;

namespace CacheManager.Serialization.DataContract
{
    /// <summary>
    /// Implementing the <see cref="ICacheSerializer"/> contract using <c>System.Runtime.Serialization</c> as a base class.
    /// </summary>
    /// <typeparam name="TSettings">Type of settings for the serializers in the namespace <c>System.Runtime.Serialization</c></typeparam>
    public abstract class DataContractCacheSerializerBase<TSettings> : ICacheSerializer
    {
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
            this.SerializerSettings = serializerSettings;
        }

        /// <inheritdoc/>
        public object Deserialize(byte[] data, Type target)
        {
            if (data == null)
            {
                return null;
            }

            var serializer = this.GetSerializer(target);
            using (MemoryStream stream = new MemoryStream(data))
            {
                return this.ReadObject(serializer, stream);
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

        /// <inheritdoc/>
        public CacheItem<T> DeserializeCacheItem<T>(byte[] value, Type valueType)
        //=> (CacheItem<T>)this.Deserialize(value, valueType);
        {
            var contractItem = (DataContractCacheItem)this.Deserialize(value, typeof(DataContractCacheItem));
            //EnsureNotNull(jsonItem, "Could not deserialize cache item");

            var deserializedValue = this.Deserialize(contractItem.Value, valueType);

            return contractItem.ToCacheItem<T>(deserializedValue);
        }

        /// <inheritdoc/>
        public byte[] Serialize<T>(T value)
        {
            if (value == null)
            {
                return null;
            }

            var serializer = this.GetSerializer(value.GetType());
            using (MemoryStream stream = new MemoryStream())
            {
                this.WriteObject(serializer, stream, value);
                return stream.ToArray();
            }
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

        /// <inheritdoc/>
        public byte[] SerializeCacheItem<T>(CacheItem<T> value)
        {
            if (value == null)
            {
                return null;
            }

            var contractValue = this.Serialize(value.Value);
            var contractItem = DataContractCacheItem.FromCacheItem(value, contractValue);

            return this.Serialize(contractItem);
        }
        /// <summary>
        /// Gets the DataContract serializer for the target type.
        /// </summary>
        /// <param name="target">The target type for serializer.</param>
        /// <returns>Returns the serializer.</returns>
        protected abstract XmlObjectSerializer GetSerializer(Type target);
    }
}
