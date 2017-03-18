using System;
using System.Linq;
using CacheManager.Core.Utility;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions to read cache manager configurations from ASP.NET Core configuration and add it to the DI framework.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds one cache manager configuration as singleton to the DI framework reading it from <paramref name="fromConfiguration"/>.
        /// This overload will throw in case there are multiple cache manager configurations defined.
        /// </summary>
        /// <param name="collection">The services collection.</param>
        /// <param name="fromConfiguration">The configuration with a cacheManagers section.</param>
        /// <returns>The services collection</returns>
        public static IServiceCollection AddCacheManagerConfiguration(this IServiceCollection collection, IConfiguration fromConfiguration)
        {
            Guard.NotNull(fromConfiguration, nameof(fromConfiguration));
            var configuration = fromConfiguration.GetCacheConfiguration();
            collection.AddSingleton(configuration);
            return collection;
        }

        /// <summary>
        /// Adds one named cache manager configuration as singleton to the DI framework reading it from <paramref name="fromConfiguration"/>.
        /// </summary>
        /// <param name="collection">The services collection.</param>
        /// <param name="fromConfiguration">The configuration with a cacheManagers section.</param>
        /// <param name="name">The name used in the configuration.</param>
        /// <returns>The services collection</returns>
        public static IServiceCollection AddCacheManagerConfiguration(this IServiceCollection collection, IConfiguration fromConfiguration, string name)
        {
            Guard.NotNull(fromConfiguration, nameof(fromConfiguration));
            var configuration = fromConfiguration.GetCacheConfiguration(name);
            collection.AddSingleton(configuration);
            return collection;
        }

        /// <summary>
        /// Adds one cache manager configuration as singleton to the DI framework reading it from <paramref name="fromConfiguration"/>.
        /// This overload will throw in case there are multiple cache manager configurations defined.
        /// </summary>
        /// <param name="collection">The services collection.</param>
        /// <param name="fromConfiguration">The configuration with a cacheManagers section.</param>
        /// <param name="configure">Can be used to further configure the configuration.</param>
        /// <returns>The services collection</returns>
        public static IServiceCollection AddCacheManagerConfiguration(this IServiceCollection collection, IConfiguration fromConfiguration, Action<CacheManager.Core.ConfigurationBuilder> configure)
        {
            Guard.NotNull(fromConfiguration, nameof(fromConfiguration));
            Guard.NotNull(configure, nameof(configure));

            var configuration = fromConfiguration.GetCacheConfiguration();
            configure(configuration.Builder);
            collection.AddSingleton(configuration);
            return collection;
        }

        /// <summary>
        /// Adds one named cache manager configuration as singleton to the DI framework reading it from <paramref name="fromConfiguration"/>.
        /// </summary>
        /// <param name="collection">The services collection.</param>
        /// <param name="fromConfiguration">The configuration with a cacheManagers section.</param>
        /// <param name="name">The name used in the configuration.</param>
        /// <param name="configure">Can be used to further configure the configuration.</param>
        /// <returns>The services collection</returns>
        public static IServiceCollection AddCacheManagerConfiguration(this IServiceCollection collection, IConfiguration fromConfiguration, string name, Action<CacheManager.Core.ConfigurationBuilder> configure)
        {
            Guard.NotNull(fromConfiguration, nameof(fromConfiguration));
            Guard.NotNull(configure, nameof(configure));

            var configuration = fromConfiguration.GetCacheConfiguration(name);
            configure(configuration.Builder);
            collection.AddSingleton(configuration);
            return collection;
        }
    }
}