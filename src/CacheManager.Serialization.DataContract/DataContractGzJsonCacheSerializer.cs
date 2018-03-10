using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace CacheManager.Serialization.DataContract
{
    /// <summary>
    /// This class (de)compresses the (de)serialized output of <c>DataContractJsonCacheSerializer</c>.
    /// </summary>
    public class DataContractGzJsonCacheSerializer : DataContractJsonCacheSerializer
    {
        /// <summary>
        /// Creates instance of <c>DataContractGzJsonCacheSerializer</c>.
        /// </summary>
        public DataContractGzJsonCacheSerializer() : this(new DataContractJsonSerializerSettings())
        {
        }

        /// <summary>
        /// Creates instance of <c>DataContractGzJsonCacheSerializer</c>.
        /// </summary>
        /// <param name="serializerSettings">Serializer's settings</param>
        public DataContractGzJsonCacheSerializer(DataContractJsonSerializerSettings serializerSettings = null) : base(serializerSettings)
        {
        }

        /// <inheritdoc/>
        protected override void WriteObject(XmlObjectSerializer serializer, Stream stream, object graph)
        {
            using (GZipStream gzipStream = new GZipStream(stream, CompressionMode.Compress, true))
            {
                base.WriteObject(serializer, gzipStream, graph);
                gzipStream.Flush();
            }
        }

        /// <inheritdoc/>
        protected override object ReadObject(XmlObjectSerializer serializer, Stream stream)
        {
            using (GZipStream gzipStream = new GZipStream(stream, CompressionMode.Decompress))
            {
                return base.ReadObject(serializer, gzipStream);
            }
        }
    }
}
