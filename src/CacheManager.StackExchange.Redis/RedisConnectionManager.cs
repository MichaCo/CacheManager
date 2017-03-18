using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CacheManager.Core.Logging;
using StackExchange.Redis;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Redis
{
    internal class RedisConnectionManager
    {
        private static IDictionary<string, IConnectionMultiplexer> _connections = new Dictionary<string, IConnectionMultiplexer>();
        private static object _connectLock = new object();

        private readonly ILogger _logger;
        private readonly string _connectionString;
        private readonly RedisConfiguration _configuration;

        public RedisConnectionManager(RedisConfiguration configuration, ILoggerFactory loggerFactory)
        {
            NotNull(configuration, nameof(configuration));
            NotNull(loggerFactory, nameof(loggerFactory));

            _connectionString = GetConnectionString(configuration);

            _configuration = configuration;
            _logger = loggerFactory.CreateLogger(this);
        }

        public IEnumerable<IServer> Servers
        {
            get
            {
                var endpoints = Connect().GetEndPoints();
                foreach (var endpoint in endpoints)
                {
                    var server = Connect().GetServer(endpoint);
                    yield return server;
                }
            }
        }

        public IDatabase Database => Connect().GetDatabase(_configuration.Database);

        public ISubscriber Subscriber => Connect().GetSubscriber();

        public RedisFeatures Features
        {
            get
            {
                var server = Servers.FirstOrDefault(p => p.IsConnected);
                if (server == null)
                {
                    throw new InvalidOperationException("No servers are connected or configured.");
                }

                return server.Features;
                ////return new RedisFeatures(new Version(2, 4));
            }
        }

        public Dictionary<System.Net.EndPoint, string> GetConfiguration(string key)
        {
            var result = new Dictionary<System.Net.EndPoint, string>();
            foreach (var server in Servers)
            {
                var values = server.ConfigGet(key).ToDictionary(k => k.Key, v => v.Value);

                if (values.ContainsKey(key))
                {
                    var value = values.FirstOrDefault(p => p.Key == key);
                    result.Add(server.EndPoint, value.Value);
                }
            }

            return result;
        }

        public void SetConfigurationAllServers(string key, string value, bool addValue)
        {
            try
            {
                foreach (var server in Servers)
                {
                    var values = server.ConfigGet(key).ToDictionary(k => k.Key, v => v.Value);

                    if (values.ContainsKey(key))
                    {
                        var oldValue = values.First(p => p.Key == key).Value;

                        if (!oldValue.Equals(value))
                        {
                            server.ConfigSet(key, addValue ? oldValue + value : value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to set '{key}' to '{value}'.", ex);
            }
        }

        public static void AddConnection(string connectionString, IConnectionMultiplexer connection)
        {
            lock (_connectLock)
            {
                if (!_connections.ContainsKey(connectionString))
                {
                    _connections.Add(connectionString, connection);
                }
            }
        }

        public static void RemoveConnection(string connectionString)
        {
            lock (_connectLock)
            {
                if (_connections.ContainsKey(connectionString))
                {
                    _connections.Remove(connectionString);
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "nope")]
        public IConnectionMultiplexer Connect()
        {
            IConnectionMultiplexer connection;
            if (!_connections.TryGetValue(_connectionString, out connection))
            {
                lock (_connectLock)
                {
                    if (!_connections.TryGetValue(_connectionString, out connection))
                    {
                        if (_logger.IsEnabled(LogLevel.Information))
                        {
                            _logger.LogInfo("Trying to connect with the following configuration: '{0}'", RemoveCredentials(_connectionString));
                        }

                        connection = ConnectionMultiplexer.Connect(_connectionString, new LogWriter(_logger));

                        if (!connection.IsConnected)
                        {
                            connection.Dispose();
                            throw new InvalidOperationException($"Connection to '{RemoveCredentials(_connectionString)}' failed.");
                        }

                        connection.ConnectionRestored += (sender, args) =>
                        {
                            _logger.LogInfo(args.Exception, "Connection restored, type: '{0}', failure: '{1}'", args.ConnectionType, args.FailureType);
                        };

                        var endpoints = connection.GetEndPoints();
                        if (!endpoints.Select(p => connection.GetServer(p))
                            .Any(p => !p.IsSlave || p.AllowSlaveWrites))
                        {
                            throw new InvalidOperationException("No writeable endpoint found.");
                        }

                        connection.PreserveAsyncOrder = false;
                        _connections.Add(_connectionString, connection);
                    }
                }
            }

            if (connection == null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Couldn't establish a connection for '{0}'.",
                        RemoveCredentials(_connectionString)));
            }

            return connection;
        }

        private static string RemoveCredentials(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return Regex.Replace(value, @"password\s*=\s*[^,]*", "password=****", RegexOptions.IgnoreCase);
        }

        private static string GetConnectionString(RedisConfiguration configuration)
        {
            var conString = configuration.ConnectionString;

            if (string.IsNullOrWhiteSpace(configuration.ConnectionString))
            {
                var options = CreateConfigurationOptions(configuration);
                conString = options.ToString();
            }

            return conString;
        }

        private static ConfigurationOptions CreateConfigurationOptions(RedisConfiguration configuration)
        {
            var configurationOptions = new ConfigurationOptions()
            {
                AllowAdmin = configuration.AllowAdmin,
                ConnectTimeout = configuration.ConnectionTimeout,
                Password = configuration.Password,
                Ssl = configuration.IsSsl,
                SslHost = configuration.SslHost,
                ConnectRetry = 10,
                AbortOnConnectFail = false,
            };

            foreach (var endpoint in configuration.Endpoints)
            {
                configurationOptions.EndPoints.Add(endpoint.Host, endpoint.Port);
            }

            return configurationOptions;
        }

        private class LogWriter : StringWriter
        {
            private readonly ILogger _logger;

            public LogWriter(ILogger logger)
            {
                _logger = logger;
            }

            public override void Write(char value)
            {
            }

            public override void Write(string value)
            {
                _logger.LogDebug(value);
            }

            public override void Write(char[] buffer, int index, int count)
            {
                var logValue = new string(buffer, index, count);
                _logger.LogDebug(RemoveCredentials(logValue));
            }
        }
    }
}