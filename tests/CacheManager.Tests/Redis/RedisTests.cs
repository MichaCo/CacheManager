using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using CacheManager.Core;
using CacheManager.Core.Configuration;
using CacheManager.StackExchange.Redis;
using CacheManager.Tests.TestCommon;
using FluentAssertions;
using Xunit;

namespace CacheManager.Tests.Redis
{
    /// <summary>
    /// To run the memcached test, run the bat files under /memcached before executing the tests!
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class RedisTests : BaseCacheManagerTest
    {
        [Fact]
        [Trait("IntegrationTest", "Redis")]
        public void Redis_Multiple_PubSub_Remove()
        {
            // arrange
            var item = new CacheItem<object>("key", "something");
            
            // act/assert
            RunMultipleCaches((cacheA, cacheB) =>
            {
                cacheA.Add(item);
                cacheB.Get(item.Key).Should().Be(item.Value);
                cacheB.Remove(item.Key);
            },
            (cache) =>
            {
                cache.GetCacheItem(item.Key).Should().BeNull();
            }, 10, this.WithSystemAndRedisCache, this.WithSystemAndRedisCache, this.WithRedisCache, this.WithSystemAndRedisCache);
        }

        [Fact]
        [Trait("IntegrationTest", "Redis")]
        public void Redis_Multiple_PubSub_Change()
        {
            // arrange
            var item = new CacheItem<object>("key", "something");

            // act/assert
            RunMultipleCaches((cacheA, cacheB) =>
            {
                cacheA.Add(item);
                cacheB.Get(item.Key).Should().Be(item.Value);
                cacheB.Put(item.Key, "new value");
            },
            (cache) =>
            {
                var val = cache.Get(item.Key);
                cache.Get(item.Key).Should().Be("new value");
            }, 10, this.WithSystemAndRedisCache, this.WithSystemAndRedisCache, this.WithRedisCache, this.WithSystemAndRedisCache);
        }

        [Fact]
        [Trait("IntegrationTest", "Redis")]
        public void Redis_Multiple_PubSub_Clear()
        {
            // arrange
            var item = new CacheItem<object>("key", "something");

            // act/assert
            RunMultipleCaches((cacheA, cacheB) =>
            {
                cacheA.Add(item);
                cacheB.Get(item.Key).Should().Be(item.Value);
                cacheB.Clear();
            },
            (cache) =>
            {
                cache.Get(item.Key).Should().BeNull();
            }, 10, this.WithSystemAndRedisCache, this.WithSystemAndRedisCache, this.WithRedisCache, this.WithSystemAndRedisCache);
        }

        [Fact]
        [Trait("IntegrationTest", "Redis")]
        public void Redis_Multiple_PubSub_ClearRegion()
        {
            // arrange
            var item = new CacheItem<object>("key", "something", "regionA");

            // act/assert
            RunMultipleCaches((cacheA, cacheB) =>
            {
                cacheA.Add(item);
                cacheB.Get(item.Key, item.Region).Should().Be(item.Value);
                cacheB.ClearRegion(item.Region);
            },
            (cache) =>
            {
                cache.Get(item.Key, item.Region).Should().BeNull();
            }, 10, this.WithSystemAndRedisCache, this.WithSystemAndRedisCache, this.WithRedisCache, this.WithSystemAndRedisCache);
        }

        private static void RunMultipleCaches<TCache>(
            Action<TCache, TCache> stepA, 
            Action<TCache> stepB,
            int iterations,
            params TCache[] caches)
            where TCache : ICacheManager<object>
        {
            for (int i = 0; i < iterations; i++)
            {
                foreach (var cache in caches)
                {
                    cache.Clear();
                }

                Thread.Sleep(10);

                if (caches.Length == 1)
                {
                    stepA(caches[0], caches[0]);
                }
                else
                {
                    stepA(caches[0], caches[1]);
                }

                Thread.Sleep(10);

                foreach (var cache in caches)
                {
                    stepB(cache);
                }
            }

            foreach (var cache in caches)
            {
                cache.Dispose();
            }
        }

        [Fact]
        [Trait("IntegrationTest", "Redis")]
        public void Redis_Absolute_DoesExpire()
        {
            // arrange
            var item = new CacheItem<object>("key", "something", ExpirationMode.Absolute, TimeSpan.FromMilliseconds(50));
            var cache = this.WithRedisCache;

            // act/assert
            using (cache)
            {
                cache.Clear();

                for (int i = 0; i < 3; i++)
                {
                    // act
                    var result = cache.Add(item);

                    // assert
                    result.Should().BeTrue();
                    Thread.Sleep(30);
                    var value = cache.GetCacheItem(item.Key);
                    value.Should().NotBeNull();

                    Thread.Sleep(30);
                    var valueExpired = cache.GetCacheItem(item.Key);
                    valueExpired.Should().BeNull();
                }
            }
        }
        
        [Fact]
        [Trait("IntegrationTest", "Redis")]
        public void Redis_Sliding_DoesExpire()
        {
            // arrange
            var item = new CacheItem<object>("key", "something", ExpirationMode.Sliding, TimeSpan.FromMilliseconds(50));
            var cache = this.WithSystemAndRedisCache;

            // act/assert
            using (cache)
            {
                cache.Clear();

                for (int i = 0; i < 3; i++)
                {
                    // act
                    var result = cache.Add(item);

                    // assert
                    result.Should().BeTrue();

                    // 450ms added so absolute would be expired on the 2nd go
                    for (int s = 0; s < 3; s++)
                    {
                        Thread.Sleep(30);
                        var value = cache.GetCacheItem(item.Key);
                        value.Should().NotBeNull();
                    }

                    Thread.Sleep(60);
                    var valueExpired = cache.GetCacheItem(item.Key);
                    valueExpired.Should().BeNull();
                }
            }
        }

        [Fact]
        [Trait("IntegrationTest", "Redis")]
        public void Redis_Sliding_DoesExpire_WithRegion()
        {
            // arrange
            var item = new CacheItem<object>("key", "something", "region", ExpirationMode.Sliding, TimeSpan.FromMilliseconds(50));
            var cache = this.WithSystemAndRedisCache;

            // act/assert
            using (cache)
            {
                cache.Clear();

                for (int i = 0; i < 3; i++)
                {
                    // act
                    var result = cache.Add(item);

                    // assert
                    result.Should().BeTrue();

                    // 450ms added so absolute would be expired on the 2nd go
                    for (int s = 0; s < 3; s++)
                    {
                        Thread.Sleep(30);
                        var value = cache.GetCacheItem(item.Key, item.Region);
                        value.Should().NotBeNull();
                    }

                    Thread.Sleep(60);
                    var valueExpired = cache.GetCacheItem(item.Key, item.Region);
                    valueExpired.Should().BeNull();
                }
            }
        }

        [Fact]
        [Trait("IntegrationTest", "Redis")]
        public void Redis_Sliding_DoesExpire_MultiClients()
        {
            // arrange
            var item = new CacheItem<object>("key", "something", ExpirationMode.Sliding, TimeSpan.FromMilliseconds(50));
            var cacheA = this.WithSystemAndRedisCache;
            var cacheB = this.WithSystemAndRedisCache;

            // act/assert
            using (cacheA)
            using (cacheB)
            {
                cacheA.Clear();
                cacheB.Clear();

                // act
                var result = cacheA.Add(item);

                var valueB = cacheB.Get(item.Key);

                // assert
                result.Should().BeTrue();
                item.Value.Should().Be(valueB);

                // 450ms added so absolute would be expired on the 2nd go
                for (int s = 0; s < 3; s++)
                {
                    Thread.Sleep(40);
                    cacheA.GetCacheItem(item.Key).Should().NotBeNull();
                    cacheB.GetCacheItem(item.Key).Should().NotBeNull();
                }

                Thread.Sleep(100);
                cacheA.GetCacheItem(item.Key).Should().BeNull();
                cacheB.GetCacheItem(item.Key).Should().BeNull();                
            }
        }
        
        [Fact]
        [Trait("IntegrationTest", "Redis")]
        public void Redis_Absolute_DoesExpire_MultiClients()
        {
            // arrange
            var cacheA = this.WithSystemAndRedisCache;
            var cacheB = this.WithSystemAndRedisCache;

            // act/assert
            using (cacheA)
            using (cacheB)
            {
                cacheA.Clear();
                cacheB.Clear();

                // act
                var item = new CacheItem<object>("key", "something", ExpirationMode.Absolute, TimeSpan.FromMilliseconds(50));
            
                var result = cacheA.Add(item);

                var itemB = cacheB.GetCacheItem(item.Key);

                // assert
                result.Should().BeTrue();
                item.Value.Should().Be(itemB.Value);

                Thread.Sleep(30);
                cacheA.GetCacheItem(item.Key).Should().NotBeNull();
                cacheB.GetCacheItem(item.Key).Should().NotBeNull();

                // after 210ms both it should be expired
                Thread.Sleep(30);
                cacheA.GetCacheItem(item.Key).Should().BeNull();
                cacheB.GetCacheItem(item.Key).Should().BeNull();
            }
        }

        [Fact]
        [Trait("IntegrationTest", "Redis")]
        public void Redis_RaceCondition_WithoutUpdate()
        {
            using (var cache = CacheFactory.Build<RaceConditionTestElement>("myCache", settings =>
            {
                settings.WithUpdateMode(CacheUpdateMode.Full)
                    .WithHandle<RedisCacheHandle<RaceConditionTestElement>>("default")
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(20));
                settings.WithRedisConfiguration(new RedisConfiguration(
                       "default",
                       new List<ServerEndPoint>() { new ServerEndPoint("localhost", 6379) },
                       allowAdmin: true
                    ));
            }))
            {
                cache.Clear();
                cache.Add("myCounter", new RaceConditionTestElement() { Counter = 0 });
                int numThreads = 5;
                int iterations = 10;
                int numInnerIterations = 10;

                // act
                ThreadTestHelper.Run(() =>
                {
                    for (int i = 0; i < numInnerIterations; i++)
                    {
                        var val = cache.Get("myCounter");
                        val.Should().NotBeNull();
                        val.Counter++;

                        cache.Put("myCounter", val);
                    }
                }, numThreads, iterations);

                // assert
                Thread.Sleep(10);
                var result = cache.Get("myCounter");
                result.Should().NotBeNull();
                Trace.TraceInformation("Counter increased to " + result.Counter);
                result.Counter.Should().NotBe(numThreads * numInnerIterations * iterations);
            }
        }

        [Fact]
        [Trait("IntegrationTest", "Redis")]
        public void Redis_NoRaceCondition_WithUpdate()
        {
            using (var cache = CacheFactory.Build<RaceConditionTestElement>("myCache", settings =>
            {
                settings.WithUpdateMode(CacheUpdateMode.Full)
                    .WithHandle<RedisCacheHandle<RaceConditionTestElement>>("default")
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(20));
                settings.WithRedisConfiguration(new RedisConfiguration(
                       "default",
                       new List<ServerEndPoint>() { new ServerEndPoint("localhost", 6379) },
                       allowAdmin: true
                    ));
            }))
            {
                cache.Remove("myCounter");
                cache.Add("myCounter", new RaceConditionTestElement() { Counter = 0 });
                int numThreads = 5;
                int iterations = 10;
                int numInnerIterations = 10;
                int countCasModifyCalls = 0;

                // act
                ThreadTestHelper.Run(() =>
                {
                    for (int i = 0; i < numInnerIterations; i++)
                    {
                        cache.Update("myCounter", (value) =>
                        {
                            value.Counter++;
                            Interlocked.Increment(ref countCasModifyCalls);
                            return value;
                        });
                    }
                }, numThreads, iterations);

                // assert
                Thread.Sleep(10);
                var result = cache.Get("myCounter");
                result.Should().NotBeNull();
                Trace.TraceInformation("Counter increased to " + result.Counter + " cas calls needed " + countCasModifyCalls);
                result.Counter.Should().Be(numThreads * numInnerIterations * iterations, "counter should be exactly the expected value");
                countCasModifyCalls.Should().BeGreaterThan((int)result.Counter, "we expect many version collisions, so cas calls should be way higher then the count result");
            }
        }
    }


    [Serializable]
    [ExcludeFromCodeCoverage]
    public class RaceConditionTestElement
    {
        public RaceConditionTestElement()
        {
        }

        public long Counter { get; set; }
    }
}