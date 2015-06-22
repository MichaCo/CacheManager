using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using CacheManager.Core;
using StackRedis = StackExchange.Redis;

namespace CacheManager.Redis
{
    internal class RedisConnectionPool
    {
        private static IDictionary<string, StackRedis.ConnectionMultiplexer> connections = new Dictionary<string, StackRedis.ConnectionMultiplexer>();

        private static object connectLock = new object();

        public static StackRedis.ConnectionMultiplexer Connect(RedisConfiguration configuration)
        {
            string connectionString = configuration.ConnectionString;

            if (string.IsNullOrWhiteSpace(configuration.ConnectionString))
            {
                var options = CreateConfigurationOptions(configuration);
                connectionString = options.ToString();
            }

            StackRedis.ConnectionMultiplexer connection;
            lock (connectLock)
            {
                if (!connections.TryGetValue(connectionString, out connection))
                {
                    var builder = new StringBuilder();
                    using (var log = new StringWriter(builder, CultureInfo.InvariantCulture))                        
                    {
                        connection = StackRedis.ConnectionMultiplexer.Connect(connectionString, log);
                    }

                    connection.ConnectionFailed += (sender, args) =>
                    {
                        connections.Remove(connectionString);
                    };

                    if (!connection.IsConnected)
                    {
                        throw new InvalidOperationException("Connection failed.\n" + builder.ToString());
                    }

                    connection.PreserveAsyncOrder = false;
                    connections.Add(connectionString, connection);                    
                }
            }

            return connection;
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
                AbortOnConnectFail = false
            };

            foreach (var endpoint in configuration.Endpoints)
            {
                configurationOptions.EndPoints.Add(endpoint.Host, endpoint.Port);
            }

            return configurationOptions;
        }
    }
}