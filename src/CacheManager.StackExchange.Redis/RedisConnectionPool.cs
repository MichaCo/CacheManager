using System;
using System.Collections.Generic;
using CacheManager.Core;
using StackRedis = StackExchange.Redis;

namespace CacheManager.Redis
{
    internal class RedisConnectionPool
    {
        private static IDictionary<string, StackRedis.ConnectionMultiplexer> connections;

        // connection string to connection multiplexer
        private static object connectLock = new object();

        static RedisConnectionPool()
        {
            connections = new Dictionary<string, StackRedis.ConnectionMultiplexer>();
        }

        public static StackRedis.ConnectionMultiplexer Connect(CacheManagerConfiguration cacheConfig, RedisConfiguration configuration)
        {
            string connectionString = configuration.ConnectionString;

            if (string.IsNullOrWhiteSpace(configuration.ConnectionString))
            {
                var options = CreateConfigurationOptions(cacheConfig, configuration);
                connectionString = options.ToString();
            }

            StackRedis.ConnectionMultiplexer connection;
            lock (connectLock)
            {
                if (!connections.TryGetValue(connectionString, out connection))
                {
                    connection = StackRedis.ConnectionMultiplexer.Connect(connectionString);

                    ////connection.ErrorMessage += (sender, args) =>
                    ////{
                    ////};

                    connection.ConnectionFailed += (sender, args) =>
                    {
                        connections.Remove(connectionString);
                    };

                    if (!connection.IsConnected)
                    {
                        throw new InvalidOperationException("Connection failed.");
                    }

                    connection.PreserveAsyncOrder = false;
                    connections.Add(connectionString, connection);                    
                }
            }

            return connection;
        }

        private static StackRedis.ConfigurationOptions CreateConfigurationOptions(CacheManagerConfiguration cacheConfig, RedisConfiguration configuration)
        {
            var configurationOptions = new StackRedis.ConfigurationOptions()
            {
                AllowAdmin = configuration.AllowAdmin,
                ConnectTimeout = configuration.ConnectionTimeout,
                Password = configuration.Password,
                Ssl = configuration.IsSsl,
                SslHost = configuration.SslHost,
                ConnectRetry = cacheConfig.MaxRetries,
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