using System;
using System.Linq;
using CacheManager.Core;
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

        /// <summary>
        /// Adds the open generic CacheManager service for <see cref="ICacheManager{TCacheValue}"/>.
        /// <para>
        /// This requires a <see cref="ICacheManagerConfiguration"/> to be registered. Use one of the <see cref="AddCacheManagerConfiguration(IServiceCollection, IConfiguration)"/> overloads or manually register one.
        /// </para>
        /// </summary>
        /// <remarks>
        /// With this setup, you can inject <see cref="ICacheManager{TCacheValue}"/> to your controllers.
        /// <para>
        /// This will create a new singleton instance of CacheManager for every type.
        /// </para>
        /// </remarks>
        /// <param name="collection">The services collection.</param>
        /// <returns>The services collection.</returns>
        public static IServiceCollection AddCacheManager(this IServiceCollection collection)
        {
            collection.AddSingleton(typeof(ICacheManager<>), typeof(BaseCacheManager<>));

            return collection;
        }

        /// <summary>
        /// Adds a CacheManager service for <see cref="ICacheManager{TCacheValue}"/> for the specified <typeparamref name="T"/>.
        /// <para>
        /// This requires a <see cref="ICacheManagerConfiguration"/> to be registered unless you pass in <paramref name="fromConfiguration"/>. 
        /// The <paramref name="name"/> and <paramref name="configure"/> is also optional. If <paramref name="name"/> is specified, the configuration for that name will be used.
        /// If <paramref name="configure"/> is specified, the configuration will be passed into the action the moment the CacheManager gets initialized.
        /// </para>
        /// </summary>
        /// <remarks>
        /// <para>
        /// Important to note, this overload does a lazy initialization of the configuration, the moment <see cref="ICacheManager{TCacheValue}"/> gets instantiated the first time.
        /// </para>
        /// With this setup, you can inject <see cref="ICacheManager{TCacheValue}"/> to your controllers.
        /// <para>
        /// This will create one singleton instance of CacheManager for the given type <typeparamref name="T"/>.
        /// </para>
        /// </remarks>
        /// <param name="collection">The services collection.</param>
        /// <param name="fromConfiguration">The configuration with a cacheManagers section.</param>
        /// <param name="name">The name used in the configuration.</param>
        /// <param name="configure">Can be used to further configure the configuration.</param>
        /// <returns>The services collection.</returns>
        public static IServiceCollection AddCacheManager<T>(this IServiceCollection collection, IConfiguration fromConfiguration = null, string name = null, Action<CacheManager.Core.ConfigurationBuilder> configure = null)
        {
            collection.AddSingleton<ICacheManager<T>, BaseCacheManager<T>>((provider) =>
            {
                var configuration = string.IsNullOrWhiteSpace(name) ? fromConfiguration.GetCacheConfiguration() : fromConfiguration.GetCacheConfiguration(name);

                configure?.Invoke(configuration.Builder);

                return new BaseCacheManager<T>(configuration);
            });

            return collection;
        }
    }
}