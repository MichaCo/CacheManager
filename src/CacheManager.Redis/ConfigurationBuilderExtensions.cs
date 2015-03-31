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

    public class RedisConfigurationBuilder
    {
        private string key = string.Empty;
        private string password = null;
        private bool isSsl = false;
        private string sslHost = null;
        private int connectionTimeout = 5000;
        private IList<ServerEndPoint> endpoints = new List<ServerEndPoint>();
        private bool allowAdmin = false;
        private int database = 0;

        public RedisConfiguration Build()
        {
            return new RedisConfiguration(key, endpoints, database, password, isSsl, sslHost, connectionTimeout, allowAdmin);            
        }

        public RedisConfigurationBuilder(string configurationKey)
        {
            if (string.IsNullOrWhiteSpace(configurationKey))
            {
                throw new ArgumentNullException("configurationKey");
            }

            this.key = configurationKey;
        }

        /// <summary>
        /// Sets the password for the redis server.
        /// </summary>
        /// <param name="password">The redis server password.</param>
        /// <returns>The builder</returns>
        public RedisConfigurationBuilder WithPassword(string password)
        {
            this.password = password;
            return this;
        }

        /// <summary>
        /// Enables SSL encryption.
        /// <para>
        /// If host is specified it will enforce a particular SSL host identity on the server's certificate.
        /// </para>
        /// </summary>
        /// <param name="sslHost">The SSL host.</param>
        /// <returns>The builder</returns>
        public RedisConfigurationBuilder WithSsl(string sslHost = null)
        {
            this.isSsl = true;
            this.sslHost = sslHost;
            return this;
        }

        /// <summary>
        /// Sets the timeout in milliseconds for connect operations.
        /// </summary>
        /// <param name="timeout">The timeout in milliseconds.</param>
        /// <returns>The builder</returns>
        public RedisConfigurationBuilder WithConnectionTimeout(int timeout)
        {
            this.connectionTimeout = timeout;
            return this;
        }

        /// <summary>
        /// Sets the database. 
        /// <para>Maximum number of database depends on the redis server configuration.</para>
        /// Default is <c>0</c>.
        /// </summary>
        /// <param name="database">The database index.</param>
        /// <returns>The builder</returns>
        public RedisConfigurationBuilder WithDatabase(int database)
        {
            this.database = database;
            return this;
        }

        /// <summary>
        /// If set to true, commands which might be risky are enabled, like Clear which will delete all 
        /// entries in the redis database.
        /// </summary>
        /// <returns>The builder</returns>
        public RedisConfigurationBuilder WithAllowAdmin()
        {
            this.allowAdmin = true;
            return this;
        }

        /// <summary>
        /// Adds an endpoint to the connection configuration.
        /// <para>
        /// Call this multiple times to add multiple endpoints.
        /// </para>
        /// </summary>
        /// <param name="host">The host or IP of the redis server.</param>
        /// <param name="port"></param>
        /// <returns>The builder</returns>
        public RedisConfigurationBuilder WithEndpoint(string host, int port)
        {
            var endpoint = new ServerEndPoint(host, port);
            this.endpoints.Add(endpoint);
            return this;
        }
    }
}