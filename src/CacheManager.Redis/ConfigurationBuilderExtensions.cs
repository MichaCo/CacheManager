using System;
using CacheManager.Core.Configuration;
using CacheManager.Redis;

namespace CacheManager.Core
{
    /// <summary>
    /// Extensions for the configuration builder specific to redis.
    /// </summary>
    public static class ConfigurationBuilderExtensions
    {
        /// <summary>
        /// Adds a redis configuration.
        /// </summary>
        /// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
        /// <param name="part">The part.</param>
        /// <param name="configurationKey">
        /// The configuration key which has to match with the cache handle name.
        /// </param>
        /// <param name="config">The redis configuration object.</param>
        /// <returns>The configuration builder.</returns>
        /// <exception cref="System.ArgumentNullException">If config is null.</exception>
        public static ConfigurationBuilderCachePart<TCacheValue> WithRedisConfiguration<TCacheValue>(this ConfigurationBuilderCachePart<TCacheValue> part, string configurationKey, Action<RedisConfigurationBuilder> config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }
            var builder = new RedisConfigurationBuilder(configurationKey);
            config(builder);
            RedisConfigurations.AddConfiguration(builder.Build());
            return part;
        }

        /// <summary>
        /// Adds a redis configuration.
        /// </summary>
        /// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
        /// <param name="part">The part.</param>
        /// <param name="configurationKey">
        /// The configuration key which has to match with the cache handle name.
        /// </param>
        /// <param name="connectionString">The redis connection string.</param>
        /// <returns>The configuration builder.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// If configurationKey or connectionString are null.
        /// </exception>
        public static ConfigurationBuilderCachePart<TCacheValue> WithRedisConfiguration<TCacheValue>(this ConfigurationBuilderCachePart<TCacheValue> part, string configurationKey, string connectionString)
        {
            if (string.IsNullOrWhiteSpace(configurationKey))
            {
                throw new ArgumentNullException("configurationKey");
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException("connectionString");
            }

            RedisConfigurations.AddConfiguration(new RedisConfiguration(configurationKey, connectionString));
            return part;
        }
    }
}