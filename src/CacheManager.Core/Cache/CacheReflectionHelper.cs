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
        public static ICacheManager<TCacheValue> FromConfiguration<TCacheValue>(string name)
        {
            CacheManagerConfiguration<TCacheValue> managerConfiguration = ConfigurationBuilder.LoadConfiguration<TCacheValue>(name);
            return FromConfiguration<TCacheValue>(managerConfiguration);
        }

        public static ICacheManager<TCacheValue> FromConfiguration<TCacheValue>(CacheManagerConfiguration<TCacheValue> managerConfiguration)
        {
            var manager = new BaseCacheManager<TCacheValue>(managerConfiguration);

            foreach (var handleConfiguration in managerConfiguration.CacheHandles)
            {
                Type handleType = handleConfiguration.HandleType;
                Type instanceType = null;

                ValidateCacheHandleGenericTypeArguments(handleType, typeof(TCacheValue));

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
                    var backPlate = (ICacheBackPlate)Activator.CreateInstance(
                        managerConfiguration.BackPlateType,
                        new object[]
                        {
                            managerConfiguration.BackPlateName,
                            managerConfiguration
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

        public static void ValidateCacheHandleGenericTypeArguments(Type handle, Type arg)
        {
            if (handle.IsInterface)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Interfaces are not allowed as handle type, try change the type of handle [{0}]",
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
                if (!handle.GetGenericArguments().First().Equals(arg))
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Item value type configured [{0}] does not match with the requested generic type argument [{1}]",
                            handle.ToString(),
                            arg.ToString()));
                }
            }

            if (!handle.GetGenericBaseTypes().Any(p => p == typeof(BaseCacheHandle<>)))
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Configured cache handle does not implement BaseCacheHandle<> [{0}].",
                        handle.ToString()));
            }
        }
    }
}