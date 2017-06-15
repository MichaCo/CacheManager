using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.Redis;
using FluentAssertions;
using StackExchange.Redis;
using Xunit;
using Xunit.Sdk;

namespace CacheManager.Tests
{
    /// <summary>
    /// To run the redis tests, make sure a local redis server instance is running. See redis folder under tools.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class RedisTests
    {
        private enum CacheEvent
        {
            OnAdd,
            OnPut,
            OnRemove,
            OnUpdate,
            OnClear,
            OnClearRegion
        }

#if NETCOREAPP
        [Fact]
        public void Redis_WithoutSerializer_ShouldThrow()
        {
            var cfg = ConfigurationBuilder.BuildConfiguration(
                settings =>
                    settings
                        .WithRedisConfiguration("redis-key", "localhost")
                        .WithRedisCacheHandle("redis-key")) as CacheManagerConfiguration;

            Action act = () => new BaseCacheManager<string>(cfg);
            act.ShouldThrow<InvalidOperationException>().WithMessage("*requires serialization*");
        }
#endif

        [Fact]
        [Trait("category", "Redis")]
        [Trait("category", "Unreliable")]
        public void Redis_Extensions_WithClient()
        {
            var configKey = Guid.NewGuid().ToString();
            var client = ConnectionMultiplexer.Connect("localhost");
            var cache = CacheFactory.Build<string>(
                s => s
                    .WithJsonSerializer()
                    .WithRedisConfiguration(configKey, client)
                    .WithRedisCacheHandle(configKey));

            var handle = cache.CacheHandles.OfType<RedisCacheHandle<string>>().First();
            var cfg = RedisConfigurations.GetConfiguration(configKey);

            Assert.Equal(handle.Configuration.Name, configKey);
            Assert.Equal(0, cfg.Database);
            Assert.Equal("localhost:6379", cfg.ConnectionString);

            // cleanup
            RedisConnectionManager.RemoveConnection(client.Configuration);
            client.Dispose();
        }

        [Fact]
        [Trait("category", "Redis")]
        [Trait("category", "Unreliable")]
        public void Redis_Extensions_WithClientWithDb()
        {
            var configKey = Guid.NewGuid().ToString();
            var client = ConnectionMultiplexer.Connect("localhost");
            var cache = CacheFactory.Build<string>(
                s => s
                    .WithJsonSerializer()
                    .WithRedisConfiguration(configKey, client, 23)
                    .WithRedisCacheHandle(configKey));

            var handle = cache.CacheHandles.OfType<RedisCacheHandle<string>>().First();
            var cfg = RedisConfigurations.GetConfiguration(configKey);

            Assert.Equal(handle.Configuration.Name, configKey);
            Assert.Equal(23, cfg.Database);
            Assert.Equal("localhost:6379", cfg.ConnectionString);

            // cleanup
            RedisConnectionManager.RemoveConnection(client.Configuration);
            client.Dispose();
        }

        [Fact]
        [Trait("category", "Redis")]
        public async Task Redis_BackplaneEvents_Add()
        {
            var key = Guid.NewGuid().ToString();

            await TestBackplaneEventDistributed<CacheActionEventArgs>(
                CacheEvent.OnAdd,
                (cacheA) =>
                {
                    cacheA.Add(key, key);
                },
                (cacheA, args) =>
                {
                    args.Key.Should().Be(key);
                    args.Region.Should().BeNull();
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Local);
                    cacheA[key].Should().Be(key);
                },
                (cacheB, args) =>
                {
                    args.Key.Should().Be(key);
                    args.Region.Should().BeNull();
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Remote);
                    cacheB[key].Should().Be(key);
                });
        }

        [Fact]
        [Trait("category", "Redis")]
        public async Task Redis_ValidateVersion_AddPutGetUpdate()
        {
            var configKey = Guid.NewGuid().ToString();
            var multi = ConnectionMultiplexer.Connect("localhost");
            var cache = CacheFactory.Build<Poco>(
                s => s
                    .WithRedisConfiguration(configKey, multi)
                    .WithBondCompactBinarySerializer()
                    .WithRedisCacheHandle(configKey));

            // don't keep it and also dispose it later (seems appveyor doesn't like too many open connections)
            RedisConnectionManager.RemoveConnection(multi.Configuration);

            // act/assert
            using (multi)
            using (cache)
            {
                var key = Guid.NewGuid().ToString();
                var value = new Poco() { Id = 23, Something = "§asdad" };
                cache.Add(key, value);
                await Task.Delay(10);

                var version = (int)multi.GetDatabase(0).HashGet(key, "version");
                version.Should().Be(1);

                cache.Put(key, value);
                await Task.Delay(10);

                version = (int)multi.GetDatabase(0).HashGet(key, "version");
                version.Should().Be(2);

                cache.Update(key, r => { r.Something = "new text"; return r; });
                await Task.Delay(10);

                version = (int)multi.GetDatabase(0).HashGet(key, "version");
                version.Should().Be(3);
                cache.Get(key).Something.Should().Be("new text");
            }
        }

        [Fact]
        [Trait("category", "Redis")]
        [Trait("category", "Unreliable")]
        public void Redis_UseExistingConnection()
        {
            var conConfig = new ConfigurationOptions()
            {
                ConnectTimeout = 10000,
                AbortOnConnectFail = false,
                ConnectRetry = 10
            };
            conConfig.EndPoints.Add("localhost:6379");

            var multiplexer = ConnectionMultiplexer.Connect(conConfig);

            var cfg = ConfigurationBuilder.BuildConfiguration(
                s => s
                    .WithJsonSerializer()
                    .WithRedisConfiguration("redisKey", multiplexer)
                    .WithRedisCacheHandle("redisKey"));

            RedisConnectionManager.RemoveConnection(multiplexer.Configuration);

            using (multiplexer)
            using (var cache = new BaseCacheManager<long>(cfg))
            {
                cache.Add(Guid.NewGuid().ToString(), 12345);
            }
        }

        [Fact]
        public async Task Redis_BackplaneEvents_AddWithRegion()
        {
            var key = Guid.NewGuid().ToString();
            var region = Guid.NewGuid().ToString();

            await TestBackplaneEventDistributed<CacheActionEventArgs>(
                CacheEvent.OnAdd,
                (cacheA) =>
                {
                    cacheA.Add(key, key, region);
                },
                (cacheA, args) =>
                {
                    args.Key.Should().Be(key);
                    args.Region.Should().Be(region);
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Local);
                    cacheA[key, region].Should().Be(key);
                },
                (cacheB, args) =>
                {
                    args.Key.Should().Be(key);
                    args.Region.Should().Be(region);
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Remote);
                    cacheB[key, region].Should().Be(key);
                });
        }

        /// <summary>
        /// Testing in memory cache only with backplane through redis (not using Redis as cache at all)
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Redis_BackplaneEvents_InMemory_AddWithRegion()
        {
            var key = Guid.NewGuid().ToString();
            var region = Guid.NewGuid().ToString();

            await TestBackplaneEventInMemory<CacheActionEventArgs>(
                CacheEvent.OnAdd,
                (cacheA, cacheB) =>
                {
                    // in memory is not distributed, adding only to CacheA the event triggerd on cache B does trigger but cacheB doesn't have the item.
                    cacheB.Add(key, key, region);
                    cacheA.Add(key, key, region);
                },
                (cacheA, args) =>
                {
                    args.Key.Should().Be(key);
                    args.Region.Should().Be(region);

                    // cannot test origin as there might be two events triggered, one local one remote
                    cacheA[key, region].Should().Be(key);
                },
                (cacheB, args) =>
                {
                    args.Key.Should().Be(key);
                    args.Region.Should().Be(region);

                    // cannot test origin as there might be two events triggered, one local one remote
                    cacheB[key, region].Should().Be(key);
                },
                expectedRemoteTriggers: 2);
        }

        [Fact]
        public async Task Redis_BackplaneEvents_Put()
        {
            var key = Guid.NewGuid().ToString();

            await TestBackplaneEventDistributed<CacheActionEventArgs>(
                CacheEvent.OnPut,
                (cacheA) =>
                {
                    cacheA.Add(key, key);
                    cacheA.Put(key, "new val");
                },
                (cacheA, args) =>
                {
                    args.Key.Should().Be(key);
                    args.Region.Should().BeNull();
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Local);
                    cacheA[key].Should().Be("new val");
                },
                (cacheB, args) =>
                {
                    args.Key.Should().Be(key);
                    args.Region.Should().BeNull();
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Remote);
                    cacheB[key].Should().Be("new val");
                });
        }

        /// <summary>
        /// Testing in memory cache only with backplane through redis (not using Redis as cache at all)
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Redis_BackplaneEvents_InMemory_Put()
        {
            var key = Guid.NewGuid().ToString();

            await TestBackplaneEventInMemory<CacheActionEventArgs>(
                CacheEvent.OnPut,
                (cacheA, cacheB) =>
                {
                    // in memory is not distributed, adding only to CacheA the event triggerd on cache B does trigger but cacheB doesn't have the item.
                    cacheA.Add(key, key);
                    cacheA.Put(key, "new val");
                },
                (cacheA, args) =>
                {
                    args.Key.Should().Be(key);
                    args.Region.Should().BeNull();
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Local);
                    cacheA[key].Should().Be("new val");
                },
                (cacheB, args) =>
                {
                    args.Key.Should().Be(key);
                    args.Region.Should().BeNull();
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Remote);
                    cacheB[key].Should().Be(null);
                },
                expectedRemoteTriggers: 1);
        }

        [Fact]
        public async Task Redis_BackplaneEvents_PutWithRegion()
        {
            var key = Guid.NewGuid().ToString();
            var region = Guid.NewGuid().ToString();

            await TestBackplaneEventDistributed<CacheActionEventArgs>(
                CacheEvent.OnPut,
                (cacheA) =>
                {
                    cacheA.Add(key, key, region);
                    cacheA.Put(key, "new val", region);
                },
                (cacheA, args) =>
                {
                    args.Key.Should().Be(key);
                    args.Region.Should().Be(region);
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Local);
                    cacheA[key, region].Should().Be("new val");
                },
                (cacheB, args) =>
                {
                    args.Key.Should().Be(key);
                    args.Region.Should().Be(region);
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Remote);
                    cacheB[key, region].Should().Be("new val");
                });
        }

        [Fact]
        public async Task Redis_BackplaneEvents_Remove()
        {
            var key = Guid.NewGuid().ToString();

            await TestBackplaneEventDistributed<CacheActionEventArgs>(
                CacheEvent.OnRemove,
                (cacheA) =>
                {
                    cacheA.Add(key, key).Should().BeTrue();
                    cacheA.Remove(key).Should().BeTrue();
                },
                (cacheA, args) =>
                {
                    args.Key.Should().Be(key);
                    args.Region.Should().BeNull();
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Local);
                    cacheA[key].Should().BeNull();
                },
                (cacheB, args) =>
                {
                    args.Key.Should().Be(key);
                    args.Region.Should().BeNull();
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Remote);
                    cacheB[key].Should().BeNull();
                });
        }

        [Fact]
        public async Task Redis_BackplaneEvents_Remove_WithRegion()
        {
            var key = Guid.NewGuid().ToString();
            var region = Guid.NewGuid().ToString();

            await TestBackplaneEventDistributed<CacheActionEventArgs>(
                CacheEvent.OnRemove,
                (cacheA) =>
                {
                    cacheA.Add(key, key).Should().BeTrue();
                    cacheA.Add(key, key, region).Should().BeTrue();
                    cacheA.Remove(key, region).Should().BeTrue();
                },
                (cacheA, args) =>
                {
                    args.Key.Should().Be(key);
                    args.Region.Should().Be(region);
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Local);
                    cacheA[key].Should().NotBeNull();
                    cacheA[key, region].Should().BeNull();
                },
                (cacheB, args) =>
                {
                    args.Key.Should().Be(key);
                    args.Region.Should().Be(region);
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Remote);
                    cacheB[key].Should().NotBeNull();
                    cacheB[key, region].Should().BeNull();
                });
        }

        /// <summary>
        /// Testing in memory cache only with backplane through redis (not using Redis as cache at all)
        /// This test in particular tests that a second in memory cache gets keys evicted if the same key
        /// got removed by another cache (both caches connected through the backplane)
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Redis_BackplaneEvents_InMemory_Remove_WithRegion()
        {
            var key = Guid.NewGuid().ToString();
            var region = Guid.NewGuid().ToString();

            await TestBackplaneEventInMemory<CacheActionEventArgs>(
                CacheEvent.OnRemove,
                (cacheA, cacheB) =>
                {
                    cacheA.Add(key, key).Should().BeTrue();
                    cacheA.Add(key, key, region).Should().BeTrue();

                    // adding to cache B, too, as we don't have a distributed cache
                    cacheB.Add(key, key, region).Should().BeTrue();

                    // remove from A only, should also remove it from B via backplane
                    cacheA.Remove(key, region).Should().BeTrue();
                },
                (cacheA, args) =>
                {
                    args.Key.Should().Be(key);
                    args.Region.Should().Be(region);
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Local);
                    cacheA[key].Should().NotBeNull();
                    cacheA[key, region].Should().BeNull();
                },
                (cacheB, args) =>
                {
                    args.Key.Should().Be(key);
                    args.Region.Should().Be(region);
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Remote);
                    cacheB[key, region].Should().BeNull();
                },
                expectedRemoteTriggers: 1);
        }

        [Fact]
        public async Task Redis_BackplaneEvents_Update()
        {
            var key = Guid.NewGuid().ToString();
            var newValue = "new value";

            await TestBackplaneEventDistributed<CacheActionEventArgs>(
                CacheEvent.OnUpdate,
                (cacheA) =>
                {
                    cacheA.Add(key, key);
                    cacheA.Update(key, v => newValue).Should().Be(newValue);
                },
                (cacheA, args) =>
                {
                    args.Key.Should().Be(key);
                    args.Region.Should().BeNull();
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Local);
                    cacheA[key].Should().Be(newValue);
                },
                (cacheB, args) =>
                {
                    args.Key.Should().Be(key);
                    args.Region.Should().BeNull();
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Remote);
                    cacheB[key].Should().Be(newValue);
                });
        }

        [Fact]
        public async Task Redis_BackplaneEvents_Update_WithgRegion()
        {
            var key = Guid.NewGuid().ToString();
            var region = Guid.NewGuid().ToString();
            var newValue = "new value";

            await TestBackplaneEventDistributed<CacheActionEventArgs>(
                CacheEvent.OnUpdate,
                (cacheA) =>
                {
                    cacheA.Add(key, key, region);
                    cacheA.Update(key, region, v => newValue).Should().Be(newValue);
                },
                (cacheA, args) =>
                {
                    args.Key.Should().Be(key);
                    args.Region.Should().Be(region);
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Local);
                    cacheA[key, region].Should().Be(newValue);
                },
                (cacheB, args) =>
                {
                    args.Key.Should().Be(key);
                    args.Region.Should().Be(region);
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Remote);
                    cacheB[key, region].Should().Be(newValue);
                });
        }

        /// <summary>
        /// Testing in memory cache only with backplane through redis (not using Redis as cache at all)
        /// This test in particular tests on update, add or put, the key in cacheB does not change or get evicted.
        /// To remove the key in all in memory cache instances, Remove must be used!
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task Redis_BackplaneEvents_InMemory_Update_WithRegion()
        {
            var key = Guid.NewGuid().ToString();
            var region = Guid.NewGuid().ToString();
            var newValue = "new value";

            await TestBackplaneEventInMemory<CacheActionEventArgs>(
                CacheEvent.OnUpdate,
                (cacheA, cacheB) =>
                {
                    cacheA.Add(key, key, region);

                    // adding to cache B, too, as we don't have a distributed cache
                    cacheB.Add(key, key, region).Should().BeTrue();

                    // the update should evict the key from cache B
                    cacheA.Update(key, region, v => newValue).Should().Be(newValue);
                },
                (cacheA, args) =>
                {
                    args.Key.Should().Be(key);
                    args.Region.Should().Be(region);
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Local);
                    cacheA[key, region].Should().Be(newValue);
                },
                (cacheB, args) =>
                {
                    args.Key.Should().Be(key);
                    args.Region.Should().Be(region);
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Remote);

                    // important to note, the key in cacheB has never been updated or removed, so it should still be the "old" value!
                    cacheB[key, region].Should().Be(key);
                },
                expectedRemoteTriggers: 1);
        }

        [Fact]
        public async Task Redis_BackplaneEvents_Clear()
        {
            var key = Guid.NewGuid().ToString();

            await TestBackplaneEventDistributed<CacheClearEventArgs>(
                CacheEvent.OnClear,
                (cacheA) =>
                {
                    cacheA.Add(key, key);
                    cacheA.Clear();
                },
                (cacheA, args) =>
                {
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Local);
                    cacheA.Get(key).Should().BeNull();
                },
                (cacheB, args) =>
                {
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Remote);
                    cacheB.Get(key).Should().BeNull();
                });
        }

        [Fact]
        public async Task Redis_BackplaneEvents_InMemory_Clear()
        {
            var key = Guid.NewGuid().ToString();

            await TestBackplaneEventInMemory<CacheClearEventArgs>(
                CacheEvent.OnClear,
                (cacheA, cacheB) =>
                {
                    cacheA.Add(key, key);
                    cacheB.Add(key, key);
                    cacheA.Clear();
                },
                (cacheA, args) =>
                {
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Local);
                    cacheA.Get(key).Should().BeNull();
                },
                (cacheB, args) =>
                {
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Remote);
                    cacheB.Get(key).Should().BeNull();
                },
                expectedRemoteTriggers: 1);
        }

        [Fact]
        public async Task Redis_BackplaneEvents_ClearRegion()
        {
            var key = Guid.NewGuid().ToString();
            var region = Guid.NewGuid().ToString();

            await TestBackplaneEventDistributed<CacheClearRegionEventArgs>(
                CacheEvent.OnClearRegion,
                (cacheA) =>
                {
                    cacheA.Add(key, key);
                    cacheA.Add(key, key, region);
                    cacheA.ClearRegion(region);
                },
                (cacheA, args) =>
                {
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Local);
                    cacheA.Get(key).Should().NotBeNull();
                    cacheA.Get(key, region).Should().BeNull();
                },
                (cacheB, args) =>
                {
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Remote);
                    cacheB.Get(key).Should().NotBeNull();
                    cacheB.Get(key, region).Should().BeNull();
                });
        }

        [Fact]
        public async Task Redis_BackplaneEvents_InMemory_ClearRegion()
        {
            var key = Guid.NewGuid().ToString();
            var region = Guid.NewGuid().ToString();

            await TestBackplaneEventInMemory<CacheClearRegionEventArgs>(
                CacheEvent.OnClearRegion,
                (cacheA, cacheB) =>
                {
                    cacheA.Add(key, key);
                    cacheA.Add(key, key, region);

                    cacheB.Add(key, key);
                    cacheB.Add(key, key, region);

                    cacheA.ClearRegion(region);
                },
                (cacheA, args) =>
                {
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Local);
                    cacheA.Get(key).Should().NotBeNull();
                    cacheA.Get(key, region).Should().BeNull();
                },
                (cacheB, args) =>
                {
                    args.Origin.Should().Be(CacheActionEventArgOrigin.Remote);
                    cacheB.Get(key).Should().NotBeNull();
                    cacheB.Get(key, region).Should().BeNull();
                },
                expectedRemoteTriggers: 1);
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

#if !NETCOREAPP
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
            string fileName = TestConfigurationHelper.GetCfgFileName(@"/Configuration/configuration.valid.allFeatures.config");

            RedisConfigurations.LoadConfiguration(fileName, RedisConfigurationSection.DefaultSectionName);
            var cfg = RedisConfigurations.GetConfiguration("redisConnectionString");
            cfg.ConnectionString.ToLower().Should().Contain("127.0.0.1:6379");//,allowAdmin = true,ssl = false");
            cfg.ConnectionString.ToLower().Should().Contain("allowadmin=true");
            cfg.ConnectionString.ToLower().Should().Contain("ssl=false");
            cfg.Database.Should().Be(131);
            cfg.StrictCompatibilityModeVersion.Should().Be("2.9");
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

        [Fact]
        [Trait("category", "Redis")]
        [Trait("category", "Unreliable")]
        public async Task Redis_Multiple_PubSub_Change()
        {
            // arrange
            var channelName = Guid.NewGuid().ToString();
            var item = new CacheItem<object>(Guid.NewGuid().ToString(), "something");

            // act/assert
            await RunMultipleCaches(
                async (cacheA, cacheB) =>
                {
                    cacheA.Put(item);
                    cacheA.Get(item.Key).Should().Be("something");
                    await Task.Delay(10);
                    var value = cacheB.Get(item.Key);
                    value.Should().Be(item.Value, cacheB.ToString());
                    cacheB.Put(item.Key, "new value");
                },
                async (cache) =>
                {
                    int tries = 0;
                    object value = null;
                    do
                    {
                        tries++;
                        await Task.Delay(100);
                        value = cache.Get(item.Key);
                    }
                    while (value.ToString() != "new value" && tries < 10);

                    value.Should().Be("new value", cache.ToString());
                },
                1,
                TestManagers.CreateRedisAndDicCacheWithBackplane(113, true, channelName, Serializer.Json),
                TestManagers.CreateRedisAndDicCacheWithBackplane(113, true, channelName, Serializer.Json),
                TestManagers.CreateRedisCache(113, false, Serializer.Json),
                TestManagers.CreateRedisAndDicCacheWithBackplane(113, true, channelName, Serializer.Json));
        }

#endif

        ////[Fact(Skip = "needs clear")]
        [Trait("category", "Redis")]
        public async Task Redis_Multiple_PubSub_Clear()
        {
            // arrange
            var item = new CacheItem<object>(Guid.NewGuid().ToString(), "something");
            var channelName = Guid.NewGuid().ToString();

            // act/assert
            await RedisTests.RunMultipleCaches(
                async (cacheA, cacheB) =>
                {
                    cacheA.Add(item);
                    cacheB.Get(item.Key).Should().Be(item.Value);
                    cacheB.Clear();
                    await Task.Delay(0);
                },
                async (cache) =>
                {
                    cache.Get(item.Key).Should().BeNull();
                    await Task.Delay(0);
                },
                2,
                TestManagers.CreateRedisAndDicCacheWithBackplane(444, true, channelName),
                TestManagers.CreateRedisAndDicCacheWithBackplane(444, true, channelName),
                TestManagers.CreateRedisCache(444),
                TestManagers.CreateRedisAndDicCacheWithBackplane(444, true, channelName));
        }

        [Fact]
        [Trait("category", "Redis")]
        public async Task Redis_Multiple_PubSub_ClearRegion()
        {
            // arrange
            var item = new CacheItem<object>(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "something");
            var channelName = Guid.NewGuid().ToString();

            // act/assert
            await RedisTests.RunMultipleCaches(
                async (cacheA, cacheB) =>
                {
                    cacheA.Add(item);
                    cacheB.Get(item.Key, item.Region).Should().Be(item.Value);
                    cacheB.ClearRegion(item.Region);
                    await Task.Delay(0);
                },
                async (cache) =>
                {
                    cache.Get(item.Key, item.Region).Should().BeNull();
                    await Task.Delay(0);
                },
                2,
                TestManagers.CreateRedisCache(5),
                TestManagers.CreateRedisCache(5),
                TestManagers.CreateRedisCache(5),
                TestManagers.CreateRedisCache(5));
        }

        [Fact]
        [Trait("category", "Redis")]
        [Trait("category", "Unreliable")]
        public async Task Redis_Multiple_PubSub_Remove()
        {
            // arrange
            var item = new CacheItem<object>(Guid.NewGuid().ToString(), "something");
            var channelName = Guid.NewGuid().ToString();

            // act/assert
            await RedisTests.RunMultipleCaches(
                async (cacheA, cacheB) =>
                {
                    cacheA.Add(item);
                    cacheB.Get(item.Key).Should().Be(item.Value);
                    cacheB.Remove(item.Key);
                    await Task.Delay(10);
                },
                async (cache) =>
                {
                    int tries = 0;
                    object value = null;
                    do
                    {
                        tries++;
                        await Task.Delay(100);
                        value = cache.GetCacheItem(item.Key);
                    }
                    while (value != null && tries < 50);

                    value.Should().BeNull();
                },
                1,
                TestManagers.CreateRedisAndDicCacheWithBackplane(6, true, channelName),
                TestManagers.CreateRedisAndDicCacheWithBackplane(6, true, channelName),
                TestManagers.CreateRedisCache(6),
                TestManagers.CreateRedisAndDicCacheWithBackplane(6, true, channelName));
        }

        [Fact]
        public void Redis_Verify_NoCredentialsLoggedOrThrown()
        {
            var testLogger = new TestLogger();
            var cfg = ConfigurationBuilder.BuildConfiguration(settings =>
            {
                settings
                    .WithRedisBackplane("redis.config")
                    .WithLogging(typeof(TestLoggerFactory), testLogger)
                    .WithJsonSerializer()
                    .WithRedisCacheHandle("redis.config", true)
                    .And
                    .WithRedisConfiguration("redis.config", config =>
                    {
                        config
                            .WithConnectionTimeout(10)
                            .WithAllowAdmin()
                            .WithDatabase(7)
                            .WithEndpoint("doesnotexist", 6379)
                            .WithPassword("mysupersecret")
                            .WithSsl();
                    });
            });

            Action act = () => new BaseCacheManager<string>(cfg).Put("key", "value");

            act.ShouldThrow<InvalidOperationException>().WithMessage("*password=***");

            testLogger.LogMessages.Any(p => p.Message.ToString().Contains("mysupersecret")).Should().BeFalse();
        }

        [Fact]
        [Trait("category", "Redis")]
        [Trait("category", "Unreliable")]
        public async Task Redis_NoRaceCondition_WithUpdate()
        {
            using (var cache = CacheFactory.Build<RaceConditionTestElement>(settings =>
            {
                settings.WithMaxRetries(int.MaxValue);
                settings.WithUpdateMode(CacheUpdateMode.Up)
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
                                },
                                int.MaxValue);

                            await Task.Delay(0);
                        }
                    },
                    numThreads,
                    iterations);

                // assert
                await Task.Delay(100);
                var result = cache.Get(key);
                result.Should().NotBeNull();
                result.Counter.Should().Be(numThreads * numInnerIterations * iterations, "counter should be exactly the expected value");
                countCasModifyCalls.Should().BeGreaterThan((int)result.Counter, "we expect many version collisions, so cas calls should be way higher then the count result");
            }
        }

        [Fact]
        [Trait("category", "Redis")]
        [Trait("category", "Unreliable")]
        public async Task Redis_RaceCondition_WithoutUpdate()
        {
            using (var cache = CacheFactory.Build<RaceConditionTestElement>(settings =>
            {
                settings.WithUpdateMode(CacheUpdateMode.Up)
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
                await ThreadTestHelper.RunAsync(
                    async () =>
                    {
                        for (int i = 0; i < numInnerIterations; i++)
                        {
                            var val = cache.Get(key);
                            val.Should().NotBeNull();
                            val.Counter++;

                            cache.Put(key, val);
                            await Task.Delay(1);
                        }
                    },
                    numThreads,
                    iterations);

                // assert
                await Task.Delay(10);
                var result = cache.Get(key);
                result.Should().NotBeNull();
                result.Counter.Should().NotBe(numThreads * numInnerIterations * iterations);
            }
        }

        /// <summary>
        /// See #165, version string can be empty is e.g. it comes from app/web.config.
        /// </summary>
        [Fact]
        [Trait("category", "Redis")]
        public void Redis_StrictMode_EmptyString_DoesnTThrow()
        {
            var redisConfigKey = Guid.NewGuid().ToString();
            var redisConfig = new RedisConfiguration(redisConfigKey, "localhost", strictCompatibilityModeVersion: "");
            RedisConfigurations.AddConfiguration(redisConfig);

            var cacheConfig = new ConfigurationBuilder()
                .WithJsonSerializer()
                .WithRedisCacheHandle(redisConfigKey)
                .Build();

            Action act = () => new BaseCacheManager<object>(cacheConfig);

            act.ShouldNotThrow();
        }

#if !NETCOREAPP

        [Fact]
        [Trait("category", "Redis")]
        public void Redis_Valid_CfgFile_LoadWithRedisBackplane()
        {
            // arrange
            string fileName = TestConfigurationHelper.GetCfgFileName(@"/Configuration/configuration.valid.allFeatures.config");
            string cacheName = "redisConfigFromConfig";

            // have to load the configuration manually because the file is not avialbale to the default ConfigurtaionManager
            RedisConfigurations.LoadConfiguration(fileName, RedisConfigurationSection.DefaultSectionName);
            var redisConfig = RedisConfigurations.GetConfiguration("redisFromCfgConfigurationId");

            // act
            var cfg = ConfigurationBuilder.LoadConfigurationFile(fileName, cacheName);

            // assert
            redisConfig.Database.Should().Be(113);
            redisConfig.ConnectionTimeout.Should().Be(1200);
            redisConfig.AllowAdmin.Should().BeTrue();
            redisConfig.KeyspaceNotificationsEnabled.Should().BeTrue();
            redisConfig.TwemproxyEnabled.Should().BeTrue();
            redisConfig.StrictCompatibilityModeVersion.Should().Be("2.7");
        }

        [Fact]
        [Trait("category", "Redis")]
        public void Redis_Valid_CfgFile_LoadWithConnectionString()
        {
            // arrange
            string fileName = TestConfigurationHelper.GetCfgFileName(@"/Configuration/configuration.valid.allFeatures.config");
            string cacheName = "redisConfigFromConnectionString";

            // have to load the configuration manually because the file is not avialbale to the default ConfigurtaionManager
            RedisConfigurations.LoadConfiguration(fileName, RedisConfigurationSection.DefaultSectionName);
            var redisConfig = RedisConfigurations.GetConfiguration("redisConnectionString");

            // act
            var cfg = ConfigurationBuilder.LoadConfigurationFile(fileName, cacheName);
            var cache = CacheFactory.FromConfiguration<object>(cfg);

            // assert
            cache.CacheHandles.Any(p => p.Configuration.IsBackplaneSource).Should().BeTrue();

            // database is the only option apart from key and connection string which must be set, database will not be set through connection string
            // to define which database should actually be used...
            redisConfig.Database.Should().Be(131);
            redisConfig.StrictCompatibilityModeVersion.Should().Be("2.9");
            redisConfig.AllowAdmin.Should().BeTrue();
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
            var cache = TestManagers.CreateRedisCache<Poco>(17, false, Serializer.Json);

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
        public void Redis_ValueConverter_Poco_Update()
        {
            var cache = TestManagers.CreateRedisCache(17, false, Serializer.Json);

            // act/assert
            using (cache)
            {
                var key = Guid.NewGuid().ToString();
                var region = Guid.NewGuid().ToString();
                var value = new Poco() { Id = 23, Something = "§asdad" };
                cache.Add(key, value, region);

                var newValue = new Poco() { Id = 24, Something = "%!else$&" };
                object resultValue = null;
                Func<bool> act = () => cache.TryUpdate(key, region, (o) => newValue, out resultValue);

                act().Should().BeTrue();
                newValue.ShouldBeEquivalentTo(resultValue);
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

        private static async Task RunMultipleCaches<TCache>(
            Func<TCache, TCache, Task> stepA,
            Func<TCache, Task> stepB,
            int iterations,
            params TCache[] caches)
            where TCache : ICacheManager<object>
        {
            for (int i = 0; i < iterations; i++)
            {
                await Task.Delay(10);

                if (caches.Length == 1)
                {
                    await stepA(caches[0], caches[0]);
                }
                else
                {
                    await stepA(caches[0], caches[1]);
                }

                await Task.Delay(100);

                foreach (var cache in caches)
                {
                    await stepB(cache);
                }
            }

            foreach (var cache in caches)
            {
                cache.Dispose();
            }
        }

        private static Task TestBackplaneEventDistributed<TEventArgs>(CacheEvent cacheEvent,
            Action<ICacheManager<object>> arrange,
            Action<ICacheManager<object>, TEventArgs> assertLocal,
            Action<ICacheManager<object>, TEventArgs> assertRemote)
            where TEventArgs : EventArgs
        {
            var channelName = Guid.NewGuid().ToString();
            var cacheA = TestManagers.CreateRedisAndDicCacheWithBackplane(1, false, channelName);
            var cacheB = TestManagers.CreateRedisAndDicCacheWithBackplane(1, false, channelName);

            return TestBackplaneEventRunner(cacheA, cacheB, cacheEvent, arrange, assertLocal, assertRemote, 1);
        }

        private static Task TestBackplaneEventInMemory<TEventArgs>(CacheEvent cacheEvent,
            Action<ICacheManager<object>, ICacheManager<object>> arrange,
            Action<ICacheManager<object>, TEventArgs> assertLocal,
            Action<ICacheManager<object>, TEventArgs> assertRemote,
            int expectedRemoteTriggers)
            where TEventArgs : EventArgs
        {
            var channelName = Guid.NewGuid().ToString();
            var cacheA = TestManagers.CreateDicCacheWithBackplane(false, channelName);
            var cacheB = TestManagers.CreateDicCacheWithBackplane(false, channelName);

            return TestBackplaneEventRunner(cacheA, cacheB, cacheEvent, (a) => arrange(cacheA, cacheB), assertLocal, assertRemote, expectedRemoteTriggers);
        }

        private static async Task TestBackplaneEventRunner<TEventArgs>(
            ICacheManager<object> cacheA,
            ICacheManager<object> cacheB,
            CacheEvent cacheEvent,
            Action<ICacheManager<object>> arrange,
            Action<ICacheManager<object>, TEventArgs> assertLocal,
            Action<ICacheManager<object>, TEventArgs> assertRemote,
            int expectedRemoteTriggers)
            where TEventArgs : EventArgs
        {
            var eventTriggeredLocal = 0;
            var eventTriggeredRemote = 0;
            Exception lastError = null;

            Action<EventArgs> testLocal = (args) =>
            {
                try
                {
                    assertLocal(cacheA, (TEventArgs)args);

                    Interlocked.Increment(ref eventTriggeredLocal);
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    throw;
                }
            };

            Action<EventArgs> testRemote = (args) =>
            {
                try
                {
                    assertRemote(cacheB, (TEventArgs)args);

                    Interlocked.Increment(ref eventTriggeredRemote);
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    throw;
                }
            };

            switch (cacheEvent)
            {
                case CacheEvent.OnAdd:
                    cacheA.OnAdd += (ev, args) =>
                    {
                        testLocal(args);
                    };

                    cacheB.OnAdd += (ev, args) =>
                    {
                        testRemote(args);
                    };
                    break;

                case CacheEvent.OnClear:
                    cacheA.OnClear += (ev, args) =>
                    {
                        testLocal(args);
                    };

                    cacheB.OnClear += (ev, args) =>
                    {
                        testRemote(args);
                    };
                    break;

                case CacheEvent.OnClearRegion:
                    cacheA.OnClearRegion += (ev, args) =>
                    {
                        testLocal(args);
                    };

                    cacheB.OnClearRegion += (ev, args) =>
                    {
                        testRemote(args);
                    };
                    break;

                case CacheEvent.OnPut:
                    cacheA.OnPut += (ev, args) =>
                    {
                        testLocal(args);
                    };

                    cacheB.OnPut += (ev, args) =>
                    {
                        testRemote(args);
                    };
                    break;

                case CacheEvent.OnRemove:
                    cacheA.OnRemove += (ev, args) =>
                    {
                        testLocal(args);
                    };

                    cacheB.OnRemove += (ev, args) =>
                    {
                        testRemote(args);
                    };
                    break;

                case CacheEvent.OnUpdate:
                    cacheA.OnUpdate += (ev, args) =>
                    {
                        testLocal(args);
                    };

                    cacheB.OnUpdate += (ev, args) =>
                    {
                        testRemote(args);
                    };
                    break;
            }

            arrange(cacheA);

            Func<int, Func<bool>, Task<bool>> waitForIt = async (tries, act) =>
            {
                var i = 0;
                var result = false;
                while (!result && i < tries)
                {
                    i++;
                    result = act();
                    if (result)
                    {
                        return true;
                    }

                    await Task.Delay(10);
                }

                return false;
            };

            Func<Exception, string> formatError = (err) =>
            {
                if (err is XunitException xunitError)
                {
                    return xunitError.Message;
                }

                return err?.ToString();
            };

            var triggerResult = await waitForIt(100, () => eventTriggeredRemote == expectedRemoteTriggers);
            lastError.Should().BeNull(formatError(lastError));
            triggerResult.Should().BeTrue("Event should get triggered through the backplane.");
            eventTriggeredLocal.Should().Be(expectedRemoteTriggers, "Local cache event should be triggered one time");
        }
    }

#if !NETCOREAPP

    [Serializable]
#endif
    [ExcludeFromCodeCoverage]
    [Bond.Schema]
    internal class Poco
    {
        [Bond.Id(1)]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "For testing only")]
        public int Id { get; set; }

        [Bond.Id(2)]
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