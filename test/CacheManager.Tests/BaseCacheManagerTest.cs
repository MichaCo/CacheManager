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
    [ExcludeFromCodeCoverage]
    public static class TestManagers
    {
        public static ICacheManager<object> WithOneMemoryCacheHandleSliding
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

        public static ICacheManager<object> WithOneDicCacheHandle
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

        public static ICacheManager<object> WithOneMemoryCacheHandle
        {
            get
            {
                return CacheFactory.Build("cache", settings => settings.WithSystemRuntimeCacheHandle("h1").EnableStatistics());
            }
        }

        public static ICacheManager<object> WithMemoryAndDictionaryHandles
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

        public static ICacheManager<object> WithManyDictionaryHandles
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

        public static ICacheManager<object> WithTwoNamedMemoryCaches
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

        public static ICacheManager<object> WithRedisCache
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
                                .WithDatabase(99)
                                .WithEndpoint("localhost", 6379);
                        })
                        // .WithRedisBackPlate("redisCache")
                        .WithRedisCacheHandle("redisCache", true)
                        .EnableStatistics();
                });

                return cache;
            }
        }

        public static ICacheManager<object> WithSystemAndRedisCache
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
                                .WithDatabase(88)
                                .WithEndpoint("localhost", 6379);
                        })
                        .WithRedisBackPlate("redisCache")
                        .WithRedisCacheHandle("redisCache", true)
                        .EnableStatistics();
                });

                return cache;
            }
        }

        public static ICacheManager<object> WithMemcached
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
    }
    
    [SuppressMessage("Microsoft.Design", "CA1053:StaticHolderTypesShouldNotHaveConstructors", Justification = "Needed for xunit"), ExcludeFromCodeCoverage]
    public class BaseCacheManagerTest
    {
        public static string GetCfgFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name should not be empty", "fileName");
            }

            return AppDomain.CurrentDomain.BaseDirectory + (fileName.StartsWith("\\") ? fileName : "\\" + fileName);
        }

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
                yield return new object[] { TestManagers.WithRedisCache };
                // yield return new object[] { TestManagers.WithSystemAndRedisCache }; 
                // yield return new object[] { TestManagers.WithMemcached };
                // yield return new object[] { TestManagers.WithCouchbaseMemcached };
            }
        }
    }
}