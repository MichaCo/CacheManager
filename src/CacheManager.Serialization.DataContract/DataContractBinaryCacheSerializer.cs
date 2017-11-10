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
