using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.Redis;
using FluentAssertions;
using Xunit;

namespace CacheManager.Tests
{
    /// <summary>
    /// To run the memcached test, run the bat files under /memcached before executing the tests.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class RedisTests
    {
        static Func<int, Func<bool>, bool> waitForIt = (tries, act) =>
        {
            var i = 0;
            var result = false;
            while (!result && i < tries)
            {
                i++;
                result = act();
                if (result) return true;
                Thread.Sleep(100);
            }

            return false;
        };

        [Fact]
        public void Redis_BackplaneEvents_Add()
        {
            var channelName = Guid.NewGuid().ToString();
            var cacheA = TestManagers.CreateRedisAndDicCacheWithBackplane(101, false, channelName);
            var cacheB = TestManagers.CreateRedisAndDicCacheWithBackplane(101, false, channelName);
            var key = Guid.NewGuid().ToString();
            var eventTriggered = 0;
            var errors = 0;

            cacheB.OnAdd += (ev, args) =>
            {
                try
                {
                    args.Key.Should().Be(key);
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Remote);

                    Interlocked.Increment(ref eventTriggered);
                }
                catch
                {
                    Interlocked.Increment(ref errors);
                    throw;
                }
            };

            cacheA.Add(key, key);

            waitForIt(10, () => eventTriggered == 1).Should().BeTrue("Event should get triggered through the backplane.");
            errors.Should().Be(0, "there should be no errors");
        }

        [Fact]
        public void Redis_BackplaneEvents_Put()
        {
            var channelName = Guid.NewGuid().ToString();
            var cacheA = TestManagers.CreateRedisAndDicCacheWithBackplane(101, false, channelName);
            var cacheB = TestManagers.CreateRedisAndDicCacheWithBackplane(101, false, channelName);
            var key = Guid.NewGuid().ToString();
            var eventTriggered = 0;
            var errors = 0;

            cacheB.OnPut += (ev, args) =>
            {
                try
                {
                    args.Key.Should().Be(key);
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Remote);
                    cacheB[key].Should().Be("new val");

                    Interlocked.Increment(ref eventTriggered);
                }
                catch
                {
                    Interlocked.Increment(ref errors);
                    throw;
                }
            };

            // two calls might actually trigger only once, but the new value should be the last changed one.
            cacheA.Put(key, key);
            cacheA.Put(key, "new val");

            waitForIt(10, () => eventTriggered >= 1).Should().BeTrue("Event should get triggered through the backplane.");
            errors.Should().Be(0, "there should be no errors");
        }

        [Fact]
        public void Redis_BackplaneEvents_Update()
        {
            var channelName = Guid.NewGuid().ToString();
            var cacheA = TestManagers.CreateRedisAndDicCacheWithBackplane(101, false, channelName);
            var cacheB = TestManagers.CreateRedisAndDicCacheWithBackplane(101, false, channelName);

            var key = Guid.NewGuid().ToString();
            var eventTriggered = 0;
            var errors = 0;

            cacheB.OnUpdate += (ev, args) =>
            {
                try
                {
                    args.Key.Should().Be(key);
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Remote);
                    cacheB[key].Should().Be("new");
                    Interlocked.Increment(ref eventTriggered);
                }
                catch
                {
                    Interlocked.Increment(ref errors);
                    throw;
                }
            };

            cacheA.Add(key, key);
            cacheA.Update(key, v => "new").Should().Be("new");

            waitForIt(10, () => eventTriggered == 1).Should().BeTrue("Event should get triggered through the backplane.");
            errors.Should().Be(0, "there should be no errors");
        }
        
        [Fact]
        public void Redis_BackplaneEvents_Clear()
        {
            var channelName = Guid.NewGuid().ToString();
            var cacheA = TestManagers.CreateRedisAndDicCacheWithBackplane(101, false, channelName);
            var cacheB = TestManagers.CreateRedisAndDicCacheWithBackplane(101, false, channelName);

            var key = Guid.NewGuid().ToString();
            var eventTriggered = 0;

            cacheB.OnClear += (ev, args) =>
            {
                Interlocked.Increment(ref eventTriggered);
            };

            cacheA.Add(key, key);
            cacheA.Clear();

            waitForIt(10, () => eventTriggered == 1).Should().BeTrue("Event should get triggered through the backplane.");
        }

        [Fact]
        public void Redis_Configuration_NoEndpoint()
        {
            Action act = () => ConfigurationBuilder.BuildConfiguration(
                s => s.WithRedisConfiguration(
                    "key",
                    c => c.WithAllowAdmin()));

            act.ShouldThrow<InvalidOperationException>().WithMessage("*endpoints*");
        }

#if !DNXCORE50
#if !NO_APP_CONFIG
        [Fact]
        [Trait("category", "NotOnMono")]
        public void Redis_Configurations_LoadStandard()
        {
            RedisConfigurations.LoadConfiguration();
        }
#endif

        [Fact]
        [Trait("category", "NotOnMono")]
        public void Redis_Configurations_LoadWithConnectionString()
        {
            string fileName = BaseCacheManagerTest.GetCfgFileName(@"/Configuration/configuration.valid.allFeatures.config");

            RedisConfigurations.LoadConfiguration(fileName, RedisConfigurationSection.DefaultSectionName);
            var cfg = RedisConfigurations.GetConfiguration("redisConnectionString");
            cfg.ConnectionString.Should().Be("127.0.0.1:6379,allowAdmin=true,ssl=true");
        }

        [Fact]
        public void Redis_Configurations_LoadSection_InvalidSectionName()
        {
            Action act = () => RedisConfigurations.LoadConfiguration((string)null);

            act.ShouldThrow<ArgumentNullException>().WithMessage("*sectionName*");
        }

        [Fact]
        public void Redis_Configurations_LoadSection_InvalidFileName()
        {
            Action act = () => RedisConfigurations.LoadConfiguration((string)null, "section");

            act.ShouldThrow<ArgumentNullException>().WithMessage("*fileName*");
        }

        [Fact]
        public void Redis_Configurations_LoadSection_SectionDoesNotExist()
        {
            Action act = () => RedisConfigurations.LoadConfiguration(Guid.NewGuid().ToString());

            act.ShouldThrow<ArgumentNullException>().WithMessage("*section*");
        }
#endif

        [Fact]
        [Trait("category", "Redis")]
        [Trait("category", "Unreliable")]
        public void Redis_Absolute_DoesExpire()
        {
            // arrange
            var item = new CacheItem<object>(Guid.NewGuid().ToString(), "something", ExpirationMode.Absolute, TimeSpan.FromMilliseconds(150));
            var cache = TestManagers.CreateRedisCache(1);

            // act/assert
            using (cache)
            {
                // act
                var result = cache.Add(item);

                // assert
                result.Should().BeTrue();
                Thread.Sleep(30);
                var value = cache.GetCacheItem(item.Key);
                value.Should().NotBeNull();

                Thread.Sleep(150);
                var valueExpired = cache.GetCacheItem(item.Key);
                valueExpired.Should().BeNull();
            }
        }

        [Fact]
        [Trait("category", "Redis")]
        [Trait("category", "Unreliable")]
        public void Redis_Absolute_DoesExpire_MultiClients()
        {
            // arrange
            var cacheA = TestManagers.CreateRedisCache(2);
            var cacheB = TestManagers.CreateRedisCache(2);

            // act/assert
            using (cacheA)
            using (cacheB)
            {
                // act
                var item = new CacheItem<object>(Guid.NewGuid().ToString(), "something", ExpirationMode.Absolute, TimeSpan.FromMilliseconds(100));

                var result = cacheA.Add(item);

                var itemB = cacheB.GetCacheItem(item.Key);

                // assert
                result.Should().BeTrue();
                item.Value.Should().Be(itemB.Value);

                Thread.Sleep(30);
                cacheA.GetCacheItem(item.Key).Should().NotBeNull();
                cacheB.GetCacheItem(item.Key).Should().NotBeNull();

                // after 130ms both it should be expired
                Thread.Sleep(100);
                cacheA.GetCacheItem(item.Key).Should().BeNull();
                cacheB.GetCacheItem(item.Key).Should().BeNull();
            }
        }

#if !DNXCORE50
        [Fact(Skip = "not working 100% of the time")]
        [Trait("category", "Redis")]
        [Trait("category", "Unreliable")]
        public void Redis_Multiple_PubSub_Change()
        {
            // arrange
            string fileName = BaseCacheManagerTest.GetCfgFileName(@"/Configuration/configuration.valid.allFeatures.config");
            var channelName = Guid.NewGuid().ToString();

            // redis config name must be same for all cache handles, configured via file and via code
            // otherwise the pub sub channel name is different
            string cacheName = "redisConfig";

            RedisConfigurations.LoadConfiguration(fileName, RedisConfigurationSection.DefaultSectionName);

            var cfg = ConfigurationBuilder.LoadConfigurationFile(fileName, cacheName);
            cfg.BackplaneChannelName = channelName;

            var cfgCache = CacheFactory.FromConfiguration<object>(cfg);

            var item = new CacheItem<object>(Guid.NewGuid().ToString(), "something");

            // act/assert
            RedisTests.RunMultipleCaches(
                (cacheA, cacheB) =>
                {
                    cacheA.Put(item);
                    Thread.Sleep(10);
                    var value = cacheB.Get(item.Key);
                    value.Should().Be(item.Value, cacheB.ToString());
                    cacheB.Put(item.Key, "new value");
                },
                (cache) =>
                {
                    int tries = 0;
                    object value = null;
                    do
                    {
                        tries++;
                        Thread.Sleep(100);
                        value = cache.Get(item.Key);
                    }
                    while (value.ToString() != "new value" && tries < 10);

                    value.Should().Be("new value", cache.ToString());
                },
                1,
                TestManagers.CreateRedisAndDicCacheWithBackplane(69, true, channelName),
                cfgCache,
                TestManagers.CreateRedisCache(69),
                TestManagers.CreateRedisAndDicCacheWithBackplane(69, true, channelName));
        }
#endif

        ////[Fact(Skip = "needs clear")]
        [Trait("category", "Redis")]
        public void Redis_Multiple_PubSub_Clear()
        {
            // arrange
            var item = new CacheItem<object>(Guid.NewGuid().ToString(), "something");
            var channelName = Guid.NewGuid().ToString();

            // act/assert
            RedisTests.RunMultipleCaches(
                (cacheA, cacheB) =>
                {
                    cacheA.Add(item);
                    cacheB.Get(item.Key).Should().Be(item.Value);
                    cacheB.Clear();
                },
                (cache) =>
                {
                    cache.Get(item.Key).Should().BeNull();
                },
                2,
                TestManagers.CreateRedisAndDicCacheWithBackplane(444, true, channelName),
                TestManagers.CreateRedisAndDicCacheWithBackplane(444, true, channelName),
                TestManagers.CreateRedisCache(444),
                TestManagers.CreateRedisAndDicCacheWithBackplane(444, true, channelName));
        }

        [Fact]
        [Trait("category", "Redis")]
        ////[Trait("category", "Unreliable")]
        public void Redis_Multiple_PubSub_ClearRegion()
        {
            // arrange
            var item = new CacheItem<object>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "something");
            var channelName = Guid.NewGuid().ToString();

            // act/assert
            RedisTests.RunMultipleCaches(
                (cacheA, cacheB) =>
                {
                    cacheA.Add(item);
                    cacheB.Get(item.Key, item.Region).Should().Be(item.Value);
                    cacheB.ClearRegion(item.Region);
                },
                (cache) =>
                {
                    cache.Get(item.Key, item.Region).Should().BeNull();
                },
                2,
                TestManagers.CreateRedisCache(5),
                TestManagers.CreateRedisCache(5),
                TestManagers.CreateRedisCache(5),
                TestManagers.CreateRedisCache(5));
        }

        [Fact(Skip = "not working 100% of the time")]
        [Trait("category", "Redis")]
        [Trait("category", "Unreliable")]
        public void Redis_Multiple_PubSub_Remove()
        {
            // arrange
            var item = new CacheItem<object>(Guid.NewGuid().ToString(), "something");
            var channelName = Guid.NewGuid().ToString();

            // act/assert
            RedisTests.RunMultipleCaches(
                (cacheA, cacheB) =>
                {
                    cacheA.Add(item);
                    cacheB.Get(item.Key).Should().Be(item.Value);
                    cacheB.Remove(item.Key);
                    Thread.Sleep(10);
                },
                (cache) =>
                {
                    int tries = 0;
                    object value = null;
                    do
                    {
                        tries++;
                        Thread.Sleep(100);
                        value = cache.GetCacheItem(item.Key);
                    }
                    while (value != null && tries < 10);

                    value.Should().BeNull();
                },
                1,
                TestManagers.CreateRedisAndDicCacheWithBackplane(6, true, channelName),
                TestManagers.CreateRedisAndDicCacheWithBackplane(6, true, channelName),
                TestManagers.CreateRedisCache(6),
                TestManagers.CreateRedisAndDicCacheWithBackplane(6, true, channelName));
        }

        [Fact]
        [Trait("category", "Redis")]
        [Trait("category", "Unreliable")]
        public void Redis_NoRaceCondition_WithUpdate()
        {
            using (var cache = CacheFactory.Build<RaceConditionTestElement>(settings =>
            {
                settings.WithUpdateMode(CacheUpdateMode.Full)
                    .WithJsonSerializer()
                    .WithRedisCacheHandle("default")
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(20));
                settings.WithRedisConfiguration("default", config =>
                {
                    config.WithAllowAdmin()
                        .WithDatabase(7)
                        .WithEndpoint("127.0.0.1", 6379);
                });
            }))
            {
                var key = Guid.NewGuid().ToString();
                cache.Remove(key);
                cache.Add(key, new RaceConditionTestElement() { Counter = 0 });
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
                            cache.Update(key, (value) =>
                            {
                                value.Counter++;
                                Interlocked.Increment(ref countCasModifyCalls);
                                return value;
                            });
                        }
                    },
                    numThreads,
                    iterations);

                // assert
                Thread.Sleep(10);
                var result = cache.Get(key);
                result.Should().NotBeNull();
                Trace.TraceInformation("Counter increased to " + result.Counter + " cas calls needed " + countCasModifyCalls);
                result.Counter.Should().Be(numThreads * numInnerIterations * iterations, "counter should be exactly the expected value");
                countCasModifyCalls.Should().BeGreaterThan((int)result.Counter, "we expect many version collisions, so cas calls should be way higher then the count result");
            }
        }

        [Fact]
        [Trait("category", "Redis")]
        [Trait("category", "Unreliable")]
        public void Redis_RaceCondition_WithoutUpdate()
        {
            using (var cache = CacheFactory.Build<RaceConditionTestElement>(settings =>
            {
                settings.WithUpdateMode(CacheUpdateMode.Full)
                    .WithJsonSerializer()
                    .WithRedisCacheHandle("default")
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(20));
                settings.WithRedisConfiguration("default", config =>
                {
                    config.WithAllowAdmin()
                        .WithDatabase(8)
                        .WithEndpoint("127.0.0.1", 6379);
                });
            }))
            {
                var key = Guid.NewGuid().ToString();
                cache.Add(key, new RaceConditionTestElement() { Counter = 0 });
                int numThreads = 5;
                int iterations = 10;
                int numInnerIterations = 10;

                // act
                ThreadTestHelper.Run(
                    () =>
                    {
                        for (int i = 0; i < numInnerIterations; i++)
                        {
                            var val = cache.Get(key);
                            val.Should().NotBeNull();
                            val.Counter++;

                            cache.Put(key, val);
                        }
                    },
                    numThreads,
                    iterations);

                // assert
                Thread.Sleep(10);
                var result = cache.Get(key);
                result.Should().NotBeNull();
                Trace.TraceInformation("Counter increased to " + result.Counter);
                result.Counter.Should().NotBe(numThreads * numInnerIterations * iterations);
            }
        }

        [Fact]
        [Trait("category", "Redis")]
        [Trait("category", "Unreliable")]
        public void Redis_Sliding_DoesExpire()
        {
            // arrange
            var item = new CacheItem<object>(Guid.NewGuid().ToString(), "something", ExpirationMode.Sliding, TimeSpan.FromMilliseconds(50));
            var cache = TestManagers.CreateRedisCache(9);

            // act/assert
            using (cache)
            {
                // act
                var result = cache.Add(item);

                // assert
                result.Should().BeTrue();

                // 450ms added so absolute would be expired on the 2nd go
                for (int s = 0; s < 3; s++)
                {
                    Thread.Sleep(20);
                    var value = cache.GetCacheItem(item.Key);
                    value.Should().NotBeNull();
                }

                Thread.Sleep(60);
                var valueExpired = cache.GetCacheItem(item.Key);
                valueExpired.Should().BeNull();
            }
        }

        [Fact]
        [Trait("category", "Redis")]
        [Trait("category", "Unreliable")]
        public void Redis_Sliding_DoesExpire_MultiClients()
        {
            // arrange
            var item = new CacheItem<object>(Guid.NewGuid().ToString(), "something", ExpirationMode.Sliding, TimeSpan.FromMilliseconds(100));
            var channelName = Guid.NewGuid().ToString();
            var cacheA = TestManagers.CreateRedisAndDicCacheWithBackplane(10, true, channelName);
            var cacheB = TestManagers.CreateRedisAndDicCacheWithBackplane(10, true, channelName);

            // act/assert
            using (cacheA)
            using (cacheB)
            {
                // act
                var result = cacheA.Add(item);

                var valueB = cacheB.Get(item.Key);

                // assert
                result.Should().BeTrue();
                item.Value.Should().Be(valueB);

                // 450ms added so absolute would be expired on the 2nd go
                for (int s = 0; s < 3; s++)
                {
                    Thread.Sleep(80);
                    cacheA.GetCacheItem(item.Key).Should().NotBeNull();
                    cacheB.GetCacheItem(item.Key).Should().NotBeNull();
                }

                Thread.Sleep(150);
                cacheA.GetCacheItem(item.Key).Should().BeNull();
                cacheB.GetCacheItem(item.Key).Should().BeNull();
            }
        }

        [Fact]
        [Trait("category", "Redis")]
        [Trait("category", "Unreliable")]
        public void Redis_Sliding_DoesExpire_WithRegion()
        {
            // arrange
            var item = new CacheItem<object>(Guid.NewGuid().ToString(), "something", "region", ExpirationMode.Sliding, TimeSpan.FromMilliseconds(50));
            var cache = TestManagers.CreateRedisCache(11);

            // act/assert
            using (cache)
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

#if !DNXCORE50
        [Fact]
        [Trait("category", "Redis")]
        public void Redis_Valid_CfgFile_LoadWithRedisBackplane()
        {
            // arrange
            string fileName = BaseCacheManagerTest.GetCfgFileName(@"/Configuration/configuration.valid.allFeatures.config");
            string cacheName = "redisConfig";

            // have to load the configuration manually because the file is not avialbale to the default ConfigurtaionManager
            RedisConfigurations.LoadConfiguration(fileName, RedisConfigurationSection.DefaultSectionName);

            // act
            var cfg = ConfigurationBuilder.LoadConfigurationFile(fileName, cacheName);
            var cache = CacheFactory.FromConfiguration<object>(cfg);

            // assert
            cache.CacheHandles.Any(p => p.Configuration.IsBackplaneSource).Should().BeTrue();
        }

#if !NO_APP_CONFIG
        [Fact]
        [Trait("category", "Redis")]
        public void Redis_LoadWithRedisBackplane_FromAppConfig()
        {
            // RedisConfigurations should load this from default section from app.config

            // arrange
            string cacheName = "redisWithBackplaneAppConfig";

            // act
            var cfg = ConfigurationBuilder.LoadConfiguration(cacheName);
            var cache = CacheFactory.FromConfiguration<object>(cfg);
            var handle = cache.CacheHandles.First(p => p.Configuration.IsBackplaneSource) as RedisCacheHandle<object>;

            // test running something on the redis handle, Count should be enough to test the connection
            Action count = () => { var x = handle.Count; };

            // assert
            handle.Should().NotBeNull();
            count.ShouldNotThrow();
        }

        [Fact]
        [Trait("category", "Redis")]
        public void Redis_LoadWithRedisBackplane_FromAppConfigConnectionStrings()
        {
            // RedisConfigurations should load this from AppSettings from app.config
            // arrange
            string cacheName = "redisWithBackplaneAppConfigConnectionStrings";

            // act
            var cfg = ConfigurationBuilder.LoadConfiguration(cacheName);
            var cache = CacheFactory.FromConfiguration<object>(cfg);
            var handle = cache.CacheHandles.First(p => p.Configuration.IsBackplaneSource) as RedisCacheHandle<object>;

            // test running something on the redis handle, Count should be enough to test the connection
            Action count = () => { var x = handle.Count; };

            // assert
            handle.Should().NotBeNull();
            count.ShouldNotThrow();
        }
#endif
#endif
        [Fact]
        [Trait("category", "Redis")]
        public void Redis_ValueConverter_CacheTypeConversion_Poco()
        {
            var cache = TestManagers.CreateRedisCache<Poco>(17);

            // act/assert
            using (cache)
            {
                var key = Guid.NewGuid().ToString();
                var value = new Poco() { Id = 23, Something = "§asdad" };
                cache.Add(key, value);
                var result = (Poco)cache.Get(key);
                value.ShouldBeEquivalentTo(result);
            }
        }

        [Fact]
        [Trait("category", "Redis")]
        public void Redis_ValueConverter_ObjectCacheTypeConversion_Poco()
        {
            var cache = TestManagers.CreateRedisCache(17);

            // act/assert
            using (cache)
            {
                var key = Guid.NewGuid().ToString();
                var value = new Poco() { Id = 23, Something = "§asdad" };
                cache.Add(key, value);
                var result = (Poco)cache.Get(key);
                value.ShouldBeEquivalentTo(result);
            }
        }

        [Theory]
        [Trait("category", "Redis")]
        [InlineData(byte.MaxValue)]
        [InlineData(new byte[] { 0, 1, 2, 3, 4 })]
        [InlineData("some string")]
        [InlineData(int.MaxValue)]
        [InlineData(uint.MaxValue)]
        [InlineData(short.MaxValue)]
        [InlineData(ushort.MaxValue)]
        [InlineData(float.MaxValue)]
        [InlineData(double.MaxValue)]
        [InlineData(true)]
        [InlineData(false)]
        [InlineData(long.MaxValue)]
        [InlineData(ulong.MaxValue)]
        [InlineData((ulong)int.MaxValue)]
        [InlineData((ulong)long.MaxValue)]
        [InlineData(char.MinValue)]
        [InlineData(char.MaxValue)]
        public void Redis_ValueConverter_ValidateValuesTypesNotUsingSerializer<T>(T value)
        {
            var redisKey = Guid.NewGuid().ToString();
            var cache = CacheFactory.Build<object>(settings =>
            {
                settings
                    .WithSerializer(typeof(FakeTestSerializer))
                    .WithRedisConfiguration(redisKey, config =>
                    {
                        config
                            .WithDatabase(66)
                            .WithEndpoint("127.0.0.1", 6379);
                    })
                    .WithRedisCacheHandle(redisKey, true);
            });

            var key = Guid.NewGuid().ToString();

            cache.Add(key, value);
            var val = cache[key];
            val.ShouldBeEquivalentTo(value);
            val.GetType().Should().Be(value.GetType());
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
                Thread.Sleep(10);

                if (caches.Length == 1)
                {
                    stepA(caches[0], caches[0]);
                }
                else
                {
                    stepA(caches[0], caches[1]);
                }

                Thread.Sleep(100);

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
    }

#if !DNXCORE50
    [Serializable]
#endif
    [ExcludeFromCodeCoverage]
    internal class Poco
    {
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "For testing only")]
        public int Id { get; set; }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "For testing only")]
        public string Something { get; set; }
    }

    [ExcludeFromCodeCoverage]
    internal class FakeTestSerializer : ICacheSerializer
    {
        public object Deserialize(byte[] data, Type target)
        {
            throw new NotImplementedException();
        }

        public CacheItem<T> DeserializeCacheItem<T>(byte[] value, Type valueType)
        {
            throw new NotImplementedException();
        }

        public byte[] Serialize<T>(T value)
        {
            throw new NotImplementedException();
        }

        public byte[] SerializeCacheItem<T>(CacheItem<T> value)
        {
            throw new NotImplementedException();
        }
    }
}