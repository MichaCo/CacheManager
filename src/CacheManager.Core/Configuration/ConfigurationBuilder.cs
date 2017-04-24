using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CacheManager.Core.Internal;

#if !NETSTANDARD
using System.Configuration;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using CacheManager.Core.Configuration;
#endif

using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core
{
    /// <summary>
    /// Helper class to load cache manager configurations from file or to build new configurations
    /// in a fluent way.
    /// <para>
    /// This only loads configurations. To build a cache manager instance, use <c>CacheFactory</c>
    /// and pass in the configuration. Or use the <c>Build</c> methods of <c>CacheFactory</c>!
    /// </para>
    /// </summary>
    /// <see cref="CacheFactory"/>
    public class ConfigurationBuilder : ConfigurationBuilderCachePart
    {
        private const string Hours = "h";
        private const string Minutes = "m";
        private const string Seconds = "s";

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationBuilder"/> class
        /// which provides fluent configuration methods.
        /// </summary>
        public ConfigurationBuilder()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationBuilder"/> class
        /// which provides fluent configuration methods.
        /// </summary>
        /// <param name="name">The name of the cache manager.</param>
        public ConfigurationBuilder(string name)
            : base()
        {
            NotNullOrWhiteSpace(name, nameof(name));
            Configuration.Name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationBuilder"/> class
        /// which provides fluent configuration methods.
        /// Creates a builder which allows to modify the existing <paramref name="forConfiguration"/>.
        /// </summary>
        /// <param name="forConfiguration">The configuration the builder should be instantiated for.</param>
        public ConfigurationBuilder(ICacheManagerConfiguration forConfiguration)
            : base((CacheManagerConfiguration)forConfiguration)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationBuilder"/> class
        /// which provides fluent configuration methods.
        /// Creates a builder which allows to modify the existing <paramref name="forConfiguration"/>.
        /// </summary>
        /// <param name="name">The name of the cache manager.</param>
        /// <param name="forConfiguration">The configuration the builder should be instantiated for.</param>
        public ConfigurationBuilder(string name, ICacheManagerConfiguration forConfiguration)
            : base((CacheManagerConfiguration)forConfiguration)
        {
            NotNullOrWhiteSpace(name, nameof(name));
            Configuration.Name = name;
        }

        /// <summary>
        /// Builds a <see cref="CacheManagerConfiguration"/> which can be used to create a new cache
        /// manager instance.
        /// <para>
        /// Pass the configuration to <see cref="CacheFactory.FromConfiguration{TCacheValue}(ICacheManagerConfiguration)"/>
        /// to create a valid cache manager.
        /// </para>
        /// </summary>
        /// <param name="settings">
        /// The configuration settings to define the cache handles and other properties.
        /// </param>
        /// <returns>The <see cref="ICacheManagerConfiguration"/>.</returns>
        public static ICacheManagerConfiguration BuildConfiguration(Action<ConfigurationBuilderCachePart> settings)
        {
            NotNull(settings, nameof(settings));

            var part = new ConfigurationBuilder();
            settings(part);
            return part.Configuration;
        }

        /// <summary>
        /// Builds a <see cref="CacheManagerConfiguration"/> which can be used to create a new cache
        /// manager instance.
        /// <para>
        /// Pass the configuration to <see cref="CacheFactory.FromConfiguration{TCacheValue}(ICacheManagerConfiguration)"/>
        /// to create a valid cache manager.
        /// </para>
        /// </summary>
        /// <param name="name">The name of the cache manager.</param>
        /// <param name="settings">
        /// The configuration settings to define the cache handles and other properties.
        /// </param>
        /// <returns>The <see cref="ICacheManagerConfiguration"/>.</returns>
        public static ICacheManagerConfiguration BuildConfiguration(string name, Action<ConfigurationBuilderCachePart> settings)
        {
            NotNullOrWhiteSpace(name, nameof(name));
            NotNull(settings, nameof(settings));

            var part = new ConfigurationBuilder();
            settings(part);
            part.Configuration.Name = name;
            return part.Configuration;
        }

#if !NETSTANDARD

        /// <summary>
        /// Loads a configuration from web.config or app.config.
        /// <para>
        /// The <paramref name="configName"/> must match with the name attribute of one of the
        /// configured cache elements.
        /// </para>
        /// </summary>
        /// <param name="configName">The name of the cache element within the config file.</param>
        /// <returns>The <c>CacheManagerConfiguration</c></returns>
        /// <see cref="ICacheManagerConfiguration"/>
        public static ICacheManagerConfiguration LoadConfiguration(string configName) =>
            LoadConfiguration(CacheManagerSection.DefaultSectionName, configName);

        /// <summary>
        /// Loads a configuration from web.config or app.config, by section and config name.
        /// <para>
        /// The <paramref name="configName"/> must match with the name attribute of one of the
        /// configured cache elements.
        /// </para>
        /// </summary>
        /// <param name="sectionName">The name of the section.</param>
        /// <param name="configName">The name of the cache element within the config file.</param>
        /// <returns>The <c>CacheManagerConfiguration</c></returns>
        /// <see cref="ICacheManagerConfiguration"/>
        public static ICacheManagerConfiguration LoadConfiguration(string sectionName, string configName)
        {
            NotNullOrWhiteSpace(sectionName, nameof(sectionName));
            NotNullOrWhiteSpace(configName, nameof(configName));

            var section = ConfigurationManager.GetSection(sectionName) as CacheManagerSection;
            EnsureNotNull(section, "No section defined with name {0}.", sectionName);

            return LoadFromSection(section, configName);
        }

        /// <summary>
        /// Loads a configuration from the given <paramref name="configFileName"/>.
        /// <para>
        /// The <paramref name="configName"/> must match with the name attribute of one of the
        /// configured cache elements.
        /// </para>
        /// </summary>
        /// <param name="configFileName">The full path of the file to load the configuration from.</param>
        /// <param name="configName">The name of the cache element within the config file.</param>
        /// <returns>The <c>CacheManagerConfiguration</c></returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="configFileName"/> or <paramref name="configName"/> are null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// If the file specified by <paramref name="configFileName"/> does not exist.
        /// </exception>
        /// <see cref="ICacheManagerConfiguration"/>
        public static ICacheManagerConfiguration LoadConfigurationFile(string configFileName, string configName) =>
            LoadConfigurationFile(configFileName, CacheManagerSection.DefaultSectionName, configName);

        /// <summary>
        /// Loads a configuration from the given <paramref name="configFileName"/> and <paramref name="sectionName"/>.
        /// <para>
        /// The <paramref name="configName"/> must match with the name attribute of one of the
        /// configured cache elements.
        /// </para>
        /// </summary>
        /// <param name="configFileName">The full path of the file to load the configuration from.</param>
        /// <param name="sectionName">The name of the configuration section.</param>
        /// <param name="configName">The name of the cache element within the config file.</param>
        /// <returns>The <c>CacheManagerConfiguration</c></returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="configFileName"/> or <paramref name="configName"/> are null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// If the file specified by <paramref name="configFileName"/> does not exist.
        /// </exception>
        /// <see cref="ICacheManagerConfiguration"/>
        public static ICacheManagerConfiguration LoadConfigurationFile(string configFileName, string sectionName, string configName)
        {
            NotNullOrWhiteSpace(configFileName, nameof(configFileName));
            NotNullOrWhiteSpace(sectionName, nameof(sectionName));
            NotNullOrWhiteSpace(configName, nameof(configName));

            Ensure(File.Exists(configFileName), "Configuration file not found [{0}].", configFileName);

            var fileConfig = new ExeConfigurationFileMap()
            {
                ExeConfigFilename = configFileName // setting exe config file name, this is the one the GetSection method expects.
            };

            // open the file map
            var cfg = ConfigurationManager.OpenMappedExeConfiguration(fileConfig, ConfigurationUserLevel.None);

            // use the opened configuration and load our section
            var section = cfg.GetSection(sectionName) as CacheManagerSection;
            EnsureNotNull(section, "No section with name {1} found in file {0}", configFileName, sectionName);

            return LoadFromSection(section, configName);
        }

        internal static CacheManagerConfiguration LoadFromSection(CacheManagerSection section, string configName)
        {
            NotNullOrWhiteSpace(configName, nameof(configName));

            var handleDefsSection = section.CacheHandleDefinitions;

            Ensure(handleDefsSection.Count > 0, "There are no cache handles defined.");

            // load handle definitions as lookup
            var handleDefs = new SortedList<string, CacheHandleConfiguration>();
            foreach (var def in handleDefsSection)
            {
                //// don't validate at this point, otherwise we will get an exception if any defined handle doesn't match with the requested type...
                //// CacheReflectionHelper.ValidateCacheHandleGenericTypeArguments(def.HandleType, cacheValue);

                var normId = def.Id.ToUpper(CultureInfo.InvariantCulture);
                handleDefs.Add(
                    normId,
                    new CacheHandleConfiguration(def.Id)
                    {
                        HandleType = def.HandleType,
                        ExpirationMode = def.DefaultExpirationMode,
                        ExpirationTimeout = GetTimeSpan(def.DefaultTimeout, "defaultTimeout")
                    });
            }

            // retrieve the handles collection with the correct name
            var managerCfg = section.CacheManagers.FirstOrDefault(p => p.Name.Equals(configName, StringComparison.OrdinalIgnoreCase));

            EnsureNotNull(managerCfg, "No cache manager configuration found for name [{0}]", configName);

            var maxRetries = managerCfg.MaximumRetries;
            if (maxRetries.HasValue && maxRetries.Value <= 0)
            {
                throw new InvalidOperationException("Maximum number of retries must be greater than zero.");
            }

            var retryTimeout = managerCfg.RetryTimeout;
            if (retryTimeout.HasValue && retryTimeout.Value < 0)
            {
                throw new InvalidOperationException("Retry timeout must be greater than or equal to zero.");
            }

            // build configuration
            var cfg = new CacheManagerConfiguration()
            {
                UpdateMode = managerCfg.UpdateMode,
                MaxRetries = maxRetries ?? 50,
                RetryTimeout = retryTimeout ?? 100
            };

            if (string.IsNullOrWhiteSpace(managerCfg.BackplaneType))
            {
                if (!string.IsNullOrWhiteSpace(managerCfg.BackplaneName))
                {
                    throw new InvalidOperationException("Backplane type cannot be null if backplane name is specified.");
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(managerCfg.BackplaneName))
                {
                    throw new InvalidOperationException("Backplane name cannot be null if backplane type is specified.");
                }

                var backplaneType = Type.GetType(managerCfg.BackplaneType, false);
                EnsureNotNull(backplaneType, "Backplane type not found, '{0}'. Make sure to install the corresponding nuget package.", managerCfg.BackplaneType);

                cfg.BackplaneType = backplaneType;
                cfg.BackplaneConfigurationKey = managerCfg.BackplaneName;
            }

            // build serializer if set
            if (!string.IsNullOrWhiteSpace(managerCfg.SerializerType))
            {
                var serializerType = Type.GetType(managerCfg.SerializerType, false);
                EnsureNotNull(serializerType, "Serializer type not found, {0}.", managerCfg.SerializerType);

                cfg.SerializerType = serializerType;
            }

            foreach (var handleItem in managerCfg)
            {
                var normRefId = handleItem.RefHandleId.ToUpper(CultureInfo.InvariantCulture);

                Ensure(
                    handleDefs.ContainsKey(normRefId),
                    "Referenced cache handle [{0}] cannot be found in cache handles definition.",
                    handleItem.RefHandleId);

                var handleDef = handleDefs[normRefId];

                var handle = new CacheHandleConfiguration(handleItem.Name)
                {
                    HandleType = handleDef.HandleType,
                    ExpirationMode = handleDef.ExpirationMode,
                    ExpirationTimeout = handleDef.ExpirationTimeout,
                    EnableStatistics = managerCfg.EnableStatistics,
                    EnablePerformanceCounters = managerCfg.EnablePerformanceCounters,
                    IsBackplaneSource = handleItem.IsBackplaneSource
                };

                // override default timeout if it is defined in this section.
                if (!string.IsNullOrWhiteSpace(handleItem.Timeout))
                {
                    handle.ExpirationTimeout = GetTimeSpan(handleItem.Timeout, "timeout");
                }

                // override default expiration mode if it is defined in this section.
                if (!string.IsNullOrWhiteSpace(handleItem.ExpirationMode))
                {
                    try
                    {
                        handle.ExpirationMode = (ExpirationMode)Enum.Parse(typeof(ExpirationMode), handleItem.ExpirationMode);
                    }
                    catch (ArgumentException ex)
                    {
                        throw new InvalidOperationException("Invalid value '" + handleItem.ExpirationMode + "'for expiration mode", ex);
                    }
                }

                if (handle.ExpirationMode != ExpirationMode.None && handle.ExpirationTimeout == TimeSpan.Zero)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Expiration mode set without a valid timeout specified for handle [{0}]",
                            handle.Name));
                }

                cfg.CacheHandleConfigurations.Add(handle);
            }

            Ensure(cfg.CacheHandleConfigurations.Count > 0, "There are no valid cache handles linked to the cache manager configuration [{0}]", configName);

            return cfg;
        }

        //// Parses the timespan setting from configuration.
        //// Cfg value can be suffixed with s|h|m for seconds hours or minutes...
        //// Depending on the suffix we have to construct the returned TimeSpan.
        private static TimeSpan GetTimeSpan(string timespanCfgValue, string propName)
        {
            if (string.IsNullOrWhiteSpace(timespanCfgValue))
            {
                // default value coming from the system.configuration seems to be empty string...
                return TimeSpan.Zero;
            }

            var normValue = timespanCfgValue.ToUpper(CultureInfo.InvariantCulture);

            var hasSuffix = Regex.IsMatch(normValue, @"\b[0-9]+[S|H|M]\b");

            var suffix = hasSuffix ? new string(normValue.Last(), 1) : string.Empty;

            var timeoutValue = 0;
            if (!int.TryParse(hasSuffix ? normValue.Substring(0, normValue.Length - 1) : normValue, out timeoutValue))
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture, "The value of the property '{1}' cannot be parsed [{0}].", timespanCfgValue, propName));
            }

            // if minutes or no suffix is defined, we use minutes.
            if (!hasSuffix || suffix.Equals(Minutes, StringComparison.OrdinalIgnoreCase))
            {
                return TimeSpan.FromMinutes(timeoutValue);
            }

            // hours
            if (suffix.Equals(Hours, StringComparison.OrdinalIgnoreCase))
            {
                return TimeSpan.FromHours(timeoutValue);
            }

            // last option would be seconds
            return TimeSpan.FromSeconds(timeoutValue);
        }

#endif
    }

    /// <summary>
    /// Used to build a <c>CacheHandleConfiguration</c>.
    /// </summary>
    /// <see cref="CacheManagerConfiguration"/>
    public sealed class ConfigurationBuilderCacheHandlePart
    {
        private ConfigurationBuilderCachePart _parent;

        internal ConfigurationBuilderCacheHandlePart(CacheHandleConfiguration cfg, ConfigurationBuilderCachePart parentPart)
        {
            Configuration = cfg;
            _parent = parentPart;
        }

        /// <summary>
        /// Gets the parent builder part to add another cache configuration. Can be used to add
        /// multiple cache handles.
        /// </summary>
        /// <value>The parent builder part.</value>
        public ConfigurationBuilderCachePart And => _parent;

        internal CacheHandleConfiguration Configuration { get; }

        /// <summary>
        /// Hands back the new <see cref="CacheManagerConfiguration"/> instance.
        /// </summary>
        /// <returns>The <see cref="CacheManagerConfiguration"/>.</returns>
        public ICacheManagerConfiguration Build()
        {
            return _parent.Build();
        }

        /// <summary>
        /// Disables performance counters for this cache handle.
        /// </summary>
        /// <returns>The builder part.</returns>
        public ConfigurationBuilderCacheHandlePart DisablePerformanceCounters()
        {
            Configuration.EnablePerformanceCounters = false;
            return this;
        }

        /// <summary>
        /// Disables statistic gathering for this cache handle.
        /// <para>This also disables performance counters as statistics are required for the counters.</para>
        /// </summary>
        /// <returns>The builder part.</returns>
        public ConfigurationBuilderCacheHandlePart DisableStatistics()
        {
            Configuration.EnableStatistics = false;
            Configuration.EnablePerformanceCounters = false;
            return this;
        }

        /// <summary>
        /// Enables performance counters for this cache handle.
        /// <para>This also enables statistics, as this is required for performance counters.</para>
        /// </summary>
        /// <returns>The builder part.</returns>
        public ConfigurationBuilderCacheHandlePart EnablePerformanceCounters()
        {
            Configuration.EnablePerformanceCounters = true;
            Configuration.EnableStatistics = true;
            return this;
        }

        /// <summary>
        /// Enables statistic gathering for this cache handle.
        /// <para>The statistics can be accessed via cacheHandle.Stats.GetStatistic.</para>
        /// </summary>
        /// <returns>The builder part.</returns>
        public ConfigurationBuilderCacheHandlePart EnableStatistics()
        {
            Configuration.EnableStatistics = true;
            return this;
        }

        /// <summary>
        /// Sets the expiration mode and timeout of the cache handle.
        /// </summary>
        /// <param name="expirationMode">The expiration mode.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns>The builder part.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// If expiration mode is not set to 'None', timeout cannot be zero.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if expiration mode is not 'None' and timeout is zero.
        /// </exception>
        /// <seealso cref="ExpirationMode"/>
        public ConfigurationBuilderCacheHandlePart WithExpiration(ExpirationMode expirationMode, TimeSpan timeout)
        {
            if (expirationMode != ExpirationMode.None && timeout == TimeSpan.Zero)
            {
                throw new InvalidOperationException("If expiration mode is not set to 'None', timeout cannot be zero.");
            }

            Configuration.ExpirationMode = expirationMode;
            Configuration.ExpirationTimeout = timeout;
            return this;
        }
    }

    /// <summary>
    /// Used to build a <c>CacheManagerConfiguration</c>.
    /// </summary>
    /// <see cref="CacheManagerConfiguration"/>
    public class ConfigurationBuilderCachePart
    {
        internal ConfigurationBuilderCachePart()
        {
            Configuration = new CacheManagerConfiguration();
        }

        internal ConfigurationBuilderCachePart(CacheManagerConfiguration forConfiguration)
        {
            NotNull(forConfiguration, nameof(forConfiguration));
            Configuration = forConfiguration;
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>The configuration.</value>
        internal CacheManagerConfiguration Configuration { get; }

        /// <summary>
        /// Configures the backplane for the cache manager.
        /// <para>
        /// This is an optional feature. If specified, see the documentation for the
        /// <paramref name="backplaneType"/>. The <paramref name="configurationKey"/> might be used to
        /// reference another configuration item.
        /// </para>
        /// <para>
        /// If a backplane is defined, at least one cache handle must be marked as backplane
        /// source. The cache manager then will try to synchronize multiple instances of the same configuration.
        /// </para>
        /// </summary>
        /// <param name="backplaneType">The type of the backplane implementation.</param>
        /// <param name="configurationKey">The name.</param>
        /// <param name="args">Additional arguments the type might need to get initialized.</param>
        /// <returns>The builder instance.</returns>
        /// <exception cref="System.ArgumentNullException">If <paramref name="configurationKey"/> is null.</exception>
        public ConfigurationBuilderCachePart WithBackplane(Type backplaneType, string configurationKey, params object[] args)
        {
            NotNull(backplaneType, nameof(backplaneType));
            NotNullOrWhiteSpace(configurationKey, nameof(configurationKey));

            Configuration.BackplaneType = backplaneType;
            Configuration.BackplaneTypeArguments = args;
            Configuration.BackplaneConfigurationKey = configurationKey;
            return this;
        }

        /// <summary>
        /// Configures the backplane for the cache manager.
        /// <para>
        /// This is an optional feature. If specified, see the documentation for the
        /// <paramref name="backplaneType"/>. The <paramref name="configurationKey"/> might be used to
        /// reference another configuration item.
        /// </para>
        /// <para>
        /// If a backplane is defined, at least one cache handle must be marked as backplane
        /// source. The cache manager then will try to synchronize multiple instances of the same configuration.
        /// </para>
        /// </summary>
        /// <param name="backplaneType">The type of the backplane implementation.</param>
        /// <param name="configurationKey">The configuration key.</param>
        /// <param name="channelName">The backplane channel name.</param>
        /// <param name="args">Additional arguments the type might need to get initialized.</param>
        /// <returns>The builder instance.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// If <paramref name="configurationKey"/> or <paramref name="channelName"/> is null.
        /// </exception>
        public ConfigurationBuilderCachePart WithBackplane(Type backplaneType, string configurationKey, string channelName, params object[] args)
        {
            NotNull(backplaneType, nameof(backplaneType));
            NotNullOrWhiteSpace(configurationKey, nameof(configurationKey));
            NotNullOrWhiteSpace(channelName, nameof(channelName));

            Configuration.BackplaneType = backplaneType;
            Configuration.BackplaneTypeArguments = args;
            Configuration.BackplaneChannelName = channelName;
            Configuration.BackplaneConfigurationKey = configurationKey;
            return this;
        }

        /// <summary>
        /// Adds a cache dictionary cache handle to the cache manager.
        /// </summary>
        /// <param name="isBackplaneSource">
        /// Set this to true if this cache handle should be the source of the backplane.
        /// <para>This setting will be ignored if no backplane is configured.</para>
        /// </param>
        /// <returns>The builder part.</returns>
        public ConfigurationBuilderCacheHandlePart WithDictionaryHandle(bool isBackplaneSource = false) =>
            WithHandle(typeof(DictionaryCacheHandle<>), Guid.NewGuid().ToString("N"), isBackplaneSource);

        /// <summary>
        /// Adds a cache dictionary cache handle to the cache manager.
        /// </summary>
        /// <returns>The builder part.</returns>
        /// <param name="handleName">The name of the cache handle.</param>
        /// <param name="isBackplaneSource">
        /// Set this to true if this cache handle should be the source of the backplane.
        /// <para>This setting will be ignored if no backplane is configured.</para>
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="handleName"/> is null.</exception>
        public ConfigurationBuilderCacheHandlePart WithDictionaryHandle(string handleName, bool isBackplaneSource = false) =>
            WithHandle(typeof(DictionaryCacheHandle<>), handleName, isBackplaneSource);

        /// <summary>
        /// Adds a cache handle with the given <c>Type</c> and name.
        /// The type must be an open generic.
        /// </summary>
        /// <param name="cacheHandleBaseType">The cache handle type.</param>
        /// <param name="handleName">The name to be used for the cache handle.</param>
        /// <param name="isBackplaneSource">
        /// Set this to true if this cache handle should be the source of the backplane.
        /// <para>This setting will be ignored if no backplane is configured.</para>
        /// </param>
        /// <param name="configurationTypes">Internally used only.</param>
        /// <returns>The builder part.</returns>
        /// <exception cref="System.ArgumentNullException">If handleName is null.</exception>
        /// <exception cref="System.InvalidOperationException">
        /// Only one cache handle can be the backplane's source.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if handleName or cacheHandleBaseType are null.
        /// </exception>
        public ConfigurationBuilderCacheHandlePart WithHandle(Type cacheHandleBaseType, string handleName, bool isBackplaneSource, params object[] configurationTypes)
        {
            NotNull(cacheHandleBaseType, nameof(cacheHandleBaseType));
            NotNullOrWhiteSpace(handleName, nameof(handleName));

            var handleCfg = new CacheHandleConfiguration(handleName)
            {
                HandleType = cacheHandleBaseType,
                ConfigurationTypes = configurationTypes
            };

            handleCfg.IsBackplaneSource = isBackplaneSource;

            if (isBackplaneSource && Configuration.CacheHandleConfigurations.Any(p => p.IsBackplaneSource))
            {
                throw new InvalidOperationException("Only one cache handle can be the backplane's source.");
            }

            Configuration.CacheHandleConfigurations.Add(handleCfg);
            var part = new ConfigurationBuilderCacheHandlePart(handleCfg, this);
            return part;
        }

        /// <summary>
        /// Adds a cache handle with the given <c>Type</c> and name.
        /// The type must be an open generic.
        /// </summary>
        /// <param name="cacheHandleBaseType">The cache handle type.</param>
        /// <param name="handleName">The name to be used for the cache handle.</param>
        /// <returns>The builder part.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if handleName or cacheHandleBaseType are null.
        /// </exception>
        public ConfigurationBuilderCacheHandlePart WithHandle(Type cacheHandleBaseType, string handleName)
            => WithHandle(cacheHandleBaseType, handleName, false);

        /// <summary>
        /// Adds a cache handle with the given <c>Type</c>.
        /// The type must be an open generic.
        /// </summary>
        /// <param name="cacheHandleBaseType">The cache handle type.</param>
        /// <returns>The builder part.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if handleName or cacheHandleBaseType are null.
        /// </exception>
        public ConfigurationBuilderCacheHandlePart WithHandle(Type cacheHandleBaseType)
            => WithHandle(cacheHandleBaseType, Guid.NewGuid().ToString("N"), false);

        /// <summary>
        /// Sets the maximum number of retries per action.
        /// <para>Default is 50.</para>
        /// <para>
        /// Not every cache handle implements this, usually only distributed caches will use it.
        /// </para>
        /// </summary>
        /// <param name="retries">The maximum number of retries.</param>
        /// <returns>The configuration builder.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// Maximum number of retries must be greater than 0.
        /// </exception>
        public ConfigurationBuilderCachePart WithMaxRetries(int retries)
        {
            Ensure(retries > 0, "Maximum number of retries must be greater than 0.");

            Configuration.MaxRetries = retries;
            return this;
        }

        /// <summary>
        /// Sets the timeout between each retry of an action in milliseconds.
        /// <para>Default is 100.</para>
        /// <para>
        /// Not every cache handle implements this, usually only distributed caches will use it.
        /// </para>
        /// </summary>
        /// <param name="timeoutMillis">The timeout in milliseconds.</param>
        /// <returns>The configuration builder.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// Retry timeout must be greater than or equal to zero.
        /// </exception>
        public ConfigurationBuilderCachePart WithRetryTimeout(int timeoutMillis)
        {
            Ensure(timeoutMillis >= 0, "Retry timeout must be greater than or equal to zero.");

            Configuration.RetryTimeout = timeoutMillis;
            return this;
        }

        /// <summary>
        /// Sets the update mode of the cache.
        /// <para>If nothing is set, the default will be <c>CacheUpdateMode.None</c>.</para>
        /// </summary>
        /// <param name="updateMode">The update mode.</param>
        /// <returns>The builder part.</returns>
        /// <seealso cref="CacheUpdateMode"/>
        public ConfigurationBuilderCachePart WithUpdateMode(CacheUpdateMode updateMode)
        {
            Configuration.UpdateMode = updateMode;
            return this;
        }

        /// <summary>
        /// Sets the serializer which should be used to serialize cache items.
        /// </summary>
        /// <param name="serializerType">The type of the serializer.</param>
        /// <param name="args">Additional arguments the type might need to get initialized.</param>
        /// <returns>The builder part.</returns>
        public ConfigurationBuilderCachePart WithSerializer(Type serializerType, params object[] args)
        {
            NotNull(serializerType, nameof(serializerType));

            Configuration.SerializerType = serializerType;
            Configuration.SerializerTypeArguments = args;
            return this;
        }

#if !NETSTANDARD

        /// <summary>
        /// Configures a <see cref="BinaryCacheSerializer"/> to be used for serialization and deserialization.
        /// </summary>
        /// <returns>The builder part.</returns>
        public ConfigurationBuilderCachePart WithBinarySerializer()
        {
            Configuration.SerializerType = typeof(BinaryCacheSerializer);
            return this;
        }

        /// <summary>
        /// Configures a <see cref="BinaryCacheSerializer"/> to be used for serialization and deserialization.
        /// </summary>
        /// <param name="serializationFormatter">The <see cref="BinaryFormatter"/> for serialization.</param>
        /// <param name="deserializationFormatter">The <see cref="BinaryFormatter"/> for deserialization.</param>
        /// <returns>The builder part.</returns>
        public ConfigurationBuilderCachePart WithBinarySerializer(BinaryFormatter serializationFormatter, BinaryFormatter deserializationFormatter)
        {
            Configuration.SerializerType = typeof(BinaryCacheSerializer);
            Configuration.SerializerTypeArguments = new object[] { serializationFormatter, deserializationFormatter };
            return this;
        }

#endif

        /// <summary>
        /// Enables logging by setting the <see cref="Logging.ILoggerFactory"/> for the cache manager instance.
        /// </summary>
        /// <param name="loggerFactoryType">The type of the logger factory.</param>
        /// <param name="args">Additional arguments the type might need to get initialized.</param>
        /// <returns>The builder part.</returns>
        public ConfigurationBuilderCachePart WithLogging(Type loggerFactoryType, params object[] args)
        {
            NotNull(loggerFactoryType, nameof(loggerFactoryType));

            Configuration.LoggerFactoryType = loggerFactoryType;
            Configuration.LoggerFactoryTypeArguments = args;
            return this;
        }

        /// <summary>
        /// Hands back the new <see cref="CacheManagerConfiguration"/> instance.
        /// </summary>
        /// <returns>The <see cref="ICacheManagerConfiguration"/>.</returns>
        public ICacheManagerConfiguration Build()
        {
            return Configuration;
        }
    }
}