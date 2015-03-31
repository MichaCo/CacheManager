using System;
using System.Collections.Generic;

namespace CacheManager.Redis
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Redis")]
    public sealed class RedisConfiguration
    {
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

        public RedisConfiguration(
            string key,
            string connectionString)
        {
            this.Key = key;
            this.ConnectionString = connectionString;
        }
        /// <summary>
        /// Gets the identifier for the redis options.
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// Gets the connection string. Will be used by some redis clients (Stackexchange.Redis).
        /// </summary>
        public string ConnectionString { get; private set; }

        public string Password { get; private set; }

        public bool IsSsl { get; private set; }

        public string SslHost { get; private set; }

        public int ConnectionTimeout { get; private set; }

        public IList<ServerEndPoint> Endpoints { get; private set; }

        public bool AllowAdmin { get; private set; }

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