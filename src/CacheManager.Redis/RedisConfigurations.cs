using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;

namespace CacheManager.Redis
{
    public static class RedisConfigurations
    {
        private static Dictionary<string, RedisConfiguration> configurations = new Dictionary<string, RedisConfiguration>();

        static RedisConfigurations()
        {
            // load defaults
            var section = ConfigurationManager.GetSection(RedisConfigurationSection.DefaultSectionName) as RedisConfigurationSection;
            if (section != null)
            {
                LoadConfiguration(section);
            }
        }

        public static void AddConfiguration(RedisConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            if (!configurations.ContainsKey(configuration.Id))
            {
                configurations.Add(configuration.Id, configuration);
            }
        }

        public static RedisConfiguration GetConfiguration(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException("id");
            }

            if (!configurations.ContainsKey(id))
            {
                throw new InvalidOperationException("No configuration added for id " + id);
            }

            return configurations[id];
        }

        public static void LoadConfiguration(string configFileName, string sectionName)
        {
            if (string.IsNullOrWhiteSpace(configFileName))
            {
                throw new ArgumentNullException("configFileName");
            }
            if (string.IsNullOrWhiteSpace(sectionName))
            {
                throw new ArgumentNullException("sectionName");
            }

            if (!File.Exists(configFileName))
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture, "Configuration file not found [{0}].", configFileName));
            }

            var fileConfig = new ExeConfigurationFileMap();
            fileConfig.ExeConfigFilename = configFileName; // setting exe config file name, this is the one the GetSection method expects.

            // open the file map
            Configuration cfg = ConfigurationManager.OpenMappedExeConfiguration(fileConfig, ConfigurationUserLevel.None);

            // use the opened configuration and load our section
            var section = cfg.GetSection(sectionName) as RedisConfigurationSection;
            if (section == null)
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture, "No section with name {1} found in file {0}", configFileName, sectionName));
            }

            LoadConfiguration(section);
        }

        public static void LoadConfiguration(RedisConfigurationSection section)
        {
            if (section == null)
            {
                throw new ArgumentNullException("section");
            }

            foreach (var redisOption in section.Connections)
            {
                var endpoints = new List<ServerEndPoint>();
                foreach (var endpoint in redisOption.Endpoints)
                {
                    endpoints.Add(new ServerEndPoint(endpoint.Host, endpoint.Port));
                }

                AddConfiguration(
                    new RedisConfiguration(
                        id: redisOption.Id,
                        database: redisOption.Database,
                        endpoints: endpoints,
                        connectionString: redisOption.ConnectionString,
                        password: redisOption.Password,
                        isSsl: redisOption.Ssl,
                        sslHost: redisOption.SslHost,
                        connectionTimeout: redisOption.ConnectionTimeout == 0 ? 5000 : redisOption.ConnectionTimeout,
                        allowAdmin: redisOption.AllowAdmin));
            }
        }

        public static void LoadConfiguration(string sectionName)
        {
            if (string.IsNullOrWhiteSpace(sectionName))
            {
                throw new ArgumentNullException("sectionName");
            }

            var section = ConfigurationManager.GetSection(sectionName) as RedisConfigurationSection;
            LoadConfiguration(section);
        }

        public static void LoadConfiguration()
        {
            LoadConfiguration(RedisConfigurationSection.DefaultSectionName);
        }
    }
}