﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core.Internal
{
    internal static class CacheReflectionHelper
    {
        ////internal static ILoggerFactory CreateLoggerFactory(ICacheManagerConfiguration configuration)
        ////{
        ////    NotNull(configuration, nameof(configuration));

        ////    if (configuration.LoggerFactoryType == null)
        ////    {
        ////        return new NullLoggerFactory();
        ////    }

        ////    CheckImplements<ILoggerFactory>(configuration.LoggerFactoryType);

        ////    var args = new object[] { configuration };
        ////    if (configuration.LoggerFactoryTypeArguments != null)
        ////    {
        ////        args = configuration.LoggerFactoryTypeArguments.Concat(args).ToArray();
        ////    }

        ////    return (ILoggerFactory)CreateInstance(configuration.LoggerFactoryType, args);
        ////}

        internal static ICacheSerializer CreateSerializer(ICacheManagerConfiguration configuration, ILoggerFactory loggerFactory)
        {
            NotNull(configuration, nameof(configuration));
            NotNull(loggerFactory, nameof(loggerFactory));

            if (configuration.SerializerType != null)
            {
                CheckImplements<ICacheSerializer>(configuration.SerializerType);

                var args = new object[] { configuration, loggerFactory };
                if (configuration.SerializerTypeArguments != null)
                {
                    args = configuration.SerializerTypeArguments.Concat(args).ToArray();
                }

                return (ICacheSerializer)CreateInstance(configuration.SerializerType, args);
            }

            return null;
        }

        internal static CacheBackplane CreateBackplane(ICacheManagerConfiguration configuration, ILoggerFactory loggerFactory)
        {
            NotNull(configuration, nameof(configuration));
            NotNull(loggerFactory, nameof(loggerFactory));

            if (configuration.BackplaneType != null)
            {
                if (!configuration.CacheHandleConfigurations.Any(p => p.IsBackplaneSource))
                {
                    throw new InvalidOperationException(
                        "At least one cache handle must be marked as the backplane source if a backplane is defined via configuration.");
                }

                CheckExtends<CacheBackplane>(configuration.BackplaneType);

                var args = new object[] { configuration, loggerFactory };
                if (configuration.BackplaneTypeArguments != null)
                {
                    args = configuration.BackplaneTypeArguments.Concat(args).ToArray();
                }

                return (CacheBackplane)CreateInstance(configuration.BackplaneType, args);
            }

            return null;
        }

        internal static ICollection<BaseCacheHandle<TCacheValue>> CreateCacheHandles<TCacheValue>(
            BaseCacheManager<TCacheValue> manager,
            ILoggerFactory loggerFactory,
            ICacheSerializer serializer)
        {
            NotNull(manager, nameof(manager));
            NotNull(loggerFactory, nameof(loggerFactory));

            var logger = loggerFactory.CreateLogger(nameof(CacheReflectionHelper));
            var managerConfiguration = (manager.Configuration as ICacheManagerConfiguration) ?? throw new ArgumentException("Manager's configuration must not be null");
            var handles = new List<BaseCacheHandle<TCacheValue>>();

            foreach (var handleConfiguration in managerConfiguration.CacheHandleConfigurations)
            {
                logger.LogInformation("Creating handle {0} of type {1}.", handleConfiguration.Name, handleConfiguration.HandleType);
                var handleType = handleConfiguration.HandleType;
                var requiresSerializer = false;

                requiresSerializer = handleType.GetTypeInfo().CustomAttributes.Any(p => p.AttributeType == typeof(RequiresSerializerAttribute));

                if (requiresSerializer && serializer == null)
                {
                    throw new InvalidOperationException($"Cache handle {handleType.FullName} requires serialization of cached values but no serializer has been configured.");
                }

                Type instanceType = null;

                ValidateCacheHandleGenericTypeArguments(handleType);

                // if the configured type doesn't have a generic type definition ( <T> is not
                // defined )

                if (handleType.GetTypeInfo().IsGenericTypeDefinition)

                {
                    instanceType = handleType.MakeGenericType(new Type[] { typeof(TCacheValue) });
                }
                else
                {
                    instanceType = handleType;
                }

                var types = new List<object>(new object[] { loggerFactory, managerConfiguration, manager, handleConfiguration });
                if (handleConfiguration.ConfigurationTypes.Length > 0)
                {
                    types.AddRange(handleConfiguration.ConfigurationTypes);
                }

                if (serializer != null)
                {
                    types.Add(serializer);
                }

                var instance = CreateInstance(instanceType, types.ToArray()) as BaseCacheHandle<TCacheValue>;

                if (instance == null)
                {
                    throw new InvalidOperationException("Couldn't initialize handle of type " + instanceType.FullName);
                }

                handles.Add(instance);
            }

            if (handles.Count == 0)
            {
                throw new InvalidOperationException("No cache handles defined.");
            }

            // validate backplane is the last handle in the cache manager (only if backplane is configured)
            if (handles.Any(p => p.Configuration.IsBackplaneSource) && manager.Configuration.BackplaneType != null)
            {
                if (!handles.Last().Configuration.IsBackplaneSource)
                {
                    throw new InvalidOperationException("The last cache handle should be the backplane source.");
                }
            }

            return handles;
        }

        internal static object CreateInstance(Type instanceType, object[] knownInstances)
        {
            var constructors = instanceType.GetTypeInfo().DeclaredConstructors;

            constructors = constructors
                .Where(p => !p.IsStatic && p.IsPublic)
                .OrderByDescending(p => p.GetParameters().Length)
                .ToArray();

            if (!constructors.Any())
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture, "No matching public non static constructor found for type {0}.", instanceType.FullName));
            }

            var args = MatchArguments(constructors, knownInstances);

            try
            {
                return Activator.CreateInstance(instanceType, args);
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Failed to initialize instance of type {0}.",
                            instanceType),
                        ex.InnerException);
                }

                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Failed to initialize instance of type {0}.",
                        instanceType),
                    ex);
            }
        }

        private static object[] MatchArguments(IEnumerable<ConstructorInfo> constructors, object[] instances)
        {
            ParameterInfo lastParamMiss = null;
            ConstructorInfo lastCtor = null;

            foreach (var constructor in constructors)
            {
                lastCtor = constructor;
                var args = new List<object>();
                var parameters = constructor.GetParameters();
                var instancesCopy = new List<object>(instances);

                foreach (var param in parameters)
                {
                    var paramValue = instancesCopy
                        .Where(p => p != null)
                        .FirstOrDefault(p => param.ParameterType.GetTypeInfo().IsAssignableFrom(p.GetType().GetTypeInfo()));

                    if (paramValue == null)
                    {
                        lastParamMiss = param;
                        break;
                    }

                    // fixing #112 by not looking at the same instance again which was already added to the args
                    instancesCopy.Remove(paramValue);
                    args.Add(paramValue);
                }

                if (parameters.Length == args.Count)
                {
                    return args.ToArray();
                }
            }

            if (constructors.Any(p => p.GetParameters().Length == 0))
            {
                // no match found, will try empty ctor
                return new object[0];
            }

            // give more detailed error of what failed
            if (lastCtor != null && lastParamMiss != null)
            {
                var ctorTypes = string.Join(", ", lastCtor.GetParameters().Select(p => p.ParameterType.Name).ToArray());

                throw new InvalidOperationException(
                    $"Could not find a matching constructor for type '{lastCtor.DeclaringType?.Name}'. Trying to match [{ctorTypes}] but missing {lastParamMiss.ParameterType?.Name}");
            }

            throw new InvalidOperationException(
                $"Could not find a matching or empty constructor for type '{lastCtor.DeclaringType?.Name}'.");
        }

        private static IEnumerable<Type> GetGenericBaseTypes(this Type type)
        {
            var baseType = type.GetTypeInfo().BaseType;
            if (baseType == null || !baseType.GetTypeInfo().IsGenericType)
            {
                return Enumerable.Empty<Type>();
            }

            var genericBaseType = baseType.GetTypeInfo().IsGenericTypeDefinition ? baseType : baseType.GetGenericTypeDefinition();
            return Enumerable.Repeat(genericBaseType, 1)
                .Concat(baseType.GetGenericBaseTypes());
        }

        private static void ValidateCacheHandleGenericTypeArguments(Type handle)
        {
            // not really needed due to the generic type from callees being restricted.
            if (!handle.GetGenericBaseTypes().Any(p => p == typeof(BaseCacheHandle<>)))
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Configured cache handle does not implement base cache handle [{0}].",
                        handle.ToString()));
            }

            if (handle.IsGenericType && !handle.IsGenericTypeDefinition)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Cache handle type [{0}] should not have any generic arguments defined.",
                        handle.ToString()));
            }
        }

        private static void CheckImplements<TValid>(Type type)
        {
            var interfaces = type.GetInterfaces();

            Ensure(
                interfaces.Any(p => p == typeof(TValid)),
                "Type must implement {0}, but {1} does not.",
                typeof(TValid).Name,
                type.FullName);
        }

        private static void CheckExtends<TValid>(Type type)
        {
            var baseType = type.BaseType;

            while (baseType != typeof(object))
            {
                if (baseType == typeof(TValid))
                {
                    return;
                }

                baseType = type.BaseType;
            }

            throw new InvalidOperationException(
                string.Format(
                    "Type {0} does not extend from {1}.",
                    type.FullName,
                    typeof(TValid).Name));
        }
    }
}
