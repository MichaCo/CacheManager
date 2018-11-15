using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using CacheManager.Core;

#if MEMCACHEDENABLED
using Enyim.Caching.Configuration;
#endif
#if COUCHBASEENABLED
using Couchbase;
using Couchbase.Configuration.Client;
#endif

namespace CacheManager.Tests
{
    [ExcludeFromCodeCoverage]
    public class TestCacheManagers : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { TestManagers.WithOneMemoryCacheHandleSliding };
            yield return new object[] { TestManagers.WithOneMemoryCacheHandle };
            yield return new object[] { TestManagers.WithMemoryAndDictionaryHandles };
            yield return new object[] { TestManagers.WithTwoNamedMemoryCaches };
#if !MSBUILD
            yield return new object[] { TestManagers.WithOneMicrosoftMemoryCacheHandle };
#endif
            yield return new object[] { TestManagers.WithManyDictionaryHandles };
            yield return new object[] { TestManagers.WithOneDicCacheHandle };
#if REDISENABLED
#if !NETCOREAPP2
            yield return new object[] { TestManagers.WithRedisCacheBinary };
#endif
            yield return new object[] { TestManagers.WithRedisCacheDataContract };
            yield return new object[] { TestManagers.WithRedisCacheDataContractBinary };
            yield return new object[] { TestManagers.WithRedisCacheDataContractGzJson };
            yield return new object[] { TestManagers.WithRedisCacheDataContractJson };

            yield return new object[] { TestManagers.WithRedisCacheJson };
            yield return new object[] { TestManagers.WithRedisCacheGzJson };
            yield return new object[] { TestManagers.WithRedisCacheProto };
            yield return new object[] { TestManagers.WithRedisCacheBondBinary };
            yield return new object[] { TestManagers.WithDicAndRedisCache };

            yield return new object[] { TestManagers.WithRedisCacheJsonNoLua };
            yield return new object[] { TestManagers.WithDicAndRedisCacheNoLua };
#endif
#if MEMCACHEDENABLED
#if !NETCOREAPP2
            yield return new object[] { TestManagers.WithMemcachedBinary };
#endif
            yield return new object[] { TestManagers.WithMemcachedJson };
            //yield return new object[] { TestManagers.WithMemcachedGzJson };
            //yield return new object[] { TestManagers.WithMemcachedProto };
            yield return new object[] { TestManagers.WithMemcachedBondBinary };
#endif
#if COUCHBASEENABLED
            // requires one "default" couchbase bucket without auth or password and one "secret-bucket" with pw: "secret"
            yield return new object[] { TestManagers.WithCouchbaseDefault };
            yield return new object[] { TestManagers.WithCouchbaseDefaultViaHelper };
            yield return new object[] { TestManagers.WithCouchbaseSecret };
#endif
#if MOCK_HTTPCONTEXT_ENABLED
            yield return new object[] { TestManagers.WithSystemWebCache };
#endif
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [ExcludeFromCodeCoverage]
    public static class TestManagers
    {
        public const string RedisHost = "127.0.0.1";
        public const int RedisPort = 6379;
        private const int StartDbCount = 0;
        private static int _databaseCount = StartDbCount;
        private const int NumDatabases = 100;

        public static ICacheManagerConfiguration BaseConfiguration
            => new ConfigurationBuilder()
                    ////.WithMicrosoftLogging(f => f.AddSerilog())
                    .Build();

        public static ICacheManager<object> WithOneDicCacheHandle
            => CacheFactory.FromConfiguration<object>(
                BaseConfiguration.Builder
                    .WithUpdateMode(CacheUpdateMode.Up)
                    .WithDictionaryHandle()
                        .EnableStatistics()
                    .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(1000))
                .Build());

        public static ICacheManager<object> WithManyDictionaryHandles
            => CacheFactory.FromConfiguration<object>(
                BaseConfiguration
                    .Builder
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
                            .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(1000))
                    .Build());

        public static ICacheManager<object> WithRedisCacheBinary
        {
            get
            {
                Interlocked.Increment(ref _databaseCount);
                if (_databaseCount >= NumDatabases)
                {
                    _databaseCount = StartDbCount;
                }

                return CreateRedisCache(_databaseCount, false, Serializer.Binary);
            }
        }

        public static ICacheManager<object> WithRedisCacheBondBinary
        {
            get
            {
                Interlocked.Increment(ref _databaseCount);
                if (_databaseCount >= NumDatabases)
                {
                    _databaseCount = StartDbCount;
                }

                return CreateRedisCache(_databaseCount, false, Serializer.BondBinary);
            }
        }

        public static ICacheManager<object> WithRedisCacheJson
        {
            get
            {
                Interlocked.Increment(ref _databaseCount);
                if (_databaseCount >= NumDatabases)
                {
                    _databaseCount = StartDbCount;
                }

                return CreateRedisCache(database: _databaseCount, sharedRedisConfig: false, serializer: Serializer.Json);
            }
        }

        public static ICacheManager<object> WithRedisCacheJsonNoLua
        {
            get
            {
                Interlocked.Increment(ref _databaseCount);
                if (_databaseCount >= NumDatabases)
                {
                    _databaseCount = StartDbCount;
                }

                return CreateRedisCache(database: _databaseCount, sharedRedisConfig: false, serializer: Serializer.Json, useLua: false);
            }
        }

        public static ICacheManager<object> WithRedisCacheGzJson
        {
            get
            {
                Interlocked.Increment(ref _databaseCount);
                if (_databaseCount >= NumDatabases)
                {
                    _databaseCount = StartDbCount;
                }

                return CreateRedisCache(_databaseCount, false, Serializer.GzJson);
            }
        }

        public static ICacheManager<object> WithRedisCacheDataContract
        {
            get
            {
                Interlocked.Increment(ref _databaseCount);
                if (_databaseCount >= NumDatabases)
                {
                    _databaseCount = StartDbCount;
                }

                return CreateRedisCache(database: _databaseCount, sharedRedisConfig: false, serializer: Serializer.DataContract);
            }
        }

        public static ICacheManager<object> WithRedisCacheDataContractJson
        {
            get
            {
                Interlocked.Increment(ref _databaseCount);
                if (_databaseCount >= NumDatabases)
                {
                    _databaseCount = StartDbCount;
                }

                return CreateRedisCache(database: _databaseCount, sharedRedisConfig: false, serializer: Serializer.DataContractJson);
            }
        }

        public static ICacheManager<object> WithRedisCacheDataContractGzJson
        {
            get
            {
                Interlocked.Increment(ref _databaseCount);
                if (_databaseCount >= NumDatabases)
                {
                    _databaseCount = StartDbCount;
                }

                return CreateRedisCache(database: _databaseCount, sharedRedisConfig: false, serializer: Serializer.DataContractGzJson);
            }
        }

        public static ICacheManager<object> WithRedisCacheDataContractBinary
        {
            get
            {
                Interlocked.Increment(ref _databaseCount);
                if (_databaseCount >= NumDatabases)
                {
                    _databaseCount = StartDbCount;
                }

                return CreateRedisCache(database: _databaseCount, sharedRedisConfig: false, serializer: Serializer.DataContractBinary);
            }
        }

        public static ICacheManager<object> WithRedisCacheProto
        {
            get
            {
                Interlocked.Increment(ref _databaseCount);
                if (_databaseCount >= NumDatabases)
                {
                    _databaseCount = StartDbCount;
                }

                return CreateRedisCache(_databaseCount, false, Serializer.Proto);
            }
        }

        public static ICacheManager<object> WithDicAndRedisCache
        {
            get
            {
                Interlocked.Increment(ref _databaseCount);
                if (_databaseCount >= NumDatabases)
                {
                    _databaseCount = StartDbCount;
                }

                return CreateRedisAndDicCacheWithBackplane(database: _databaseCount, sharedRedisConfig: false, channelName: Guid.NewGuid().ToString(), useLua: true);
            }
        }

        public static ICacheManager<object> WithDicAndRedisCacheNoLua
        {
            get
            {
                Interlocked.Increment(ref _databaseCount);
                if (_databaseCount >= NumDatabases)
                {
                    _databaseCount = StartDbCount;
                }

                return CreateRedisAndDicCacheWithBackplane(database: _databaseCount, sharedRedisConfig: false, channelName: Guid.NewGuid().ToString(), useLua: false);
            }
        }

#if !MSBUILD

        public static ICacheManager<object> WithOneMicrosoftMemoryCacheHandle
          => CacheFactory.Build(settings => settings.WithMicrosoftMemoryCacheHandle().EnableStatistics());

#endif

        public static ICacheManager<object> WithOneMemoryCacheHandleSliding
            => CacheFactory.FromConfiguration<object>(
                BaseConfiguration
                    .Builder
                    .WithSystemRuntimeCacheHandle()
                        .EnableStatistics()
                        .EnablePerformanceCounters()
                    .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(1000))
                .Build());

        public static ICacheManager<object> WithOneMemoryCacheHandle
            => CacheFactory.Build(settings => settings.WithSystemRuntimeCacheHandle().EnableStatistics());

        public static ICacheManager<object> WithMemoryAndDictionaryHandles
            => CacheFactory.FromConfiguration<object>(
                BaseConfiguration
                    .Builder
                        .WithSystemRuntimeCacheHandle()
                            .EnableStatistics()
                        .And.WithSystemRuntimeCacheHandle()
                            .EnableStatistics()
                            .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(1000))
                        .And.WithDictionaryHandle()
                            .EnableStatistics()
                        .And.WithDictionaryHandle()
                            .EnableStatistics()
                            .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(1000))
                    .Build());

        public static ICacheManager<object> WithTwoNamedMemoryCaches
            => CacheFactory.FromConfiguration<object>(
                BaseConfiguration
                    .Builder
                        .WithSystemRuntimeCacheHandle("cacheHandleA")
                            .EnableStatistics()
                        .And.WithSystemRuntimeCacheHandle("cacheHandleB")
                            .EnableStatistics()
                .Build());

#if MOCK_HTTPCONTEXT_ENABLED

        public static ICacheManager<object> WithSystemWebCache
            => CacheFactory.FromConfiguration<object>(
                BaseConfiguration
                    .Builder
                    .WithHandle(typeof(SystemWebCacheHandleWrapper<>))
                        .EnableStatistics()
                    .Build());

#endif

#if COUCHBASEENABLED

        public static ICacheManager<object> WithCouchbaseDefault
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
                        .WithCouchbaseCacheHandle("couchbase") // using default bucket without pw or auth
                            .EnableStatistics();
                });

                return cache;
            }
        }

        public static ICacheManager<object> WithCouchbaseDefaultViaHelper
        {
            get
            {
                ClusterHelper.Initialize();

                var cacheConfig = new ConfigurationBuilder()
                    .WithCouchbaseCacheHandle("somekeywhichdoesntexist") // using default bucket again
                        .EnableStatistics()
                    .Build();

                return new BaseCacheManager<object>(cacheConfig);
            }
        }

        public static ICacheManager<object> WithCouchbaseSecret
        {
            get
            {
                var cacheConfig = new ConfigurationBuilder()
                    .WithCouchbaseConfiguration("cb", new ClientConfiguration())
                    .WithCouchbaseCacheHandle("cb", "secret-bucket", "secret")
                        .EnableStatistics()
                    .Build();

                return new BaseCacheManager<object>(cacheConfig);
            }
        }

#endif

        public static ICacheManager<object> CreateRedisAndDicCacheWithBackplane(int database = 0, bool sharedRedisConfig = true, string channelName = null, Serializer serializer = Serializer.Proto, bool useLua = true)
        {
            if (database > NumDatabases)
            {
                throw new ArgumentOutOfRangeException(nameof(database));
            }

            var redisKey = sharedRedisConfig ? "redisConfig" + database : Guid.NewGuid().ToString();

            var builder = BaseConfiguration.Builder;

            builder.WithUpdateMode(CacheUpdateMode.Up)
                    .WithDictionaryHandle()
                        .EnableStatistics()
                    .And
                    .WithMaxRetries(int.MaxValue)
                    .TestSerializer(serializer)
                    .WithRetryTimeout(1000)
                    .WithRedisConfiguration(redisKey, config =>
                    {
                        config
                            .WithAllowAdmin()
                            .WithDatabase(database)
                            .WithEndpoint(RedisHost, RedisPort);

                        if (!useLua)
                        {
                            config.UseCompatibilityMode("2.4");
                        }
                    })
                    .WithRedisCacheHandle(redisKey, true)
                    .EnableStatistics();

            if (channelName != null)
            {
                builder.WithRedisBackplane(redisKey, channelName);
            }
            else
            {
                builder.WithRedisBackplane(redisKey);
            }

            var cache = CacheFactory.FromConfiguration<object>(
                $"{database}|{sharedRedisConfig}|{serializer}|{useLua}" + Guid.NewGuid().ToString(),
                builder.Build());

            return cache;
        }

        public static ICacheManager<object> CreateDicCacheWithBackplane(bool sharedRedisConfig = true, string channelName = null)
        {
            var redisKey = sharedRedisConfig ? "redisConfig0" : Guid.NewGuid().ToString();

            var builder = BaseConfiguration.Builder;

            builder
                    .WithDictionaryHandle(isBackplaneSource: true)
                        .EnableStatistics()
                    .And
                    .WithRedisConfiguration(redisKey, config =>
                    {
                        config
                            .WithAllowAdmin()
                            .WithDatabase(0)
                            .WithEndpoint(RedisHost, RedisPort);
                    });

            if (channelName != null)
            {
                builder.WithRedisBackplane(redisKey, channelName);
            }
            else
            {
                builder.WithRedisBackplane(redisKey);
            }

            var cache = CacheFactory.FromConfiguration<object>(
                $"{sharedRedisConfig}" + Guid.NewGuid().ToString(),
                builder.Build());

            return cache;
        }

        public static ICacheManager<object> CreateRedisCache(int database = 0, bool sharedRedisConfig = true, Serializer serializer = Serializer.GzJson, bool useLua = true)
        {
            if (database > NumDatabases)
            {
                throw new ArgumentOutOfRangeException(nameof(database));
            }

            var redisKey = sharedRedisConfig ? "redisConfig" + database : Guid.NewGuid().ToString();
            var cache = CacheFactory.FromConfiguration<object>(
                $"{database}|{sharedRedisConfig}|{serializer}|{useLua}" + Guid.NewGuid().ToString(),
                BaseConfiguration.Builder
                    .WithMaxRetries(int.MaxValue)
                    .TestSerializer(serializer)
                    .WithRetryTimeout(1000)
                    .WithRedisConfiguration(redisKey, config =>
                    {
                        config
                            .WithDatabase(database)
                            .WithEndpoint(RedisHost, RedisPort);

                        if (!useLua)
                        {
                            config.UseCompatibilityMode("2.4");
                        }
                    })
                    ////.WithRedisBackplane(redisKey)
                    .WithRedisCacheHandle(redisKey, true)
                    .EnableStatistics()
                .Build());

            return cache;
        }

        public static ICacheManager<T> CreateRedisCache<T>(int database = 0, bool sharedRedisConfig = true, Serializer serializer = Serializer.GzJson)
        {
            if (database > NumDatabases)
            {
                throw new ArgumentOutOfRangeException(nameof(database));
            }

            var redisKey = sharedRedisConfig ? "redisConfig" + database : Guid.NewGuid().ToString();
            var cache = CacheFactory.FromConfiguration<T>(
                BaseConfiguration.Builder
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
                    .EnableStatistics()
                .Build());

            return cache;
        }

#if MEMCACHEDENABLED

        public static ICacheManager<object> WithMemcachedBinary => CreateMemcachedCache<object>(Serializer.Binary);

        public static ICacheManager<object> WithMemcachedJson => CreateMemcachedCache<object>(Serializer.Json);

        public static ICacheManager<object> WithMemcachedGzJson => CreateMemcachedCache<object>(Serializer.GzJson);

        public static ICacheManager<object> WithMemcachedProto => CreateMemcachedCache<object>(Serializer.Proto);

        public static ICacheManager<object> WithMemcachedBondBinary => CreateMemcachedCache<object>(Serializer.BondBinary);

        public static ICacheManager<T> CreateMemcachedCache<T>(Serializer serializer = Serializer.Json)
        {
            var memConfig = new MemcachedClientConfiguration();
            memConfig.AddServer("localhost", 11211);
            return CacheFactory.FromConfiguration<T>(
                BaseConfiguration.Builder
                    .WithUpdateMode(CacheUpdateMode.Up)
                    .TestSerializer(serializer)
                    .WithMemcachedCacheHandle(memConfig)
                        .EnableStatistics()
                        .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(1000))
                .Build());
        }

#endif

        private static string NewKey() => Guid.NewGuid().ToString();
    }

    public enum Serializer
    {
        Binary,
        Json,
        GzJson,
        Proto,
        BondBinary,
        DataContractJson,
        DataContractGzJson,
        DataContractBinary,
        DataContract
    }

    [ExcludeFromCodeCoverage]
    public static class ConfigurationExtension
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

                case Serializer.DataContract:
                    part.WithDataContractSerializer();
                    break;

                case Serializer.DataContractBinary:
                    part.WithDataContractBinarySerializer();
                    break;

                case Serializer.DataContractGzJson:
                    part.WithDataContractGzJsonSerializer();
                    break;

                case Serializer.DataContractJson:
                    part.WithDataContractJsonSerializer();
                    break;

                default:
                    throw new InvalidOperationException("Unknown serializer");
            }
            return part;
        }
    }
}
