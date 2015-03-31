using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CacheManager.Core;
using CacheManager.Core.Cache;
using CacheManager.Core.Configuration;
using CacheManager.Redis;
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
                settings.WithRedisBackPlate("redis");
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
                settings.WithRedisBackPlate("redis");
                settings.WithSystemRuntimeCacheHandle("h1", true);
                settings.WithSystemRuntimeCacheHandle("h2", true);
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
                settings.WithRedisBackPlate("redis");
                settings.WithSystemRuntimeCacheHandle("h1", true);
            });

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("*No configuration added for id redis*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithRedisBackPlateNoName()
        {
            // arrange
            // act
            Action act = () => CacheFactory.Build<object>("cacheName", settings =>
            {
                settings.WithRedisBackPlate("");                
            });

            // assert
            act.ShouldThrow<ArgumentNullException>()
                .WithMessage("*Parameter name: name*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithRedisConfigurationNoKeyA()
        {
            // arrange
            // act
            Action act = () => CacheFactory.Build<object>("cacheName", settings =>
            {
                settings.WithRedisConfiguration("", "");
            });

            // assert
            act.ShouldThrow<ArgumentNullException>()
                .WithMessage("*Parameter name: configurationKey*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithRedisConfigurationNoKeyB()
        {
            // arrange
            // act
            Action act = () => CacheFactory.Build<object>("cacheName", settings =>
            {
                settings.WithRedisConfiguration("", config => { });
            });

            // assert
            act.ShouldThrow<ArgumentNullException>()
                .WithMessage("*Parameter name: configurationKey*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithRedisConfigurationInvalidEndpoint()
        {
            // arrange
            // act
            Action act = () => CacheFactory.Build<object>("cacheName", settings =>
            {
                settings.WithRedisConfiguration("redis", config => config.WithEndpoint("", 0));
            });

            // assert
            act.ShouldThrow<ArgumentNullException>()
                .WithMessage("*Parameter name: host*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithRedisConfigurationConnectionString()
        {
            // arrange
            var connection = "localhost:8080,allowAdmin=true,name=myName,ssl=true";
            // act
            CacheFactory.Build<object>("cacheName", settings =>
            {
                settings.WithRedisConfiguration("redisWithConnectionString", connection);
            });

            var config = RedisConfigurations.GetConfiguration("redisWithConnectionString");
            
            // assert
            config.ConnectionString.Should().Be(connection);
            config.Key.Should().Be("redisWithConnectionString");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithRedisConfigurationValidateBuilder()
        {
            // arrange
            // act
            CacheFactory.Build<object>("cacheName", settings =>
            {
                settings.WithRedisConfiguration("redisBuildUpConfiguration", config =>
                {
                    config.WithAllowAdmin()
                        .WithConnectionTimeout(221113)
                        .WithDatabase(22)
                        .WithEndpoint("localhost", 2323)
                        .WithEndpoint("nohost", 99999)
                        .WithPassword("secret")
                        .WithSsl("mySslHost");
                });
            });

            var configuration = RedisConfigurations.GetConfiguration("redisBuildUpConfiguration");

            // assert
            configuration.Key.Should().Be("redisBuildUpConfiguration");
            configuration.ConnectionTimeout.Should().Be(221113);
            configuration.Database.Should().Be(22);
            configuration.Password.Should().Be("secret");
            configuration.IsSsl.Should().BeTrue();
            configuration.SslHost.Should().Be("mySslHost");
            configuration.Endpoints.ShouldBeEquivalentTo(new[] { new ServerEndPoint("localhost", 2323), new ServerEndPoint("nohost", 99999) });
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_ValidateSettings()
        {
            // act
            var act = CacheFactory.Build<string>("stringCache", settings =>
            {
                settings
                    .WithRedisConfiguration("myRedis", config =>
                    {
                        config.WithAllowAdmin()
                            .WithDatabase(0)
                            .WithEndpoint("localhost", 6379);
                    })
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
            RedisConfigurations.GetConfiguration("myRedis").Should().NotBeNull();
            act.Configuration.CacheUpdateMode.Should().Be(CacheUpdateMode.Full);
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