using System;
using System.Linq;
using CacheManager.Core.Utility;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="fromConfiguration"></param>
        /// <returns></returns>
        public static IServiceCollection AddCacheManagerConfiguration(this IServiceCollection collection, IConfiguration fromConfiguration)
        {
            Guard.NotNull(fromConfiguration, nameof(fromConfiguration));
            var configuration = fromConfiguration.GetCacheConfiguration();
            collection.AddSingleton(configuration);
            return collection;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="fromConfiguration"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IServiceCollection AddCacheManagerConfiguration(this IServiceCollection collection, IConfiguration fromConfiguration, string name)
        {
            Guard.NotNull(fromConfiguration, nameof(fromConfiguration));
            var configuration = fromConfiguration.GetCacheConfiguration(name);
            collection.AddSingleton(configuration);
            return collection;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="fromConfiguration"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
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
        ///
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="fromConfiguration"></param>
        /// <param name="name"></param>
        /// <param name="configure"></param>
        /// <returns></returns>
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