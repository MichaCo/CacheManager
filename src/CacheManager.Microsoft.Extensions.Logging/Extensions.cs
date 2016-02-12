using System;
using CacheManager.Logging;
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
        public static ConfigurationBuilderCachePart WithMicrosoftLogging(this ConfigurationBuilderCachePart part, Action<ILoggerFactory> factory)
        {
            NotNull(part, nameof(part));
            NotNull(factory, nameof(factory));
            var externalFactory = new LoggerFactory();
            factory(externalFactory);
            return part.WithLogging(typeof(MicrosoftLoggerFactoryAdapter), externalFactory);
        }

        /// <summary>
        /// Enables logging for the cache manager instance.
        /// This will add an <see cref="Logging.ILoggerFactory"/> using the <c>Microsoft.Extensions.Logging</c> framework.
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="loggerFactory">The logger factory which should be used.</param>
        /// <returns>The builder.</returns>
        public static ConfigurationBuilderCachePart WithMicrosoftLogging(this ConfigurationBuilderCachePart part, ILoggerFactory loggerFactory)
        {
            NotNull(part, nameof(part));
            NotNull(loggerFactory, nameof(loggerFactory));
            return part.WithLogging(typeof(MicrosoftLoggerFactoryAdapter), loggerFactory);
        }
    }

    /// <summary>
    /// Extensions for the <see cref="CacheManagerConfiguration"/>.
    /// </summary>
    public static class CacheManagerConfigurationExtensions
    {
        /// <summary>
        /// Enables logging for the cache manager instance.
        /// This will add an <see cref="Logging.ILoggerFactory"/> using the <c>Microsoft.Extensions.Logging</c> framework.
        /// <para>
        /// Use the <paramref name="factory"/> to configure the logger factory and add <see cref="ILoggerProvider"/>s as needed.
        /// </para>
        /// </summary>
        /// <param name="configuration">The source configuration.</param>
        /// <param name="factory">The logger factory used for configuring logging.</param>
        /// <returns>The configuration.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "not owning it")]
        public static CacheManagerConfiguration WithMicrosoftLogging(this CacheManagerConfiguration configuration, Action<ILoggerFactory> factory)
        {
            NotNull(configuration, nameof(configuration));
            NotNull(factory, nameof(factory));
            var externalFactory = new LoggerFactory();
            factory(externalFactory);
            configuration.LoggerFactoryType = typeof(MicrosoftLoggerFactoryAdapter);
            configuration.LoggerFactoryTypeArguments = new object[] { externalFactory };
            return configuration;
        }

        /// <summary>
        /// Enables logging for the cache manager instance.
        /// This will add an <see cref="Logging.ILoggerFactory"/> using the <c>Microsoft.Extensions.Logging</c> framework.
        /// </summary>
        /// <param name="configuration">The source configuration.</param>
        /// <param name="loggerFactory">The logger factory which should be used.</param>
        /// <returns>The configuration.</returns>
        public static CacheManagerConfiguration WithMicrosoftLogging(this CacheManagerConfiguration configuration, ILoggerFactory loggerFactory)
        {
            NotNull(configuration, nameof(configuration));
            NotNull(loggerFactory, nameof(loggerFactory));
            configuration.LoggerFactoryType = typeof(MicrosoftLoggerFactoryAdapter);
            configuration.LoggerFactoryTypeArguments = new object[] { loggerFactory };
            return configuration;
        }
    }
}