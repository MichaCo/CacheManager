using CacheManager.Serialization.Bond;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core
{
    /// <summary>
    /// Extensions for the configuration builder for the <code>Microsoft.Bond</code> based <see cref="CacheManager.Core.Internal.ICacheSerializer"/>.
    /// </summary>
    public static class BondConfigurationBuilderExtensions
    {
        /// <summary>
        /// Configures the cache manager to use the <code>Microsoft.Bond</code> based cache serializer.
        /// This version uses <see cref="Bond.Protocols.CompactBinaryWriter{O}"/> and <see cref="Bond.Protocols.CompactBinaryReader{I}"/>.
        /// </summary>
        /// <param name="part">The configuration part.</param>
        /// <param name="defaultWriteBufferSize">The buffer size used to serialize objects. Can be used to tune Bond performance.</param>
        /// <returns>The builder instance.</returns>
        public static ConfigurationBuilderCachePart WithBondCompactBinarySerializer(this ConfigurationBuilderCachePart part, int defaultWriteBufferSize = 1024)
        {
            NotNull(part, nameof(part));

            return part.WithSerializer(typeof(BondCompactBinaryCacheSerializer), defaultWriteBufferSize);
        }

        /// <summary>
        /// Configures the cache manager to use the <code>Microsoft.Bond</code> based cache serializer.
        /// This version uses <see cref="Bond.Protocols.FastBinaryWriter{O}"/> and <see cref="Bond.Protocols.FastBinaryReader{I}"/>.
        /// </summary>
        /// <param name="part">The configuration part.</param>
        /// <param name="defaultWriteBufferSize">The buffer size used to serialize objects. Can be used to tune Bond performance.</param>
        /// <returns>The builder instance.</returns>
        public static ConfigurationBuilderCachePart WithBondFastBinarySerializer(this ConfigurationBuilderCachePart part, int defaultWriteBufferSize = 1024)
        {
            NotNull(part, nameof(part));

            return part.WithSerializer(typeof(BondFastBinaryCacheSerializer), defaultWriteBufferSize);
        }

        /// <summary>
        /// Configures the cache manager to use the <code>Microsoft.Bond</code> based cache serializer.
        /// This version uses <see cref="Bond.Protocols.FastBinaryWriter{O}"/> and <see cref="Bond.Protocols.FastBinaryReader{I}"/>.
        /// </summary>
        /// <param name="part">The configuration part.</param>
        /// <returns>The builder instance.</returns>
        public static ConfigurationBuilderCachePart WithBondSimpleJsonSerializer(this ConfigurationBuilderCachePart part)
        {
            NotNull(part, nameof(part));

            return part.WithSerializer(typeof(BondSimpleJsonCacheSerializer));
        }
    }
}