using System;
using CacheManager.Core.Configuration;

namespace CacheManager.Redis
{
    public static class ConfigurationBuilderExtensions
    {
        /// <summary>
        /// Adds a redis configuration.
        /// </summary>
        /// <param name="config">The redis configuration object.</param>
        /// <returns>The configuration builder.</returns>
        public static ConfigurationBuilderCachePart<TCacheValue> WithRedisConfiguration<TCacheValue>(this ConfigurationBuilderCachePart<TCacheValue> part, RedisConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            RedisConfigurations.AddConfiguration(config);
            return part;
        }
    }
}