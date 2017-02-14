#if MEMCACHEDENABLED
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CacheManager.Core;
using CacheManager.Memcached;
using Enyim.Caching;
using Enyim.Caching.Configuration;
using FluentAssertions;
using Xunit;

namespace CacheManager.Tests
{
    [ExcludeFromCodeCoverage]
    public class MemcachedTests
    {
        private object logObj = new object();

        public static MemcachedClientConfiguration Configuration
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
        public void Memcached_ExtensionsWork_WithClient()
        {
            var client = new MemcachedClient(Configuration);
            var cache = CacheFactory.Build(
                settings =>
                settings.WithMemcachedCacheHandle(client));

            Assert.NotNull(cache);
            var handle = cache.CacheHandles.OfType<MemcachedCacheHandle<object>>().First();
            Assert.Equal(client, handle.Cache);
        }

        [Fact]
        [Trait("category", "Memcached")]
        public void Memcached_ExtensionsWork_WithClientNamed()
        {
            var client = new MemcachedClient(Configuration);
            var cache = CacheFactory.Build(
                settings =>
                settings.WithMemcachedCacheHandle("memcachedname", client));

            Assert.NotNull(cache);
            var handle = cache.CacheHandles.OfType<MemcachedCacheHandle<object>>().First();
            Assert.Equal(client, handle.Cache);
        }

        [Fact]
        [Trait("category", "Memcached")]
        public void Memcached_ExtensionsWork_WithClientNull()
        {
            Action act = () => CacheFactory.Build(
                settings =>
                settings.WithMemcachedCacheHandle("name", (MemcachedClient)null));

            var ex = Record.Exception(act);

            // doesn't actually throw check on client because it hits the standard ctor without the client because of the Null value.
            Assert.IsType<InvalidOperationException>(ex);
        }

        [Fact]
        [Trait("category", "Memcached")]
        public void Memcached_ExtensionsWork_WithConfiguration()
        {
            var cache = CacheFactory.Build(
                settings =>
                settings.WithMemcachedCacheHandle(Configuration));

            Assert.NotNull(cache);
        }

        [Fact]
        [Trait("category", "Memcached")]
        public void Memcached_ExtensionsWork_WithConfigurationNamed()
        {
            var cache = CacheFactory.Build(
                settings =>
                settings.WithMemcachedCacheHandle("cachename", Configuration));

            Assert.NotNull(cache);
        }

        [Fact]
        [Trait("category", "Memcached")]
        public void Memcached_ExtensionsWork_WithConfigurationNull()
        {
            Action act = () => CacheFactory.Build(
                settings =>
                settings.WithMemcachedCacheHandle("name", (MemcachedClientConfiguration)null));

            var ex = Record.Exception(act);

            // doesn't actually throw check on client because it hits the standard ctor without the client because of the Null value.
            Assert.IsType<InvalidOperationException>(ex);
        }

        [Fact]
        [Trait("category", "Memcached")]
        public void Memcached_KeySizeLimit()
        {
            // arrange
            var longKey = string.Join(string.Empty, Enumerable.Repeat("a", 300));

            var item = new CacheItem<string>(longKey, "something");
            var cache = CacheFactory.FromConfiguration<string>(
                TestManagers.BaseConfiguration.Builder
                    .WithUpdateMode(CacheUpdateMode.Up)
                    .WithMemcachedCacheHandle(Configuration)
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromHours(1))
                    .Build());

            // act
            using (cache)
            {
                try
                {
                    cache.Remove(item.Key);
                    Func<bool> act = () => cache.Add(item);
                    Func<string> act2 = () => cache[item.Key];

                    // assert
                    act().Should().BeTrue();
                    act2().Should().Be(item.Value);
                }
                catch
                {
                    throw;
                }
            }
        }

        [Fact]
        [Trait("category", "Memcached")]
        public void Memcached_KeySizeLimit_WithRegion()
        {
            // arrange
            var longKey = string.Join(string.Empty, Enumerable.Repeat("a", 300));

            var item = new CacheItem<string>(longKey, "someRegion", "something");
            var cache = CacheFactory.FromConfiguration<string>(
                TestManagers.BaseConfiguration.Builder
                    .WithUpdateMode(CacheUpdateMode.Up)
                    .WithMemcachedCacheHandle(Configuration)
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(1))
                    .Build());

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
        public async Task Memcached_NoRaceCondition_WithCasButTooFiewRetries()
        {
            // arrange
            using (var cache = CacheFactory.FromConfiguration<RaceConditionTestElement>(
                TestManagers.BaseConfiguration.Builder
                    .WithUpdateMode(CacheUpdateMode.Up)
                    .WithMemcachedCacheHandle(Configuration)
                        .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromHours(10))
                    .Build()))
            {
                cache.Remove("myCounter");
                cache.Add("myCounter", new RaceConditionTestElement() { Counter = 0 });
                int numThreads = 5;
                int iterations = 10;
                int numInnerIterations = 10;
                int countCasModifyCalls = 0;
                int retries = 0;

                // act
                await ThreadTestHelper.RunAsync(
                    async () =>
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

                        await Task.Delay(10);
                    },
                    numThreads,
                    iterations);

                // assert
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
        public async Task Memcached_NoRaceCondition_WithCasHandling()
        {
            // arrange
            using (var cache = CacheFactory.FromConfiguration<RaceConditionTestElement>(
                TestManagers.BaseConfiguration.Builder
                    .WithMaxRetries(int.MaxValue)
                    .WithSystemRuntimeCacheHandle()
                        .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMilliseconds(1))
                    .And
                    .WithMemcachedCacheHandle(Configuration)
                        .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(60))
                    .Build()))
            {
                var key = Guid.NewGuid().ToString();
                cache.Remove(key);
                cache.Add(key, new RaceConditionTestElement() { Counter = 0 }).Should().BeTrue();
                int numThreads = 5;
                int iterations = 10;
                int numInnerIterations = 10;
                int countCasModifyCalls = 0;

                // act
                await ThreadTestHelper.RunAsync(
                    async () =>
                    {
                        for (int i = 0; i < numInnerIterations; i++)
                        {
                            cache.Update(
                                key,
                                (value) =>
                                {
                                    value.Counter++;
                                    Interlocked.Increment(ref countCasModifyCalls);
                                    return value;
                                });
                        }

                        await Task.Delay(10);
                    },
                    numThreads,
                    iterations);

                // assert
                var result = cache.Get(key);
                result.Should().NotBeNull();
                Trace.WriteLine("Counter increased to " + result.Counter + " cas calls needed " + countCasModifyCalls);
                result.Counter.Should().Be(numThreads * numInnerIterations * iterations, "counter should be exactly the expected value");
                countCasModifyCalls.Should().BeGreaterThan((int)result.Counter, "we expect many version collisions, so cas calls should be way higher then the count result");
            }
        }

        [Fact]
        [Trait("category", "Memcached")]
        public async Task Memcached_NoRaceCondition_WithCasHandling_WithRegion()
        {
            // arrange
            using (var cache = CacheFactory.FromConfiguration<RaceConditionTestElement>(
                TestManagers.BaseConfiguration.Builder
                    .WithMaxRetries(int.MaxValue)
                    .WithMemcachedCacheHandle(Configuration)
                        .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(10))
                    .Build()))
            {
                var region = Guid.NewGuid().ToString();
                var key = Guid.NewGuid().ToString();
                cache.Remove(key, region);
                cache.Add(key, new RaceConditionTestElement() { Counter = 0 }, region);
                int numThreads = 5;
                int iterations = 10;
                int numInnerIterations = 10;
                int countCasModifyCalls = 0;

                // act
                await ThreadTestHelper.RunAsync(
                    async () =>
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
                                });
                        }
                        await Task.Delay(10);
                    },
                    numThreads,
                    iterations);

                // assert
                var result = cache.Get(key, region);
                result.Should().NotBeNull();
                Trace.TraceInformation("Counter increased to " + result.Counter + " cas calls needed " + countCasModifyCalls);
                result.Counter.Should().Be(numThreads * numInnerIterations * iterations, "counter should be exactly the expected value");
                countCasModifyCalls.Should().BeGreaterThan((int)result.Counter, "we expect many version collisions, so cas calls should be way higher then the count result");
            }
        }

        [Fact]
        [Trait("category", "Memcached")]
        public async Task Memcached_RaceCondition_WithoutCasHandling()
        {
            // arrange
            using (var cache = CacheFactory.FromConfiguration<RaceConditionTestElement>(
                TestManagers.BaseConfiguration.Builder
                    .WithUpdateMode(CacheUpdateMode.Up)
                    .WithMemcachedCacheHandle(Configuration)
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(20))
                .Build()))
            {
                var key = Guid.NewGuid().ToString();
                cache.Remove(key);
                cache.Add(key, new RaceConditionTestElement() { Counter = 0 });
                int numThreads = 5;
                int iterations = 10;
                int numInnerIterations = 10;

                // act
                await ThreadTestHelper.RunAsync(
                    async () =>
                    {
                        for (int i = 0; i < numInnerIterations; i++)
                        {
                            var val = cache.Get(key);
                            val.Should().NotBeNull();
                            val.Counter++;

                            cache.Put(key, val);
                        }

                        await Task.Delay(10);
                    },
                    numThreads,
                    iterations);

                // assert
                var result = cache.Get(key);
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
            using (var cache = CacheFactory.FromConfiguration<RaceConditionTestElement>(
                TestManagers.BaseConfiguration.Builder
                    .WithUpdateMode(CacheUpdateMode.Up)
                    .WithMemcachedCacheHandle(Configuration)
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(20))
                .Build()))
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
            using (var cache = CacheFactory.FromConfiguration<object>(
                TestManagers.BaseConfiguration.Builder
                    .WithUpdateMode(CacheUpdateMode.Up)
                    .WithMemcachedCacheHandle(Configuration)
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromDays(40))
                .Build()))
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