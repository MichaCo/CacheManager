using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CacheManager.Core;
using CacheManager.Core.Cache;
using CacheManager.Core.Configuration;
using CacheManager.StackExchange.Redis;
using CacheManager.SystemRuntimeCaching;
using CacheManager.Tests.TestCommon;
using FluentAssertions;
using Xunit;

namespace CacheManager.Tests.Core
{
    /// <summary>
    ///
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class CacheFactoryTests
    {

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_FromConfig_A()
        {
            // arrange

            // act
            Action act = () => CacheFactory.FromConfiguration((CacheManagerConfiguration<string>)null);

            // assert
            act.ShouldThrow<ArgumentNullException>()
                .WithMessage("*Parameter name: configuration*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_FromConfig_B()
        {
            // arrange

            // act
            Action act = () => CacheFactory.FromConfiguration<object>((string)null);

            // assert
            act.ShouldThrow<ArgumentNullException>()
                .WithMessage("*Parameter name: configName*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_ParamA()
        {
            // arrange

            // act
            Action act = () => CacheFactory.Build(null, settings => { });

            // assert
            act.ShouldThrow<ArgumentNullException>()
                .WithMessage("*Parameter name: cacheName*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithHandle_WithoutName()
        {
            // arrange

            // act
            Action act = () => CacheFactory.Build("cacheName", settings =>
            {
                settings.WithHandle<DictionaryCacheHandle<object>>(null);
            });

            // assert
            act.ShouldThrow<ArgumentNullException>()
                .WithMessage("*Parameter name: handleName*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_ParamB()
        {
            // arrange

            // act
            Action act = () => CacheFactory.Build("myCache", null);

            // assert
            act.ShouldThrow<ArgumentNullException>()
                .WithMessage("*Parameter name: settings*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_InvalidHandle_Interface()
        {
            // act
            Action act = () => CacheFactory.Build<string>("stringCache", settings =>
            {
                settings.WithUpdateMode(CacheUpdateMode.Full)
                    .WithHandle<ICacheHandle<string>>("h1");
            });

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("Interfaces are not allowed*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_DisablePerfCounters()
        {
            // act
            Func<ICacheManager<string>> act = () => CacheFactory.Build<string>("stringCache", settings =>
            {
                settings.WithUpdateMode(CacheUpdateMode.Full)
                    .WithHandle<DictionaryCacheHandle<string>>("h1")
                    .DisablePerformanceCounters();
            });

            // assert
            act().CacheHandles.ElementAt(0).Configuration.EnablePerformanceCounters.Should().BeFalse();
            act().CacheHandles.ElementAt(0).Configuration.EnableStatistics.Should().BeFalse("this is the default value");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_EnablePerfCounters()
        {
            // act
            Func<ICacheManager<string>> act = () => CacheFactory.Build<string>("stringCache", settings =>
            {
                settings.WithUpdateMode(CacheUpdateMode.Full)
                    .WithHandle<DictionaryCacheHandle<string>>("h1")
                    .DisableStatistics()            // disable it first
                    .EnablePerformanceCounters();   // should enable stats
            });

            // assert
            act().CacheHandles.ElementAt(0).Configuration.EnablePerformanceCounters.Should().BeTrue();
            act().CacheHandles.ElementAt(0).Configuration.EnableStatistics.Should().BeTrue("is required for perf counters");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_EnableStats()
        {
            // act
            Func<ICacheManager<string>> act = () => CacheFactory.Build<string>("stringCache", settings =>
            {
                settings.WithUpdateMode(CacheUpdateMode.Full)
                    .WithHandle<DictionaryCacheHandle<string>>("h1")
                    .EnableStatistics();
            });

            // assert
            act().CacheHandles.ElementAt(0).Configuration.EnablePerformanceCounters.Should().BeFalse("is default");
            act().CacheHandles.ElementAt(0).Configuration.EnableStatistics.Should().BeTrue();
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_DefaultStatsAndPerf()
        {
            // act
            Func<ICacheManager<string>> act = () => CacheFactory.Build<string>("stringCache", settings =>
            {
                settings.WithUpdateMode(CacheUpdateMode.Full)
                    .WithHandle<DictionaryCacheHandle<string>>("h1");
            });

            // assert
            act().CacheHandles.ElementAt(0).Configuration.EnablePerformanceCounters.Should().BeFalse("is default");
            act().CacheHandles.ElementAt(0).Configuration.EnableStatistics.Should().BeFalse("is default");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithInvalidExpiration()
        {
            // act
            Action act = () => CacheFactory.Build<string>("stringCache", settings =>
            {
                settings.WithUpdateMode(CacheUpdateMode.Full)
                    .WithHandle<DictionaryCacheHandle<string>>("h1")
                        .WithExpiration(ExpirationMode.Absolute, TimeSpan.Zero);
            });

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("If expiration mode*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithInvalidMaxRetries()
        {
            // act
            Action act = () => CacheFactory.Build<string>("stringCache", settings =>
            {
                settings.WithMaxRetries(0);
            });

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("Maximum number of retries must be greater*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithInvalidRetryTimeout()
        {
            // act
            Action act = () => CacheFactory.Build<string>("stringCache", settings =>
            {
                settings.WithRetryTimeout(-1);
            });

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("Retry timeout must be greater*");
        }
        
        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithRedisBackPlateNoBackplateSource()
        {
            // arrange
            // act
            Action act = () => CacheFactory.Build<object>("cacheName", settings =>
            {
                settings.WithBackPlate<RedisCacheBackPlate>("redis");
            });

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("*At least one cache handle must be*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithRedisBackPlateTooManyBackplateSources()
        {
            // arrange
            // act
            Action act = () => CacheFactory.Build<object>("cacheName", settings =>
            {
                settings.WithBackPlate<RedisCacheBackPlate>("redis");
                settings.WithHandle<MemoryCacheHandle>("h1", true);
                settings.WithHandle<MemoryCacheHandle>("h2", true);
            });

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("*Only one cache handle can be *");
        }
        
        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithRedisBackPlateNoRedisConfig()
        {
            // arrange
            // act
            Action act = () => CacheFactory.Build<object>("cacheName", settings =>
            {
                settings.WithBackPlate<RedisCacheBackPlate>("redis");
                settings.WithHandle<MemoryCacheHandle>("h1", true);
            });

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("*No redis configuration found *");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithRedisBackPlateNoName()
        {
            // arrange
            // act
            Action act = () => CacheFactory.Build<object>("cacheName", settings =>
            {
                settings.WithBackPlate<RedisCacheBackPlate>("");                
            });

            // assert
            act.ShouldThrow<ArgumentNullException>()
                .WithMessage("*Parameter name: name*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_ValidateSettings()
        {
            // act
            var act = CacheFactory.Build<string>("stringCache", settings =>
            {
                settings
                    .WithRedisConfiguration(new RedisConfiguration("myRedis", new List<ServerEndPoint>() { new ServerEndPoint("host", 101) }))
                    .WithMaxRetries(22)
                    .WithRetryTimeout(2223)
                    .WithUpdateMode(CacheUpdateMode.Full)
                    .WithHandle<DictionaryCacheHandle<string>>("h1")
                        .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromHours(12))
                        .EnablePerformanceCounters()
                    .And.WithHandle<DictionaryCacheHandle<string>>("h2")
                        .WithExpiration(ExpirationMode.None, TimeSpan.Zero)
                        .DisableStatistics()
                    .And.WithHandle<DictionaryCacheHandle<string>>("h3")
                        .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(231))
                        .EnableStatistics();
            });

            // assert
            act.Configuration.CacheUpdateMode.Should().Be(CacheUpdateMode.Full);
            act.Configuration.RedisConfigurations.Count.Should().Be(1);
            act.Configuration.MaxRetries.Should().Be(22);
            act.Configuration.RetryTimeout.Should().Be(2223);
            act.CacheHandles.ElementAt(0).Configuration.CacheName.Should().Be("stringCache");
            act.CacheHandles.ElementAt(0).Configuration.HandleName.Should().Be("h1");
            act.CacheHandles.ElementAt(0).Configuration.EnablePerformanceCounters.Should().BeTrue();
            act.CacheHandles.ElementAt(0).Configuration.EnableStatistics.Should().BeTrue();
            act.CacheHandles.ElementAt(0).Configuration.ExpirationMode.Should().Be(ExpirationMode.Absolute);
            act.CacheHandles.ElementAt(0).Configuration.ExpirationTimeout.Should().Be(new TimeSpan(12, 0, 0));

            act.CacheHandles.ElementAt(1).Configuration.CacheName.Should().Be("stringCache");
            act.CacheHandles.ElementAt(1).Configuration.HandleName.Should().Be("h2");
            act.CacheHandles.ElementAt(1).Configuration.EnablePerformanceCounters.Should().BeFalse();
            act.CacheHandles.ElementAt(1).Configuration.EnableStatistics.Should().BeFalse();
            act.CacheHandles.ElementAt(1).Configuration.ExpirationMode.Should().Be(ExpirationMode.None);
            act.CacheHandles.ElementAt(1).Configuration.ExpirationTimeout.Should().Be(new TimeSpan(0, 0, 0));

            act.CacheHandles.ElementAt(2).Configuration.CacheName.Should().Be("stringCache");
            act.CacheHandles.ElementAt(2).Configuration.HandleName.Should().Be("h3");
            act.CacheHandles.ElementAt(2).Configuration.EnablePerformanceCounters.Should().BeFalse();
            act.CacheHandles.ElementAt(2).Configuration.EnableStatistics.Should().BeTrue();
            act.CacheHandles.ElementAt(2).Configuration.ExpirationMode.Should().Be(ExpirationMode.Sliding);
            act.CacheHandles.ElementAt(2).Configuration.ExpirationTimeout.Should().Be(new TimeSpan(0, 0, 231));
        }
    }
}