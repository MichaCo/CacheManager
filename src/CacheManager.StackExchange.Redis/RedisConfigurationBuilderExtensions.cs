using System;
using CacheManager.Redis;
using StackExchange.Redis;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core
{
    /// <summary>
    /// Extensions for the configuration builder specific to the redis cache handle.
    /// </summary>
    public static class RedisConfigurationBuilderExtensions
    {
        /// <summary>
        /// Adds a redis configuration with the given <paramref name="configurationKey"/>.
        /// </summary>
        /// <param name="part">The builder instance.</param>
        /// <param name="configurationKey">
        /// The configuration key which can be used to refernce this configuration by a redis cache handle or backplane.
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
        /// The configuration key which can be used to refernce this configuration by a redis cache handle or backplane.
        /// </param>
        /// <param name="connectionString">The redis connection string.</param>
        /// <param name="database">The redis database to be used.</param>
        /// <param name="enableKeyspaceNotifications">
        /// Enables keyspace notifications to react on eviction/expiration of items.
        /// Make sure that all servers are configured correctly and 'notify-keyspace-events' is at least set to 'Exe', otherwise CacheManager will not retrieve any events.
        /// See <see href="https://redis.io/topics/notifications#configuration"/> for configuration details.
        /// </param>
        /// <returns>The configuration builder.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="configurationKey"/> or <paramref name="connectionString"/> are null.
        /// </exception>
        public static ConfigurationBuilderCachePart WithRedisConfiguration(this ConfigurationBuilderCachePart part, string configurationKey, string connectionString, int database = 0, bool enableKeyspaceNotifications = false)
        {
            NotNullOrWhiteSpace(configurationKey, nameof(configurationKey));

            NotNullOrWhiteSpace(connectionString, nameof(connectionString));

            RedisConfigurations.AddConfiguration(new RedisConfiguration(configurationKey, connectionString, database, enableKeyspaceNotifications));
            return part;
        }

        /// <summary>
        /// Adds an existing <see cref="IConnectionMultiplexer"/> to the cache manager configuration which can be referenced by redis cache handle and/or backplane.
        /// </summary>
        /// <param name="part">The builder instance.</param>
        /// <param name="configurationKey">
        /// The configuration key which can be used to refernce this configuration by a redis cache handle or backplane.
        /// </param>
        /// <param name="redisClient">The connection multiplexer instance.</param>
        /// <param name="database">The redis database to use for caching.</param>
        /// <param name="enableKeyspaceNotifications">
        /// Enables keyspace notifications to react on eviction/expiration of items.
        /// Make sure that all servers are configured correctly and 'notify-keyspace-events' is at least set to 'Exe', otherwise CacheManager will not retrieve any events.
        /// See <see href="https://redis.io/topics/notifications#configuration"/> for configuration details.
        /// </param>
        /// <returns>The configuration builder.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="configurationKey"/> or <paramref name="redisClient"/> are null.
        /// </exception>
        [CLSCompliant(false)]
        public static ConfigurationBuilderCachePart WithRedisConfiguration(this ConfigurationBuilderCachePart part, string configurationKey, IConnectionMultiplexer redisClient, int database = 0, bool enableKeyspaceNotifications = false)
        {
            NotNullOrWhiteSpace(configurationKey, nameof(configurationKey));

            NotNull(redisClient, nameof(redisClient));

            var connectionString = redisClient.Configuration;
            part.WithRedisConfiguration(configurationKey, connectionString, database, enableKeyspaceNotifications);

            RedisConnectionManager.AddConnection(connectionString, redisClient);

            return part;
        }

#pragma warning disable SA1625

        /// <summary>
        /// Configures a cache backplane for the cache manager.
        /// The <paramref name="redisConfigurationKey"/> is used to find a matching redis configuration.
        /// <para>
        /// If a backplane is defined, at least one cache handle must be marked as backplane
        /// source. The cache manager then will try to synchronize multiple instances of the same configuration.
        /// </para>
        /// </summary>
        /// <param name="part">The builder instance.</param>
        /// <param name="redisConfigurationKey">
        /// The redis configuration key will be used to find a matching redis connection configuration.
        /// </param>
        /// <returns>The builder instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="redisConfigurationKey"/> is null.</exception>
        public static ConfigurationBuilderCachePart WithRedisBackplane(this ConfigurationBuilderCachePart part, string redisConfigurationKey)
        {
            NotNull(part, nameof(part));

            return part.WithBackplane(typeof(RedisCacheBackplane), redisConfigurationKey);
        }

        /// <summary>
        /// Configures a cache backplane for the cache manager.
        /// The <paramref name="redisConfigurationKey"/> is used to find a matching redis configuration.
        /// <para>
        /// If a backplane is defined, at least one cache handle must be marked as backplane
        /// source. The cache manager then will try to synchronize multiple instances of the same configuration.
        /// </para>
        /// </summary>
        /// <param name="part">The builder instance.</param>
        /// <param name="redisConfigurationKey">
        /// The redis configuration key will be used to find a matching redis connection configuration.
        /// </param>
        /// <param name="channelName">The pub sub channel name the backplane should use.</param>
        /// <returns>The builder instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="redisConfigurationKey"/> is null.</exception>
        public static ConfigurationBuilderCachePart WithRedisBackplane(this ConfigurationBuilderCachePart part, string redisConfigurationKey, string channelName)
        {
            NotNull(part, nameof(part));

            return part.WithBackplane(typeof(RedisCacheBackplane), redisConfigurationKey, channelName);
        }
        
        /// <summary>
        /// Adds a <see cref="RedisCacheHandle{TCacheValue}"/>.
        /// This handle requires a redis configuration to be defined with the given <paramref name="redisConfigurationKey"/>.
        /// </summary>
        /// <param name="part">The builder instance.</param>
        /// <param name="redisConfigurationKey">
        /// The redis configuration key will be used to find a matching redis connection configuration.
        /// </param>
        /// <param name="isBackplaneSource">
        /// Set this to true if this cache handle should be the source of the backplane.
        /// This setting will be ignored if no backplane is configured.
        /// </param>
        /// <returns>The builder instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="redisConfigurationKey"/> is null.</exception>
        public static ConfigurationBuilderCacheHandlePart WithRedisCacheHandle(this ConfigurationBuilderCachePart part, string redisConfigurationKey, bool isBackplaneSource = true)
        {
            NotNull(part, nameof(part));

            return part.WithHandle(typeof(RedisCacheHandle<>), redisConfigurationKey, isBackplaneSource);
        }

#pragma warning restore SA1625
    }
}