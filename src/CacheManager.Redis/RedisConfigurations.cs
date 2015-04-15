using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;

namespace CacheManager.Redis
{
    /// <summary>
    /// Manages redis client configurations for the cache handle.
    /// <para>
    /// Configurations will be added by the cache configuration builder/factory or the configuration
    /// loader. The cache handle will pick up the configuration matching the handle's name.
    /// </para>
    /// </summary>
    public static class RedisConfigurations
    {
        private static Dictionary<string, RedisConfiguration> configurations = new Dictionary<string, RedisConfiguration>();

        /// <summary>
        /// Initializes static members of the <see cref="RedisConfigurations"/> class.
        /// </summary>
        static RedisConfigurations()
        {
            // load defaults
            var section = ConfigurationManager.GetSection(RedisConfigurationSection.DefaultSectionName) as RedisConfigurationSection;
            if (section != null)
            {
                LoadConfiguration(section);
            }
        }

        /// <summary>
        /// Adds the configuration.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <exception cref="System.ArgumentNullException">If configuration is null.</exception>
        public static void AddConfiguration(RedisConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            if (!configurations.ContainsKey(configuration.Key))
            {
                configurations.Add(configuration.Key, configuration);
            }
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <param name="configurationName">The identifier.</param>
        /// <returns>The <c>RedisConfiguration</c>.</returns>
        /// <exception cref="System.ArgumentNullException">If id is null.</exception>
        /// <exception cref="System.InvalidOperationException">
        /// If no configuration was added for the id.
        /// </exception>
        public static RedisConfiguration GetConfiguration(string configurationName)
        {
            if (string.IsNullOrWhiteSpace(configurationName))
            {
                throw new ArgumentNullException("configurationName");
            }

            if (!configurations.ContainsKey(configurationName))
            {
                // check connection strings if there is one matching the name
                var connectionStringHolder = ConfigurationManager.ConnectionStrings[configurationName];
                if (connectionStringHolder == null || string.IsNullOrWhiteSpace(connectionStringHolder.ConnectionString))
                {
                    throw new InvalidOperationException("No configuration added for configurationName " + configurationName);
                }

                var configuration = new RedisConfiguration(configurationName, connectionStringHolder.ConnectionString);
                configurations.Add(configurationName, configuration);
            }

            return configurations[configurationName];
        }

        /// <summary>
        /// Loads the configuration.
        /// </summary>
        /// <param name="configFileName">Name of the configuration file.</param>
        /// <param name="sectionName">Name of the section.</param>
        /// <exception cref="System.ArgumentNullException">
        /// If configFileName or sectionName are null.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// If the configuration file could not be found.
        /// </exception>
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

        /// <summary>
        /// Loads the configuration.
        /// </summary>
        /// <param name="section">The section.</param>
        /// <exception cref="System.ArgumentNullException">If section is null.</exception>
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

                if (string.IsNullOrWhiteSpace(redisOption.ConnectionString))
                {
                    AddConfiguration(
                        new RedisConfiguration(
                            key: redisOption.Id,
                            database: redisOption.Database,
                            endpoints: endpoints,
                            password: redisOption.Password,
                            isSsl: redisOption.Ssl,
                            sslHost: redisOption.SslHost,
                            connectionTimeout: redisOption.ConnectionTimeout == 0 ? 5000 : redisOption.ConnectionTimeout,
                            allowAdmin: redisOption.AllowAdmin));
                }
                else
                {
                    AddConfiguration(
                        new RedisConfiguration(
                            key: redisOption.Id,
                            connectionString: redisOption.ConnectionString));
                }
            }
        }

        /// <summary>
        /// Loads the configuration.
        /// </summary>
        /// <param name="sectionName">Name of the section.</param>
        /// <exception cref="System.ArgumentNullException">If sectionName is null.</exception>
        public static void LoadConfiguration(string sectionName)
        {
            if (string.IsNullOrWhiteSpace(sectionName))
            {
                throw new ArgumentNullException("sectionName");
            }

            var section = ConfigurationManager.GetSection(sectionName) as RedisConfigurationSection;
            LoadConfiguration(section);
        }

        /// <summary>
        /// Loads the configuration from the default section name 'cacheManager.Redis'.
        /// </summary>
        public static void LoadConfiguration()
        {
            LoadConfiguration(RedisConfigurationSection.DefaultSectionName);
        }
    }
}