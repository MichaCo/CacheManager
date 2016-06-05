using System;
using System.IO;
using System.IO.Compression;
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Is checked by GetString")]
        public override object Deserialize(byte[] data, Type target)
        {
            var compressedData = this.Decompression(data);

            return base.Deserialize(compressedData, target);
        }

        /// <inheritdoc/>
        public override byte[] Serialize<T>(T value)
        {
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
            Guard.NotNull(data, nameof(data));

            using (var bytesBuilder = new MemoryStream())
            {
                using (var gzWriter = new GZipStream(bytesBuilder, CompressionMode.Compress))
                {
                    gzWriter.Write(data, 0, data.Length);
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
            Guard.NotNull(compressedData, nameof(compressedData));

            using (var inputStream = new MemoryStream(compressedData))
            using (var gzReader = new GZipStream(inputStream, CompressionMode.Decompress))
            using (var bytesBuilder = new MemoryStream())
            {
                gzReader.CopyTo(bytesBuilder);
                return bytesBuilder.ToArray();
            }
        }
    }
}
