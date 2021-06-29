using System;
using System.Linq;
using CacheManager.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core
{
    /// <summary>
    /// Extensions for the configuration builder for logging.
    /// </summary>
    public static class MicrosoftLoggingBuilderExtensions
    {
        /// <summary>
        /// Enables logging for the cache manager instance using an existing <c>Microsoft.Extensions.Logging.ILoggerFactory</c> as target.
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="loggerFactory">The logger factory which should be used.</param>
        /// <returns>The builder.</returns>
        public static ConfigurationBuilderCachePart WithMicrosoftLogging(this ConfigurationBuilderCachePart part, ILoggerFactory loggerFactory)
        {
            NotNull(part, nameof(part));
            NotNull(loggerFactory, nameof(loggerFactory));
            return part.WithLogging(typeof(MicrosoftLoggerFactoryAdapter), new Func<ILoggerFactory>(() => loggerFactory));
        }
    }
}
