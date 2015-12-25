using System;
using CacheManager.Redis;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core
{
    /// <summary>
    /// Extensions for the configuration builder specific to the redis cache handle.
    /// </summary>
    public static class ConfigurationBuilderExtensions
    {
        /// <summary>
        /// Adds a redis configuration with the given <paramref name="configurationKey"/>.
        /// </summary>
        /// <param name="part">The builder instance.</param>
        /// <param name="configurationKey">
        /// The configuration key which can be used to refernce this configuration by a redis cache handle or backplate.
        /// </param>
        /// <param name="configuration">The redis configuration object.</param>
        /// <returns>The configuration builder.</returns>
        /// <exception cref="System.ArgumentNullException">If <paramref name="configuration"/> or <paramref name="configurationKey"/> are null.</exception>
        public static ConfigurationBuilderCachePart WithRedisConfiguration(this ConfigurationBuilderCachePart part, string configurationKey, Action<RedisConfigurationBuilder> configuration)
        {
            NotNull(configuration, nameof(configuration));

            var builder = new RedisConfigurationBuilder(configurationKey);
            configuration(builder);
            RedisConfigurations.AddConfiguration(builder.Build());
            return part;
        }

        /// <summary>
        /// Adds a redis configuration with the given <paramref name="configurationKey"/>.
        /// </summary>
        /// <param name="part">The builder instance.</param>
        /// <param name="configurationKey">
        /// The configuration key which can be used to refernce this configuration by a redis cache handle or backplate.
        /// </param>
        /// <param name="connectionString">The redis connection string.</param>
        /// <returns>The configuration builder.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="configurationKey"/> or <paramref name="connectionString"/> are null.
        /// </exception>
        public static ConfigurationBuilderCachePart WithRedisConfiguration(this ConfigurationBuilderCachePart part, string configurationKey, string connectionString)
        {
            NotNullOrWhiteSpace(configurationKey, nameof(configurationKey));

            NotNullOrWhiteSpace(connectionString, nameof(connectionString));

            RedisConfigurations.AddConfiguration(new RedisConfiguration(configurationKey, connectionString));
            return part;
        }

#pragma warning disable SA1625
        /// <summary>
        /// Configures a cache back-plate for the cache manager.
        /// The <paramref name="redisConfigurationKey"/> is used to find a matching redis configuration.
        /// <para>
        /// If a back plate is defined, at least one cache handle must be marked as back plate
        /// source. The cache manager then will try to synchronize multiple instances of the same configuration.
        /// </para>
        /// </summary>
        /// <param name="part">The builder instance.</param>
        /// <param name="redisConfigurationKey">
        /// The redis configuration key will be used to find a matching redis connection configuration.
        /// </param>
        /// <returns>The builder instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="redisConfigurationKey"/> is null.</exception>
        public static ConfigurationBuilderCachePart WithRedisBackPlate(this ConfigurationBuilderCachePart part, string redisConfigurationKey)
        {
            NotNull(part, nameof(part));

            return part.WithBackPlate<RedisCacheBackPlate>(redisConfigurationKey);
        }

        /// <summary>
        /// Adds a <see cref="RedisCacheHandle{TCacheValue}"/>.
        /// This handle requires a redis configuration to be defined with the given <paramref name="redisConfigurationKey"/>.
        /// </summary>
        /// <param name="part">The builder instance.</param>
        /// <param name="redisConfigurationKey">
        /// The redis configuration key will be used to find a matching redis connection configuration.
        /// </param>
        /// <returns>The builder instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="redisConfigurationKey"/> is null.</exception>
        public static ConfigurationBuilderCacheHandlePart WithRedisCacheHandle(this ConfigurationBuilderCachePart part, string redisConfigurationKey) =>
            WithRedisCacheHandle(part, redisConfigurationKey, false);

        /// <summary>
        /// Adds a <see cref="RedisCacheHandle{TCacheValue}"/>.
        /// This handle requires a redis configuration to be defined with the given <paramref name="redisConfigurationKey"/>.
        /// </summary>
        /// <param name="part">The builder instance.</param>
        /// <param name="redisConfigurationKey">
        /// The redis configuration key will be used to find a matching redis connection configuration.
        /// </param>
        /// <param name="isBackPlateSource">
        /// Set this to true if this cache handle should be the source of the back plate.
        /// This setting will be ignored if no back plate is configured.
        /// </param>
        /// <returns>The builder instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="redisConfigurationKey"/> is null.</exception>
        public static ConfigurationBuilderCacheHandlePart WithRedisCacheHandle(this ConfigurationBuilderCachePart part, string redisConfigurationKey, bool isBackPlateSource)
        {
            NotNull(part, nameof(part));

            return part.WithHandle(typeof(RedisCacheHandle<>), redisConfigurationKey, isBackPlateSource);
        }
#pragma warning restore SA1625
    }
}