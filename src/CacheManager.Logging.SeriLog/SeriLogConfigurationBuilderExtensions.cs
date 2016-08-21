using CacheManager.Logging.SeriLog;

namespace CacheManager.Core
{
    public static class SeriLogConfigurationBuilderExtensions
    {
        /// <summary>
        /// Configures the cache manager to use the <code>SeriLog</code> logging factory.
        /// </summary>
        /// <param name="part">The configuration part.</param>
        /// <returns>The builder instance.</returns>
        public static ConfigurationBuilderCachePart WithSeriLogger(this ConfigurationBuilderCachePart part)
        {
            Utility.Guard.NotNull(part, nameof(part));

            return part.WithLogging(typeof(SeriLogFactory));
        }

        public static ConfigurationBuilderCachePart WithSeriLogger(this ConfigurationBuilderCachePart part, string category)
        {
            Utility.Guard.NotNull(part, nameof(part));

            return part.WithLogging(typeof(SeriLogFactory), category);
        }
    }
}
