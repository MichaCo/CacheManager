using System;
using CacheManager.Serialization.Json;
using Newtonsoft.Json;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core
{
    /// <summary>
    /// Extensions for the configuration builder specific to the redis cache handle.
    /// </summary>
    public static class ConfigurationBuilderExtensions
    {
        /// <summary>
        /// Configures the cache manager to use the Newtonsoft.Json based cache serializer.
        /// </summary>
        /// <param name="part">The part.</param>
        /// <returns>The builder instance.</returns>
        public static ConfigurationBuilderCachePart WithJsonSerializer(this ConfigurationBuilderCachePart part)
        {
            NotNull(part, nameof(part));

            return part.WithSerializer(typeof(JsonCacheSerializer));
        }

        /// <summary>
        /// Configures the cache manager to use the Newtonsoft.Json based cache serializer.
        /// </summary>
        /// <param name="serializationSettings">Settings to be used during serialization.</param>
        /// <param name="deserializationSettings">Settings to be used during deserialization.</param>
        /// <param name="part">The part.</param>
        /// <returns>The builder instance.</returns>
        public static ConfigurationBuilderCachePart WithJsonSerializer(this ConfigurationBuilderCachePart part, JsonSerializerSettings serializationSettings, JsonSerializerSettings deserializationSettings)
        {
            NotNull(part, nameof(part));

            return part.WithSerializer(typeof(JsonCacheSerializer), serializationSettings, deserializationSettings);
        }
    }
}