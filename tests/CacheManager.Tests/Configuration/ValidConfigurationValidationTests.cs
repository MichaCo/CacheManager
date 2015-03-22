using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CacheManager.Core;
using CacheManager.Core.Configuration;
using CacheManager.SystemRuntimeCaching;
using FluentAssertions;
using Xunit;

namespace CacheManager.Tests.Configuration
{
    [ExcludeFromCodeCoverage]
    public class ValidConfigurationValidationTests
    {
        [Fact]
        public void Cfg_Valid_AppConfig_ByNameByLoader()
        {
            // arrange
            string cacheName = "C1";

            // act
            var cfg = ConfigurationBuilder.LoadConfiguration<object>(cacheName);
            var cache = CacheFactory.FromConfiguration(cfg);

            // assert
            cache.Configuration.CacheUpdateMode.Should().Be(CacheUpdateMode.Up);
            cache.CacheHandles.Count.Should().Be(3);
            AssertCacheHandleConfig(cache.CacheHandles[0], "h1", ExpirationMode.None, new TimeSpan(0, 0, 50));
            AssertCacheHandleConfig(cache.CacheHandles[1], "h2", ExpirationMode.Absolute, new TimeSpan(0, 20, 0));
            AssertCacheHandleConfig(cache.CacheHandles[2], "h3", ExpirationMode.Sliding, new TimeSpan(20, 0, 0));
        }

        [Fact]
        public void Cfg_Valid_AppConfig_ByName()
        {
            // arrange
            string cacheName = "C1";

            // act
            var cache = CacheFactory.FromConfiguration<object>(cacheName);

            // assert
            cache.Configuration.CacheUpdateMode.Should().Be(CacheUpdateMode.Up);
            cache.CacheHandles.Count.Should().Be(3);
            AssertCacheHandleConfig(cache.CacheHandles[0], "h1", ExpirationMode.None, new TimeSpan(0, 0, 50));
            AssertCacheHandleConfig(cache.CacheHandles[1], "h2", ExpirationMode.Absolute, new TimeSpan(0, 20, 0));
            AssertCacheHandleConfig(cache.CacheHandles[2], "h3", ExpirationMode.Sliding, new TimeSpan(20, 0, 0));
        }

        [Fact]
        public void Cfg_Valid_CfgFile_ExpirationVariances()
        {
            // arrange
            string fileName = GetCfgFileName(@"\Configuration\configuration.valid.allFeatures.config");
            string cacheName = "ExpirationVariances";

            // act
            var cfg = ConfigurationBuilder.LoadConfigurationFile<object>(fileName, cacheName);
            var cache = CacheFactory.FromConfiguration(cfg);

            // assert
            cache.Configuration.CacheUpdateMode.Should().Be(CacheUpdateMode.Full);
            cache.CacheHandles.Count.Should().Be(4);
            AssertCacheHandleConfig(cache.CacheHandles[0], "h1", ExpirationMode.None, new TimeSpan(0, 0, 50));
            AssertCacheHandleConfig(cache.CacheHandles[1], "h2", ExpirationMode.Sliding, new TimeSpan(0, 5, 0));
            AssertCacheHandleConfig(cache.CacheHandles[2], "h3", ExpirationMode.None, new TimeSpan(0, 0, 0));
            AssertCacheHandleConfig(cache.CacheHandles[3], "h4", ExpirationMode.Absolute, new TimeSpan(0, 20, 0));
        }

        [Fact]
        public void Cfg_Valid_CfgFile_DefaultSysMemCache()
        {
            // arrange
            string fileName = GetCfgFileName(@"\Configuration\configuration.valid.allFeatures.config");
            string cacheName = "DefaultSysMemCache";

            // act
            var cfg = ConfigurationBuilder.LoadConfigurationFile<object>(fileName, cacheName);
            var cache = CacheFactory.FromConfiguration(cfg);

            var memHandle = cache.CacheHandles[0] as MemoryCacheHandle<object>;

            memHandle.CacheSettings.Get(0).Should().Be("42");
            memHandle.CacheSettings.Get(1).Should().Be("69");
            memHandle.CacheSettings.Get(2).Should().Be("00:10:00");

            // assert
            cache.Configuration.CacheUpdateMode.Should().Be(CacheUpdateMode.None);
            cache.CacheHandles.Count.Should().Be(1);
            AssertCacheHandleConfig(cache.CacheHandles[0], "default", ExpirationMode.Sliding, new TimeSpan(0, 5, 0));
        }

        [Fact]
        public void Cfg_Valid_CfgFile_EnabledStatsAndPerformanceCounters()
        {
            // arrange
            string fileName = GetCfgFileName(@"\Configuration\configuration.valid.allFeatures.config");
            string cacheName = "ExpirationVariances";

            // act
            var cfg = ConfigurationBuilder.LoadConfigurationFile<object>(fileName, cacheName);
            var cache = CacheFactory.FromConfiguration(cfg);

            // assert
            cache.CacheHandles.Select(p => p.Configuration.EnableStatistics)
                .ShouldAllBeEquivalentTo(Enumerable.Repeat(true, cache.CacheHandles.Count));
            cache.CacheHandles.Select(p => p.Configuration.EnablePerformanceCounters)
                .ShouldAllBeEquivalentTo(Enumerable.Repeat(true, cache.CacheHandles.Count));
        }

        /// <summary>
        /// Expecting not defined enableStats and enablePerformanceCounters in config.
        /// And validating the default fall back to stats = true and performanceCounters = false.
        /// </summary>
        [Fact]
        public void Cfg_Valid_CfgFile_EnabledStatsPerformanceCountersDefaults()
        {
            // arrange
            string fileName = GetCfgFileName(@"\Configuration\configuration.valid.allFeatures.config");
            string cacheName = "DefaultSysMemCache";

            // act
            var cfg = ConfigurationBuilder.LoadConfigurationFile<object>(fileName, cacheName);
            var cache = CacheFactory.FromConfiguration(cfg);

            // assert
            cache.CacheHandles.Select(p => p.Configuration.EnableStatistics)
                .ShouldAllBeEquivalentTo(Enumerable.Repeat(true, cache.CacheHandles.Count));
            cache.CacheHandles.Select(p => p.Configuration.EnablePerformanceCounters)
                .ShouldAllBeEquivalentTo(Enumerable.Repeat(false, cache.CacheHandles.Count));
        }

        [Fact]
        public void Cfg_Valid_CfgFile_DisableStatsAndPerformanceCounters()
        {
            // arrange
            string fileName = GetCfgFileName(@"\Configuration\configuration.valid.allFeatures.config");
            string cacheName = "c3";

            // act
            var cfg = ConfigurationBuilder.LoadConfigurationFile<object>(fileName, cacheName);
            var cache = CacheFactory.FromConfiguration(cfg);

            // assert
            cache.CacheHandles.Select(p => p.Configuration.EnableStatistics)
                .ShouldAllBeEquivalentTo(Enumerable.Repeat(false, cache.CacheHandles.Count));
            cache.CacheHandles.Select(p => p.Configuration.EnablePerformanceCounters)
                .ShouldAllBeEquivalentTo(Enumerable.Repeat(false, cache.CacheHandles.Count));
        }

        private static string GetCfgFileName(string fileName)
        {
            return AppDomain.CurrentDomain.BaseDirectory + (fileName.StartsWith("\\") ? fileName : "\\" + fileName);
        }

        private static void AssertCacheHandleConfig<T>(ICacheHandle<T> handle, string name, ExpirationMode mode, TimeSpan timeout)
        {
            var cfg = handle.Configuration;
            cfg.HandleName.Should().Be(name);
            cfg.ExpirationMode.Should().Be(mode);
            cfg.ExpirationTimeout.Should().Be(timeout);
        }
    }
}