using System;
using System.Collections.Generic;

namespace CacheManager.Redis
{
    /// <summary>
    /// <see cref="RedisConfigurationBuilder"/> helps creating the <see cref="RedisConfiguration"/>
    /// object via code.
    /// </summary>
    public class RedisConfigurationBuilder
    {
        private bool allowAdmin = false;
        private int connectionTimeout = 5000;
        private int database = 0;
        private IList<ServerEndPoint> endpoints = new List<ServerEndPoint>();
        private bool isSsl = false;
        private string key = string.Empty;
        private string password = null;
        private string sslHost = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisConfigurationBuilder"/> class.
        /// </summary>
        /// <param name="configurationKey">The configuration key.</param>
        /// <exception cref="System.ArgumentNullException">If configurationKey is null.</exception>
        public RedisConfigurationBuilder(string configurationKey)
        {
            if (string.IsNullOrWhiteSpace(configurationKey))
            {
                throw new ArgumentNullException("configurationKey");
            }

            this.key = configurationKey;
        }

        /// <summary>
        /// Creates the <see cref="RedisConfiguration"/> out of the currently specified properties,
        /// if possible.
        /// </summary>
        /// <returns>The <c>RedisConfiguration</c></returns>
        public RedisConfiguration Build()
        {
            return new RedisConfiguration(this.key, this.endpoints, this.database, this.password, this.isSsl, this.sslHost, this.connectionTimeout, this.allowAdmin);
        }

        /// <summary>
        /// If set to true, commands which might be risky are enabled, like Clear which will delete
        /// all entries in the redis database.
        /// </summary>
        /// <returns>The builder.</returns>
        public RedisConfigurationBuilder WithAllowAdmin()
        {
            this.allowAdmin = true;
            return this;
        }

        /// <summary>
        /// Sets the timeout in milliseconds for connect operations.
        /// </summary>
        /// <param name="timeout">The timeout in milliseconds.</param>
        /// <returns>The builder.</returns>
        public RedisConfigurationBuilder WithConnectionTimeout(int timeout)
        {
            this.connectionTimeout = timeout;
            return this;
        }

        /// <summary>
        /// Sets the database.
        /// <para>Maximum number of database depends on the redis server configuration.</para>Default
        /// is <c>0</c>.
        /// </summary>
        /// <param name="database">The database index.</param>
        /// <returns>The builder.</returns>
        public RedisConfigurationBuilder WithDatabase(int database)
        {
            this.database = database;
            return this;
        }

        /// <summary>
        /// Adds an endpoint to the connection configuration.
        /// <para>Call this multiple times to add multiple endpoints.</para>
        /// </summary>
        /// <param name="host">The host or IP of the redis server.</param>
        /// <param name="port">The port of the redis server.</param>
        /// <returns>The builder.</returns>
        public RedisConfigurationBuilder WithEndpoint(string host, int port)
        {
            var endpoint = new ServerEndPoint(host, port);
            this.endpoints.Add(endpoint);
            return this;
        }

        /// <summary>
        /// Sets the password for the redis server.
        /// </summary>
        /// <param name="password">The redis server password.</param>
        /// <returns>The builder.</returns>
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
        /// <returns>The builder.</returns>
        public RedisConfigurationBuilder WithSsl(string sslHost = null)
        {
            this.isSsl = true;
            this.sslHost = sslHost;
            return this;
        }
    }
}