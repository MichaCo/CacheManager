using CacheManager.Serialization.MessagePack;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core
{
    /// <summary>
    /// Extensions for the configuration builder for the <c>MessagePack</c> based <c>ICacheSerializer</c>.
    /// </summary>
    public static class MessagePackConfigurarionBuilderExtensions
    {
        /// <summary>
        /// Configures the cache manager to use the <code>MessagePack</code> based cache serializer.
        /// </summary>
        /// <param name="part">The configuration part.</param>
        /// <returns>The builder instance.</returns>
        public static ConfigurationBuilderCachePart WithMessagePackSerializer(this ConfigurationBuilderCachePart part)
        {
            NotNull(part, nameof(part));

            return part.WithSerializer(typeof(MessagePackCacheSerializer));
        }

        /// <summary>
        /// Configures the cache manager to use the <code>LZ4MessagePack</code> based cache serializer.
        /// </summary>
        /// <param name="part">The configuration part.</param>
        /// <returns>The builder instance.</returns>
        public static ConfigurationBuilderCachePart WithLZ4MessagePackSerializer(this ConfigurationBuilderCachePart part)
        {
            NotNull(part, nameof(part));

            return part.WithSerializer(typeof(LZ4MessagePackCacheSerializer));
        }
    }
}
