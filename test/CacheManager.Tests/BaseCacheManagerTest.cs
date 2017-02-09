using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using CacheManager.Core;
using CacheManager.Redis;

#if MEMCACHEDENABLED
using Enyim.Caching.Configuration;
#endif

using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Tests
{
    public enum Serializer
    {
        Binary,
        Json,
        GzJson,
        Proto,
        BondBinary
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

        public static ICacheManager<object> WithRedisCacheBinary
        {
            get
            {
                Interlocked.Increment(ref databaseCount);
                if (databaseCount >= 2000)
                {
                    databaseCount = StartDbCount;
                }

                return CreateRedisCache(databaseCount, false, Serializer.Binary);
            }
        }

        public static ICacheManager<object> WithRedisCacheBondBinary
        {
            get
            {
                Interlocked.Increment(ref databaseCount);
                if (databaseCount >= 2000)
                {
                    databaseCount = StartDbCount;
                }

                return CreateRedisCache(databaseCount, false, Serializer.BondBinary);
            }
        }

        public static ICacheManager<object> WithRedisCacheJson
        {
            get
            {
                Interlocked.Increment(ref databaseCount);
                if (databaseCount >= 2000)
                {
                    databaseCount = StartDbCount;
                }

                return CreateRedisCache(database: databaseCount, sharedRedisConfig: false, serializer: Serializer.Json);
            }
        }

        public static ICacheManager<object> WithRedisCacheJsonNoLua
        {
            get
            {
                Interlocked.Increment(ref databaseCount);
                if (databaseCount >= 2000)
                {
                    databaseCount = StartDbCount;
                }

                return CreateRedisCache(database: databaseCount, sharedRedisConfig: false, serializer: Serializer.Json, useLua: false);
            }
        }

        public static ICacheManager<object> WithRedisCacheGzJson
        {
            get
            {
                Interlocked.Increment(ref databaseCount);
                if (databaseCount >= 2000)
                {
                    databaseCount = StartDbCount;
                }

                return CreateRedisCache(databaseCount, false, Serializer.GzJson);
            }
        }

        public static ICacheManager<object> WithRedisCacheProto
        {
            get
            {
                Interlocked.Increment(ref databaseCount);
                if (databaseCount >= 2000)
                {
                    databaseCount = StartDbCount;
                }

                return CreateRedisCache(databaseCount, false, Serializer.Proto);
            }
        }

        public static ICacheManager<object> WithDicAndRedisCache
        {
            get
            {
                Interlocked.Increment(ref databaseCount);
                if (databaseCount >= 2000)
                {
                    databaseCount = StartDbCount;
                }

                return CreateRedisAndDicCacheWithBackplane(database: databaseCount, sharedRedisConfig: false, channelName: Guid.NewGuid().ToString(), useLua: true);
            }
        }

        public static ICacheManager<object> WithDicAndRedisCacheNoLua
        {
            get
            {
                Interlocked.Increment(ref databaseCount);
                if (databaseCount >= 2000)
                {
                    databaseCount = StartDbCount;
                }

                return CreateRedisAndDicCacheWithBackplane(database: databaseCount, sharedRedisConfig: false, channelName: Guid.NewGuid().ToString(), useLua: false);
            }
        }

#if !MSBUILD

        public static ICacheManager<object> WithOneMicrosoftMemoryCacheHandle
          => CacheFactory.Build(settings => settings.WithMicrosoftMemoryCacheHandle().EnableStatistics());

#endif

#if !NETCOREAPP

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
                        .WithUpdateMode(CacheUpdateMode.Up)
                        .WithSystemRuntimeCacheHandle("cacheHandleA")
                            .EnableStatistics()
                        .And.WithSystemRuntimeCacheHandle("cacheHandleB")
                            .EnableStatistics();
                });

#endif
#if !NET40 && MOCK_HTTPCONTEXT_ENABLED && !NETCOREAPP

        public static ICacheManager<object> WithSystemWebCache
            => CacheFactory.Build(
                settings =>
                {
                    settings
                    .WithHandle(typeof(SystemWebCacheHandleWrapper<>))
                        .EnableStatistics();
                });

#endif

#if COUCHBASEENABLED

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

        public static ICacheManager<object> CreateRedisAndDicCacheWithBackplane(int database = 0, bool sharedRedisConfig = true, string channelName = null, Serializer serializer = Serializer.Proto, bool useLua = true)
        {
            var redisKey = sharedRedisConfig ? "redisConfig" + database : Guid.NewGuid().ToString();
            var cache = CacheFactory.Build(settings =>
            {
                settings
                    .WithUpdateMode(CacheUpdateMode.Up)
                    .WithDictionaryHandle()
                        .EnableStatistics();
                settings
                    .WithMaxRetries(int.MaxValue)
                    .TestSerializer(serializer)
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

            foreach (var h in cache.CacheHandles.OfType<RedisCacheHandle<object>>())
            {
                h.UseLua = useLua;
            }

            return cache;
        }

        public static ICacheManager<object> CreateRedisCache(int database = 0, bool sharedRedisConfig = true, Serializer serializer = Serializer.GzJson, bool useLua = true)
        {
            var redisKey = sharedRedisConfig ? "redisConfig" + database : Guid.NewGuid().ToString();
            var cache = CacheFactory.Build(settings =>
            {
                settings
                    .WithMaxRetries(int.MaxValue)
                    .TestSerializer(serializer)
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

            foreach (var h in cache.CacheHandles.OfType<RedisCacheHandle<object>>())
            {
                h.UseLua = useLua;
            }

            return cache;
        }

        public static ICacheManager<T> CreateRedisCache<T>(int database = 0, bool sharedRedisConfig = true, Serializer serializer = Serializer.GzJson)
        {
            var redisKey = sharedRedisConfig ? "redisConfig" + database : Guid.NewGuid().ToString();
            var cache = CacheFactory.Build<T>(settings =>
            {
                settings
                    .TestSerializer(serializer)
                    .WithMaxRetries(int.MaxValue)
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

#if MEMCACHEDENABLED

        public static ICacheManager<object> WithMemcachedBinary
        {
            get
            {
                return CreateMemcachedCache<object>(Serializer.Binary);
            }
        }

        public static ICacheManager<object> WithMemcachedJson
        {
            get
            {
                return CreateMemcachedCache<object>(Serializer.Json);
            }
        }

        public static ICacheManager<object> WithMemcachedGzJson
        {
            get
            {
                return CreateMemcachedCache<object>(Serializer.GzJson);
            }
        }

        public static ICacheManager<object> WithMemcachedProto
        {
            get
            {
                return CreateMemcachedCache<object>(Serializer.Proto);
            }
        }

        public static ICacheManager<object> WithMemcachedBondBinary
        {
            get
            {
                return CreateMemcachedCache<object>(Serializer.BondBinary);
            }
        }

        public static ICacheManager<T> CreateMemcachedCache<T>(Serializer serializer = Serializer.Json)
        {
            var memConfig = new MemcachedClientConfiguration();
            memConfig.AddServer("localhost", 11211);
            return CacheFactory.Build<T>(settings =>
            {
                settings.WithUpdateMode(CacheUpdateMode.Full)
                    .TestSerializer(serializer)
                    .WithMemcachedCacheHandle(memConfig)
                        .EnableStatistics()
                        .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(1000));
            });
        }

#endif

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
#if !NETCOREAPP
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
#if !NETCOREAPP
                yield return new object[] { TestManagers.WithRedisCacheBinary };
#endif
                yield return new object[] { TestManagers.WithRedisCacheJson };
                yield return new object[] { TestManagers.WithRedisCacheGzJson };
                yield return new object[] { TestManagers.WithRedisCacheProto };
                yield return new object[] { TestManagers.WithRedisCacheBondBinary };
                yield return new object[] { TestManagers.WithDicAndRedisCache };

                yield return new object[] { TestManagers.WithRedisCacheJsonNoLua };
                yield return new object[] { TestManagers.WithDicAndRedisCacheNoLua };
#endif
#if MEMCACHEDENABLED
#if !NETCOREAPP
                yield return new object[] { TestManagers.WithMemcachedBinary };
#endif
                yield return new object[] { TestManagers.WithMemcachedJson };
                yield return new object[] { TestManagers.WithMemcachedGzJson };
                yield return new object[] { TestManagers.WithMemcachedProto };
                yield return new object[] { TestManagers.WithMemcachedBondBinary };
#endif
#if COUCHBASEENABLED
                yield return new object[] { TestManagers.WithCouchbaseMemcached };
#endif
#if !NET40 && MOCK_HTTPCONTEXT_ENABLED
                yield return new object[] { TestManagers.WithSystemWebCache };
#endif
            }
        }

#if !NETCOREAPP

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

                case Serializer.BondBinary:
                    part.WithBondCompactBinarySerializer(2048);
                    break;
            }
            return part;
        }
    }
}

#if NETCOREAPP
namespace System.Diagnostics.CodeAnalysis
{
    [Conditional("DEBUG")]
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    internal sealed class ExcludeFromCodeCoverageAttribute : Attribute
    {
    }
}
#endif