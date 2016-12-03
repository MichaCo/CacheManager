#if !NETCOREAPP
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.SystemRuntimeCaching;
using FluentAssertions;
using Xunit;

namespace CacheManager.Tests
{
    [ExcludeFromCodeCoverage]
#if NET40
    [Trait("Framework", "NET40")]
#else
    [Trait("Framework", "NET45")]
#endif
    public class ValidConfigurationValidationTests : BaseCacheManagerTest
    {
        [Fact]
        public void Cfg_Valid_AppConfig_ByNameByLoader()
        {
            // arrange
            string fileName = GetCfgFileName(@"/app.config");
            string cacheName = "C1";

            // act
            var cfg = ConfigurationBuilder.LoadConfigurationFile(fileName, cacheName);
            var cache = CacheFactory.FromConfiguration<object>(cfg);

            // assert
            cache.Configuration.UpdateMode.Should().Be(CacheUpdateMode.Up);
            cache.CacheHandles.Count().Should().Be(3);
            AssertCacheHandleConfig(cache.CacheHandles.ElementAt(0), "h1", ExpirationMode.None, new TimeSpan(0, 0, 50));
            AssertCacheHandleConfig(cache.CacheHandles.ElementAt(1), "h2", ExpirationMode.Absolute, new TimeSpan(0, 20, 0));
            AssertCacheHandleConfig(cache.CacheHandles.ElementAt(2), "h3", ExpirationMode.Sliding, new TimeSpan(20, 0, 0));
        }

        [Fact]
        public void Cfg_Valid_AppConfig_ByName()
        {
            // arrange
            string fileName = GetCfgFileName(@"/app.config");
            string cacheName = "C1";

            // act
            var cfg = ConfigurationBuilder.LoadConfigurationFile(fileName, cacheName);
            var cache = CacheFactory.FromConfiguration<object>(cfg);

            // assert
            cache.Configuration.UpdateMode.Should().Be(CacheUpdateMode.Up);
            cache.CacheHandles.Count().Should().Be(3);
            AssertCacheHandleConfig(cache.CacheHandles.ElementAt(0), "h1", ExpirationMode.None, new TimeSpan(0, 0, 50));
            AssertCacheHandleConfig(cache.CacheHandles.ElementAt(1), "h2", ExpirationMode.Absolute, new TimeSpan(0, 20, 0));
            AssertCacheHandleConfig(cache.CacheHandles.ElementAt(2), "h3", ExpirationMode.Sliding, new TimeSpan(20, 0, 0));
        }

        [Fact]
        public void Cfg_Valid_CfgFile_ExpirationVariances()
        {
            // arrange
            string fileName = GetCfgFileName(@"/Configuration/configuration.valid.allFeatures.config");
            string cacheName = "ExpirationVariances";

            // act
            var cfg = ConfigurationBuilder.LoadConfigurationFile(fileName, cacheName);
            var cache = CacheFactory.FromConfiguration<object>(cfg);

            // assert
            cache.Configuration.UpdateMode.Should().Be(CacheUpdateMode.Full);
            cache.CacheHandles.Count().Should().Be(4);
            AssertCacheHandleConfig(cache.CacheHandles.ElementAt(0), "h1", ExpirationMode.None, new TimeSpan(0, 0, 50));
            AssertCacheHandleConfig(cache.CacheHandles.ElementAt(1), "h2", ExpirationMode.Sliding, new TimeSpan(0, 5, 0));
            AssertCacheHandleConfig(cache.CacheHandles.ElementAt(2), "h3", ExpirationMode.None, new TimeSpan(0, 0, 0));
            AssertCacheHandleConfig(cache.CacheHandles.ElementAt(3), "h4", ExpirationMode.Absolute, new TimeSpan(0, 20, 0));
        }

#if !NO_APP_CONFIG
        [Fact]
        [Trait("category", "NotOnMono")]
        public void Cfg_Valid_CfgFile_DefaultSysMemCache()
        {
            // arrange
            string fileName = GetCfgFileName(@"/Configuration/configuration.valid.allFeatures.config");
            string cacheName = "DefaultSysMemCache";

            // act
            var cfg = ConfigurationBuilder.LoadConfigurationFile(fileName, cacheName);
            var cache = CacheFactory.FromConfiguration<object>(cfg);

            var memHandle = cache.CacheHandles.ElementAt(0) as MemoryCacheHandle<object>;

            memHandle.CacheSettings.Get(0).Should().Be("42");
            memHandle.CacheSettings.Get(1).Should().Be("69");
            memHandle.CacheSettings.Get(2).Should().Be("00:10:00");

            // assert
            cache.Configuration.UpdateMode.Should().Be(CacheUpdateMode.None);
            cache.CacheHandles.Count().Should().Be(1);
            AssertCacheHandleConfig(cache.CacheHandles.ElementAt(0), "default", ExpirationMode.Sliding, new TimeSpan(0, 5, 0));
        }
#endif

        [Fact]
        public void Cfg_Valid_CfgFile_EnabledStatsAndPerformanceCounters()
        {
            // arrange
            string fileName = GetCfgFileName(@"/Configuration/configuration.valid.allFeatures.config");
            string cacheName = "ExpirationVariances";

            // act
            var cfg = ConfigurationBuilder.LoadConfigurationFile(fileName, cacheName);
            var cache = CacheFactory.FromConfiguration<object>(cfg);

            // assert
            cache.CacheHandles.Select(p => p.Configuration.EnableStatistics)
                .ShouldAllBeEquivalentTo(Enumerable.Repeat(true, cache.CacheHandles.Count()));
            cache.CacheHandles.Select(p => p.Configuration.EnablePerformanceCounters)
                .ShouldAllBeEquivalentTo(Enumerable.Repeat(true, cache.CacheHandles.Count()));
        }

        /// <summary>
        /// Expecting not defined enableStats and enablePerformanceCounters in config. And
        /// validating the default fall back to stats = true and performanceCounters = false.
        /// </summary>
        [Fact]
        public void Cfg_Valid_CfgFile_EnabledStatsPerformanceCountersDefaults()
        {
            // arrange
            string fileName = GetCfgFileName(@"/Configuration/configuration.valid.allFeatures.config");
            string cacheName = "DefaultSysMemCache";

            // act
            var cfg = ConfigurationBuilder.LoadConfigurationFile(fileName, cacheName);
            var cache = CacheFactory.FromConfiguration<object>(cfg);

            // assert
            cache.CacheHandles.Select(p => p.Configuration.EnableStatistics)
                .ShouldAllBeEquivalentTo(Enumerable.Repeat(true, cache.CacheHandles.Count()));
            cache.CacheHandles.Select(p => p.Configuration.EnablePerformanceCounters)
                .ShouldAllBeEquivalentTo(Enumerable.Repeat(false, cache.CacheHandles.Count()));
        }

        [Fact]
        public void Cfg_Valid_CfgFile_DisableStatsAndPerformanceCounters()
        {
            // arrange
            string fileName = GetCfgFileName(@"/Configuration/configuration.valid.allFeatures.config");
            string cacheName = "c3";

            // act
            var cfg = ConfigurationBuilder.LoadConfigurationFile(fileName, cacheName);
            var cache = CacheFactory.FromConfiguration<object>(cfg);

            // assert
            cache.CacheHandles.Select(p => p.Configuration.EnableStatistics)
                .ShouldAllBeEquivalentTo(Enumerable.Repeat(false, cache.CacheHandles.Count()));
            cache.CacheHandles.Select(p => p.Configuration.EnablePerformanceCounters)
                .ShouldAllBeEquivalentTo(Enumerable.Repeat(false, cache.CacheHandles.Count()));
        }

        [Fact]
        [Trait("category", "NotOnMono")]
        public void Cfg_Valid_CfgFile_AllDefaults()
        {
            // arrange
            string fileName = GetCfgFileName(@"/Configuration/configuration.valid.allFeatures.config");
            string cacheName = "onlyDefaultsCache";

            // act
            var cfg = ConfigurationBuilder.LoadConfigurationFile(fileName, cacheName);
            var cache = CacheFactory.FromConfiguration<string>(cfg);

            // assert
            cache.Configuration.UpdateMode.Should().Be(CacheUpdateMode.Up);
            cache.Configuration.SerializerType.Should().BeNull();
            cache.Configuration.LoggerFactoryType.Should().BeNull();
            cache.Configuration.BackplaneType.Should().BeNull();
            cache.Configuration.RetryTimeout.Should().Be(100);
            cache.Configuration.MaxRetries.Should().Be(50);
            cache.CacheHandles.Count().Should().Be(1);
            AssertCacheHandleConfig(cache.CacheHandles.ElementAt(0), "defaultsHandle", ExpirationMode.None, TimeSpan.Zero);
        }

        private static void AssertCacheHandleConfig<T>(BaseCacheHandle<T> handle, string name, ExpirationMode mode, TimeSpan timeout)
        {
            var cfg = handle.Configuration;
            cfg.Name.Should().Be(name);
            cfg.ExpirationMode.Should().Be(mode);
            cfg.ExpirationTimeout.Should().Be(timeout);
        }
    }
}
#endif