using System;
using CacheManager.Core.Configuration;
using CacheManager.Redis;

namespace CacheManager.Core
{
    /// <summary>
    /// Extensions for the configuration builder specific to the redis cache handle.
    /// </summary>
    public static class ConfigurationBuilderExtensions
    {
        /// <summary>
        /// Adds a redis configuration.
        /// </summary>
        /// <param name="part">The part.</param>
        /// <param name="configurationKey">
        /// The configuration key which has to match with the cache handle name.
        /// </param>
        /// <param name="config">The redis configuration object.</param>
        /// <returns>The configuration builder.</returns>
        /// <exception cref="System.ArgumentNullException">If config is null.</exception>
        public static ConfigurationBuilderCachePart WithRedisConfiguration(this ConfigurationBuilderCachePart part, string configurationKey, Action<RedisConfigurationBuilder> config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            var builder = new RedisConfigurationBuilder(configurationKey);
            config(builder);
            RedisConfigurations.AddConfiguration(builder.Build());
            return part;
        }

        /// <summary>
        /// Adds a redis configuration.
        /// </summary>
        /// <param name="part">The part.</param>
        /// <param name="configurationKey">
        /// The configuration key which has to match with the cache handle name.
        /// </param>
        /// <param name="connectionString">The redis connection string.</param>
        /// <returns>The configuration builder.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// If configurationKey or connectionString are null.
        /// </exception>
        public static ConfigurationBuilderCachePart WithRedisConfiguration(this ConfigurationBuilderCachePart part, string configurationKey, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(configurationKey))
            {
                throw new ArgumentNullException(nameof(configurationKey));
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            RedisConfigurations.AddConfiguration(new RedisConfiguration(configurationKey, connectionString));
            return part;
        }

        /// <summary>
        /// Configures the back plate for the cache manager.
        /// <para>
        /// The <paramref name="redisConfigurationId"/> is used to define the redis configuration,
        /// the back plate should use to connect to the redis server.
        /// </para>
        /// <para>
        /// If a back plate is defined, at least one cache handle must be marked as back plate
        /// source. The cache manager then will try to synchronize multiple instances of the same configuration.
        /// </para>
        /// </summary>
        /// <param name="part">The part.</param>
        /// <param name="redisConfigurationId">
        /// The id of the configuration the back plate should use.
        /// </param>
        /// <returns>The builder instance.</returns>
        public static ConfigurationBuilderCachePart WithRedisBackPlate(this ConfigurationBuilderCachePart part, string redisConfigurationId)
        {
            if (part == null)
            {
                throw new ArgumentNullException(nameof(part));
            }

            return part.WithBackPlate<RedisCacheBackPlate>(redisConfigurationId);
        }

        /// <summary>
        /// Add a <see cref="RedisCacheHandle"/> with the required name.
        /// <para>
        /// This handle requires a redis configuration to be defined with the
        /// <paramref name="redisConfigurationId"/> matching the configuration's id.
        /// </para>
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="redisConfigurationId">
        /// The redis configuration identifier will be used as name for the cache handle and to
        /// retrieve the connection configuration.
        /// </param>
        /// <returns>The part.</returns>
        /// <exception cref="ArgumentNullException">Thrown if handleName is null.</exception>
        public static ConfigurationBuilderCacheHandlePart WithRedisCacheHandle(this ConfigurationBuilderCachePart part, string redisConfigurationId)
        {
            return WithRedisCacheHandle(part, redisConfigurationId, false);
        }

        /// <summary>
        /// Add a <see cref="RedisCacheHandle"/> with the required name.
        /// <para>
        /// This handle requires a redis configuration to be defined with the
        /// <paramref name="redisConfigurationId"/> matching the configuration's id.
        /// </para>
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="redisConfigurationId">
        /// The redis configuration identifier will be used as name for the cache handle and to
        /// retrieve the connection configuration.
        /// </param>
        /// <param name="isBackPlateSource">
        /// Set this to true if this cache handle should be the source of the back plate.
        /// <para>This setting will be ignored if no back plate is configured.</para>
        /// </param>
        /// <returns>The part.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if handleName or handleType are null.
        /// </exception>
        public static ConfigurationBuilderCacheHandlePart WithRedisCacheHandle(this ConfigurationBuilderCachePart part, string redisConfigurationId, bool isBackPlateSource)
        {
            if (part == null)
            {
                throw new ArgumentNullException(nameof(part));
            }

            return part.WithHandle(typeof(RedisCacheHandle<>), redisConfigurationId, isBackPlateSource);
        }
    }
}