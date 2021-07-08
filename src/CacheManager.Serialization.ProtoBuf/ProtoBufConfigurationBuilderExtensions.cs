using CacheManager.Serialization.ProtoBuf;

using Microsoft.IO;

namespace CacheManager.Core
{
    /// <summary>
    /// Configuration builder extensions for the <c>ProtoBuf</c> based <see cref="CacheManager.Core.Internal.ICacheSerializer"/>.
    /// </summary>
    public static class ProtoBufConfigurationBuilderExtensions
    {
        /// <summary>
        /// Configures the cache manager to use the <code>ProtoBuf</code> based cache serializer.
        /// </summary>
        /// <param name="part">The configuration part.</param>
        /// <returns>The builder instance.</returns>
        public static ConfigurationBuilderCachePart WithProtoBufSerializer(this ConfigurationBuilderCachePart part)
        {
            Utility.Guard.NotNull(part, nameof(part));

            return part.WithSerializer(typeof(ProtoBufSerializer));
        }

        /// <summary>
        /// Configures the cache manager to use the <code>ProtoBuf</code> based cache serializer.
        /// </summary>
        /// <param name="part">The configuration part.</param>
        /// <param name="recyclableMemoryStreamManager">The memory stream manager to use for the serialization streams</param>
        /// <returns>The builder instance.</returns>
        public static ConfigurationBuilderCachePart WithProtoBufSerializer(
            this ConfigurationBuilderCachePart part,
#pragma warning disable CS3001 // Argument type is not CLS-compliant
            RecyclableMemoryStreamManager recyclableMemoryStreamManager)
#pragma warning restore CS3001 // Argument type is not CLS-compliant
        {
            Utility.Guard.NotNull(part, nameof(part));

            return part.WithSerializer(typeof(ProtoBufSerializer), recyclableMemoryStreamManager);
        }
    }
}
