using CacheManager.Serialization.Json;
using Newtonsoft.Json;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core
{
    /// <summary>
    /// Extensions for the configuration builder for the <c>Newtonsoft.Json</c> based <c>ICacheSerializer</c>.
    /// </summary>
    public static class JsonConfigurationBuilderExtensions
    {
        /// <summary>
        /// Configures the cache manager to use the <code>Newtonsoft.Json</code> based cache serializer.
        /// </summary>
        /// <param name="part">The configuration part.</param>
        /// <returns>The builder instance.</returns>
        public static ConfigurationBuilderCachePart WithJsonSerializer(this ConfigurationBuilderCachePart part)
        {
            NotNull(part, nameof(part));

            return part.WithSerializer(typeof(JsonCacheSerializer));
        }

        /// <summary>
        /// Configures the cache manager to use the <code>Newtonsoft.Json</code> based cache serializer.
        /// </summary>
        /// <param name="part">The configuration part.</param>
        /// <param name="serializationSettings">The settings to be used during serialization.</param>
        /// <param name="deserializationSettings">The settings to be used during deserialization.</param>
        /// <returns>The builder instance.</returns>
        public static ConfigurationBuilderCachePart WithJsonSerializer(this ConfigurationBuilderCachePart part, JsonSerializerSettings serializationSettings, JsonSerializerSettings deserializationSettings)
        {
            NotNull(part, nameof(part));

            return part.WithSerializer(typeof(JsonCacheSerializer), serializationSettings, deserializationSettings);
        }

        /// <summary>
        /// Configures the cache manager to use the <code>Newtonsoft.Json</code> based cache serializer with compression.
        /// </summary>
        /// <param name="part">The configuration part.</param>
        /// <returns>The builder instance.</returns>
        public static ConfigurationBuilderCachePart WithGzJsonSerializer(this ConfigurationBuilderCachePart part)
        {
            NotNull(part, nameof(part));

            return part.WithSerializer(typeof(GzJsonCacheSerializer));
        }

        /// <summary>
        /// Configures the cache manager to use the <code>Newtonsoft.Json</code> based cache serializer with compression.
        /// </summary>
        /// <param name="part">The configuration part.</param>
        /// <param name="serializationSettings">The settings to be used during serialization.</param>
        /// <param name="deserializationSettings">The settings to be used during deserialization.</param>
        /// <returns>The builder instance.</returns>
        public static ConfigurationBuilderCachePart WithGzJsonSerializer(this ConfigurationBuilderCachePart part, JsonSerializerSettings serializationSettings, JsonSerializerSettings deserializationSettings)
        {
            NotNull(part, nameof(part));

            return part.WithSerializer(typeof(GzJsonCacheSerializer), serializationSettings, deserializationSettings);
        }
    }
}