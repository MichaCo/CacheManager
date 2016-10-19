using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using CacheManager.Core;

#if !NET40 && !DNXCORE50

using Couchbase.Configuration.Client;

#endif

using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Tests
{
    public enum Serializer
    {
        Binary,
        Json,
        GzJson,
        Proto
    }

    [ExcludeFromCodeCoverage]
    public static class TestManagers
    {
        ////private const string RedisHost = "ubuntu-local";
        ////private const int RedisPort = 7024; // redis 2.4
        private const string RedisHost = "127.0.0.1";
        private const int RedisPort = 6379;
        private const int StartDbCount = 100;
        private static int databaseCount = StartDbCount;

        public static ICacheManager<object> WithOneDicCacheHandle
            => CacheFactory.Build(
                settings => settings
                    .WithUpdateMode(CacheUpdateMode.Full)
                    .WithDictionaryHandle()
                        .EnableStatistics()
                    .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(1000)));

        public static ICacheManager<object> WithManyDictionaryHandles
            => CacheFactory.Build(
                "manyDicts",
                settings =>
                {
                    settings
                        .WithUpdateMode(CacheUpdateMode.Up)
                        .WithDictionaryHandle()
                            .EnableStatistics()
                        .And.WithDictionaryHandle()
                            .EnableStatistics()
                            .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(1000))
                        .And.WithDictionaryHandle()
                            .EnableStatistics()
                            .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(1000))
                        .And.WithDictionaryHandle()
                            .EnableStatistics()
                            .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(1000))
                        .And.WithDictionaryHandle()
                            .EnableStatistics()
                            .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(1000))
                        .And.WithDictionaryHandle()
                            .EnableStatistics()
                            .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(1000))
                        .And.WithDictionaryHandle()
                            .EnableStatistics()
                            .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(1000));
                });

        public static ICacheManager<object> WithRedisCache
        {
            get
            {
                Interlocked.Increment(ref databaseCount);
                if (databaseCount >= 2000)
                {
                    databaseCount = StartDbCount;
                }

                return CreateRedisCache(databaseCount, false);
            }
        }

        public static ICacheManager<object> WithSystemAndRedisCache
        {
            get
            {
                Interlocked.Increment(ref databaseCount);
                if (databaseCount >= 2000)
                {
                    databaseCount = StartDbCount;
                }

                return CreateRedisAndDicCacheWithBackplane(databaseCount, false, Guid.NewGuid().ToString());
            }
        }

#if !MSBUILD
        public static ICacheManager<object> WithOneMicrosoftMemoryCacheHandle
          => CacheFactory.Build(settings => settings.WithMicrosoftMemoryCacheHandle().EnableStatistics());
#endif

#if !DNXCORE50

        public static ICacheManager<object> WithOneMemoryCacheHandleSliding
            => CacheFactory.Build(
                settings => settings
                    .WithUpdateMode(CacheUpdateMode.Up)
                    .WithSystemRuntimeCacheHandle()
                        .EnableStatistics()
                        .EnablePerformanceCounters()
                    .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(1000)));

        public static ICacheManager<object> WithOneMemoryCacheHandle
            => CacheFactory.Build(settings => settings.WithSystemRuntimeCacheHandle().EnableStatistics());

        public static ICacheManager<object> WithMemoryAndDictionaryHandles
            => CacheFactory.Build(
                settings =>
                {
                    settings
                        .WithUpdateMode(CacheUpdateMode.None)
                        .WithSystemRuntimeCacheHandle()
                            .EnableStatistics()
                        .And.WithSystemRuntimeCacheHandle()
                            .EnableStatistics()
                            .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(1000))
                        .And.WithDictionaryHandle()
                            .EnableStatistics()
                        .And.WithDictionaryHandle()
                            .EnableStatistics()
                            .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(1000));
                });

        public static ICacheManager<object> WithTwoNamedMemoryCaches
            => CacheFactory.Build(
                settings =>
                {
                    settings
                        .WithUpdateMode(CacheUpdateMode.Full)
                        .WithSystemRuntimeCacheHandle("cacheHandleA")
                            .EnableStatistics()
                        .And.WithSystemRuntimeCacheHandle("cacheHandleB")
                            .EnableStatistics();
                });

        public static ICacheManager<object> WithMemcached
        {
            get
            {
                var cache = CacheFactory.Build(settings =>
                {
                    settings.WithUpdateMode(CacheUpdateMode.Full)
                        .WithMemcachedCacheHandle("enyim.com/memcached")
                            .EnableStatistics()
                            .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(1000));
                });

                return cache;
            }
        }

#endif
#if !NET40 && MOCK_HTTPCONTEXT_ENABLED && !DNXCORE50

        public static ICacheManager<object> WithSystemWebCache
            => CacheFactory.Build(
                settings =>
                {
                    settings
                    .WithHandle(typeof(SystemWebCacheHandleWrapper<>))
                        .EnableStatistics();
                });

#endif

#if !NET40 && !DNXCORE50

        public static ICacheManager<object> WithCouchbaseMemcached
        {
            get
            {
                var clientConfiguration = new ClientConfiguration()
                {
                    Servers = new List<Uri>()
                    {
                        new Uri("http://127.0.0.1:8091/pools")
                    },
                    UseSsl = false,
                    BucketConfigs = new Dictionary<string, BucketConfiguration>
                    {
                        {
                            "default",
                            new BucketConfiguration
                            {
                                BucketName = "default",
                                UseSsl = false,
                                PoolConfiguration = new PoolConfiguration
                                {
                                    MaxSize = 10,
                                    MinSize = 5
                                }
                            }
                        }
                    }
                };

                var cache = CacheFactory.Build(settings =>
                {
                    settings
                        .WithCouchbaseConfiguration("couchbase", clientConfiguration)
                        .WithCouchbaseCacheHandle("couchbase", "default")
                            .EnableStatistics();
                });

                return cache;
            }
        }

#endif

        public static ICacheManager<object> CreateRedisAndDicCacheWithBackplane(int database = 0, bool sharedRedisConfig = true, string channelName = null, Serializer serializer = Serializer.Proto)
        {
            var redisKey = sharedRedisConfig ? "redisConfig" : Guid.NewGuid().ToString();
            return CacheFactory.Build(settings =>
            {
                settings
                    .WithUpdateMode(CacheUpdateMode.Up)
                    .WithDictionaryHandle()
                        .EnableStatistics();
                settings
                    .TestSerializer(serializer)
                    .WithMaxRetries(100)
                    .WithRetryTimeout(1000)
                    .WithRedisConfiguration(redisKey, config =>
                    {
                        config
                            .WithAllowAdmin()
                            .WithDatabase(database)
                            .WithEndpoint(RedisHost, RedisPort);
                    })
                    .WithRedisCacheHandle(redisKey, true)
                    .EnableStatistics();

                if (channelName != null)
                {
                    settings.WithRedisBackplane(redisKey, channelName);
                }
                else
                {
                    settings.WithRedisBackplane(redisKey);
                }
            });
        }

        public static ICacheManager<object> CreateRedisCache(int database = 0, bool sharedRedisConfig = true, Serializer serializer = Serializer.GzJson)
        {
            var redisKey = sharedRedisConfig ? "redisConfig" : Guid.NewGuid().ToString();
            var cache = CacheFactory.Build(settings =>
            {
                settings
                    .TestSerializer(serializer)
                    .WithMaxRetries(100)
                    .WithRetryTimeout(1000)
                    .WithRedisConfiguration(redisKey, config =>
                    {
                        config
                            .WithDatabase(database)
                            .WithEndpoint(RedisHost, RedisPort);
                    })
                    ////.WithRedisBackplane(redisKey)
                    .WithRedisCacheHandle(redisKey, true)
                    .EnableStatistics();
            });

            return cache;
        }

        public static ICacheManager<T> CreateRedisCache<T>(int database = 0, bool sharedRedisConfig = true, Serializer serializer = Serializer.GzJson)
        {
            var redisKey = sharedRedisConfig ? "redisConfig" : Guid.NewGuid().ToString();
            var cache = CacheFactory.Build<T>(settings =>
            {
                settings
                    .TestSerializer(serializer)
                    .WithMaxRetries(100)
                    .WithRetryTimeout(1000)
                    .WithRedisConfiguration(redisKey, config =>
                    {
                        config
                            .WithDatabase(database)
                            .WithEndpoint(RedisHost, RedisPort);
                    })
                    .WithRedisBackplane(redisKey)
                    .WithRedisCacheHandle(redisKey, true)
                    .EnableStatistics();
            });

            return cache;
        }

        private static string NewKey() => Guid.NewGuid().ToString();
    }

    [SuppressMessage("Microsoft.Design", "CA1053:StaticHolderTypesShouldNotHaveConstructors", Justification = "Needed for xunit")]
    [ExcludeFromCodeCoverage]
    public class BaseCacheManagerTest
    {
        public static IEnumerable<object[]> TestCacheManagers
        {
            get
            {
#if !DNXCORE50
                yield return new object[] { TestManagers.WithOneMemoryCacheHandleSliding };
                yield return new object[] { TestManagers.WithOneMemoryCacheHandle };
                yield return new object[] { TestManagers.WithMemoryAndDictionaryHandles };
                yield return new object[] { TestManagers.WithTwoNamedMemoryCaches };
#endif
#if !MSBUILD
                yield return new object[] { TestManagers.WithOneMicrosoftMemoryCacheHandle };
#endif
                yield return new object[] { TestManagers.WithManyDictionaryHandles };
                yield return new object[] { TestManagers.WithOneDicCacheHandle };
#if REDISENABLED
                yield return new object[] { TestManagers.WithRedisCache };
                yield return new object[] { TestManagers.WithSystemAndRedisCache };
#endif
                //// yield return new object[] { TestManagers.WithMemcached };
                //// yield return new object[] { TestManagers.WithCouchbaseMemcached };
#if !NET40 && MOCK_HTTPCONTEXT_ENABLED
                yield return new object[] { TestManagers.WithSystemWebCache };
#endif
            }
        }

#if !DNXCORE50

        public static string GetCfgFileName(string fileName)
        {
            NotNullOrWhiteSpace(fileName, nameof(fileName));
            var basePath = Environment.CurrentDirectory;
            return basePath + (fileName.StartsWith("/") ? fileName : "/" + fileName);
        }

#endif
    }

    internal static class ConfigurationExtension
    {
        public static ConfigurationBuilderCachePart TestSerializer(this ConfigurationBuilderCachePart part, Serializer serializer)
        {
            switch (serializer)
            {
                case Serializer.Binary:
                    break;
                case Serializer.GzJson:
                    part.WithGzJsonSerializer();
                    break;
                case Serializer.Json:
                    part.WithJsonSerializer();
                    break;
                case Serializer.Proto:
                    part.WithProtoBufSerializer();
                    break;
            }
            return part;
        }
    }
}

#if DNXCORE50
namespace System.Diagnostics.CodeAnalysis
{
    [Conditional("DEBUG")]
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    internal sealed class ExcludeFromCodeCoverageAttribute : Attribute
    {
    }
}
#endif