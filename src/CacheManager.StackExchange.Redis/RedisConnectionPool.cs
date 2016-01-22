using System;
using System.Collections.Generic;
using System.Linq;
using static CacheManager.Core.Utility.Guard;
using StackRedis = StackExchange.Redis;

namespace CacheManager.Redis
{
    internal class RedisConnectionPool
    {
        private static IDictionary<string, StackRedis.ConnectionMultiplexer> connections = new Dictionary<string, StackRedis.ConnectionMultiplexer>();

        private static object connectLock = new object();

        public static void DisposeConnection(string connectionString)
        {
            NotNullOrWhiteSpace(connectionString, nameof(connectionString));

            lock (connectLock)
            {
                StackRedis.ConnectionMultiplexer connection;
                if (connections.TryGetValue(connectionString, out connection))
                {
                    // don't dispose the connection, might still be used somewhere
                    // just remove it from the pool so that new connects create new instances
                    connections.Remove(connectionString);
                }
            }
        }

        public static void DisposeConnection(RedisConfiguration configuration)
        {
            NotNull(configuration, nameof(configuration));
            DisposeConnection(GetConnectionString(configuration));
        }

        public static StackRedis.ConnectionMultiplexer Connect(string connectionString)
        {
            if (!connections.ContainsKey(connectionString))
            {
                lock (connectLock)
                {
                    StackRedis.ConnectionMultiplexer connection;
                    if (!connections.TryGetValue(connectionString, out connection))
                    {
                        connection = StackRedis.ConnectionMultiplexer.Connect(connectionString);

                        connection.ConnectionFailed += (sender, args) =>
                        {
                            connections.Remove(connectionString);
                        };

                        if (!connection.IsConnected)
                        {
                            throw new InvalidOperationException("Connection failed.");
                        }

                        var endpoints = connection.GetEndPoints();
                        if (!endpoints.Select(p => connection.GetServer(p))
                            .Any(p => !p.IsSlave || p.AllowSlaveWrites))
                        {
                            throw new InvalidOperationException("No writeable endpoint found.");
                        }

                        connection.PreserveAsyncOrder = false;
                        connections.Add(connectionString, connection);
                    }
                }
            }

            return connections[connectionString];
        }

        public static StackRedis.ConnectionMultiplexer Connect(RedisConfiguration configuration)
        {
            NotNull(configuration, nameof(configuration));
            return Connect(GetConnectionString(configuration));
        }

        public static string GetConnectionString(RedisConfiguration configuration)
        {
            string connectionString = configuration.ConnectionString;

            if (string.IsNullOrWhiteSpace(configuration.ConnectionString))
            {
                var options = CreateConfigurationOptions(configuration);
                connectionString = options.ToString();
            }

            return connectionString;
        }

        private static StackRedis.ConfigurationOptions CreateConfigurationOptions(RedisConfiguration configuration)
        {
            var configurationOptions = new StackRedis.ConfigurationOptions()
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
    }
}