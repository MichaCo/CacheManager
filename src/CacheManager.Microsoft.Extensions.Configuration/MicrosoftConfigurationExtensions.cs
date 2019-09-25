using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CacheManager.Core;
using CacheManager.Core.Internal;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Extensions for the Microsoft configuration framework to load <see cref="CacheManagerConfiguration"/>s.
    /// </summary>
    public static class MicrosoftConfigurationExtensions
    {
        private const string CacheManagersSection = "cacheManagers";
        private const string RedisSection = "redis";
        private const string SerializerSection = "serializer";
        private const string LoggerFactorySection = "loggerFactory";
        private const string HandlesSection = "handles";
        private const string ConfigurationKey = "key";
        private const string ConfigurationName = "name";
        private const string ConfigurationType = "type";
        private const string ConfigurationKnownType = "knownType";
        private const string TypeJsonCacheSerializer = "CacheManager.Serialization.Json.JsonCacheSerializer, CacheManager.Serialization.Json";
        private const string TypeGzJsonCacheSerializer = "CacheManager.Serialization.Json.GzJsonCacheSerializer, CacheManager.Serialization.Json";
        private const string TypeProtobufCacheSerializer = "CacheManager.Serialization.ProtoBuf.ProtoBufSerializer, CacheManager.Serialization.ProtoBuf";
        private const string TypeBondCompactBinarySerializer = "CacheManager.Serialization.Bond.BondCompactBinaryCacheSerializer, CacheManager.Serialization.Bond";
        private const string TypeBondFastBinarySerializer = "CacheManager.Serialization.Bond.BondFastBinaryCacheSerializer, CacheManager.Serialization.Bond";
        private const string TypeBondSimpleJsonSerializer = "CacheManager.Serialization.Bond.BondSimpleJsonCacheSerializer, CacheManager.Serialization.Bond";
        private const string TypeMicrosoftLoggerFactory = "CacheManager.Logging.MicrosoftLoggerFactoryAdapter, CacheManager.Microsoft.Extensions.Logging";
        private const string TypeRedisBackplane = "CacheManager.Redis.RedisCacheBackplane, CacheManager.StackExchange.Redis";
        private const string TypeSystemRuntimeHandle = "CacheManager.SystemRuntimeCaching.MemoryCacheHandle`1, CacheManager.SystemRuntimeCaching";
        private const string TypeSystemWebHandle = "CacheManager.Web.SystemWebCacheHandle`1, CacheManager.Web";
        private const string TypeRedisHandle = "CacheManager.Redis.RedisCacheHandle`1, CacheManager.StackExchange.Redis";
        private const string TypeCouchbaseHandle = "CacheManager.Couchbase.BucketCacheHandle`1, CacheManager.Couchbase";
        private const string TypeMemcachedHandle = "CacheManager.Memcached.MemcachedCacheHandle`1, CacheManager.Memcached";
        private const string TypeMsExtensionMemoryCacheHandle = "CacheManager.MicrosoftCachingMemory.MemoryCacheHandle`1, CacheManager.Microsoft.Extensions.Caching.Memory";
        private const string TypeRedisConfiguration = "CacheManager.Redis.RedisConfiguration, CacheManager.StackExchange.Redis";
        private const string TypeRedisConfigurations = "CacheManager.Redis.RedisConfigurations, CacheManager.StackExchange.Redis";
        private const string KnonwSerializerBinary = "binary";
        private const string KnonwSerializerJson = "json";
        private const string KnonwSerializerGzJson = "gzjson";
        private const string KnonwSerializerProto = "protobuf";
        private const string KnonwSerializerBondCompact = "bondcompactbinary";
        private const string KnonwSerializerBondFast = "bondfastbinary";
        private const string KnonwSerializerBondJson = "bondsimplejson";

        /// <summary>
        /// Gets the first and only <see cref="CacheManagerConfiguration"/> defined in
        /// the <code>cacheManagers</code> section of the provided <paramref name="configuration"/>.
        /// </summary>
        /// <param name="configuration">The source configuration.</param>
        /// <returns>The <c cref="ICacheManagerConfiguration"/>.</returns>
        /// <exception cref="InvalidOperationException">If no cacheManagers section is defined or more than one manager is configured.</exception>
        public static ICacheManagerConfiguration GetCacheConfiguration(this IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            configuration.LoadRedisConfigurations();

            var managersSection = configuration.GetSection(CacheManagersSection);
            if (managersSection.GetChildren().Count() == 0)
            {
                throw new InvalidOperationException(
                    $"No '{CacheManagersSection}' section found in the configuration provided.");
            }

            if (managersSection.GetChildren().Count() > 1)
            {
                throw new InvalidOperationException(
                    $"The '{CacheManagersSection}' section has more than one configuration defined. Please specifiy which one to load by name.");
            }

            return GetFromConfiguration(managersSection.GetChildren().First());
        }

        /// <summary>
        /// Retrieve a <see cref="CacheManagerConfiguration"/> defined in
        /// the <code>cacheManagers</code> section of the provided <paramref name="configuration"/> by <paramref name="name"/>.
        /// </summary>
        /// <returns>The <see cref="ICacheManagerConfiguration"/>.</returns>
        /// <param name="configuration">The source configuration.</param>
        /// <param name="name">The name of the cache.</param>
        /// <exception cref="ArgumentNullException">
        /// If either <paramref name="configuration"/> or <paramref name="name"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// If no <code>cacheManagers</code> section is defined in the <paramref name="configuration"/>,
        /// or if no configuration was found for the <paramref name="name"/>.
        /// </exception>
        public static ICacheManagerConfiguration GetCacheConfiguration(this IConfiguration configuration, string name)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            configuration.LoadRedisConfigurations();

            var managersSection = configuration.GetSection(CacheManagersSection);
            if (managersSection.GetChildren().Count() > 0)
            {
                return GetByName(managersSection, name);
            }

            throw new InvalidOperationException($"No '{CacheManagersSection}' section found in the configuration provided.");
        }

        /// <summary>
        /// Retrieves all configured <see cref="CacheManagerConfiguration"/>s defined in
        /// the <code>cacheManagers</code> section of the provided <paramref name="configuration"/>.
        /// </summary>
        /// <param name="configuration">The source configuration.</param>
        /// <returns>The list of <see cref="ICacheManagerConfiguration"/>s.</returns>
        /// <exception cref="InvalidOperationException">If no <code>cacheManagers</code> section is defined.</exception>
        public static IEnumerable<ICacheManagerConfiguration> GetCacheConfigurations(this IConfiguration configuration)
        {
            configuration.LoadRedisConfigurations();

            var managersSection = configuration.GetSection(CacheManagersSection);
            if (managersSection.GetChildren().Count() == 0)
            {
                throw new InvalidOperationException($"No '{CacheManagersSection}' section found in the configuration provided.");
            }

            foreach (var managerConfig in managersSection.GetChildren())
            {
                yield return GetFromConfiguration(managerConfig);
            }
        }

        /// <summary>
        /// Loads all available Redis configurations from the the <code>redis</code> section of the provided <paramref name="configuration"/>.
        /// </summary>
        /// <remarks>
        /// This method always gets invoked by <see cref="GetCacheConfiguration(IConfiguration)"/> or the other overloads.
        /// You do not have to call this explicitly. The method also does not throw an exception if the <code>redis</code> section is
        /// not defined.
        /// </remarks>
        /// <param name="configuration">The source configuration.</param>
        /// <exception cref="InvalidOperationException">If the CacheManager.StackExchange.Redis package is not installed.</exception>
        public static void LoadRedisConfigurations(this IConfiguration configuration)
        {
            // load redis configurations if available
            if (configuration.GetSection(RedisSection).GetChildren().Count() > 0)
            {
                try
                {
                    var redisConfigurationType = Type.GetType(TypeRedisConfiguration, true);
                    var redisConfigurationsType = Type.GetType(TypeRedisConfigurations, true);

                    var addRedisConfiguration = redisConfigurationsType
                        .GetTypeInfo()
                        .DeclaredMethods
                        .FirstOrDefault(
                            p => p.Name == "AddConfiguration" &&
                            p.GetParameters().Length == 1 &&
                            p.GetParameters().First().ParameterType == redisConfigurationType);

                    if (addRedisConfiguration == null)
                    {
                        throw new InvalidOperationException("RedisConfigurations type might have changed or cannot be invoked.");
                    }

                    foreach (var redisConfig in configuration.GetSection(RedisSection).GetChildren())
                    {
                        if (string.IsNullOrWhiteSpace(redisConfig[ConfigurationKey]))
                        {
                            throw new InvalidOperationException(
                                $"Key is required in redis configuration but is not configured in '{redisConfig.Path}'.");
                        }

                        if (string.IsNullOrWhiteSpace(redisConfig["connectionString"]) &&
                            redisConfig.GetSection("endpoints").GetChildren().Count() == 0)
                        {
                            throw new InvalidOperationException(
                                $"Either connection string or endpoints must be configured in '{redisConfig.Path}' for a redis connection.");
                        }

                        var configInstance = Activator.CreateInstance(redisConfigurationType);
                        redisConfig.Bind(configInstance);
                        addRedisConfiguration.Invoke(null, new object[] { configInstance });
                    }
                }
                catch (FileNotFoundException ex)
                {
                    throw new InvalidOperationException(
                        "Redis types could not be loaded. Make sure that you have the CacheManager.Stackexchange.Redis package installed.",
                        ex);
                }
                catch (TypeLoadException ex)
                {
                    throw new InvalidOperationException(
                        "Redis types could not be loaded. Make sure that you have the CacheManager.Stackexchange.Redis package installed.",
                        ex);
                }
            }
        }

        private static CacheManagerConfiguration GetByName(IConfiguration configuration, string name)
        {
            var section = configuration.GetChildren().FirstOrDefault(p => p[ConfigurationName] == name);
            if (section == null)
            {
                throw new InvalidOperationException(
                    $"CacheManager configuration for name '{name}' not found.");
            }

            return GetFromConfiguration(section);
        }

        private static CacheManagerConfiguration GetFromConfiguration(IConfigurationSection configuration)
        {
            var managerConfiguration = new CacheManagerConfiguration();
            configuration.Bind(managerConfiguration);

            var handlesConfiguration = configuration.GetSection(HandlesSection);

            if (handlesConfiguration.GetChildren().Count() == 0)
            {
                throw new InvalidOperationException(
                    $"No cache handles defined in '{configuration.Path}'.");
            }

            foreach (var handleConfiguration in handlesConfiguration.GetChildren())
            {
                var cacheHandleConfiguration = GetHandleFromConfiguration(handleConfiguration);
                managerConfiguration.CacheHandleConfigurations.Add(cacheHandleConfiguration);
            }

            GetBackplaneConfiguration(managerConfiguration, configuration);
            GetLoggerFactoryConfiguration(managerConfiguration, configuration);
            GetSerializerConfiguration(managerConfiguration, configuration);

            return managerConfiguration;
        }

        private static CacheHandleConfiguration GetHandleFromConfiguration(IConfigurationSection handleConfiguration)
        {
            var type = handleConfiguration[ConfigurationType];
            var knownType = handleConfiguration[ConfigurationKnownType];
            var key = handleConfiguration[ConfigurationKey] ?? handleConfiguration[ConfigurationName];    // name fallback for key
            var name = handleConfiguration[ConfigurationName];

            var cacheHandleConfiguration = new CacheHandleConfiguration();
            handleConfiguration.Bind(cacheHandleConfiguration);
            cacheHandleConfiguration.Key = key;
            cacheHandleConfiguration.Name = name ?? cacheHandleConfiguration.Name;

            if (string.IsNullOrEmpty(type) && string.IsNullOrEmpty(knownType))
            {
                throw new InvalidOperationException(
                    $"No '{ConfigurationType}' or '{ConfigurationKnownType}' defined in cache handle configuration '{handleConfiguration.Path}'.");
            }

            if (string.IsNullOrWhiteSpace(type))
            {
                var keyRequired = false;
                cacheHandleConfiguration.HandleType = GetKnownHandleType(knownType, handleConfiguration.Path, out keyRequired);

                // some handles require name or key to be set to link to other parts of the configuration
                // lets check if that condition is satisfied
                if (keyRequired && string.IsNullOrWhiteSpace(key) && string.IsNullOrWhiteSpace(name))
                {
                    throw new InvalidOperationException(
                        $@"Known handle of type '{knownType}' requires '{ConfigurationKey}' or '{ConfigurationName}' to be defined.
                            Check configuration at '{handleConfiguration.Path}'.");
                }
            }
            else
            {
                cacheHandleConfiguration.HandleType = Type.GetType(type, true);
            }

            return cacheHandleConfiguration;
        }

        private static Type GetKnownHandleType(string knownTypeName, string path, out bool keyRequired)
        {
            keyRequired = false;
            try
            {
                switch (knownTypeName.ToLowerInvariant())
                {
                    case "systemruntime":
                        return Type.GetType(TypeSystemRuntimeHandle, true);

                    case "dictionary":
                        return typeof(DictionaryCacheHandle<>);

                    case "systemweb":
                        return Type.GetType(TypeSystemWebHandle, true);

                    case "msmemory":
                        return Type.GetType(TypeMsExtensionMemoryCacheHandle, true);

                    case "redis":
                        keyRequired = true;
                        return Type.GetType(TypeRedisHandle, true);

                    case "couchbase":
                        keyRequired = true;
                        return Type.GetType(TypeCouchbaseHandle, true);

                    case "memcached":
                        keyRequired = true;
                        return Type.GetType(TypeMemcachedHandle, true);
                }
            }
            catch (FileNotFoundException ex)
            {
                throw new InvalidOperationException(
                    $"Type for '{ConfigurationKnownType}' '{knownTypeName}' could not be loaded. Make sure you have installed the corresponding NuGet package.",
                    ex);
            }
            catch (TypeLoadException ex)
            {
                throw new InvalidOperationException(
                    $"Type for '{ConfigurationKnownType}' '{knownTypeName}' could not be loaded. Make sure you have installed the corresponding NuGet package.",
                    ex);
            }

            throw new InvalidOperationException(
                $"Known handle type '{knownTypeName}' is invalid. Check configuration at '{path}'.");
        }

        private static void GetBackplaneConfiguration(CacheManagerConfiguration managerConfiguration, IConfigurationSection configuration)
        {
            var backplaneSection = configuration.GetSection("backplane");
            if (backplaneSection.GetChildren().Count() == 0)
            {
                // no backplane
                return;
            }

            var type = backplaneSection[ConfigurationType];
            var knownType = backplaneSection[ConfigurationKnownType];
            var key = backplaneSection[ConfigurationKey];
            var channelName = backplaneSection["channelName"];

            if (string.IsNullOrEmpty(type) && string.IsNullOrEmpty(knownType))
            {
                throw new InvalidOperationException(
                    $"No '{ConfigurationType}' or '{ConfigurationKnownType}' defined in backplane configuration '{backplaneSection.Path}'.");
            }

            if (string.IsNullOrWhiteSpace(type))
            {
                var keyRequired = false;
                managerConfiguration.BackplaneType = GetKnownBackplaneType(knownType, backplaneSection.Path, out keyRequired);
                if (keyRequired && string.IsNullOrWhiteSpace(key))
                {
                    throw new InvalidOperationException(
                        $"The key property is required for the '{knownType}' backplane, but is not configured in '{backplaneSection.Path}'.");
                }
            }
            else
            {
                managerConfiguration.BackplaneType = Type.GetType(type, true);
            }

            managerConfiguration.BackplaneChannelName = channelName;
            managerConfiguration.BackplaneConfigurationKey = key;
        }

        private static Type GetKnownBackplaneType(string knownTypeName, string path, out bool keyRequired)
        {
            switch (knownTypeName.ToLowerInvariant())
            {
                case "redis":
                    keyRequired = true;
                    return Type.GetType(TypeRedisBackplane, true);
            }

            throw new InvalidOperationException(
                $"Known backplane type '{knownTypeName}' is invalid. Check configuration at '{path}'.");
        }

        private static void GetLoggerFactoryConfiguration(CacheManagerConfiguration managerConfiguration, IConfigurationSection configuration)
        {
            var loggerFactorySection = configuration.GetSection(LoggerFactorySection);

            if (loggerFactorySection.GetChildren().Count() == 0)
            {
                // no logger factory
                return;
            }

            var knownType = loggerFactorySection[ConfigurationKnownType];
            var type = loggerFactorySection[ConfigurationType];

            if (string.IsNullOrWhiteSpace(knownType) && string.IsNullOrWhiteSpace(type))
            {
                throw new InvalidOperationException(
                    $"No '{ConfigurationType}' or '{ConfigurationKnownType}' defined in logger factory configuration '{loggerFactorySection.Path}'.");
            }

            if (string.IsNullOrWhiteSpace(type))
            {
                managerConfiguration.LoggerFactoryType = GetKnownLoggerFactoryType(knownType, loggerFactorySection.Path);
            }
            else
            {
                managerConfiguration.LoggerFactoryType = Type.GetType(type, true);
            }
        }

        private static Type GetKnownLoggerFactoryType(string knownTypeName, string path)
        {
            switch (knownTypeName.ToLowerInvariant())
            {
                case "microsoft":
                    return Type.GetType(TypeMicrosoftLoggerFactory, true);
            }

            throw new InvalidOperationException(
                $"Known logger factory type '{knownTypeName}' is invalid. Check configuration at '{path}'.");
        }

        private static void GetSerializerConfiguration(CacheManagerConfiguration managerConfiguration, IConfigurationSection configuration)
        {
            var serializerSection = configuration.GetSection(SerializerSection);

            if (serializerSection.GetChildren().Count() == 0)
            {
                // no serializer
                return;
            }

            var knownType = serializerSection[ConfigurationKnownType];
            var type = serializerSection[ConfigurationType];

            if (string.IsNullOrWhiteSpace(knownType) && string.IsNullOrWhiteSpace(type))
            {
                throw new InvalidOperationException(
                    $"No '{ConfigurationType}' or '{ConfigurationKnownType}' defined in serializer configuration '{serializerSection.Path}'.");
            }

            if (string.IsNullOrWhiteSpace(type))
            {
                managerConfiguration.SerializerType = GetKnownSerializerType(knownType, serializerSection.Path);
            }
            else
            {
                managerConfiguration.SerializerType = Type.GetType(type, true);
            }
        }

        private static Type GetKnownSerializerType(string knownTypeName, string path)
        {
            try
            {
                switch (knownTypeName.ToLowerInvariant())
                {
#if !NET461
                    case KnonwSerializerBinary:
                        throw new PlatformNotSupportedException("BinaryCacheSerializer is not available on this platform");
#else
                    case KnonwSerializerBinary:
                        return typeof(BinaryCacheSerializer);

#endif

                    case KnonwSerializerJson:
                        return Type.GetType(TypeJsonCacheSerializer, true);

                    case KnonwSerializerGzJson:
                        return Type.GetType(TypeGzJsonCacheSerializer, true);

                    case KnonwSerializerProto:
                        return Type.GetType(TypeProtobufCacheSerializer, true);

                    case KnonwSerializerBondCompact:
                        return Type.GetType(TypeBondCompactBinarySerializer, true);

                    case KnonwSerializerBondFast:
                        return Type.GetType(TypeBondFastBinarySerializer, true);

                    case KnonwSerializerBondJson:
                        return Type.GetType(TypeBondSimpleJsonSerializer, true);
                }
            }
            catch (Exception ex) when (!(ex is PlatformNotSupportedException))
            {
                throw new InvalidOperationException(
                    $"Known serializer type '{knownTypeName}' could not be loaded. Make sure the corresponding Nuget package is installed and check configuration at '{path}'.", ex);
            }

            // specified known type is actually not known - configuration error...
            throw new InvalidOperationException(
                $"Known serializer type '{knownTypeName}' is invalid. Check configuration at '{path}'.");
        }
    }
}
