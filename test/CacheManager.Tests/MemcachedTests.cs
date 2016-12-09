#if MEMCACHEDENABLED
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using CacheManager.Core;
using Enyim.Caching.Configuration;
using FluentAssertions;
using Xunit;

namespace CacheManager.Tests
{
    [ExcludeFromCodeCoverage]
    public class MemcachedTests
    {
        private static MemcachedClientConfiguration Configuration
        {
            get
            {
                var memConfig = new MemcachedClientConfiguration();
                memConfig.AddServer("localhost", 11211);
                return memConfig;
            }
        }

        [Fact]
        [Trait("category", "Memcached")]
        [Trait("category", "Unreliable")]
        public void Memcached_Absolute_DoesExpire()
        {
            var cache = CacheFactory.Build(settings =>
            {
                settings.WithUpdateMode(CacheUpdateMode.Full)
                    .WithMemcachedCacheHandle(Configuration)
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(1));
            });

            using (cache)
            {
                for (int i = 0; i < 3; i++)
                {
                    // act
                    var result = cache.Add("key" + i, "value" + i);

                    // assert
                    result.Should().BeTrue();
                    Thread.Sleep(10);
                    var value = cache.GetCacheItem("key" + i);
                    value.Should().NotBeNull();

                    Thread.Sleep(2000);
                    var valueExpired = cache.GetCacheItem("key" + i);
                    valueExpired.Should().BeNull();
                }
            }
        }

        [Fact]
#if NET40
        [Trait("Framework", "NET40")]
#else
        [Trait("Framework", "NET45")]
#endif
        public void Memcached_Ctor()
        {
            // arrange act
            Action act = () => CacheFactory.Build<IAmNotSerializable>(settings =>
            {
                settings.WithUpdateMode(CacheUpdateMode.Full)
                    .WithMemcachedCacheHandle(Configuration)
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(1));
            });

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithInnerMessage("The cache value type must be serializable*");
        }

        [Fact]
        [Trait("category", "Memcached")]
        public void Memcached_KeySizeLimit()
        {
            // arrange
            var longKey = string.Join(string.Empty, Enumerable.Repeat("a", 300));

            var item = new CacheItem<string>(longKey, "something");
            var cache = CacheFactory.Build<string>(settings =>
            {
                settings.WithUpdateMode(CacheUpdateMode.Full)
                    .WithMemcachedCacheHandle(Configuration)
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(1));
            });

            // act
            using (cache)
            {
                cache.Remove(item.Key);
                Func<bool> act = () => cache.Add(item);
                Func<string> act2 = () => cache[item.Key];

                // assert
                act().Should().BeTrue();
                act2().Should().Be(item.Value);
            }
        }

        [Fact]
        [Trait("category", "Memcached")]
        public void Memcached_KeySizeLimit_WithRegion()
        {
            // arrange
            var longKey = string.Join(string.Empty, Enumerable.Repeat("a", 300));

            var item = new CacheItem<string>(longKey, "someRegion", "something");
            var cache = CacheFactory.Build<string>(settings =>
            {
                settings.WithUpdateMode(CacheUpdateMode.Full)
                    .WithMemcachedCacheHandle(Configuration)
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(1));
            });

            // act
            using (cache)
            {
                cache.Remove(item.Key, item.Region);
                Func<bool> act = () => cache.Add(item);
                Func<string> act2 = () => cache[item.Key, item.Region];

                // assert
                act().Should().BeTrue();
                act2().Should().Be(item.Value);
            }
        }

        [Fact]
        [Trait("category", "Memcached")]
        public void Memcached_NoRaceCondition_WithCasButTooFiewRetries()
        {
            // arrange
            using (var cache = CacheFactory.Build<RaceConditionTestElement>(settings =>
            {
                settings.WithUpdateMode(CacheUpdateMode.Full)
                    .WithMemcachedCacheHandle(Configuration)
                        .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromHours(10));
            }))
            {
                cache.Remove("myCounter");
                cache.Add("myCounter", new RaceConditionTestElement() { Counter = 0 });
                int numThreads = 5;
                int iterations = 10;
                int numInnerIterations = 10;
                int countCasModifyCalls = 0;
                int retries = 0;

                // act
                ThreadTestHelper.Run(
                    () =>
                    {
                        for (int i = 0; i < numInnerIterations; i++)
                        {
                            RaceConditionTestElement newValue;
                            cache.TryUpdate(
                                "myCounter",
                                (value) =>
                                {
                                    value.Counter++;
                                    Interlocked.Increment(ref countCasModifyCalls);
                                    return value;
                                },
                                retries,
                                out newValue);
                        }
                    },
                    numThreads,
                    iterations);

                // assert
                Thread.Sleep(10);
                var result = cache.Get("myCounter");
                result.Should().NotBeNull();
                Trace.TraceInformation("Counter increased to " + result.Counter + " cas calls needed " + countCasModifyCalls);
                result.Counter.Should().BeLessThan(
                    numThreads * numInnerIterations * iterations,
                    "counter should NOT be exactly the expected value");
                countCasModifyCalls.Should().Be(
                    numThreads * numInnerIterations * iterations,
                    "with one try, we exactly one update call per iteration");
            }
        }

        [Fact]
        [Trait("category", "Memcached")]
        public void Memcached_NoRaceCondition_WithCasHandling()
        {
            // arrange
            using (var cache = CacheFactory.Build<RaceConditionTestElement>(settings =>
            {
                settings.WithUpdateMode(CacheUpdateMode.Full)
                    .WithSystemRuntimeCacheHandle()
                        .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMilliseconds(1))
                    .And
                    .WithMemcachedCacheHandle(Configuration)
                        .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(10));
            }))
            {
                cache.Remove("myCounter");
                cache.Add("myCounter", new RaceConditionTestElement() { Counter = 0 });
                int numThreads = 5;
                int iterations = 10;
                int numInnerIterations = 10;
                int countCasModifyCalls = 0;

                // act
                ThreadTestHelper.Run(
                    () =>
                    {
                        for (int i = 0; i < numInnerIterations; i++)
                        {
                            cache.Update(
                                "myCounter",
                                (value) =>
                                {
                                    value.Counter++;
                                    Interlocked.Increment(ref countCasModifyCalls);
                                    return value;
                                },
                                500);
                        }
                    },
                    numThreads,
                    iterations);

                // assert
                Thread.Sleep(10);
                var result = cache.Get("myCounter");
                result.Should().NotBeNull();
                Trace.WriteLine("Counter increased to " + result.Counter + " cas calls needed " + countCasModifyCalls);
                result.Counter.Should().Be(numThreads * numInnerIterations * iterations, "counter should be exactly the expected value");
                countCasModifyCalls.Should().BeGreaterThan((int)result.Counter, "we expect many version collisions, so cas calls should be way higher then the count result");
            }
        }

        [Fact]
        [Trait("category", "Memcached")]
        public void Memcached_NoRaceCondition_WithCasHandling_WithRegion()
        {
            // arrange
            using (var cache = CacheFactory.Build<RaceConditionTestElement>(settings =>
            {
                settings.WithUpdateMode(CacheUpdateMode.Full)
                    .WithMemcachedCacheHandle(Configuration)
                        .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(10));
            }))
            {
                var region = "region";
                var key = "myKey";
                cache.Remove(key, region);
                cache.Add(key, new RaceConditionTestElement() { Counter = 0 }, region);
                int numThreads = 5;
                int iterations = 10;
                int numInnerIterations = 10;
                int countCasModifyCalls = 0;

                // act
                ThreadTestHelper.Run(
                    () =>
                    {
                        for (int i = 0; i < numInnerIterations; i++)
                        {
                            cache.Update(
                                key,
                                region,
                                (value) =>
                                {
                                    value.Counter++;
                                    Interlocked.Increment(ref countCasModifyCalls);
                                    return value;
                                },
                                500);
                        }
                    },
                    numThreads,
                    iterations);

                // assert
                Thread.Sleep(10);
                var result = cache.Get(key, region);
                result.Should().NotBeNull();
                Trace.TraceInformation("Counter increased to " + result.Counter + " cas calls needed " + countCasModifyCalls);
                result.Counter.Should().Be(numThreads * numInnerIterations * iterations, "counter should be exactly the expected value");
                countCasModifyCalls.Should().BeGreaterThan((int)result.Counter, "we expect many version collisions, so cas calls should be way higher then the count result");
            }
        }

        [Fact]
        [Trait("category", "Memcached")]
        public void Memcached_RaceCondition_WithoutCasHandling()
        {
            // arrange
            using (var cache = CacheFactory.Build<RaceConditionTestElement>(settings =>
            {
                settings.WithUpdateMode(CacheUpdateMode.Full)
                    .WithMemcachedCacheHandle(Configuration)
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(20));
            }))
            {
                cache.Remove("myCounter");
                cache.Add("myCounter", new RaceConditionTestElement() { Counter = 0 });
                int numThreads = 5;
                int iterations = 10;
                int numInnerIterations = 10;

                // act
                ThreadTestHelper.Run(
                    () =>
                    {
                        for (int i = 0; i < numInnerIterations; i++)
                        {
                            var val = cache.Get("myCounter");
                            val.Should().NotBeNull();
                            val.Counter++;

                            cache.Put("myCounter", val);
                        }
                    },
                    numThreads,
                    iterations);

                // assert
                Thread.Sleep(10);
                var result = cache.Get("myCounter");
                result.Should().NotBeNull();
                Trace.TraceInformation("Counter increased to " + result.Counter);
                result.Counter.Should().NotBe(numThreads * numInnerIterations * iterations);
            }
        }

        [Fact]
        [Trait("category", "Memcached")]
        public void Memcached_Update_ItemNotAdded()
        {
            // arrange
            using (var cache = CacheFactory.Build<RaceConditionTestElement>(settings =>
            {
                settings.WithUpdateMode(CacheUpdateMode.Full)
                    .WithMemcachedCacheHandle(Configuration)
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(20));
            }))
            {
                RaceConditionTestElement value;

                // act
                Func<bool> act = () => cache.TryUpdate(Guid.NewGuid().ToString(), item => item, out value);

                // assert
                act().Should().BeFalse("Item has not been added to the cache");
            }
        }

        [Fact]
        [Trait("category", "Memcached")]
        public void Memcached_TimeoutNotBeGreaterThan30Days()
        {
            // arrange
            using (var cache = CacheFactory.Build<object>(settings =>
            {
                settings.WithUpdateMode(CacheUpdateMode.Up)
                    .WithMemcachedCacheHandle(Configuration)
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromDays(40));
            }))
            {
                // act
                Action act = () => cache.Add(Guid.NewGuid().ToString(), "test");

                // assert
                act.ShouldThrow<InvalidOperationException>("*30 days*");
            }
        }
    }
}
#endif