using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using CacheManager.Core;
using CacheManager.Core.Internal;

#if !NET40
using Couchbase.Configuration.Client;
#endif
#if DNX451
#endif

namespace CacheManager.Tests
{
    [ExcludeFromCodeCoverage]
    public static class TestManagers
    {
        private const int StartDbCount = 100;
        private static int databaseCount = StartDbCount;

        public static ICacheManager<object> WithOneMemoryCacheHandleSliding
            => CacheFactory.Build(
                NewKey(), 
                settings => settings
                    .WithUpdateMode(CacheUpdateMode.Up)
                    .WithSystemRuntimeCacheHandle(NewKey())
                        .EnableStatistics()
                        .EnablePerformanceCounters()
                    .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(1000)));

        public static ICacheManager<object> WithOneDicCacheHandle
            => CacheFactory.Build(
                NewKey(), 
                settings => settings
                    .WithUpdateMode(CacheUpdateMode.Full)
                    .WithHandle(typeof(DictionaryCacheHandle<>), NewKey())
                        .EnableStatistics()
                    .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(1000)));

        public static ICacheManager<object> WithOneMemoryCacheHandle
            => CacheFactory.Build(NewKey(), settings => settings.WithSystemRuntimeCacheHandle(NewKey()).EnableStatistics());

        public static ICacheManager<object> WithMemoryAndDictionaryHandles
            =>  CacheFactory.Build(
                NewKey(), 
                settings =>
                {
                    settings
                        .WithUpdateMode(CacheUpdateMode.None)
                        .WithSystemRuntimeCacheHandle(NewKey())
                            .EnableStatistics()
                        .And.WithSystemRuntimeCacheHandle("h2")
                            .EnableStatistics()
                            .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(1000))
                        .And.WithHandle(typeof(DictionaryCacheHandle<>), "h3")
                            .EnableStatistics()
                        .And.WithHandle(typeof(DictionaryCacheHandle<>), "h4")
                            .EnableStatistics()
                            .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(1000));
                });

        public static ICacheManager<object> WithManyDictionaryHandles
            => CacheFactory.Build(
                NewKey(), 
                settings =>
                {
                    settings
                        .WithUpdateMode(CacheUpdateMode.Up)
                        .WithHandle(typeof(DictionaryCacheHandle<>), NewKey())
                            .EnableStatistics()
                        .And.WithHandle(typeof(DictionaryCacheHandle<>), NewKey())
                            .EnableStatistics()
                            .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(1000))
                        .And.WithHandle(typeof(DictionaryCacheHandle<>), NewKey())
                            .EnableStatistics()
                            .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(1000))
                        .And.WithHandle(typeof(DictionaryCacheHandle<>), NewKey())
                            .EnableStatistics()
                            .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(1000))
                        .And.WithHandle(typeof(DictionaryCacheHandle<>), NewKey())
                            .EnableStatistics()
                            .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(1000))
                        .And.WithHandle(typeof(DictionaryCacheHandle<>), NewKey())
                            .EnableStatistics()
                            .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(1000))
                        .And.WithHandle(typeof(DictionaryCacheHandle<>), NewKey())
                            .EnableStatistics()
                            .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(1000));
                });

        public static ICacheManager<object> WithTwoNamedMemoryCaches
            => CacheFactory.Build(
                NewKey(), 
                settings =>
                {
                    settings
                        .WithUpdateMode(CacheUpdateMode.Full)
                        .WithSystemRuntimeCacheHandle(NewKey())
                            .EnableStatistics()
                        .And.WithSystemRuntimeCacheHandle(NewKey())
                            .EnableStatistics();
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

                return CreateRedisAndSystemCacheWithBackPlate(databaseCount, false);
            }
        }

        public static ICacheManager<object> WithMemcached
        {
            get
            {
                var cache = CacheFactory.Build(NewKey(), settings =>
                {
                    settings.WithUpdateMode(CacheUpdateMode.Full)
                        .WithMemcachedCacheHandle("enyim.com/memcached")
                            .EnableStatistics()
                            .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(1000));
                });

                return cache;
            }
        }

#if !NET40

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

                var cache = CacheFactory.Build(NewKey(), settings =>
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

        public static ICacheManager<object> CreateRedisAndSystemCacheWithBackPlate(int database = 0, bool sharedRedisConfig = true)
        {
            var redisKey = sharedRedisConfig ? "redisConfig" : Guid.NewGuid().ToString();
            return CacheFactory.Build(redisKey, settings =>
            {
                settings
                    .WithUpdateMode(CacheUpdateMode.Up)
                    .WithSystemRuntimeCacheHandle(NewKey())
                        .EnableStatistics();
                settings
                    .WithMaxRetries(100)
                    .WithRetryTimeout(1000)
                    .WithRedisConfiguration(redisKey, config =>
                    {
                        config
                            .WithDatabase(database)
                            .WithEndpoint("127.0.0.1", 6379);
                    })
                    .WithRedisBackPlate(redisKey)
                    .WithRedisCacheHandle(redisKey, true)
                    .EnableStatistics();
            });
        }

        public static ICacheManager<object> CreateRedisCache(int database = 0, bool sharedRedisConfig = true)
        {
            var redisKey = sharedRedisConfig ? "redisConfig" : Guid.NewGuid().ToString();
            var cache = CacheFactory.Build(redisKey, settings =>
            {
                settings
                    .WithMaxRetries(100)
                    .WithRetryTimeout(1000)
                    .WithRedisConfiguration(redisKey, config =>
                    {
                        config
                            .WithDatabase(database)
                            .WithEndpoint("127.0.0.1", 6379);
                    })
                    .WithRedisBackPlate(redisKey)
                    .WithRedisCacheHandle(redisKey, true)
                    .EnableStatistics();
            });

            return cache;
        }

        private static string NewKey() => Guid.NewGuid().ToString();
    }

    [SuppressMessage("Microsoft.Design", "CA1053:StaticHolderTypesShouldNotHaveConstructors", Justification = "Needed for xunit"), ExcludeFromCodeCoverage]
    public class BaseCacheManagerTest
    {
        public static IEnumerable<object[]> TestCacheManagers
        {
            get
            {
                yield return new object[] { TestManagers.WithOneMemoryCacheHandleSliding };
                yield return new object[] { TestManagers.WithOneDicCacheHandle };
                yield return new object[] { TestManagers.WithOneMemoryCacheHandle };
                yield return new object[] { TestManagers.WithMemoryAndDictionaryHandles };
                yield return new object[] { TestManagers.WithManyDictionaryHandles };
                yield return new object[] { TestManagers.WithTwoNamedMemoryCaches };
#if REDISENABLED
                yield return new object[] { TestManagers.WithRedisCache };
                yield return new object[] { TestManagers.WithSystemAndRedisCache };
#endif
                //// yield return new object[] { TestManagers.WithMemcached };
                //// yield return new object[] { TestManagers.WithCouchbaseMemcached };
            }
        }

        public static string GetCfgFileName(string fileName)
        {
#if DNX451
            // var appEnv = CallContextServiceLocator.Locator.ServiceProvider
            //    .GetService(typeof(IApplicationEnvironment)) as IApplicationEnvironment;
            // var basePath = appEnv.ApplicationBasePath;
            var basePath = Environment.CurrentDirectory;
#else
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
#endif

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name should not be empty", nameof(fileName));
            }

            return basePath + (fileName.StartsWith("/") ? fileName : "/" + fileName);
        }
    }
}