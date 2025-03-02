using System;
using CacheManager.Core;
using CacheManager.Core.Utility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions to read cache manager configurations from ASP.NET Core configuration and add it to the DI framework.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a new <see cref="ICacheManagerConfiguration"/> as singleton to the DI framework.
        /// </summary>
        /// <param name="collection">The services collection.</param>
        /// <param name="configure">The <see cref="CacheManager.Core.CacheConfigurationBuilder"/> used for defining the <see cref="ICacheManagerConfiguration"/>.</param>
        /// <param name="name">The (optional) name to be used for the <see cref="ICacheManagerConfiguration"/>.</param>
        public static IServiceCollection AddCacheManagerConfiguration(this IServiceCollection collection, Action<CacheManager.Core.CacheConfigurationBuilder> configure, string name = null)
        {
            Guard.NotNull(collection, nameof(collection));
            Guard.NotNull(configure, nameof(configure));
            var builder = string.IsNullOrWhiteSpace(name) ?
                new CacheManager.Core.CacheConfigurationBuilder() :
                new CacheManager.Core.CacheConfigurationBuilder(name);

            configure(builder);
            collection.AddSingleton(builder.Build());
            return collection;
        }

        /// <summary>
        /// Adds one <see cref="ICacheManagerConfiguration"/> as singleton to the DI framework reading it from <paramref name="fromConfiguration"/>.
        /// This overload will throw in case there are multiple cache manager configurations defined.
        /// </summary>
        /// <param name="collection">The services collection.</param>
        /// <param name="fromConfiguration">The <see cref="IConfiguration"/> section which contains a <c>cacheManagers</c> section.</param>
        /// <returns>The services collection</returns>
        public static IServiceCollection AddCacheManagerConfiguration(this IServiceCollection collection, IConfiguration fromConfiguration)
        {
            Guard.NotNull(collection, nameof(collection));
            Guard.NotNull(fromConfiguration, nameof(fromConfiguration));
            var configuration = fromConfiguration.GetCacheConfiguration();
            collection.AddSingleton(configuration);
            return collection;
        }

        /// <summary>
        /// Adds one named <see cref="ICacheManagerConfiguration"/> as singleton to the DI framework reading it from <paramref name="fromConfiguration"/>.
        /// </summary>
        /// <param name="collection">The services collection.</param>
        /// <param name="fromConfiguration">The <see cref="IConfiguration"/> section which contains a <c>cacheManagers</c> section.</param>
        /// <param name="name">The name used in the configuration.</param>
        /// <returns>The services collection</returns>
        public static IServiceCollection AddCacheManagerConfiguration(this IServiceCollection collection, IConfiguration fromConfiguration, string name)
        {
            Guard.NotNull(collection, nameof(collection));
            Guard.NotNull(fromConfiguration, nameof(fromConfiguration));
            var configuration = fromConfiguration.GetCacheConfiguration(name);
            collection.AddSingleton(configuration);
            return collection;
        }

        /// <summary>
        /// Adds one <see cref="ICacheManagerConfiguration"/> as singleton to the DI framework reading it from <paramref name="fromConfiguration"/>.
        /// This overload will throw in case there are multiple cache manager configurations defined.
        /// </summary>
        /// <param name="collection">The services collection.</param>
        /// <param name="fromConfiguration">The <see cref="IConfiguration"/> section which contains a <c>cacheManagers</c> section.</param>
        /// <param name="configure">Can be used to further configure the configuration.</param>
        /// <returns>The services collection</returns>
        public static IServiceCollection AddCacheManagerConfiguration(this IServiceCollection collection, IConfiguration fromConfiguration, Action<CacheManager.Core.CacheConfigurationBuilder> configure)
        {
            Guard.NotNull(collection, nameof(collection));
            Guard.NotNull(fromConfiguration, nameof(fromConfiguration));
            Guard.NotNull(configure, nameof(configure));

            var configuration = fromConfiguration.GetCacheConfiguration();
            configure(configuration.Builder);
            collection.AddSingleton(configuration);
            return collection;
        }

        /// <summary>
        /// Adds one named <see cref="ICacheManagerConfiguration"/> as singleton to the DI framework reading it from <paramref name="fromConfiguration"/>.
        /// </summary>
        /// <param name="collection">The services collection.</param>
        /// <param name="fromConfiguration">The <see cref="IConfiguration"/> section which contains a <c>cacheManagers</c> section.</param>
        /// <param name="name">The name used in the configuration.</param>
        /// <param name="configure">Can be used to further configure the configuration.</param>
        /// <returns>The services collection</returns>
        public static IServiceCollection AddCacheManagerConfiguration(this IServiceCollection collection, IConfiguration fromConfiguration, string name, Action<CacheManager.Core.CacheConfigurationBuilder> configure)
        {
            Guard.NotNull(collection, nameof(collection));
            Guard.NotNull(fromConfiguration, nameof(fromConfiguration));
            Guard.NotNull(configure, nameof(configure));

            var configuration = fromConfiguration.GetCacheConfiguration(name);
            configure(configuration.Builder);
            collection.AddSingleton(configuration);
            return collection;
        }

        /// <summary>
        /// Adds a singleton open generic service for <see cref="ICacheManager{TCacheValue}"/> to the <see cref="IServiceCollection"/>.
        /// <para>
        /// This requires one <see cref="ICacheManagerConfiguration"/> to be registered.
        /// </para>
        /// </summary>
        /// <remarks>
        /// With this setup, you can inject <see cref="ICacheManager{TCacheValue}"/> with any kind ot <c>T</c> to your controllers and the DI framework will resolve a new singleton instance for each type.
        /// </remarks>
        /// <param name="collection">The services collection.</param>
        /// <returns>The services collection.</returns>
        public static IServiceCollection AddCacheManager(this IServiceCollection collection)
        {
            Guard.NotNull(collection, nameof(collection));
            collection.AddSingleton(typeof(ICacheManager<>), typeof(BaseCacheManager<>));

            return collection;
        }

        /// <summary>
        /// Adds a singleton service for <see cref="ICacheManager{TCacheValue}"/> for the specified <typeparamref name="T"/> to the <see cref="IServiceCollection"/>.
        /// <para>
        /// This requires at least one <see cref="ICacheManagerConfiguration"/> to be registered. 
        /// If more than one <see cref="ICacheManagerConfiguration"/>s are registered, use <paramref name="configurationName"/> to specify which one to use.
        /// </para>
        /// </summary>
        /// <param name="collection">The services collection.</param>
        /// <param name="fromConfiguration">The <see cref="IConfiguration"/> section which contains a <c>cacheManagers</c> section.</param>
        /// <param name="configurationName">The name of the <see cref="ICacheManagerConfiguration"/> to use.</param>
        /// <param name="configure">Can be used to further configure the <see cref="ICacheManagerConfiguration"/>.</param>
        /// <returns>The services collection.</returns>
        public static IServiceCollection AddCacheManager<T>(this IServiceCollection collection, IConfiguration fromConfiguration, string configurationName = null, Action<CacheManager.Core.CacheConfigurationBuilder> configure = null)
        {
            Guard.NotNull(collection, nameof(collection));
            Guard.NotNull(fromConfiguration, nameof(fromConfiguration));

            collection.AddSingleton<ICacheManager<T>, BaseCacheManager<T>>((provider) =>
            {
                var loggerFactory = provider.GetService<ILoggerFactory>();
                var configuration = string.IsNullOrWhiteSpace(configurationName) ? fromConfiguration.GetCacheConfiguration() : fromConfiguration.GetCacheConfiguration(configurationName);

                configure?.Invoke(configuration.Builder);

                return new BaseCacheManager<T>(configuration, loggerFactory);
            });

            return collection;
        }

        /// <summary>
        /// Adds a singleton service for <see cref="ICacheManager{TCacheValue}"/> for the specified <typeparamref name="T"/> to the <see cref="IServiceCollection"/> 
        /// using the inline configuration defined by <paramref name="configure"/>.
        /// </summary>
        /// <param name="collection">The services collection.</param>
        /// <param name="configure">Used to configure the instance of <see cref="ICacheManager{TCacheValue}"/>.</param>
        /// <param name="name">The (optional) name for the <see cref="ICacheManagerConfiguration"/>.</param>
        /// <returns>The services collection.</returns>
        public static IServiceCollection AddCacheManager<T>(this IServiceCollection collection, Action<CacheManager.Core.CacheConfigurationBuilder> configure, string name = null)
        {
            Guard.NotNull(collection, nameof(collection));
            Guard.NotNull(configure, nameof(configure));

            collection.AddSingleton<ICacheManager<T>, BaseCacheManager<T>>((provider) =>
            {
                Guard.NotNull(configure, nameof(configure));
                var builder = string.IsNullOrWhiteSpace(name) ?
                    new CacheManager.Core.CacheConfigurationBuilder() :
                    new CacheManager.Core.CacheConfigurationBuilder(name);

                configure(builder);

                var loggerFactory = provider.GetService<ILoggerFactory>();
                return new BaseCacheManager<T>(builder.Build(), loggerFactory);
            });

            return collection;
        }
    }
}
