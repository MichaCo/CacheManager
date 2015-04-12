using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using CacheManager.Core.Configuration;

namespace CacheManager.Core.Cache
{
    internal static class CacheReflectionHelper
    {
        public static ICacheManager<TCacheValue> FromConfiguration<TCacheValue>(string cacheName, string configName)
        {
            CacheManagerConfiguration managerConfiguration = ConfigurationBuilder.LoadConfiguration(configName);
            return FromConfiguration<TCacheValue>(cacheName, managerConfiguration);
        }

        public static ICacheManager<TCacheValue> FromConfiguration<TCacheValue>(string cacheName, CacheManagerConfiguration managerConfiguration)
        {
            var manager = new BaseCacheManager<TCacheValue>(cacheName, managerConfiguration);

            foreach (var handleConfiguration in managerConfiguration.CacheHandles)
            {
                Type handleType = handleConfiguration.HandleType;
                Type instanceType = null;

                ValidateCacheHandleGenericTypeArguments(handleType);

                // if the configured type doesn't have a generic type definition ( <T> is not
                // defined )
                if (handleType.IsGenericTypeDefinition)
                {
                    instanceType = handleType.MakeGenericType(new Type[] { typeof(TCacheValue) });
                }
                else
                {
                    instanceType = handleType;
                }

                var handleInstance = Activator.CreateInstance(instanceType, new object[] { manager, handleConfiguration });
                var instance = handleInstance as BaseCacheHandle<TCacheValue>;

                manager.AddCacheHandle(instance);
            }

            if (managerConfiguration.BackPlateType != null)
            {
                if (!manager.CacheHandles.Any(p => p.Configuration.IsBackPlateSource))
                {
                    throw new InvalidOperationException("At least one cache handle must be marked as the backplate's source.");
                }

                try
                {
                    var backPlate = (CacheBackPlate)Activator.CreateInstance(
                        managerConfiguration.BackPlateType,
                        new object[]
                        {
                            managerConfiguration.BackPlateName,
                            cacheName
                        });

                    manager.SetCacheBackPlate(backPlate);
                }
                catch (TargetInvocationException e)
                {
                    throw e.InnerException;
                }
            }

            return manager;
        }

        public static IEnumerable<Type> GetGenericBaseTypes(this Type type)
        {
            if (!type.BaseType.IsGenericType)
            {
                return Enumerable.Empty<Type>();
            }

            var genericBaseType = type.BaseType.IsGenericTypeDefinition ? type.BaseType : type.BaseType.GetGenericTypeDefinition();
            return Enumerable.Repeat(genericBaseType, 1)
                             .Concat(type.BaseType.GetGenericBaseTypes());
        }

        public static void ValidateCacheHandleGenericTypeArguments(Type handle)
        {
            // not really needed due to the generic type from callees being restricted.
            if (!handle.GetGenericBaseTypes().Any(p => p == typeof(BaseCacheHandle<>)))
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Configured cache handle does not implement BaseCacheHandle<> [{0}].",
                        handle.ToString()));
            }

            if (handle.IsGenericType && !handle.IsGenericTypeDefinition)
            {
                if (handle.GetGenericArguments().Count() != 1)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Invalid number of generic type arguments found for handle [{0}].",
                            handle.ToString()));
                }
                if (handle.GetGenericArguments().Any())
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Cache handle type [{0}] should not have any generic arguments defined. Use typeof(MyType<>).",
                            handle.ToString()));
                }
            }
        }
    }
}