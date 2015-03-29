using System;
using CacheManager.Core.Configuration;

namespace CacheManager.Redis
{
    public static class ConfigurationBuilderExtensions
    {
        /// <summary>
        /// Defines a redis configuration.
        /// <para>
        /// This will only be used and is only needed if the redis cache handle implementation will be used.
        /// </para>
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