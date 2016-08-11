using CacheManager.Serialization.ProtoBuf;

namespace CacheManager.Core
{
    public static class ProtoBufConfigurationBuilderExtensions
    {
        /// <summary>
        /// Configures the cache manager to use the <code>ProtoBuf</code> based cache serializer.
        /// </summary>
        /// <param name="part">The configuration part.</param>
        /// <returns>The builder instance.</returns>
        public static ConfigurationBuilderCachePart WithProtoBufSerializer(this ConfigurationBuilderCachePart part)
        {
            Utility.Guard.NotNull<ConfigurationBuilderCachePart>(part, nameof(part));

            return part.WithSerializer(typeof(ProtoBufSerializer));
        }
    }
}
