using System;
using System.Collections.Generic;
using CacheManager.Core.Configuration;
using CacheManager.Redis;

namespace CacheManager.Core
{
    public static class ConfigurationBuilderExtensions
    {
        /// <summary>
        /// Adds a redis configuration.
        /// </summary>
        /// <param name="configurationKey">The configuration key which has to match with the cache handle name.</param>
        /// <param name="config">The redis configuration object.</param>
        /// <returns>The configuration builder.</returns>
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
        /// <param name="configurationKey">The configuration key which has to match with the cache handle name.</param>
        /// <param name="connectionString">The redis connection string.</param>
        /// <returns>The configuration builder.</returns>
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