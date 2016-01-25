using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using CacheManager.Core;
#if !NET40 && !NET45
using Microsoft.Extensions.Logging;
#endif
#if !NET40
using Couchbase.Configuration.Client;
#endif
#if DNX451
#endif

using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Tests
{
    [ExcludeFromCodeCoverage]
    public static class TestManagers
    {
        private const int StartDbCount = 100;
        private static int databaseCount = StartDbCount;

        public static ICacheManager<object> WithOneMemoryCacheHandleSliding
            => CacheFactory.Build(
                settings => settings
                    .WithUpdateMode(CacheUpdateMode.Up)
                    .WithSystemRuntimeCacheHandle()
                        .EnableStatistics()
                        .EnablePerformanceCounters()
                    .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(1000)));

        public static ICacheManager<object> WithOneDicCacheHandle
            => CacheFactory.Build(
                settings => settings
                    .WithUpdateMode(CacheUpdateMode.Full)
                    .WithDictionaryHandle()
                        .EnableStatistics()
                    .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(1000)));

        public static ICacheManager<object> WithOneMemoryCacheHandle
            => CacheFactory.Build(settings => settings.WithSystemRuntimeCacheHandle().EnableStatistics());

        public static ICacheManager<object> WithMemoryAndDictionaryHandles
            => CacheFactory.Build(
                settings =>
                {
                    settings
#if !NET40 && !NET45
                        .WithAspNetLogging(f => f.AddDebug())
#endif
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

        public static ICacheManager<object> WithManyDictionaryHandles
            => CacheFactory.Build(
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

#if !NET40 && MOCK_HTTPCONTEXT_ENABLED
        public static ICacheManager<object> WithSystemWebCache
            => CacheFactory.Build(
                settings =>
                {
                    settings
                    .WithHandle(typeof(SystemWebCacheHandleWrapper<>))
                        .EnableStatistics();
                });
#endif

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

        public static ICacheManager<object> CreateRedisAndSystemCacheWithBackPlate(int database = 0, bool sharedRedisConfig = true, string channelName = null)
        {
            var redisKey = sharedRedisConfig ? "redisConfig" : Guid.NewGuid().ToString();
            return CacheFactory.Build(settings =>
            {
                settings
                    .WithUpdateMode(CacheUpdateMode.Up)
                    .WithSystemRuntimeCacheHandle()
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
                    .WithRedisCacheHandle(redisKey, true)
                    .EnableStatistics();

                if (channelName != null)
                {
                    settings.WithRedisBackPlate(redisKey, channelName);
                }
                else
                {
                    settings.WithRedisBackPlate(redisKey);
                }
            });
        }

        public static ICacheManager<object> CreateRedisCache(int database = 0, bool sharedRedisConfig = true)
        {
            var redisKey = sharedRedisConfig ? "redisConfig" : Guid.NewGuid().ToString();
            var cache = CacheFactory.Build(settings =>
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
                    ////.WithRedisBackPlate(redisKey)
                    .WithRedisCacheHandle(redisKey, true)
                    .EnableStatistics();
            });

            return cache;
        }

        public static ICacheManager<T> CreateRedisCache<T>(int database = 0, bool sharedRedisConfig = true)
        {
            var redisKey = sharedRedisConfig ? "redisConfig" : Guid.NewGuid().ToString();
            var cache = CacheFactory.Build<T>(settings =>
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

    [SuppressMessage("Microsoft.Design", "CA1053:StaticHolderTypesShouldNotHaveConstructors", Justification = "Needed for xunit")]
    [ExcludeFromCodeCoverage]
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
#if !NET40 && MOCK_HTTPCONTEXT_ENABLED
                yield return new object[] { TestManagers.WithSystemWebCache };
#endif
            }
        }

        public static string GetCfgFileName(string fileName)
        {
            NotNullOrWhiteSpace(fileName, nameof(fileName));
#if DNX451
            // var appEnv = CallContextServiceLocator.Locator.ServiceProvider
            //    .GetService(typeof(IApplicationEnvironment)) as IApplicationEnvironment;
            // var basePath = appEnv.ApplicationBasePath;
            var basePath = Environment.CurrentDirectory;
#else
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
#endif

            return basePath + (fileName.StartsWith("/") ? fileName : "/" + fileName);
        }
    }
}