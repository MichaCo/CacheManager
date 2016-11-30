using System;
using System.Collections.Generic;
#if !NETSTANDARD
using System.Configuration;
#endif
using System.IO;
using static CacheManager.Core.Utility.Guard;

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
        private static Dictionary<string, RedisConfiguration> config = null;
        private static object configLock = new object();

        private static Dictionary<string, RedisConfiguration> Configurations
        {
            get
            {
                if (config == null)
                {
                    lock (configLock)
                    {
                        if (config == null)
                        {
                            config = new Dictionary<string, RedisConfiguration>();

#if !NETSTANDARD
                            var section = ConfigurationManager.GetSection(RedisConfigurationSection.DefaultSectionName) as RedisConfigurationSection;
                            if (section != null)
                            {
                                LoadConfiguration(section);
                            }
#endif
                        }
                    }
                }

                return config;
            }
        }

        /// <summary>
        /// Adds the configuration.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <exception cref="System.ArgumentNullException">If configuration is null.</exception>
        public static void AddConfiguration(RedisConfiguration configuration)
        {
            lock (configLock)
            {
                NotNull(configuration, nameof(configuration));
                NotNullOrWhiteSpace(configuration.Key, nameof(configuration.Key));

                if (!Configurations.ContainsKey(configuration.Key))
                {
                    Configurations.Add(configuration.Key, configuration);
                }
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
            NotNullOrWhiteSpace(configurationName, nameof(configurationName));

            if (!Configurations.ContainsKey(configurationName))
            {
#if NETSTANDARD
                throw new InvalidOperationException("No configuration added for configuration name " + configurationName);
#else
                // check connection strings if there is one matching the name
                var connectionStringHolder = ConfigurationManager.ConnectionStrings[configurationName];
                if (connectionStringHolder == null || string.IsNullOrWhiteSpace(connectionStringHolder.ConnectionString))
                {
                    throw new InvalidOperationException("No configuration added for configuration name " + configurationName);
                }

                // defaulting to database 0, no way to set it via connection strings atm.
                var configuration = new RedisConfiguration(configurationName, connectionStringHolder.ConnectionString, 0);
                AddConfiguration(configuration);
#endif
            }

            return Configurations[configurationName];
        }

#if !NETSTANDARD
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
            NotNullOrWhiteSpace(configFileName, nameof(configFileName));
            NotNullOrWhiteSpace(sectionName, nameof(sectionName));

            Ensure(File.Exists(configFileName), "Configuration file not found [{0}].", configFileName);

            var fileConfig = new ExeConfigurationFileMap();
            fileConfig.ExeConfigFilename = configFileName; // setting exe config file name, this is the one the GetSection method expects.

            // open the file map
            Configuration cfg = ConfigurationManager.OpenMappedExeConfiguration(fileConfig, ConfigurationUserLevel.None);

            // use the opened configuration and load our section
            var section = cfg.GetSection(sectionName) as RedisConfigurationSection;
            EnsureNotNull(section, "No section with name {1} found in file {0}", configFileName, sectionName);

            LoadConfiguration(section);
        }

        /// <summary>
        /// Loads the configuration.
        /// </summary>
        /// <param name="section">The section.</param>
        /// <exception cref="System.ArgumentNullException">If section is null.</exception>
        public static void LoadConfiguration(RedisConfigurationSection section)
        {
            NotNull(section, nameof(section));

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
                            connectionString: redisOption.ConnectionString,
                            database: redisOption.Database));    // fixes #114
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
            NotNullOrWhiteSpace(sectionName, nameof(sectionName));

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
#endif
    }
}