using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CacheManager.Core.Internal;

#if !PORTABLE
using System.Configuration;
using System.IO;
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
    public static class ConfigurationBuilder
    {
        private const string Hours = "h";
        private const string Minutes = "m";
        private const string Seconds = "s";

        /// <summary>
        /// Builds a <c>CacheManagerConfiguration</c> which can be used to create a new cache
        /// manager instance.
        /// <para>
        /// Pass the configuration to <c>CacheFactory.FromConfiguration</c> to create a valid cache manager.
        /// </para>
        /// </summary>
        /// <param name="settings">
        /// The configuration settings to define the cache handles and other properties.
        /// </param>
        /// <returns>The <c>CacheManagerConfiguration</c>.</returns>
        public static CacheManagerConfiguration BuildConfiguration(Action<ConfigurationBuilderCachePart> settings)
        {
            NotNull(settings, nameof(settings));

            var part = new ConfigurationBuilderCachePart();
            settings(part);
            return part.Configuration;
        }

#if !PORTABLE

        /// <summary>
        /// Loads a configuration from web.config or app.config.
        /// <para>
        /// The <paramref name="configName"/> must match with the name attribute of one of the
        /// configured cache elements.
        /// </para>
        /// </summary>
        /// <param name="configName">The name of the cache element within the config file.</param>
        /// <returns>The <c>CacheManagerConfiguration</c></returns>
        /// <see cref="CacheManagerConfiguration"/>
        public static CacheManagerConfiguration LoadConfiguration(string configName) =>
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
        /// <see cref="CacheManagerConfiguration"/>
        public static CacheManagerConfiguration LoadConfiguration(string sectionName, string configName)
        {
            NotNullOrWhiteSpace(sectionName, nameof(sectionName));
            NotNullOrWhiteSpace(configName, nameof(configName));

            var section = ConfigurationManager.GetSection(sectionName) as CacheManagerSection;
            EnsureNotNull(section, "No section defined with name " + sectionName);

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
        /// <see cref="CacheManagerConfiguration"/>
        public static CacheManagerConfiguration LoadConfigurationFile(string configFileName, string configName) =>
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
        /// <see cref="CacheManagerConfiguration"/>
        public static CacheManagerConfiguration LoadConfigurationFile(string configFileName, string sectionName, string configName)
        {
            NotNullOrWhiteSpace(configFileName, nameof(configFileName));
            NotNullOrWhiteSpace(sectionName, nameof(sectionName));
            NotNullOrWhiteSpace(configName, nameof(configName));

            Ensure(File.Exists(configFileName), "Configuration file not found [{0}].", configFileName);

            var fileConfig = new ExeConfigurationFileMap();
            fileConfig.ExeConfigFilename = configFileName; // setting exe config file name, this is the one the GetSection method expects.

            // open the file map
            System.Configuration.Configuration cfg = ConfigurationManager.OpenMappedExeConfiguration(fileConfig, ConfigurationUserLevel.None);

            // use the opened configuration and load our section
            var section = cfg.GetSection(sectionName) as CacheManagerSection;
            EnsureNotNull(section, "No section with name {1} found in file {0}", configFileName, sectionName);

            return LoadFromSection(section, configName);
        }

        // todo: refactor -> high complexity
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "BackPlateName", Justification = "no.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "BackPlateType", Justification = "no")]
        internal static CacheManagerConfiguration LoadFromSection(CacheManagerSection section, string configName)
        {
            NotNullOrWhiteSpace(configName, nameof(configName));

            var handleDefsSection = section.CacheHandleDefinitions;

            Ensure(handleDefsSection.Count > 0, "There are no cache handles defined.");

            // load handle definitions as lookup
            var handleDefs = new SortedList<string, CacheHandleConfiguration>();
            foreach (CacheHandleDefinition def in handleDefsSection)
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
            CacheManagerHandleCollection managerCfg = section.CacheManagers.FirstOrDefault(p => p.Name.Equals(configName, StringComparison.OrdinalIgnoreCase));

            EnsureNotNull(managerCfg, "No cache manager configuration found for name [{0}]", configName);

            int? maxRetries = managerCfg.MaximumRetries;
            if (maxRetries.HasValue && maxRetries.Value <= 0)
            {
                throw new InvalidOperationException("Maximum number of retries must be greater than zero.");
            }

            int? retryTimeout = managerCfg.RetryTimeout;
            if (retryTimeout.HasValue && retryTimeout.Value < 0)
            {
                throw new InvalidOperationException("Retry timeout must be greater than or equal to zero.");
            }

            // build configuration
            var cfg = new CacheManagerConfiguration(managerCfg.UpdateMode, maxRetries.HasValue ? maxRetries.Value : int.MaxValue, retryTimeout.HasValue ? retryTimeout.Value : 10);

            if (string.IsNullOrWhiteSpace(managerCfg.BackPlateType))
            {
                if (!string.IsNullOrWhiteSpace(managerCfg.BackPlateName))
                {
                    throw new InvalidOperationException("BackPlateType cannot be null if BackPlateName is specified.");
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(managerCfg.BackPlateName))
                {
                    throw new InvalidOperationException("BackPlateName cannot be null if BackPlateType is specified.");
                }

                cfg = cfg.WithBackPlate(
                    Type.GetType(managerCfg.BackPlateType, true),
                    managerCfg.BackPlateName);
            }

            foreach (CacheManagerHandle handleItem in managerCfg)
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
                    IsBackPlateSource = handleItem.IsBackPlateSource
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
                            handle.HandleName));
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

            string normValue = timespanCfgValue.ToUpper(CultureInfo.InvariantCulture);

            bool hasSuffix = Regex.IsMatch(normValue, @"\b[0-9]+[S|H|M]\b");

            string suffix = hasSuffix ? new string(normValue.Last(), 1) : string.Empty;

            int timeoutValue = 0;
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
        private ConfigurationBuilderCachePart parent;

        internal ConfigurationBuilderCacheHandlePart(CacheHandleConfiguration cfg, ConfigurationBuilderCachePart parentPart)
        {
            this.Configuration = cfg;
            this.parent = parentPart;
        }

        /// <summary>
        /// Gets the parent builder part to add another cache configuration. Can be used to add
        /// multiple cache handles.
        /// </summary>
        /// <value>The parent builder part.</value>
        public ConfigurationBuilderCachePart And => this.parent;

        internal CacheHandleConfiguration Configuration { get; }

        /// <summary>
        /// Disables performance counters for this cache handle.
        /// </summary>
        /// <returns>The builder part.</returns>
        public ConfigurationBuilderCacheHandlePart DisablePerformanceCounters()
        {
            this.Configuration.EnablePerformanceCounters = false;
            return this;
        }

        /// <summary>
        /// Disables statistic gathering for this cache handle.
        /// <para>This also disables performance counters as statistics are required for the counters.</para>
        /// </summary>
        /// <returns>The builder part.</returns>
        public ConfigurationBuilderCacheHandlePart DisableStatistics()
        {
            this.Configuration.EnableStatistics = false;
            this.Configuration.EnablePerformanceCounters = false;
            return this;
        }

        /// <summary>
        /// Enables performance counters for this cache handle.
        /// <para>This also enables statistics, as this is required for performance counters.</para>
        /// </summary>
        /// <returns>The builder part.</returns>
        public ConfigurationBuilderCacheHandlePart EnablePerformanceCounters()
        {
            this.Configuration.EnablePerformanceCounters = true;
            this.Configuration.EnableStatistics = true;
            return this;
        }

        /// <summary>
        /// Enables statistic gathering for this cache handle.
        /// <para>The statistics can be accessed via cacheHandle.Stats.GetStatistic.</para>
        /// </summary>
        /// <returns>The builder part.</returns>
        public ConfigurationBuilderCacheHandlePart EnableStatistics()
        {
            this.Configuration.EnableStatistics = true;
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

            this.Configuration.ExpirationMode = expirationMode;
            this.Configuration.ExpirationTimeout = timeout;
            return this;
        }
    }

    /// <summary>
    /// Used to build a <c>CacheManagerConfiguration</c>.
    /// </summary>
    /// <see cref="CacheManagerConfiguration"/>
    public sealed class ConfigurationBuilderCachePart
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationBuilderCachePart"/> class.
        /// </summary>
        internal ConfigurationBuilderCachePart()
        {
            this.Configuration = new CacheManagerConfiguration();
        }

        /// <summary>
        /// Gets or sets the configuration.
        /// </summary>
        /// <value>The configuration.</value>
        internal CacheManagerConfiguration Configuration { get; set; }

        /// <summary>
        /// Configures the back plate for the cache manager.
        /// <para>
        /// This is an optional feature. If specified, see the documentation for the
        /// <typeparamref name="TBackPlate"/>. The <paramref name="name"/> might be used to
        /// reference another configuration item.
        /// </para>
        /// <para>
        /// If a back plate is defined, at least one cache handle must be marked as back plate
        /// source. The cache manager then will try to synchronize multiple instances of the same configuration.
        /// </para>
        /// </summary>
        /// <typeparam name="TBackPlate">The type of the back plate implementation.</typeparam>
        /// <param name="name">The name.</param>
        /// <returns>The builder instance.</returns>
        /// <exception cref="System.ArgumentNullException">If name is null.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "Users should use the extensions.")]
        public ConfigurationBuilderCachePart WithBackPlate<TBackPlate>(string name)
            where TBackPlate : CacheBackPlate
        {
            NotNullOrWhiteSpace(name, nameof(name));

            this.Configuration = this.Configuration.WithBackPlate(typeof(TBackPlate), name);
            return this;
        }

        /// <summary>
        /// Add a cache handle configuration with the required name and type attributes.
        /// </summary>
        /// <param name="cacheHandleBaseType">The handle's type without generic attribute.</param>
        /// <param name="handleName">The name to be used for the cache handle.</param>
        /// <returns>The builder part.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if handleName or cacheHandleBaseType are null.
        /// </exception>
        public ConfigurationBuilderCacheHandlePart WithHandle(Type cacheHandleBaseType, string handleName) =>
            this.WithHandle(cacheHandleBaseType, handleName, false);

        /// <summary>
        /// Adds a cache dictionary cache handle with the required name.
        /// </summary>
        /// <param name="handleName">The name to be used for the cache handle.</param>
        /// <returns>The builder part.</returns>
        /// <exception cref="ArgumentNullException">Thrown if handleName is null.</exception>
        public ConfigurationBuilderCacheHandlePart WithDictionaryHandle(string handleName) =>
            this.WithHandle(typeof(DictionaryCacheHandle<>), handleName, false);

        /// <summary>
        /// Add a cache handle configuration with the required name and type attributes.
        /// </summary>
        /// <param name="cacheHandleBaseType">The handle's type without generic attribute.</param>
        /// <param name="handleName">The name to be used for the cache handle.</param>
        /// <param name="isBackPlateSource">
        /// Set this to true if this cache handle should be the source of the back plate.
        /// <para>This setting will be ignored if no back plate is configured.</para>
        /// </param>
        /// <returns>The builder part.</returns>
        /// <exception cref="System.ArgumentNullException">If handleName is null.</exception>
        /// <exception cref="System.InvalidOperationException">
        /// Only one cache handle can be the backplate's source.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if handleName or cacheHandleBaseType are null.
        /// </exception>
        public ConfigurationBuilderCacheHandlePart WithHandle(Type cacheHandleBaseType, string handleName, bool isBackPlateSource)
        {
            NotNull(cacheHandleBaseType, nameof(cacheHandleBaseType));
            NotNullOrWhiteSpace(handleName, nameof(handleName));

            var handleCfg = new CacheHandleConfiguration(handleName)
            {
                HandleType = cacheHandleBaseType
            };

            handleCfg.IsBackPlateSource = isBackPlateSource;

            if (this.Configuration.CacheHandleConfigurations.Any(p => p.IsBackPlateSource))
            {
                throw new InvalidOperationException("Only one cache handle can be the back plate's source.");
            }

            this.Configuration.CacheHandleConfigurations.Add(handleCfg);
            var part = new ConfigurationBuilderCacheHandlePart(handleCfg, this);
            return part;
        }

        /// <summary>
        /// Sets the maximum number of retries per action.
        /// <para>Default is <see cref="int.MaxValue"/>.</para>
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

            this.Configuration.MaxRetries = retries;
            return this;
        }

        /// <summary>
        /// Sets the timeout between each retry of an action in milliseconds.
        /// <para>Default is 10.</para>
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

            this.Configuration.RetryTimeout = timeoutMillis;
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
            this.Configuration.CacheUpdateMode = updateMode;
            return this;
        }
    }
}