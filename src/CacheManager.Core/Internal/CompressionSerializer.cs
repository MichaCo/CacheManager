using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CacheManager.Core.Utility;

namespace CacheManager.Core.Internal
{
    /// <summary>
    /// Wrapper for other serializer to add compression capabilities
    /// </summary>
    public class CompressionSerializer : ICacheSerializer
    {
        /// <summary>
        /// The serializer that we used after decompression and before compression.
        /// </summary>
        public ICacheSerializer InternalSerializer { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompressionSerializer"/> class.
        /// </summary>
        /// <param name="internalSerializer">Serializer that we used after decompression and before compression.</param>
        public CompressionSerializer(ICacheSerializer internalSerializer)
        {
            this.InternalSerializer = internalSerializer;
        }

        /// <inheritdoc/>
        public CacheItem<T> DeserializeCacheItem<T>(byte[] value, Type valueType)
        {
            return InternalSerializer.DeserializeCacheItem<T>(value, valueType);
        }

        /// <inheritdoc/>
        public byte[] SerializeCacheItem<T>(CacheItem<T> value)
        {
            return InternalSerializer.SerializeCacheItem<T>(value);
        }

        /// <inheritdoc/>
        public object Deserialize(byte[] data, Type target)
        {
            Guard.NotNull(data, nameof(data));
            var compressedData = Decompression(data);

            return InternalSerializer.Deserialize(compressedData, target);
        }

        /// <inheritdoc/>
        public byte[] Serialize<T>(T value)
        {
            Guard.NotNull(value, nameof(value));
            var data = InternalSerializer.Serialize<T>(value);

            return Compression(data);
        }

        /// <summary>
        /// Compress the serialized <paramref name="data"/> using <see cref="GZipStream "/>.
        /// </summary>
        /// <param name="data">The data which should be compressed.</param>
        /// <returns>The compressed data.</returns>
        protected virtual byte[] Compression(byte[] data)
        {
            using (var bytesBuilder = new MemoryStream())
            {
                using (var gzWriter = new GZipStream(bytesBuilder, CompressionLevel.Fastest, true))
                {
                    gzWriter.Write(data, 0, data.Length);
                    bytesBuilder.Flush();
                }

                return bytesBuilder.ToArray();
            }
        }

        /// <summary>
        /// Decompress the <paramref name="compressedData"/> into the base serialized data.
        /// </summary>
        /// <param name="compressedData">The data which should be decompressed.</param>
        /// <returns>The uncompressed data.</returns>
        protected virtual byte[] Decompression(byte[] compressedData)
        {
            var buffer = new byte[compressedData.Length * 2];
            using (var inputStream = new MemoryStream(compressedData, 0, compressedData.Length))
            using (var gzReader = new GZipStream(inputStream, CompressionMode.Decompress))
            using (var stream = new MemoryStream(compressedData.Length * 2))
            {
                var readBytes = 0;
                while ((readBytes = gzReader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    stream.Write(buffer, 0, readBytes);
                }

                return stream.ToArray();
            }
        }
    }
}
