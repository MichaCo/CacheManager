using System;
using System.Collections.Generic;
using StackExchange.Redis;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Redis
{
    /// <summary>
    /// <see cref="RedisConfiguration"/> will be used for configuring e.g. StackExchange.Redis by
    /// code or configuration file.
    /// <para>
    /// The element was added only because StackExchange.Redis doesn't support configuration via web/app.config
    /// </para>
    /// </summary>
    public class RedisConfiguration
    {
        private string _connectionString;
        private ConfigurationOptions _configurationOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisConfiguration"/> class.
        /// </summary>
        public RedisConfiguration()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisConfiguration"/> class.
        /// </summary>
        /// <param name="key">
        /// The configuration key which will be used by the cache handle to find a configuration for
        /// the cache handle's name.
        /// </param>
        /// <param name="endpoints">
        /// The list of <see cref="ServerEndPoint"/> s to be used to connect to Redis server.
        /// </param>
        /// <param name="database">The Redis database index.</param>
        /// <param name="password">The password of the Redis server.</param>
        /// <param name="isSsl">If <c>true</c> instructs the cache to use SSL encryption.</param>
        /// <param name="sslHost">If specified, the connection will set the SSL host.</param>
        /// <param name="connectionTimeout">Sets the timeout used for connect operations.</param>
        /// <param name="allowAdmin">
        /// If set to <c>True</c> it enables the cache to use features which might be risky.
        /// <c>Clear</c> for example.
        /// </param>
        /// <param name="keyspaceNotificationsEnabled">Enables keyspace notifications to react on eviction/expiration of items.</param>
        /// <param name="twemproxyEnabled">Enables Twemproxy mode.</param>
        /// <param name="strictCompatibilityModeVersion">
        /// Gets or sets a version number to eventually reduce the avaible features accessible by cachemanager.
        /// </param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "Using it for configuration data only.")]
        public RedisConfiguration(
            string key,
            IList<ServerEndPoint> endpoints,
            int database = 0,
            string password = null,
            bool isSsl = false,
            string sslHost = null,
            int connectionTimeout = 5000,
            bool allowAdmin = false,
            bool keyspaceNotificationsEnabled = false,
            bool twemproxyEnabled = false,
            string strictCompatibilityModeVersion = null)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNull(endpoints, nameof(endpoints));

            if (endpoints.Count == 0)
            {
                throw new InvalidOperationException("List of endpoints must not be empty.");
            }

            Key = key;
            Database = database;
            Endpoints = endpoints;
            Password = password;
            IsSsl = isSsl;
            SslHost = sslHost;
            ConnectionTimeout = connectionTimeout;
            AllowAdmin = allowAdmin;
            KeyspaceNotificationsEnabled = keyspaceNotificationsEnabled;
            TwemproxyEnabled = twemproxyEnabled;
            StrictCompatibilityModeVersion = strictCompatibilityModeVersion;

            _configurationOptions = CreateConfigurationOptions();

            // is used later on as key in the connection lookup, and this object should be consistent no matter which ctor is used
            ConnectionString = _configurationOptions.ToString();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisConfiguration"/> class.
        /// </summary>
        /// <param name="key">
        /// The configuration key which will be used by the cache handle to find a configuration for
        /// the cache handle's name.
        /// </param>
        /// <param name="connectionString">
        /// Instead of specifying all the properties, this can also be done via one connection string.
        /// </param>
        /// <param name="database">The redis database to use.</param>
        /// <param name="keyspaceNotificationsEnabled">Enables keyspace notifications to react on eviction/expiration of items.</param>
        /// <param name="strictCompatibilityModeVersion">
        /// Gets or sets a version number to eventually reduce the avaible features accessible by cachemanager.
        /// </param>
        public RedisConfiguration(
            string key,
            string connectionString,
            int database = 0,
            bool keyspaceNotificationsEnabled = false,
            string strictCompatibilityModeVersion = null)
        {
            Key = key;
            ConnectionString = connectionString;
            Database = database;
            KeyspaceNotificationsEnabled = keyspaceNotificationsEnabled;
            StrictCompatibilityModeVersion = strictCompatibilityModeVersion;
        }
        
        private ConfigurationOptions CreateConfigurationOptions()
        {
            var configurationOptions = new ConfigurationOptions()
            {
                AllowAdmin = AllowAdmin,
                ConnectTimeout = ConnectionTimeout,
                Password = Password,
                Ssl = IsSsl,
                SslHost = SslHost,
                ConnectRetry = 10,
                AbortOnConnectFail = false,
                Proxy = TwemproxyEnabled ? Proxy.Twemproxy : Proxy.None
            };

            foreach (var endpoint in Endpoints)
            {
                configurationOptions.EndPoints.Add(endpoint.Host, endpoint.Port);
            }

            return configurationOptions;
        }

        /// <summary>
        /// Gets the <see cref="StackExchange.Redis.ConfigurationOptions"/> defined by this configuration.
        /// </summary>
        [CLSCompliant(false)]
        public ConfigurationOptions ConfigurationOptions => _configurationOptions;

        /// <summary>
        /// Gets or sets a version number to eventually reduce the avaible features accessible by cachemanager.
        /// E.g. set this to <c>"2.4"</c> to disable LUA support.
        /// </summary>
        /// <remarks>
        /// This can also be used when automatic feature detection is not possible. Which is the
        /// case for example if TwemProxy is used, because the servers collection. to query the features, is not available/supported.
        /// CacheManager per default falls back to a version which supports LUA. If you are using a Redis server behind TwemPoxy which
        /// does not allow LUA, use this property!
        /// </remarks>
        public string StrictCompatibilityModeVersion { get; set; }

        /// <summary>
        /// Gets or sets the identifier for the redis configuration.
        /// <para>
        /// This might have to match with the cache handle's name to make the cache handle use this configuration.
        /// </para>
        /// </summary>
        /// <value>
        /// The key.
        /// </value>
        public string Key { get; set; }
        
        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        /// <value>
        /// The connection string.
        /// </value>
        public string ConnectionString {
            get => _connectionString;
            set
            {
                _configurationOptions = ConfigurationOptions.Parse(value, true);

                // read some properties back to be able to react on the settings in RedisCacheHandle
                TwemproxyEnabled = _configurationOptions.Proxy == Proxy.Twemproxy;
                AllowAdmin = _configurationOptions.AllowAdmin;
                ConnectionTimeout = _configurationOptions.ConnectTimeout;
                IsSsl = _configurationOptions.Ssl;
                SslHost = _configurationOptions.SslHost;
                _connectionString = _configurationOptions.ToString();
            }
        }

        /// <summary>
        /// Gets or sets the password to be used to connect to the Redis server.
        /// </summary>
        /// <value>
        /// The password.
        /// </value>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use SSL encryption.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is SSL; otherwise, <c>false</c>.
        /// </value>
        public bool IsSsl { get; set; }

        /// <summary>
        /// Gets or sets the SSL Host.
        /// If set, it will enforce this particular host on the server's certificate.
        /// </summary>
        /// <value>
        /// The SSL host.
        /// </value>
        public string SslHost { get; set; }

        /// <summary>
        /// Gets or sets the timeout for any connect operations.
        /// </summary>
        /// <value>
        /// The connection timeout.
        /// </value>
        public int ConnectionTimeout { get; set; }

        /// <summary>
        /// Gets the list of endpoints to be used to connect to the Redis server.
        /// </summary>
        /// <value>
        /// The endpoints.
        /// </value>
        public IList<ServerEndPoint> Endpoints { get; } = new List<ServerEndPoint>();

        /// <summary>
        /// Gets or sets a value indicating whether to allow the connection to run certain 'risky' commands, or not.
        /// <para><c>cache.Clear</c> requires this to be set to true because we will flush the Redis database.
        /// </para>
        /// </summary>
        /// <value>
        ///   <c>true</c> if 'risky' commands are allowed; otherwise, <c>false</c>.
        /// </value>
        public bool AllowAdmin { get; set; }

        /// <summary>
        /// Gets or sets the Redis database index the cache will use.
        /// </summary>
        /// <value>
        /// The database.
        /// </value>
        public int Database { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the redis cache handle should use keyspace notifications to react on
        /// evictions or expired events from redis and then forward those events to the cache manager.
        /// See <see href="https://redis.io/topics/notifications"/> for technical details.
        /// <para>
        /// To use this feature, you might have to enable this feature on your redis server(s).
        /// </para>
        /// </summary>
        public bool KeyspaceNotificationsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value to indicate if Termproxy is being used.
        /// </summary>
        public bool TwemproxyEnabled { get; set; }
    }

    /// <summary>
    /// Defines an endpoint.
    /// </summary>
    public sealed class ServerEndPoint
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerEndPoint"/> class.
        /// </summary>
        public ServerEndPoint()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerEndPoint"/> class.
        /// </summary>
        /// <param name="host">The host.</param>
        /// <param name="port">The port.</param>
        /// <exception cref="System.ArgumentNullException">If host is null.</exception>
        public ServerEndPoint(string host, int port)
        {
            NotNullOrWhiteSpace(host, nameof(host));

            Host = host;
            Port = port;
        }

        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        /// <value>The port.</value>
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets the host.
        /// </summary>
        /// <value>The host.</value>
        public string Host { get; set; }
    }
}