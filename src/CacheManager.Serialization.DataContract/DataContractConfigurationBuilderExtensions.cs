using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using CacheManager.Serialization.DataContract;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core
{
    /// <summary>
    /// Extensions for the configuration builder for the <c>DataContract</c> based <c>ICacheSerializer</c>.
    /// </summary>
    public static class DataContractConfigurationBuilderExtensions
    {
        /// <summary>
        /// Configures the cache manager to use the <code>DataContract</code> based cache serializer.
        /// </summary>
        /// <param name="part">The configuration part.</param>
        /// <param name="serializerSettings">Settings for the serializer.</param>
        /// <returns>The builder instance.</returns>
        public static ConfigurationBuilderCachePart WithDataContractSerializer(this ConfigurationBuilderCachePart part, DataContractSerializerSettings serializerSettings = null)
        {
            NotNull(part, nameof(part));

            if (serializerSettings == null)
            {
                return part.WithSerializer(typeof(DataContractCacheSerializer));
            }
            else
            {
                return part.WithSerializer(typeof(DataContractCacheSerializer), serializerSettings);
            }
        }

        /// <summary>
        /// Configures the cache manager to use the <code>DataContract</code> based cache serializer in Json format.
        /// </summary>
        /// <param name="part">The configuration part.</param>
        /// <param name="serializerSettings">Settings for the serializer.</param>
        /// <returns>The builder instance.</returns>
        public static ConfigurationBuilderCachePart WithDataContractJsonSerializer(this ConfigurationBuilderCachePart part, DataContractJsonSerializerSettings serializerSettings = null)
        {
            NotNull(part, nameof(part));

            if (serializerSettings == null)
            {
                return part.WithSerializer(typeof(DataContractJsonCacheSerializer));
            }
            else
            {
                return part.WithSerializer(typeof(DataContractJsonCacheSerializer), serializerSettings);
            }
        }

        /// <summary>
        /// Configures the cache manager to use the <code>DataContract</code> based cache serializer in Json format with compression.
        /// </summary>
        /// <param name="part">The configuration part.</param>
        /// <param name="serializerSettings">Settings for the serializer.</param>
        /// <returns>The builder instance.</returns>
        public static ConfigurationBuilderCachePart WithDataContractGzJsonSerializer(this ConfigurationBuilderCachePart part, DataContractJsonSerializerSettings serializerSettings = null)
        {
            NotNull(part, nameof(part));

            if (serializerSettings == null)
            {
                return part.WithSerializer(typeof(DataContractGzJsonCacheSerializer));
            }
            else
            {
                return part.WithSerializer(typeof(DataContractGzJsonCacheSerializer), serializerSettings);
            }
        }

        /// <summary>
        /// Configures the cache manager to use the <code>DataContract</code> based cache serializer in binary format.
        /// </summary>
        /// <param name="part">The configuration part.</param>
        /// <param name="serializerSettings">Settings for the serializer.</param>
        /// <returns>The builder instance.</returns>
        public static ConfigurationBuilderCachePart WithDataContractBinarySerializer(this ConfigurationBuilderCachePart part, DataContractSerializerSettings serializerSettings = null)
        {
            NotNull(part, nameof(part));

            if (serializerSettings == null)
            {
                return part.WithSerializer(typeof(DataContractBinaryCacheSerializer));
            }
            else
            {
                return part.WithSerializer(typeof(DataContractBinaryCacheSerializer), serializerSettings);
            }
        }
    }
}
