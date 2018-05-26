using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace CacheManager.Serialization.DataContract
{
    /// <summary>
    /// This class (de)serializes objects with binary format via using <c>DataContractSerializer</c>.
    /// </summary>
    public class DataContractBinaryCacheSerializer : DataContractCacheSerializer
    {
        /// <summary>
        /// Creates instance of <c>DataContractBinaryCacheSerializer</c>.
        /// </summary>
        public DataContractBinaryCacheSerializer() : this(new DataContractSerializerSettings())
        {
        }

        /// <summary>
        /// Creates instance of <c>DataContractBinaryCacheSerializer</c>.
        /// </summary>
        /// <param name="serializerSettings">The settings for <c>DataContractSerializer</c>.</param>
        public DataContractBinaryCacheSerializer(DataContractSerializerSettings serializerSettings = null) : base(serializerSettings)
        {
        }

        /// <inheritdoc/>
        protected override object ReadObject(XmlObjectSerializer serializer, Stream stream)
        {
            var binaryDictionaryReader = XmlDictionaryReader.CreateBinaryReader(stream, new XmlDictionaryReaderQuotas());
            return serializer.ReadObject(binaryDictionaryReader);
        }

        /// <inheritdoc/>
        protected override void WriteObject(XmlObjectSerializer serializer, Stream stream, object graph)
        {
            var binaryDictionaryWriter = XmlDictionaryWriter.CreateBinaryWriter(stream);
            serializer.WriteObject(binaryDictionaryWriter, graph);
            binaryDictionaryWriter.Flush();
        }
    }
}
