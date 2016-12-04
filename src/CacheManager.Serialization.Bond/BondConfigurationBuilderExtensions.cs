using CacheManager.Core;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Serialization.Bond
{
    public static class BondConfigurationBuilderExtensions
    {
        /// <summary>
        /// Configures the cache manager to use the <code>Bond</code> based cache serializer.
        /// </summary>
        /// <param name="part">The configuration part.</param>
        /// <returns>The builder instance.</returns>
        public static ConfigurationBuilderCachePart WithBondSerializer(this ConfigurationBuilderCachePart part)
        {
            NotNull(part, nameof(part));

            return part.WithSerializer(typeof(BondSerializer));
        }
    }
}
