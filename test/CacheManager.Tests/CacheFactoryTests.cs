using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.Redis;
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
    public class CacheFactoryTests
    {
        [Fact]
        [ReplaceCulture]
        public void CacheFactory_FromConfig_NullCheck_A()
        {
            // arrange

            // act
            Action act = () => CacheFactory.FromConfiguration<object>("name", (CacheManagerConfiguration)null);

            // assert
            act.ShouldThrow<ArgumentNullException>()
                .WithMessage("*Parameter name: configuration*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_FromConfig_NullCheck_B()
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
        public void CacheFactory_FromConfig_NullCheck_C()
        {
            // arrange

            // act
            Action act = () => CacheFactory.FromConfiguration<object>(null, new CacheManagerConfiguration());

            // assert
            act.ShouldThrow<ArgumentNullException>()
                .WithMessage("*name*");
        }

        [Fact]
        [ReplaceCulture]
        [Trait("category", "Mono")]
        public void CacheFactory_FromConfig_NonGeneric_NullCheck_A()
        {
            // arrange

            // act
            Action act = () => CacheFactory.FromConfiguration((Type)null, "c1");

            // assert
            act.ShouldThrow<ArgumentNullException>()
                .WithMessage("*cacheValueType*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_FromConfig_NonGeneric_NullCheck_B()
        {
            // arrange

            // act
            Action act = () => CacheFactory.FromConfiguration((Type)null, "something", (CacheManagerConfiguration)null);

            // assert
            act.ShouldThrow<ArgumentNullException>()
                .WithMessage("*cacheValueType*");
        }

        [Fact]
        [ReplaceCulture]
        [Trait("category", "Mono")]
        public void CacheFactory_FromConfig_NonGeneric_NullCheck_C()
        {
            // arrange

            // act
            Action act = () => CacheFactory.FromConfiguration((Type)null, "c1", "cacheManager");

            // assert
            act.ShouldThrow<ArgumentNullException>()
                .WithMessage("*cacheValueType*");
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
                .WithMessage("*Parameter name: name*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithHandle_WithoutName()
        {
            // arrange

            // act
            Action act = () => CacheFactory.Build("cacheName", settings =>
            {
                settings.WithHandle(typeof(DictionaryCacheHandle<>), null);
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
        public void CacheFactory_Build_DisablePerfCounters()
        {
            // act
            Func<ICacheManager<string>> act = () => CacheFactory.Build<string>("stringCache", settings =>
            {
                settings.WithUpdateMode(CacheUpdateMode.Full)
                    .WithHandle(typeof(DictionaryCacheHandle<>), "h1")
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
                    .WithHandle(typeof(DictionaryCacheHandle<>), "h1")
                    .DisableStatistics() // disable it first
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
                    .WithHandle(typeof(DictionaryCacheHandle<>), "h1")
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
                    .WithHandle(typeof(DictionaryCacheHandle<>), "h1");
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
                    .WithHandle(typeof(DictionaryCacheHandle<>), "h1")
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
            // arrange act
            Action act = () =>
            {
                var cache = CacheFactory.Build<object>("cacheName", settings =>
                {
                    settings.WithRedisBackPlate("redis");
                });

                cache.Add("test", "test");
                cache.Remove("test");
            };

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("*At least one cache handle must be*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithRedisBackPlateTooManyBackplateSources()
        {
            // arrange act
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
            // arrange act
            Action act = () => CacheFactory.Build<object>("cacheName", settings =>
            {
                settings.WithRedisBackPlate("redis");
                settings.WithSystemRuntimeCacheHandle("h1", true);
            });

            // assert
            act.ShouldThrow<InvalidOperationException>()
                .WithMessage("*No configuration added for configuration name redis*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithRedisBackPlateNoName()
        {
            // arrange act
            Action act = () => CacheFactory.Build<object>("cacheName", settings =>
            {
                settings.WithRedisBackPlate(string.Empty);
            });

            // assert
            act.ShouldThrow<ArgumentException>()
                .WithMessage("*Parameter name: name*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithRedisConfigurationNoKeyA()
        {
            // arrange act
            Action act = () => CacheFactory.Build<object>("cacheName", settings =>
            {
                settings.WithRedisConfiguration(string.Empty, string.Empty);
            });

            // assert
            act.ShouldThrow<ArgumentException>()
                .WithMessage("*Parameter name: configurationKey*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithRedisConfigurationNoKeyB()
        {
            // arrange act
            Action act = () => CacheFactory.Build<object>("cacheName", settings =>
            {
                settings.WithRedisConfiguration(string.Empty, config => { });
            });

            // assert
            act.ShouldThrow<ArgumentException>()
                .WithMessage("*Parameter name: configurationKey*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithRedisConfigurationInvalidEndpoint()
        {
            // arrange act
            Action act = () => CacheFactory.Build<object>("cacheName", settings =>
            {
                settings.WithRedisConfiguration("redis", config => config.WithEndpoint(string.Empty, 0));
            });

            // assert
            act.ShouldThrow<ArgumentException>()
                .WithMessage("*Parameter name: host*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithRedisConfigurationConnectionString()
        {
            // arrange
            var connection = "127.0.0.1:8080,allowAdmin=true,name=myName,ssl=true";

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
            // arrange act
            CacheFactory.Build<object>("cacheName", settings =>
            {
                settings.WithRedisConfiguration("redisBuildUpConfiguration", config =>
                {
                    config.WithAllowAdmin()
                        .WithConnectionTimeout(221113)
                        .WithDatabase(22)
                        .WithEndpoint("127.0.0.1", 2323)
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
            configuration.Endpoints.ShouldBeEquivalentTo(new[] { new ServerEndPoint("127.0.0.1", 2323), new ServerEndPoint("nohost", 99999) });
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
                            .WithEndpoint("127.0.0.1", 6379);
                    })
                    .WithMaxRetries(22)
                    .WithRetryTimeout(2223)
                    .WithUpdateMode(CacheUpdateMode.Full)
                    .WithHandle(typeof(DictionaryCacheHandle<>), "h1")
                        .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromHours(12))
                        .EnablePerformanceCounters()
                    .And.WithHandle(typeof(DictionaryCacheHandle<>), "h2")
                        .WithExpiration(ExpirationMode.None, TimeSpan.Zero)
                        .DisableStatistics()
                    .And.WithHandle(typeof(DictionaryCacheHandle<>), "h3")
                        .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(231))
                        .EnableStatistics();
            });

            // assert
            RedisConfigurations.GetConfiguration("myRedis").Should().NotBeNull();
            act.Configuration.CacheUpdateMode.Should().Be(CacheUpdateMode.Full);
            act.Configuration.MaxRetries.Should().Be(22);
            act.Configuration.RetryTimeout.Should().Be(2223);
            act.Name.Should().Be("stringCache");
            act.CacheHandles.ElementAt(0).Configuration.HandleName.Should().Be("h1");
            act.CacheHandles.ElementAt(0).Configuration.EnablePerformanceCounters.Should().BeTrue();
            act.CacheHandles.ElementAt(0).Configuration.EnableStatistics.Should().BeTrue();
            act.CacheHandles.ElementAt(0).Configuration.ExpirationMode.Should().Be(ExpirationMode.Absolute);
            act.CacheHandles.ElementAt(0).Configuration.ExpirationTimeout.Should().Be(new TimeSpan(12, 0, 0));

            act.CacheHandles.ElementAt(1).Configuration.HandleName.Should().Be("h2");
            act.CacheHandles.ElementAt(1).Configuration.EnablePerformanceCounters.Should().BeFalse();
            act.CacheHandles.ElementAt(1).Configuration.EnableStatistics.Should().BeFalse();
            act.CacheHandles.ElementAt(1).Configuration.ExpirationMode.Should().Be(ExpirationMode.None);
            act.CacheHandles.ElementAt(1).Configuration.ExpirationTimeout.Should().Be(new TimeSpan(0, 0, 0));

            act.CacheHandles.ElementAt(2).Configuration.HandleName.Should().Be("h3");
            act.CacheHandles.ElementAt(2).Configuration.EnablePerformanceCounters.Should().BeFalse();
            act.CacheHandles.ElementAt(2).Configuration.EnableStatistics.Should().BeTrue();
            act.CacheHandles.ElementAt(2).Configuration.ExpirationMode.Should().Be(ExpirationMode.Sliding);
            act.CacheHandles.ElementAt(2).Configuration.ExpirationTimeout.Should().Be(new TimeSpan(0, 0, 231));
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_NonGenericWithType()
        {
            var cache = CacheFactory.Build(
                typeof(string),
                "myCache",
                settings => settings.WithSystemRuntimeCacheHandle("h1")) as ICacheManager<string>;

            cache.Should().NotBeNull();
            cache.CacheHandles.Count().Should().Be(1);
            cache.Name.Should().Be("myCache");
        }

        [Fact]
        [ReplaceCulture]
        [Trait("category", "Mono")]
        public void CacheFactory_FromConfig_NonGeneric_A()
        {
            var cache = CacheFactory.FromConfiguration(typeof(string), "c1") as ICacheManager<string>;

            cache.Should().NotBeNull();
            cache.CacheHandles.Count().Should().Be(3);
            cache.Name.Should().Be("c1");
        }

        [Fact]
        [ReplaceCulture]
        [Trait("category", "Mono")]
        public void CacheFactory_FromConfig_NonGeneric_B()
        {
            var cache = CacheFactory.FromConfiguration(typeof(string), "c1", "cacheManager") as ICacheManager<string>;

            cache.Should().NotBeNull();
            cache.CacheHandles.Count().Should().Be(3);
            cache.Name.Should().Be("c1");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_FromConfig_NonGeneric_C()
        {
            var cache = CacheFactory.FromConfiguration(
                typeof(string),
                "cacheName",
                ConfigurationBuilder.BuildConfiguration(cfg => cfg.WithSystemRuntimeCacheHandle("h1"))) as ICacheManager<string>;

            cache.Should().NotBeNull();
            cache.CacheHandles.Count().Should().Be(1);
            cache.Name.Should().Be("cacheName");
        }
    }
}