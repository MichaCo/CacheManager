#if !NETCOREAPP
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using CacheManager.Core;
using CacheManager.MicrosoftCachingMemory;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace CacheManager.Tests
{
    [ExcludeFromCodeCoverage]
#if NET40
    [Trait("Framework", "NET40")]
#else
    [Trait("Framework", "NET45")]
#endif
    public class MemoryCacheTests
    {
        #region MS Memory Cache

        [Fact]
        public void MsMemory_Extensions_Simple()
        {
            var expectedCacheOptions = new MemoryCacheOptions();
            var cfg = new ConfigurationBuilder().WithMicrosoftMemoryCacheHandle().Build();
            var cache = new BaseCacheManager<string>(cfg);

            cfg.CacheHandleConfigurations.First()
                .ConfigurationTypes.First().ShouldBeEquivalentTo(expectedCacheOptions);
            cfg.CacheHandleConfigurations.Count.Should().Be(1);
            cfg.CacheHandleConfigurations.First().Name.Should().NotBeNullOrWhiteSpace();
            cfg.CacheHandleConfigurations.First().IsBackplaneSource.Should().BeFalse();

            cache.CacheHandles.Count().Should().Be(1);
        }

        [Fact]
        public void MsMemory_Extensions_Named()
        {
            string name = "some instance name";
            var expectedCacheOptions = new MemoryCacheOptions();
            var cfg = new ConfigurationBuilder().WithMicrosoftMemoryCacheHandle(name).Build();
            var cache = new BaseCacheManager<string>(cfg);

            cfg.CacheHandleConfigurations.First()
                .ConfigurationTypes.First().ShouldBeEquivalentTo(expectedCacheOptions);
            cfg.CacheHandleConfigurations.Count.Should().Be(1);
            cfg.CacheHandleConfigurations.First().Name.Should().Be(name);
            cfg.CacheHandleConfigurations.First().IsBackplaneSource.Should().BeFalse();

            cache.CacheHandles.Count().Should().Be(1);
        }

        [Fact]
        public void MsMemory_Extensions_NamedB()
        {
            string name = "some instance name";
            var expectedCacheOptions = new MemoryCacheOptions();
            var cfg = new ConfigurationBuilder().WithMicrosoftMemoryCacheHandle(name, true).Build();
            var cache = new BaseCacheManager<string>(cfg);

            cfg.CacheHandleConfigurations.First()
                .ConfigurationTypes.First().ShouldBeEquivalentTo(expectedCacheOptions);
            cfg.CacheHandleConfigurations.Count.Should().Be(1);
            cfg.CacheHandleConfigurations.First().Name.Should().Be(name);
            cfg.CacheHandleConfigurations.First().IsBackplaneSource.Should().BeTrue();

            cache.CacheHandles.Count().Should().Be(1);
        }

        [Fact]
        public void MsMemory_Extensions_SimpleWithCfg()
        {
            var expectedCacheOptions = new MemoryCacheOptions()
            {
                Clock = new Microsoft.Extensions.Internal.SystemClock(),
                CompactOnMemoryPressure = true,
                ExpirationScanFrequency = TimeSpan.FromSeconds(20)
            };

            var cfg = new ConfigurationBuilder().WithMicrosoftMemoryCacheHandle(expectedCacheOptions).Build();
            var cache = new BaseCacheManager<string>(cfg);

            cfg.CacheHandleConfigurations.First()
                .ConfigurationTypes.First().ShouldBeEquivalentTo(expectedCacheOptions);
            cfg.CacheHandleConfigurations.Count.Should().Be(1);
            cfg.CacheHandleConfigurations.First().Name.Should().NotBeNullOrWhiteSpace();
            cfg.CacheHandleConfigurations.First().IsBackplaneSource.Should().BeFalse();

            cache.CacheHandles.Count().Should().Be(1);
            cache.CacheHandles.OfType<MemoryCacheHandle<string>>().First().memoryCacheOptions.ShouldBeEquivalentTo(expectedCacheOptions);
        }

        [Fact]
        public void MsMemory_Extensions_SimpleWithCfgNamed()
        {
            string name = "some instance name";
            var expectedCacheOptions = new MemoryCacheOptions()
            {
                Clock = new Microsoft.Extensions.Internal.SystemClock(),
                CompactOnMemoryPressure = true,
                ExpirationScanFrequency = TimeSpan.FromSeconds(20)
            };

            var cfg = new ConfigurationBuilder().WithMicrosoftMemoryCacheHandle(name, expectedCacheOptions).Build();
            var cache = new BaseCacheManager<string>(cfg);

            cfg.CacheHandleConfigurations.First()
                .ConfigurationTypes.First().ShouldBeEquivalentTo(expectedCacheOptions);
            cfg.CacheHandleConfigurations.Count.Should().Be(1);
            cfg.CacheHandleConfigurations.First().Name.Should().Be(name);
            cfg.CacheHandleConfigurations.First().IsBackplaneSource.Should().BeFalse();

            cache.CacheHandles.Count().Should().Be(1);
            cache.CacheHandles.OfType<MemoryCacheHandle<string>>().First().memoryCacheOptions.ShouldBeEquivalentTo(expectedCacheOptions);
        }

        [Fact]
        public void MsMemory_Extensions_SimpleWithCfgNamedB()
        {
            string name = "some instance name";
            var expectedCacheOptions = new MemoryCacheOptions()
            {
                Clock = new Microsoft.Extensions.Internal.SystemClock(),
                CompactOnMemoryPressure = true,
                ExpirationScanFrequency = TimeSpan.FromSeconds(20)
            };

            var cfg = new ConfigurationBuilder().WithMicrosoftMemoryCacheHandle(name, true, expectedCacheOptions).Build();
            var cache = new BaseCacheManager<string>(cfg);

            cfg.CacheHandleConfigurations.First()
                .ConfigurationTypes.First().ShouldBeEquivalentTo(expectedCacheOptions);
            cfg.CacheHandleConfigurations.Count.Should().Be(1);
            cfg.CacheHandleConfigurations.First().Name.Should().Be(name);
            cfg.CacheHandleConfigurations.First().IsBackplaneSource.Should().BeTrue();

            cache.CacheHandles.Count().Should().Be(1);
            cache.CacheHandles.OfType<MemoryCacheHandle<string>>().First().memoryCacheOptions.ShouldBeEquivalentTo(expectedCacheOptions);
        }

        [Fact]
        [Trait("category", "Unreliable")]
        public void MsMemory_Absolute_DoesExpire()
        {
            // arrange
            var key = Guid.NewGuid().ToString();
            var item = new CacheItem<object>(key, "something", ExpirationMode.Absolute, new TimeSpan(0, 0, 0, 0, 300));

            // act
            using (var act = CacheFactory.Build(_ => _.WithMicrosoftMemoryCacheHandle()))
            {
                // act
                act.Add(item);
                act[key].Should().NotBeNull();

                Thread.Sleep(310);

                // assert
                act[key].Should().BeNull();
            }
        }

        [Fact]
        [Trait("category", "Unreliable")]
        public void MsMemory_Sliding_DoesExpire()
        {
            // arrange
            var key = Guid.NewGuid().ToString();
            var item = new CacheItem<object>(key, "something", ExpirationMode.Sliding, new TimeSpan(0, 0, 0, 0, 8));

            // act
            using (var act = CacheFactory.Build(_ => _.WithMicrosoftMemoryCacheHandle()))
            {
                // act
                act.Add(item);

                Thread.Sleep(15);

                // assert
                act[key].Should().BeNull();
            }
        }

        [Fact]
        [Trait("category", "Unreliable")]
        public void MsMemory_Sliding_DoesSlide()
        {
            // arrange
            var key = Guid.NewGuid().ToString();
            var item = new CacheItem<object>(key, "something", ExpirationMode.Sliding, TimeSpan.FromMilliseconds(200));

            // act
            var act = CacheFactory.Build(_ => _.WithMicrosoftMemoryCacheHandle());
            {
                // act
                act.Add(item);

                var valid = true;
                var state = 0;
                var t = new Thread(new ThreadStart(() =>
                {
                    Thread.Sleep(100);
                    valid = act[key] != null;

                    if (valid)
                    {
                        state = 1;
                        Thread.Sleep(100);
                        valid = act[key] != null;
                    }

                    if (valid)
                    {
                        state = 2;
                        Thread.Sleep(200);
                        valid = act[key] == null;
                    }
                }));

                t.Start();
                t.Join();
                valid.Should().BeTrue("State: " + state);
            }
        }

        #endregion MS Memory Cache

        #region System Runtime Caching

        [Fact]
        public void SysRuntime_Extensions_Simple()
        {
            var cfg = new ConfigurationBuilder().WithSystemRuntimeCacheHandle().Build();
            var cache = new BaseCacheManager<string>(cfg);

            cfg.CacheHandleConfigurations.Count.Should().Be(1);
            cfg.CacheHandleConfigurations.First().Name.Should().NotBeNullOrWhiteSpace();
            cfg.CacheHandleConfigurations.First().IsBackplaneSource.Should().BeFalse();

            cache.CacheHandles.Count().Should().Be(1);
        }

        [Fact]
        public void SysRuntime_Extensions_Named()
        {
            string name = "instanceName";
            var cfg = new ConfigurationBuilder().WithSystemRuntimeCacheHandle(name).Build();
            var cache = new BaseCacheManager<string>(cfg);

            cfg.CacheHandleConfigurations.Count.Should().Be(1);
            cfg.CacheHandleConfigurations.First().Name.Should().Be(name);
            cfg.CacheHandleConfigurations.First().IsBackplaneSource.Should().BeFalse();

            cache.CacheHandles.Count().Should().Be(1);
        }

        [Fact]
        public void SysRuntime_Extensions_NamedB()
        {
            string name = "instanceName";
            var cfg = new ConfigurationBuilder().WithSystemRuntimeCacheHandle(name, true).Build();
            var cache = new BaseCacheManager<string>(cfg);

            cfg.CacheHandleConfigurations.Count.Should().Be(1);
            cfg.CacheHandleConfigurations.First().Name.Should().Be(name);
            cfg.CacheHandleConfigurations.First().IsBackplaneSource.Should().BeTrue();

            cache.CacheHandles.Count().Should().Be(1);
        }

        [Fact]
        [Trait("category", "Unreliable")]
        public void SysRuntime_Absolute_DoesExpire()
        {
            // arrange
            var key = Guid.NewGuid().ToString();
            var item = new CacheItem<object>(key, "something", ExpirationMode.Absolute, new TimeSpan(0, 0, 0, 0, 300));

            // act
            using (var act = CacheFactory.Build(_ => _.WithSystemRuntimeCacheHandle()))
            {
                // act
                act.Add(item);
                act[key].Should().NotBeNull();

                Thread.Sleep(310);

                // assert
                act[key].Should().BeNull();
            }
        }

#if !NO_APP_CONFIG
        [Fact]
        [Trait("category", "NotOnMono")]
        public void SysRuntime_CreateDefaultCache()
        {
            using (var act = CacheFactory.Build(_ => _.WithSystemRuntimeCacheHandle()))
            {
                // arrange
                var settings = ((RuntimeCache.MemoryCacheHandle<object>)act.CacheHandles.ElementAt(0)).CacheSettings;

                // act assert
                settings["CacheMemoryLimitMegabytes"].Should().Be("42");
                settings["PhysicalMemoryLimitPercentage"].Should().Be("69");
                settings["PollingInterval"].Should().Be("00:10:00");
            }
        }

        [Fact]
        [Trait("category", "NotOnMono")]
        public void SysRuntime_CreateNamedCache()
        {
            using (var act = CacheFactory.Build(_ => _.WithSystemRuntimeCacheHandle("NamedTest")))
            {
                // arrange
                var settings = ((RuntimeCache.MemoryCacheHandle<object>)act.CacheHandles.ElementAt(0)).CacheSettings;

                // act assert
                settings["CacheMemoryLimitMegabytes"].Should().Be("12");
                settings["PhysicalMemoryLimitPercentage"].Should().Be("23");
                settings["PollingInterval"].Should().Be("00:02:00");
            }
        }
#endif

        [Fact]
        [Trait("category", "Unreliable")]
        public void SysRuntime_Sliding_DoesExpire()
        {
            // arrange
            var key = Guid.NewGuid().ToString();
            var item = new CacheItem<object>(key, "something", ExpirationMode.Sliding, new TimeSpan(0, 0, 0, 0, 8));

            // act
            using (var act = CacheFactory.Build(_ => _.WithSystemRuntimeCacheHandle()))
            {
                // act
                act.Add(item);

                Thread.Sleep(15);

                // assert
                act[key].Should().BeNull();
            }
        }

        [Fact]
        [Trait("category", "Unreliable")]
        public void SysRuntime_Sliding_DoesSlide()
        {
            // arrange
            var key = Guid.NewGuid().ToString();
            var item = new CacheItem<object>(key, "something", ExpirationMode.Sliding, TimeSpan.FromMilliseconds(200));

            // act
            var act = CacheFactory.Build(_ => _.WithSystemRuntimeCacheHandle());
            {
                // act
                act.Add(item);

                var valid = true;
                var state = 0;
                var t = new Thread(new ThreadStart(() =>
                {
                    Thread.Sleep(100);
                    valid = act[key] != null;

                    if (valid)
                    {
                        state = 1;
                        Thread.Sleep(100);
                        valid = act[key] != null;
                    }

                    if (valid)
                    {
                        state = 2;
                        Thread.Sleep(200);
                        valid = act[key] == null;
                    }
                }));

                t.Start();
                t.Join();
                valid.Should().BeTrue("State: " + state);
            }
        }

        #endregion
    }
}
#endif