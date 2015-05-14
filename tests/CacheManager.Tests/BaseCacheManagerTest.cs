using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CacheManager.Core;
using CacheManager.Core.Cache;

#if !NET40

using Couchbase.Configuration.Client;

#endif

namespace CacheManager.Tests
{
    /// <summary>
    /// Provides some pre configured caches with different setups for testing. <remarks>Do not add
    /// NullCacheHandle to any of the config. Some tests expect a working implementation...</remarks>
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class BaseCacheManagerTest
    {
        public ICacheManager<object> WithOneMemoryCacheHandleSliding
        {
            get
            {
                return CacheFactory.Build("cache", settings => settings
                    .WithUpdateMode(CacheUpdateMode.Up)
                    .WithSystemRuntimeCacheHandle("h1")
                        .EnableStatistics()
                        .EnablePerformanceCounters()
                    .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10)));
            }
        }

        public ICacheManager<object> WithOneDicCacheHandle
        {
            get
            {
                return CacheFactory.Build("cache", settings => settings
                    .WithUpdateMode(CacheUpdateMode.Full)
                    .WithHandle(typeof(DictionaryCacheHandle<>), "h1")
                        .EnableStatistics()
                    .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10)));
            }
        }

        public ICacheManager<object> WithOneMemoryCacheHandle
        {
            get
            {
                return CacheFactory.Build("cache", settings => settings.WithSystemRuntimeCacheHandle("h1").EnableStatistics());
            }
        }

        public ICacheManager<object> WithMemoryAndDictionaryHandles
        {
            get
            {
                return CacheFactory.Build("cache", settings =>
                {
                    settings
                        .WithUpdateMode(CacheUpdateMode.None)
                        .WithSystemRuntimeCacheHandle("h1")
                            .EnableStatistics()
                        .And.WithSystemRuntimeCacheHandle("h2")
                            .EnableStatistics()
                            .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(10))
                        .And.WithHandle(typeof(DictionaryCacheHandle<>), "h3")
                            .EnableStatistics()
                        .And.WithHandle(typeof(DictionaryCacheHandle<>), "h4")
                            .EnableStatistics()
                            .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(10));
                });
            }
        }

        public ICacheManager<object> WithManyDictionaryHandles
        {
            get
            {
                return CacheFactory.Build("cache", settings =>
                {
                    settings
                        .WithUpdateMode(CacheUpdateMode.Up)
                        .WithHandle(typeof(DictionaryCacheHandle<>), "h1")
                            .EnableStatistics()
                        .And.WithHandle(typeof(DictionaryCacheHandle<>), "h2")
                            .EnableStatistics()
                            .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10))
                        .And.WithHandle(typeof(DictionaryCacheHandle<>), "h3")
                            .EnableStatistics()
                            .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(20))
                        .And.WithHandle(typeof(DictionaryCacheHandle<>), "h4")
                            .EnableStatistics()
                            .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10))
                        .And.WithHandle(typeof(DictionaryCacheHandle<>), "h5")
                            .EnableStatistics()
                            .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(20))
                        .And.WithHandle(typeof(DictionaryCacheHandle<>), "h6")
                            .EnableStatistics()
                            .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10))
                        .And.WithHandle(typeof(DictionaryCacheHandle<>), "h7")
                            .EnableStatistics()
                            .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(20));
                });
            }
        }

        public ICacheManager<object> WithTwoNamedMemoryCaches
        {
            get
            {
                return CacheFactory.Build("cache", settings =>
                {
                    settings
                        .WithUpdateMode(CacheUpdateMode.Full)
                        .WithSystemRuntimeCacheHandle("cache1")
                            .EnableStatistics()
                        .And.WithSystemRuntimeCacheHandle("cache2")
                            .EnableStatistics();
                });
            }
        }

        public ICacheManager<object> WithRedisCache
        {
            get
            {
                var cache = CacheFactory.Build("cache", settings =>
                {
                    settings
                        .WithMaxRetries(100)
                        .WithRetryTimeout(1000)
                        .WithRedisConfiguration("redisCache", config =>
                        {
                            config.WithAllowAdmin()
                                .WithDatabase(0)
                                .WithEndpoint("localhost", 6379);
                        })
                        .WithRedisBackPlate("redisCache")
                        .WithRedisCacheHandle("redisCache", true)
                        .EnableStatistics();
                });

                return cache;
            }
        }

        public ICacheManager<object> WithSystemAndRedisCache
        {
            get
            {
                var cache = CacheFactory.Build("cache", settings =>
                {
                    settings
                        .WithUpdateMode(CacheUpdateMode.Up)
                        .WithSystemRuntimeCacheHandle("cache1")
                            .EnableStatistics();
                    settings
                        .WithMaxRetries(100)
                        .WithRetryTimeout(1000)
                        .WithRedisConfiguration("redisCache", config =>
                        {
                            config.WithAllowAdmin()
                                .WithDatabase(0)
                                .WithEndpoint("localhost", 6379);
                        })
                        .WithRedisBackPlate("redisCache")
                        .WithRedisCacheHandle("redisCache", true)
                        .EnableStatistics();
                });

                return cache;
            }
        }

        public ICacheManager<object> WithMemcached
        {
            get
            {
                var cache = CacheFactory.Build("myCache", settings =>
                {
                    settings.WithUpdateMode(CacheUpdateMode.Full)
                        .WithMemcachedCacheHandle("enyim.com/memcached")
                            .EnableStatistics()
                            .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(100));
                });

                cache.Clear();
                return cache;
            }
        }

#if !NET40

        public ICacheManager<object> WithCouchbaseMemcached
        {
            get
            {
                var clientConfiguration = new ClientConfiguration()
                {
                    Servers = new List<Uri>()
                    {
                        new Uri("http://192.168.178.27:8091/pools")
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

                var cache = CacheFactory.Build("myCache", settings =>
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

        public static IEnumerable<object[]> GetCacheManagers()
        {
            var data = new BaseCacheManagerTest();
            yield return new object[] { data.WithOneMemoryCacheHandleSliding };
            yield return new object[] { data.WithOneDicCacheHandle };
            yield return new object[] { data.WithOneMemoryCacheHandle };
            yield return new object[] { data.WithMemoryAndDictionaryHandles };
            yield return new object[] { data.WithManyDictionaryHandles };
            yield return new object[] { data.WithTwoNamedMemoryCaches };
            // yield return new object[] { data.WithRedisCache }; yield return new object[] {
            // data.WithSystemAndRedisCache }; yield return new object[] { data.WithMemcached };
            // yield return new object[] { data.WithCouchbaseMemcached };
        }

        public static string GetCfgFileName(string fileName)
        {
            return AppDomain.CurrentDomain.BaseDirectory + (fileName.StartsWith("\\") ? fileName : "\\" + fileName);
        }
    }
}