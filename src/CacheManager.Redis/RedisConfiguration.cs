using System;
using System.Collections.Generic;

namespace CacheManager.Redis
{
    /// <summary>
    /// <see cref="RedisConfiguration"/> will be used for configuring e.g. StackExchange.Redis by code or configuration file.
    /// <para>
    /// The element was added only because StackExchange.Redis doesn't support configuration via web/app.config
    /// </para>
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Redis")]
    public sealed class RedisConfiguration
    {
        /// <summary>
        /// Creates a new <see cref="RedisConfiguration"/> element.
        /// </summary>
        /// <param name="key">The configuration key which will be used by the cache handle to find a configuration for the cache handle's name.</param>
        /// <param name="endpoints">The list of <see cref="ServerEndPoint"/>s to be used to connect to Redis server.</param>
        /// <param name="database">The Redis database index.</param>
        /// <param name="password">The password of the Redis server.</param>
        /// <param name="isSsl">If <c>true</c> instructs the cache to use SSL encryption.</param>
        /// <param name="sslHost">If specified, the connection will set the ssl host.</param>
        /// <param name="connectionTimeout">Sets the timeout used for connect operations.</param>
        /// <param name="allowAdmin">If set to <c>True</c> it enables the cache to use features which might be risky. <c>Clear</c> for example.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public RedisConfiguration(
            string key, 
            IList<ServerEndPoint> endpoints,
            int database = 0,
            string password = null,
            bool isSsl = false,
            string sslHost = null,
            int connectionTimeout = 5000,
            bool allowAdmin = false)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("id");
            }

            if (endpoints == null)
            {
                throw new ArgumentNullException("endpoints");
            }

            if (endpoints.Count == 0)
            {
                throw new InvalidOperationException("List of endpoints must not be empty.");
            }

            this.Key = key;
            this.Database = database;
            this.Endpoints = endpoints;
            this.Password = password;
            this.IsSsl = isSsl;
            this.SslHost = sslHost;
            this.ConnectionTimeout = connectionTimeout;
            this.AllowAdmin = allowAdmin;
        }

        /// <summary>
        /// Creates a new <see cref="RedisConfiguration"/> element.
        /// </summary>
        /// <param name="key">The configuration key which will be used by the cache handle to find a configuration for the cache handle's name.</param>
        /// <param name="connectionString">Instead of specifying all the properties, this can also be done via one connection string.</param>
        public RedisConfiguration(
            string key,
            string connectionString)
        {
            this.Key = key;
            this.ConnectionString = connectionString;
        }
        /// <summary>
        /// Gets the identifier for the redis configuration.
        /// <para>
        /// This might have to match with the cache handle's name to make the cache handle use this configuration.
        /// </para>
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// Gets the password to be used to connect to the Redis server.
        /// </summary>
        public string Password { get; private set; }

        /// <summary>
        /// Instructs the Redis connection to use SSL encryption.
        /// </summary>
        public bool IsSsl { get; private set; }

        /// <summary>
        /// If set, it will enforce this particular host on the server's certificate.
        /// </summary>
        public string SslHost { get; private set; }

        /// <summary>
        /// Gets the timeout for any connect operations.
        /// </summary>
        public int ConnectionTimeout { get; private set; }

        /// <summary>
        /// Gets the list of endpoints to be used to connect to the Redis server.
        /// </summary>
        public IList<ServerEndPoint> Endpoints { get; private set; }

        /// <summary>
        /// Indicates if the connection is allowed to run certain 'risky' commands.
        /// <para>
        /// <c>cache.Clear</c> requires this to be set to true because we will flush the Redis database.
        /// </para>
        /// </summary>
        public bool AllowAdmin { get; private set; }

        /// <summary>
        /// Gets the Redis database index the cache will use.
        /// </summary>
        public int Database { get; private set; }
    }

    public sealed class ServerEndPoint
    {
        public ServerEndPoint(string host, int port)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new ArgumentNullException("host");
            }

            this.Host = host;
            this.Port = port;
        }

        public int Port { get; private set; }

        public string Host { get; private set; }
    }
}