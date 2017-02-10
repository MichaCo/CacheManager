using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using CacheManager.Core.Internal;
using CacheManager.Core.Utility;
using Newtonsoft.Json;

namespace CacheManager.Serialization.Json
{
    /// <summary>
    /// Implements the <see cref="ICacheSerializer"/> contract using <c>Newtonsoft.Json</c> and the <see cref="GZipStream "/> loseless compression.
    /// </summary>
    public class GzJsonCacheSerializer : JsonCacheSerializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GzJsonCacheSerializer"/> class.
        /// </summary>
        public GzJsonCacheSerializer()
            : base(new JsonSerializerSettings(), new JsonSerializerSettings())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GzJsonCacheSerializer"/> class.
        /// With this overload the settings for de-/serialization can be set independently.
        /// </summary>
        /// <param name="serializationSettings">The settings which should be used during serialization.</param>
        /// <param name="deserializationSettings">The settings which should be used during deserialization.</param>
        public GzJsonCacheSerializer(JsonSerializerSettings serializationSettings, JsonSerializerSettings deserializationSettings)
            : base(serializationSettings, deserializationSettings)
        {
        }

        /// <inheritdoc/>
        public override object Deserialize(byte[] data, Type target)
        {
            Guard.NotNull(data, nameof(data));
            var compressedData = this.Decompression(data);

            return base.Deserialize(compressedData, target);
        }

        /// <inheritdoc/>
        public override byte[] Serialize<T>(T value)
        {
            Guard.NotNull(value, nameof(value));
            var data = base.Serialize<T>(value);

            return this.Compression(data);
        }

        /// <summary>
        /// Compress the serialized <paramref name="data"/> using <see cref="GZipStream "/>.
        /// </summary>
        /// <param name="data">The data which should be compressed.</param>
        /// <returns>The compressed data.</returns>
        protected virtual byte[] Compression(byte[] data)
        {
            var buffer = new byte[data.Length];
            using (var bytesBuilder = new MemoryStream(buffer))
            {
#if NET40
                using (var gzWriter = new GZipStream(bytesBuilder, CompressionMode.Compress, true))
#else
                using (var gzWriter = new GZipStream(bytesBuilder, CompressionLevel.Fastest, true))
#endif
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
            byte[] buffer = new byte[compressedData.Length * 2];
            using (var inputStream = new MemoryStream(compressedData, 0, compressedData.Length))
            using (var gzReader = new GZipStream(inputStream, CompressionMode.Decompress))
            using (var stream = new MemoryStream(compressedData.Length * 2))
            {
                int readBytes = 0;
                while ((readBytes = gzReader.Read(buffer, 0, buffer.Length)) > 0)
                {
                    stream.Write(buffer, 0, readBytes);
                }

                return stream.ToArray();
            }
        }
    }
}