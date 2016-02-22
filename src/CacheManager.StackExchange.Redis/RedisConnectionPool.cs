using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CacheManager.Core.Logging;
using static CacheManager.Core.Utility.Guard;
using StackRedis = StackExchange.Redis;

namespace CacheManager.Redis
{
    internal class LogWriter : StringWriter
    {
        private readonly ILogger logger;

        public LogWriter(ILogger logger)
        {
            this.logger = logger;
        }

        public LogWriter(StringBuilder sb) : base(sb)
        {
        }

        public LogWriter(IFormatProvider formatProvider) : base(formatProvider)
        {
        }

        public LogWriter(StringBuilder sb, IFormatProvider formatProvider) : base(sb, formatProvider)
        {
        }

        public override void Write(char value)
        {
        }

        public override void Write(string value)
        {
            this.logger.LogDebug(value);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            this.logger.LogDebug(new string(buffer, index, count));
        }
    }

    internal class RedisConnectionManager
    {
        private static IDictionary<string, StackRedis.ConnectionMultiplexer> connections = new Dictionary<string, StackRedis.ConnectionMultiplexer>();
        private static IDictionary<string, int> connectionRefs = new Dictionary<string, int>();
        private static object connectLock = new object();

        private readonly ILogger logger;
        private readonly string connectionString;
        private readonly RedisConfiguration configuration;
        private readonly Action<StackRedis.ConnectionType> onRestoreConnection;

        public RedisConnectionManager(RedisConfiguration configuration, ILoggerFactory loggerFactory, Action<StackRedis.ConnectionType> onRestoreConnection = null)
        {
            NotNull(configuration, nameof(configuration));
            NotNull(loggerFactory, nameof(loggerFactory));

            this.connectionString = GetConnectionString(configuration);

            this.configuration = configuration;
            this.logger = loggerFactory.CreateLogger(this);
            this.onRestoreConnection = onRestoreConnection;
        }

        public StackRedis.IDatabase Database => this.Connect().GetDatabase(this.configuration.Database);

        public StackRedis.ISubscriber Subscriber => this.Connect().GetSubscriber();

        public void RemoveConnection()
        {
            lock (connectLock)
            {
                ////StackRedis.ConnectionMultiplexer connection;
                ////if (connections.TryGetValue(this.connectionString, out connection))
                ////{
                ////    this.logger.LogInfo("Removing stale redis connection.");
                ////    connections.Remove(this.connectionString);
                ////}
            }
        }

        public StackRedis.ConnectionMultiplexer Connect()
        {
            StackRedis.ConnectionMultiplexer connection;
            if (!connections.TryGetValue(this.connectionString, out connection))
            {
                lock (connectLock)
                {
                    if (!connections.TryGetValue(this.connectionString, out connection))
                    {
                        if (!connections.TryGetValue(this.connectionString, out connection))
                        {
                            this.logger.LogInfo("Connecting to redis: '{0}'", this.connectionString);
                            connection = StackRedis.ConnectionMultiplexer.Connect(this.connectionString, new LogWriter(this.logger));

                            if (!connection.IsConnected)
                            {
                                connection.Dispose();
                                throw new InvalidOperationException("Connection failed.");
                            }
                            
                            connection.ConnectionRestored += (sender, args) =>
                            {
                                this.logger.LogInfo(args.Exception, "Connection restored, type: '{0}', failure: '{1}'", args.ConnectionType, args.FailureType);
                            };

                            var endpoints = connection.GetEndPoints();
                            if (!endpoints.Select(p => connection.GetServer(p))
                                .Any(p => !p.IsSlave || p.AllowSlaveWrites))
                            {
                                throw new InvalidOperationException("No writeable endpoint found.");
                            }

                            connection.PreserveAsyncOrder = false;
                            connections.Add(this.connectionString, connection);
                        }
                    }
                }
            }

            if(connection == null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Couldn't esteblish a connection for {0}.", 
                        this.connectionString));
            }

            return connection;
        }

        private static string GetConnectionString(RedisConfiguration configuration)
        {
            string conString = configuration.ConnectionString;

            if (string.IsNullOrWhiteSpace(configuration.ConnectionString))
            {
                var options = CreateConfigurationOptions(configuration);
                conString = options.ToString();
            }

            return conString;
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