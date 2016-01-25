using System;
using CacheManager.AspNetCore.Logging;
using Microsoft.Extensions.Logging;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core
{
    /// <summary>
    /// Extensions for the configuration builder for logging.
    /// </summary>
    public static class ConfigurationBuilderExtensions
    {
        /// <summary>
        /// Enables logging for the cache manager instance.
        /// This will add an <see cref="Logging.ILoggerFactory"/> using the <c>Microsoft.Extensions.Logging</c> framework.
        /// <para>
        /// Use the <paramref name="factory"/> to configure the logger factory and add <see cref="ILoggerProvider"/>s as needed.
        /// </para>
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="factory">The logger factory used for configuring logging.</param>
        /// <returns>The builder.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "not owning it")]
        public static ConfigurationBuilderCachePart WithAspNetLogging(this ConfigurationBuilderCachePart part, Action<ILoggerFactory> factory)
        {
            NotNull(part, nameof(part));
            NotNull(factory, nameof(factory));
            var loggerFactory = new AspNetLoggerFactory();
            factory(loggerFactory);
            return part.WithLogging(loggerFactory);
        }
    }
}