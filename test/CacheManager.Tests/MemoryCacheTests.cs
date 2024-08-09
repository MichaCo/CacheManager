﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CacheManager.Core;
using CacheManager.MicrosoftCachingMemory;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace CacheManager.Tests
{
    [ExcludeFromCodeCoverage]
    public class MemoryCacheTests
    {
        #region MS Memory Cache

        [Fact]
        public void MsMemory_Extensions_Simple()
        {
            var expectedCacheOptions = new MemoryCacheOptions();
            var cfg = new CacheConfigurationBuilder().WithMicrosoftMemoryCacheHandle().Build();
            var cache = new BaseCacheManager<string>(cfg);

            // disabling cfg check as they seem to alter the configuration internally after adding it... internal ms bs implementation
            //cfg.CacheHandleConfigurations.First()
            //    .ConfigurationTypes.First().Should().BeEquivalentTo(expectedCacheOptions);
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
            var cfg = new CacheConfigurationBuilder().WithMicrosoftMemoryCacheHandle(name).Build();
            var cache = new BaseCacheManager<string>(cfg);

            // disabling cfg check as they seem to alter the configuration internally after adding it... internal ms bs implementation
            //cfg.CacheHandleConfigurations.First()
            //    .ConfigurationTypes.First().Should().BeEquivalentTo(expectedCacheOptions);
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
            var cfg = new CacheConfigurationBuilder().WithMicrosoftMemoryCacheHandle(name, true).Build();
            var cache = new BaseCacheManager<string>(cfg);

            // disabling cfg check as they seem to alter the configuration internally after adding it... internal ms bs implementation
            //cfg.CacheHandleConfigurations.First()
            //    .ConfigurationTypes.First().Should().BeEquivalentTo(expectedCacheOptions);
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
                // CompactOnMemoryPressure = true,
                ExpirationScanFrequency = TimeSpan.FromSeconds(20)
            };

            var cfg = new CacheConfigurationBuilder().WithMicrosoftMemoryCacheHandle(expectedCacheOptions).Build();
            var cache = new BaseCacheManager<string>(cfg);

            cfg.CacheHandleConfigurations.First()
                .ConfigurationTypes.First().Should().BeEquivalentTo(expectedCacheOptions);
            cfg.CacheHandleConfigurations.Count.Should().Be(1);
            cfg.CacheHandleConfigurations.First().Name.Should().NotBeNullOrWhiteSpace();
            cfg.CacheHandleConfigurations.First().IsBackplaneSource.Should().BeFalse();

            cache.CacheHandles.Count().Should().Be(1);
            cache.CacheHandles.OfType<MemoryCacheHandle<string>>().First().MemoryCacheOptions.Should().BeEquivalentTo(expectedCacheOptions);
        }

        [Fact]
        public void MsMemory_Extensions_SimpleWithCfgNamed()
        {
            string name = "some instance name";
            var expectedCacheOptions = new MemoryCacheOptions()
            {
                Clock = new Microsoft.Extensions.Internal.SystemClock(),
                // CompactOnMemoryPressure = true,
                ExpirationScanFrequency = TimeSpan.FromSeconds(20)
            };

            var cfg = new CacheConfigurationBuilder().WithMicrosoftMemoryCacheHandle(name, expectedCacheOptions).Build();
            var cache = new BaseCacheManager<string>(cfg);

            cfg.CacheHandleConfigurations.First()
                .ConfigurationTypes.First().Should().BeEquivalentTo(expectedCacheOptions);
            cfg.CacheHandleConfigurations.Count.Should().Be(1);
            cfg.CacheHandleConfigurations.First().Name.Should().Be(name);
            cfg.CacheHandleConfigurations.First().IsBackplaneSource.Should().BeFalse();

            cache.CacheHandles.Count().Should().Be(1);
            cache.CacheHandles.OfType<MemoryCacheHandle<string>>().First().MemoryCacheOptions.Should().BeEquivalentTo(expectedCacheOptions);
        }

        [Fact]
        public void MsMemory_Extensions_SimpleWithCfgNamedB()
        {
            string name = "some instance name";
            var expectedCacheOptions = new MemoryCacheOptions()
            {
                Clock = new Microsoft.Extensions.Internal.SystemClock(),
                // CompactOnMemoryPressure = true,
                ExpirationScanFrequency = TimeSpan.FromSeconds(20)
            };

            var cfg = new CacheConfigurationBuilder().WithMicrosoftMemoryCacheHandle(name, true, expectedCacheOptions).Build();
            var cache = new BaseCacheManager<string>(cfg);

            cfg.CacheHandleConfigurations.First()
                .ConfigurationTypes.First().Should().BeEquivalentTo(expectedCacheOptions);
            cfg.CacheHandleConfigurations.Count.Should().Be(1);
            cfg.CacheHandleConfigurations.First().Name.Should().Be(name);
            cfg.CacheHandleConfigurations.First().IsBackplaneSource.Should().BeTrue();

            cache.CacheHandles.Count().Should().Be(1);
            cache.CacheHandles.OfType<MemoryCacheHandle<string>>().First().MemoryCacheOptions.Should().BeEquivalentTo(expectedCacheOptions);
        }

        #endregion MS Memory Cache

        #region System Runtime Caching

        [Fact]
        public void SysRuntime_Extensions_Simple()
        {
            var cfg = new CacheConfigurationBuilder().WithSystemRuntimeCacheHandle().Build();
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
            var cfg = new CacheConfigurationBuilder().WithSystemRuntimeCacheHandle(name).Build();
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
            var cfg = new CacheConfigurationBuilder().WithSystemRuntimeCacheHandle(name, true).Build();
            var cache = new BaseCacheManager<string>(cfg);

            cfg.CacheHandleConfigurations.Count.Should().Be(1);
            cfg.CacheHandleConfigurations.First().Name.Should().Be(name);
            cfg.CacheHandleConfigurations.First().IsBackplaneSource.Should().BeTrue();

            cache.CacheHandles.Count().Should().Be(1);
        }

        [Fact]
        public void SysRuntime_Extensions_NamedWithCodeCfg()
        {
            var expectedCacheOptions = new CacheManager.SystemRuntimeCaching.RuntimeMemoryCacheOptions()
            {
                CacheMemoryLimitMegabytes = 13,
                PhysicalMemoryLimitPercentage = 24,
                PollingInterval = TimeSpan.FromMinutes(3)
            };

            using (var act = CacheFactory.Build(_ => _.WithSystemRuntimeCacheHandle("NamedTestWithCfg", expectedCacheOptions)))
            {
                // arrange
                var settings = ((CacheManager.SystemRuntimeCaching.MemoryCacheHandle<object>)act.CacheHandles.ElementAt(0)).CacheSettings;

                // act assert
                settings["CacheMemoryLimitMegabytes"].Should().Be(expectedCacheOptions.CacheMemoryLimitMegabytes.ToString(CultureInfo.InvariantCulture));
                settings["PhysicalMemoryLimitPercentage"].Should().Be(expectedCacheOptions.PhysicalMemoryLimitPercentage.ToString(CultureInfo.InvariantCulture));
                settings["PollingInterval"].Should().Be(expectedCacheOptions.PollingInterval.ToString("c"));
            }
        }

        [Fact]
        public void SysRuntime_Extensions_DefaultWithCodeCfg()
        {
            var expectedCacheOptions = new CacheManager.SystemRuntimeCaching.RuntimeMemoryCacheOptions()
            {
                CacheMemoryLimitMegabytes = 13,
                PhysicalMemoryLimitPercentage = 24,
                PollingInterval = TimeSpan.FromMinutes(3)
            };

            Action act = () => CacheFactory.Build(_ => _.WithSystemRuntimeCacheHandle("default", expectedCacheOptions));

            act.Should().Throw<InvalidOperationException>().WithMessage("*Default*app/web.config*");
        }

        // disabling for netstandard 2 as it doesn't seem to read the "default" configuration from app.config. Might be an xunit/runner issue as the configuration stuff has been ported
        // TODO: re-test
        [Fact]
        [Trait("category", "NotOnMono")]
        public void SysRuntime_CreateDefaultCache()
        {
            using (var act = CacheFactory.Build(_ => _.WithSystemRuntimeCacheHandle()))
            {
                // arrange
                var settings = ((CacheManager.SystemRuntimeCaching.MemoryCacheHandle<object>)act.CacheHandles.ElementAt(0)).CacheSettings;

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
                var settings = ((CacheManager.SystemRuntimeCaching.MemoryCacheHandle<object>)act.CacheHandles.ElementAt(0)).CacheSettings;

                // act assert
                settings["CacheMemoryLimitMegabytes"].Should().Be("12");
                settings["PhysicalMemoryLimitPercentage"].Should().Be("23");
                settings["PollingInterval"].Should().Be("00:02:00");
            }
        }

        [Fact]
        [Trait("category", "NotOnMono")]
        public void SysRuntime_CreateNamedCacheOverrideWithCodeCfg()
        {
            var expectedCacheOptions = new CacheManager.SystemRuntimeCaching.RuntimeMemoryCacheOptions()
            {
                CacheMemoryLimitMegabytes = 11,
                PhysicalMemoryLimitPercentage = 22,
                PollingInterval = TimeSpan.FromMinutes(4)
            };

            using (var act = CacheFactory.Build(_ => _.WithSystemRuntimeCacheHandle("NamedTest", expectedCacheOptions)))
            {
                // arrange
                var settings = ((CacheManager.SystemRuntimeCaching.MemoryCacheHandle<object>)act.CacheHandles.ElementAt(0)).CacheSettings;

                // act assert
                settings["CacheMemoryLimitMegabytes"].Should().Be(expectedCacheOptions.CacheMemoryLimitMegabytes.ToString(CultureInfo.InvariantCulture));
                settings["PhysicalMemoryLimitPercentage"].Should().Be(expectedCacheOptions.PhysicalMemoryLimitPercentage.ToString(CultureInfo.InvariantCulture));
                settings["PollingInterval"].Should().Be(expectedCacheOptions.PollingInterval.ToString("c"));
            }
        }

        #endregion System Runtime Caching

        [Fact]
        public void Dictionary_ExpiredRacecondition()
        {
            var _cache = CacheFactory.Build(s => s
                 .WithJsonSerializer()
                 .WithDictionaryHandle()
                 .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMilliseconds(10))
                 .And
                 .WithDictionaryHandle()
                 .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMilliseconds(20))
                 .And
                 .WithDictionaryHandle()
                 .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMilliseconds(30))
                 .And
                 .WithDictionaryHandle()
                 .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMilliseconds(40))
                 .And
                 .WithDictionaryHandle()
                 .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMilliseconds(50))
                 );

            _cache.OnRemoveByHandle += (s, a) =>
            {
                Console.Write(a.Level);
            };

            var exceptions = 0;
            for (var i = 0; i < 100; i++)
            {
                Action act = () =>
                {
                    try
                    {
                        var val = _cache.Get("some_key");
                        if (val == null)
                        {
                            _cache.Put("some_key", "value");
                        }
                    }
                    catch (NullReferenceException ex)
                    {
                        exceptions++;
                        Console.WriteLine(ex);
                    }
                };

                Parallel.Invoke(Enumerable.Repeat(act, 1000).ToArray());
            }

            Assert.True(exceptions == 0);
        }
    }
}
