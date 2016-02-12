using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using CacheManager.Core.Internal;
using Microsoft.Extensions.Configuration;

namespace CacheManager.Core
{
    public static class ConfigurationExtensions
    {
        public static CacheManagerConfiguration GetCacheConfiguration(this IConfiguration configuration, string name)
        {
            configuration.LoadRedisConfigurations();

            var managersSection = configuration.GetSection("cacheManagers");
            if (managersSection.GetChildren().Count() > 0)
            {
                return GetByNameFromRoot(managersSection, name);
            }

            throw new InvalidOperationException("No 'cacheManagers' section found in the configuration provided.");
        }

        public static void LoadRedisConfigurations(this IConfiguration configuration)
        {
            // load redis configurations if available
            if (configuration.GetSection("redis").GetChildren().Count() > 0)
            {
                try
                {
                    var redisConfigurationType = Type.GetType("CacheManager.Redis.RedisConfiguration, CacheManager.StackExchange.Redis", true);
                    var redisConfigurationsType = Type.GetType("CacheManager.Redis.RedisConfigurations, CacheManager.StackExchange.Redis", true);

                    var addRedisConfiguration = redisConfigurationsType
                        .GetTypeInfo()
                        .DeclaredMethods
                        .FirstOrDefault(p => p.Name == "AddConfiguration" && p.GetParameters().Length == 1 && p.GetParameters().First().ParameterType == redisConfigurationType);

                    if (addRedisConfiguration == null)
                    {
                        throw new InvalidOperationException("RedisConfigurations type might have changed or cannot be invoked.");
                    }

                    foreach (var redisConfig in configuration.GetSection("redis").GetChildren())
                    {
                        if (string.IsNullOrWhiteSpace(redisConfig["key"]))
                        {
                            throw new InvalidOperationException(
                                string.Format(
                                    CultureInfo.InvariantCulture,
                                    "Key is required in redis configuration but is not configured in '{0}'.",
                                    redisConfig.Path));
                        }

                        if (string.IsNullOrWhiteSpace(redisConfig["connectionString"]) &&
                            redisConfig.GetSection("endpoints").GetChildren().Count() == 0)
                        {
                            throw new InvalidOperationException(
                                string.Format(
                                    CultureInfo.InvariantCulture,
                                    "Either connection string or endpoints must be configured in '{0}' for a redis connection.",
                                    redisConfig.Path));
                        }

                        var redis = redisConfig.Get(redisConfigurationType);
                        addRedisConfiguration.Invoke(null, new object[] { redis });
                    }
                }
                catch (TypeLoadException ex)
                {
                    throw new InvalidOperationException("Redis types could not be loaded. Make sure that you have installed the CacheManager.Redis.Stackexchange package.", ex);
                }
            }
        }

        private static CacheManagerConfiguration GetByNameFromRoot(IConfiguration configuration, string name)
        {
            var section = configuration.GetChildren().FirstOrDefault(p => p["name"] == name);
            if (section == null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "CacheManager configuration for name '{0}' not found.",
                        name));
            }

            return GetFromConfiguration(section);
        }

        private static CacheManagerConfiguration GetFromConfiguration(IConfigurationSection configuration)
        {
            var managerConfiguration = configuration.Get<CacheManagerConfiguration>();

            var handlesConfiguration = configuration.GetSection("handles");

            if (handlesConfiguration.GetChildren().Count() == 0)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "No cache handles defined in '{0}'.",
                        configuration.Path));
            }

            foreach (var handleConfiguration in handlesConfiguration.GetChildren())
            {
                var cacheHandleConfiguration = GetHandleFromConfiguration(handleConfiguration);
                managerConfiguration.CacheHandleConfigurations.Add(cacheHandleConfiguration);
            }

            GetBackPlateConfiguration(managerConfiguration, configuration);
            GetLoggerFactoryConfiguration(managerConfiguration, configuration);
            GetSerializerConfiguration(managerConfiguration, configuration);

            return managerConfiguration;
        }

        private static CacheHandleConfiguration GetHandleFromConfiguration(IConfigurationSection handleConfiguration)
        {
            var type = handleConfiguration["type"];
            var knownType = handleConfiguration["knownType"];
            var key = handleConfiguration["key"] ?? handleConfiguration["name"];    // name fallback for key
            var name = handleConfiguration["name"];

            var cacheHandleConfiguration = handleConfiguration.Get<CacheHandleConfiguration>();
            cacheHandleConfiguration.Key = key;
            cacheHandleConfiguration.Name = name ?? cacheHandleConfiguration.Name;

            if (string.IsNullOrEmpty(type) && string.IsNullOrEmpty(knownType))
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "No type or known type defined in cache handle configuration '{0}'.",
                        handleConfiguration.Path));
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
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Known handle of type '{0}' requires 'key' or 'name' to be defined. Check configuration at '{1}'.",
                            knownType,
                            handleConfiguration.Path));
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
            switch (knownTypeName.ToLowerInvariant())
            {
                case "systemruntime":
                    return Type.GetType("CacheManager.SystemRuntimeCaching.MemoryCacheHandle`1, CacheManager.SystemRuntimeCaching", true);
                case "dictionary":
                    return typeof(DictionaryCacheHandle<>);
                case "web":
                    return Type.GetType("CacheManager.Web.SystemWebCacheHandle`1, CacheManager.Web", true);
                case "redis":
                    keyRequired = true;
                    return Type.GetType("CacheManager.Redis.RedisCacheHandle`1, CacheManager.StackExchange.Redis", true);
                case "couchbase":
                    keyRequired = true;
                    return Type.GetType("CacheManager.Couchbase.BucketCacheHandle`1, CacheManager.Couchbase", true);
                case "memcached":
                    keyRequired = true;
                    return Type.GetType("CacheManager.Memcached.MemcachedCacheHandle`1, CacheManager.Memcached", true);
            }

            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Known handle type '{0}' is invalid. Check configuration at '{1}'.",
                    knownTypeName,
                    path));
        }

        private static void GetBackPlateConfiguration(CacheManagerConfiguration managerConfiguration, IConfigurationSection configuration)
        {
            var backPlateSection = configuration.GetSection("backPlate");
            if (backPlateSection.GetChildren().Count() == 0)
            {
                // no backplate
                return;
            }

            var type = backPlateSection["type"];
            var knownType = backPlateSection["knownType"];
            var key = backPlateSection["key"];
            var channelName = backPlateSection["channelName"];

            if (string.IsNullOrEmpty(type) && string.IsNullOrEmpty(knownType))
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "No type or known type defined in back plate configuration {0}.",
                        backPlateSection.Path));
            }

            if (string.IsNullOrWhiteSpace(type))
            {
                var keyRequired = false;
                managerConfiguration.BackPlateType = GetKnownBackPlateType(knownType, backPlateSection.Path, out keyRequired);
                if (keyRequired && string.IsNullOrWhiteSpace(key))
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The key property is required for the {0} back plate, but is not configured in {1}.",
                            knownType,
                            backPlateSection.Path));
                }
            }
            else
            {
                managerConfiguration.BackPlateType = Type.GetType(type, true);
            }

            managerConfiguration.BackPlateChannelName = channelName;
            managerConfiguration.BackPlateConfigurationKey = key;
        }

        private static Type GetKnownBackPlateType(string knownTypeName, string path, out bool keyRequired)
        {
            switch (knownTypeName.ToLowerInvariant())
            {
                case "redis":
                    keyRequired = true;
                    return Type.GetType("CacheManager.Redis.RedisCacheBackPlate, CacheManager.StackExchange.Redis", true);
            }

            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Known back-plate type '{0}' is invalid. Check configuration at {1}.",
                    knownTypeName,
                    path));
        }

        private static void GetLoggerFactoryConfiguration(CacheManagerConfiguration managerConfiguration, IConfigurationSection configuration)
        {
            var loggerFactorySection = configuration.GetSection("loggerFactory");

            if (loggerFactorySection.GetChildren().Count() == 0)
            {
                // no logger factory
                return;
            }

            var knownType = loggerFactorySection["knownType"];
            var type = loggerFactorySection["type"];

            if (string.IsNullOrWhiteSpace(knownType) && string.IsNullOrWhiteSpace(type))
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "No type or known type defined in logger factory configuration '{0}'.",
                        loggerFactorySection.Path));
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
                    return Type.GetType("CacheManager.Logging.MicrosoftLoggerFactory, CacheManager.Microsoft.Extensions.Logging", true);
            }

            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Known logger factory type '{0}' is invalid. Check configuration at '{1}'.",
                    knownTypeName,
                    path));
        }

        private static void GetSerializerConfiguration(CacheManagerConfiguration managerConfiguration, IConfigurationSection configuration)
        {
            var serializerSection = configuration.GetSection("serializer");

            if (serializerSection.GetChildren().Count() == 0)
            {
                // no serializer
                return;
            }

            var knownType = serializerSection["knownType"];
            var type = serializerSection["type"];

            if (string.IsNullOrWhiteSpace(knownType) && string.IsNullOrWhiteSpace(type))
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "No type or known type defined in serializer configuration '{0}'.",
                        serializerSection.Path));
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
            switch (knownTypeName.ToLowerInvariant())
            {
                case "binary":
#if DOTNET5_4
                    throw new InvalidOperationException("BinaryCacheSerializer is not available on this platform");
#else
                    return typeof(BinaryCacheSerializer);
#endif
                case "json":
                    return Type.GetType("CacheManager.Serialization.Json.JsonCacheSerializer, CacheManager.Serialization.Json", true);
            }

            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Known serializer type '{0}' is invalid. Check configuration at '{1}'.",
                    knownTypeName,
                    path));
        }
    }
}
