using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.Redis;
using CacheManager.Serialization.Json;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace CacheManager.Tests
{
    [ExcludeFromCodeCoverage]
    public class CacheFactoryTests
    {
        [Fact]
        public void ConfigurationBuilder_EmptyCtor()
        {
            var builder = new ConfigurationBuilder();
            var cfg = builder.Build();

            cfg.Should().NotBeNull();
            cfg.Name.Should().NotBeNull();
        }

        [Fact]
        public void ConfigurationBuilder_NamedCtorNull()
        {
            Action act = () => new ConfigurationBuilder((string)null);

            act.Should().Throw<ArgumentNullException>().WithMessage("*name*");
        }

        [Fact]
        public void ConfigurationBuilder_ForConfigCtorNull()
        {
            Action act = () => new ConfigurationBuilder((ICacheManagerConfiguration)null);

            act.Should().Throw<ArgumentNullException>().WithMessage("*forConfiguration*");
        }

        [Fact]
        public void ConfigurationBuilder_NamedForConfigCtorNull()
        {
            Action act = () => new ConfigurationBuilder(null, null);

            act.Should().Throw<ArgumentNullException>().WithMessage("*name*");
        }

        [Fact]
        public void ConfigurationBuilder_NamedForConfigCtorNullB()
        {
            Action act = () => new ConfigurationBuilder("name", null);

            act.Should().Throw<ArgumentNullException>().WithMessage("*forConfiguration*");
        }

        [Fact]
        public void ConfigurationBuilder_EmptyCtorAdd()
        {
            var builder = new ConfigurationBuilder();
            builder.WithDictionaryHandle();
            var cfg = builder.Build();

            cfg.CacheHandleConfigurations.Count.Should().Be(1);
        }

        [Fact]
        public void ConfigurationBuilder_ForConfiguration()
        {
            var builder = new ConfigurationBuilder("name");
            builder.WithDictionaryHandle().WithExpiration(ExpirationMode.Sliding, TimeSpan.FromMinutes(10));
            builder.WithJsonSerializer();
            var cfg = builder.Build();

            var forCfg = new ConfigurationBuilder("newName", cfg);
            forCfg.WithDictionaryHandle().WithExpiration(ExpirationMode.Absolute, TimeSpan.FromHours(1));
            forCfg.WithGzJsonSerializer();

            cfg.CacheHandleConfigurations.Count.Should().Be(2);
            cfg.Name.Should().Be("newName");
            cfg.CacheHandleConfigurations.First().ExpirationMode.Should().Be(ExpirationMode.Sliding);
            cfg.CacheHandleConfigurations.First().ExpirationTimeout.Should().Be(TimeSpan.FromMinutes(10));
            cfg.CacheHandleConfigurations.Last().ExpirationMode.Should().Be(ExpirationMode.Absolute);
            cfg.CacheHandleConfigurations.Last().ExpirationTimeout.Should().Be(TimeSpan.FromHours(1));
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_FromConfig_NullCheck_A()
        {
            // arrange

            // act
            Action act = () => CacheFactory.FromConfiguration<object>((CacheManagerConfiguration)null);

            // assert
            act.Should().Throw<ArgumentNullException>()
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
            act.Should().Throw<ArgumentNullException>()
                .WithMessage("*Parameter name: configName*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_FromConfig_TestInit_A()
        {
            // arrange
            var config = ConfigurationBuilder.BuildConfiguration(s => s.WithDictionaryHandle());

            // act
            Action act = () => CacheFactory.FromConfiguration<object>(config);

            // assert
            act.Should().NotThrow();
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_FromConfig_TestInit_B()
        {
            // arrange
            var config = ConfigurationBuilder.BuildConfiguration(s => s.WithDictionaryHandle());

            // act
            var cache = CacheFactory.FromConfiguration<object>("custom name", config);

            // assert
            cache.Name.Should().Be("custom name");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_FromConfig_TestInit_C()
        {
            // arrange
            var config = ConfigurationBuilder.BuildConfiguration(s => s.WithDictionaryHandle());

            // act
            var cache = CacheFactory.FromConfiguration(typeof(object), "custom name", config) as ICacheManager<object>;

            // assert
            cache.Name.Should().Be("custom name");
        }

#if !NO_APP_CONFIG

        [Fact]
        [ReplaceCulture]
        [Trait("category", "NotOnMono")]
        public void CacheFactory_FromConfig_NonGeneric_NullCheck_A()
        {
            // arrange

            // act
            Action act = () => CacheFactory.FromConfiguration(null, "c1");

            // assert
            act.Should().Throw<ArgumentNullException>()
                .WithMessage("*cacheValueType*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_FromConfig_NonGeneric_NullCheck_B()
        {
            // arrange

            // act
            Action act = () => CacheFactory.FromConfiguration(null, (CacheManagerConfiguration)null);

            // assert
            act.Should().Throw<ArgumentNullException>()
                .WithMessage("*cacheValueType*");
        }

        [Fact]
        [ReplaceCulture]
        [Trait("category", "NotOnMono")]
        public void CacheFactory_FromConfig_NonGeneric_NullCheck_C()
        {
            // arrange

            // act
            Action act = () => CacheFactory.FromConfiguration((Type)null, "c1", "cacheManager");

            // assert
            act.Should().Throw<ArgumentNullException>()
                .WithMessage("*cacheValueType*");
        }

#endif

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithHandle_WithoutName()
        {
            // arrange

            // act
            Action act = () => CacheFactory.Build(settings =>
            {
                settings.WithDictionaryHandle(null);
            });

            // assert
            act.Should().Throw<ArgumentNullException>()
                .WithMessage("*Parameter name: handleName*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_ParamB()
        {
            // arrange

            // act
            Action act = () => CacheFactory.Build(null);

            // assert
            act.Should().Throw<ArgumentNullException>()
                .WithMessage("*Parameter name: settings*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_DisablePerfCounters()
        {
            // act
            Func<ICacheManager<string>> act = () => CacheFactory.Build<string>(settings =>
            {
                settings.WithUpdateMode(CacheUpdateMode.Up)
                    .WithDictionaryHandle("h1")
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
            Func<ICacheManager<string>> act = () => CacheFactory.Build<string>(settings =>
            {
                settings
                    .WithDictionaryHandle("h1")
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
            Func<ICacheManager<string>> act = () => CacheFactory.Build<string>(settings =>
            {
                settings
                    .WithDictionaryHandle("h1")
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
            Func<ICacheManager<string>> act = () => CacheFactory.Build<string>(settings =>
            {
                settings
                    .WithDictionaryHandle("h1");
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
            Action act = () => CacheFactory.Build<string>(settings =>
            {
                settings
                    .WithDictionaryHandle("h1")
                        .WithExpiration(ExpirationMode.Absolute, TimeSpan.Zero);
            });

            // assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("If expiration mode*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithInvalidMaxRetries()
        {
            // act
            Action act = () => CacheFactory.Build<string>(settings =>
            {
                settings.WithMaxRetries(0);
            });

            // assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Maximum number of retries must be greater*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithInvalidRetryTimeout()
        {
            // act
            Action act = () => CacheFactory.Build<string>(settings =>
            {
                settings.WithRetryTimeout(-1);
            });

            // assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Retry timeout must be greater*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithRedisBackplaneNoBackplaneSource()
        {
            // arrange act
            Action act = () =>
            {
                var cache = CacheFactory.Build<object>(settings =>
               {
                   settings.WithDictionaryHandle();
                   settings.WithRedisBackplane("redis");
               });

                cache.Add("test", "test");
                cache.Remove("test");
            };

            // assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*At least one cache handle must be*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithRedisBackplaneTooManyBackplaneSources()
        {
            // arrange act
            Action act = () => CacheFactory.Build<object>(settings =>
           {
               settings.WithRedisBackplane("redis");
               settings.WithSystemRuntimeCacheHandle("redis", true);
               settings.WithSystemRuntimeCacheHandle("redis", true);
           });

            // assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Only one cache handle can be *");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithRedisBackplaneNoRedisConfig()
        {
            // arrange act
            var redisKey = Guid.NewGuid().ToString();
            Action act = () => CacheFactory.Build<object>(settings =>
           {
               settings.WithRedisBackplane(redisKey);
               settings.WithSystemRuntimeCacheHandle(redisKey, true);
           });

            // assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*No configuration added for configuration name*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithRedisBackplaneNoName()
        {
            // arrange act
            Action act = () => CacheFactory.Build<object>(settings =>
           {
               settings.WithRedisBackplane(string.Empty);
           });

            // assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Parameter name: configurationKey*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithRedisConfigurationNoKeyA()
        {
            // arrange act
            Action act = () => CacheFactory.Build<object>(settings =>
           {
               settings.WithRedisConfiguration(string.Empty, string.Empty);
           });

            // assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Parameter name: configurationKey*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithRedisConfigurationNoKeyB()
        {
            // arrange act
            Action act = () => CacheFactory.Build<object>(settings =>
            {
                settings.WithRedisConfiguration(string.Empty, config => { });
            });

            // assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Parameter name: configurationKey*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithRedisConfigurationInvalidEndpoint()
        {
            // arrange act
            Action act = () => CacheFactory.Build<object>(settings =>
            {
                settings.WithRedisConfiguration("redis", config => config.WithEndpoint(string.Empty, 0));
            });

            // assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Parameter name: host*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithRedisConfigurationConnectionString()
        {
            // arrange
            var name = Guid.NewGuid().ToString();
            var connection = "127.0.0.1:8080,allowAdmin=true,name=myName,defaultDatabase=1";
            var expected = StackExchange.Redis.ConfigurationOptions.Parse(connection);

            // act
            CacheFactory.Build<object>(settings =>
            {
                settings.WithDictionaryHandle();
                settings.WithRedisConfiguration(name, connection);
            });

            var config = RedisConfigurations.GetConfiguration(name);

            // assert
            config.ConfigurationOptions.Should().BeEquivalentTo(expected);
            config.TwemproxyEnabled.Should().BeFalse();
            config.AllowAdmin.Should().BeTrue();
            config.IsSsl.Should().BeFalse();
            config.Key.Should().Be(name);
            config.Database.Should().Be(1);
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithRedisConnectionStringWithProxy()
        {
            // arrange
            var name = Guid.NewGuid().ToString();
            var connection = "127.0.0.1:8080,name=myName,ssl=true,proxy=Twemproxy";
            var expected = StackExchange.Redis.ConfigurationOptions.Parse(connection);

            // act
            CacheFactory.Build<object>(settings =>
            {
                settings.WithDictionaryHandle();
                settings.WithRedisConfiguration(name, connection);
            });

            var config = RedisConfigurations.GetConfiguration(name);

            // assert
            config.ConfigurationOptions.Should().BeEquivalentTo(expected);
            config.TwemproxyEnabled.Should().BeTrue();
            config.AllowAdmin.Should().BeFalse();
            config.IsSsl.Should().BeTrue();
            config.Key.Should().Be(name);
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithRedisConfigurationValidateBuilder()
        {
            // arrange act
            var name = Guid.NewGuid().ToString();
            CacheFactory.Build<object>(settings =>
            {
                settings.WithDictionaryHandle();
                settings.WithRedisConfiguration(name, config =>
                {
                    config
                        .WithAllowAdmin()
                        .UseCompatibilityMode("2.8")
                        .UseTwemproxy()
                        .WithConnectionTimeout(221113)
                        .WithDatabase(22)
                        .WithEndpoint("127.0.0.1", 2323)
                        .WithEndpoint("nohost", 60999)
                        .WithPassword("secret")
                        .WithSsl("mySslHost");
                });
            });

            var configuration = RedisConfigurations.GetConfiguration(name);

            // assert
            configuration.Key.Should().Be(name);
            configuration.ConnectionTimeout.Should().Be(221113);
            configuration.TwemproxyEnabled.Should().BeTrue();
            configuration.StrictCompatibilityModeVersion.Should().Be("2.8");
            configuration.Database.Should().Be(22);
            configuration.Password.Should().Be("secret");
            configuration.IsSsl.Should().BeTrue();
            configuration.SslHost.Should().Be("mySslHost");
            configuration.Endpoints.Should().BeEquivalentTo(new[] { new ServerEndPoint("127.0.0.1", 2323), new ServerEndPoint("nohost", 60999) });
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_ValidateSettings()
        {
            // act
            var act = CacheFactory.Build<string>(settings =>
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
                    .WithUpdateMode(CacheUpdateMode.None)
                    .WithDictionaryHandle("h1")
                        .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromHours(12))
                        .EnablePerformanceCounters()
                    .And.WithDictionaryHandle("h2")
                        .WithExpiration(ExpirationMode.None, TimeSpan.Zero)
                        .DisableStatistics()
                    .And.WithDictionaryHandle("h3")
                        .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(231))
                        .EnableStatistics();
            });

            // assert
            RedisConfigurations.GetConfiguration("myRedis").Should().NotBeNull();
            act.Configuration.UpdateMode.Should().Be(CacheUpdateMode.None);
            act.Configuration.MaxRetries.Should().Be(22);
            act.Configuration.RetryTimeout.Should().Be(2223);
            act.CacheHandles.ElementAt(0).Configuration.Name.Should().Be("h1");
            act.CacheHandles.ElementAt(0).Configuration.EnablePerformanceCounters.Should().BeTrue();
            act.CacheHandles.ElementAt(0).Configuration.EnableStatistics.Should().BeTrue();
            act.CacheHandles.ElementAt(0).Configuration.ExpirationMode.Should().Be(ExpirationMode.Absolute);
            act.CacheHandles.ElementAt(0).Configuration.ExpirationTimeout.Should().Be(new TimeSpan(12, 0, 0));

            act.CacheHandles.ElementAt(1).Configuration.Name.Should().Be("h2");
            act.CacheHandles.ElementAt(1).Configuration.EnablePerformanceCounters.Should().BeFalse();
            act.CacheHandles.ElementAt(1).Configuration.EnableStatistics.Should().BeFalse();
            act.CacheHandles.ElementAt(1).Configuration.ExpirationMode.Should().Be(ExpirationMode.None);
            act.CacheHandles.ElementAt(1).Configuration.ExpirationTimeout.Should().Be(new TimeSpan(0, 0, 0));

            act.CacheHandles.ElementAt(2).Configuration.Name.Should().Be("h3");
            act.CacheHandles.ElementAt(2).Configuration.EnablePerformanceCounters.Should().BeFalse();
            act.CacheHandles.ElementAt(2).Configuration.EnableStatistics.Should().BeTrue();
            act.CacheHandles.ElementAt(2).Configuration.ExpirationMode.Should().Be(ExpirationMode.Sliding);
            act.CacheHandles.ElementAt(2).Configuration.ExpirationTimeout.Should().Be(new TimeSpan(0, 0, 231));
        }

        [Fact]
        [ReplaceCulture]
        [Trait("category", "NotOnMono")]
        public void CacheFactory_FromConfig_Generic_A()
        {
            var cache = CacheFactory.FromConfiguration<byte[]>("c1");

            cache.Should().NotBeNull();
            cache.CacheHandles.Count().Should().Be(3);
            cache.Name.Should().Be("c1");
        }

        [Fact]
        [ReplaceCulture]
        [Trait("category", "NotOnMono")]
        public void CacheFactory_FromConfig_Generic_B()
        {
            var cache = CacheFactory.FromConfiguration<byte[]>("c1", "cacheManager");

            cache.Should().NotBeNull();
            cache.CacheHandles.Count().Should().Be(3);
            cache.Name.Should().Be("c1");
        }

        [Fact]
        [ReplaceCulture]
        [Trait("category", "NotOnMono")]
        public void CacheFactory_FromConfig_NonGeneric_A()
        {
            var cache = CacheFactory.FromConfiguration(typeof(string), "c1") as ICacheManager<string>;

            cache.Should().NotBeNull();
            cache.CacheHandles.Count().Should().Be(3);
            cache.Name.Should().Be("c1");
        }

        [Fact]
        [ReplaceCulture]
        [Trait("category", "NotOnMono")]
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
                ConfigurationBuilder.BuildConfiguration(cfg => cfg.WithSystemRuntimeCacheHandle())) as ICacheManager<string>;

            cache.Should().NotBeNull();
            cache.CacheHandles.Count().Should().Be(1);
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_NonGenericWithType()
        {
            var cache = CacheFactory.Build(
                typeof(string),
                settings => settings.WithSystemRuntimeCacheHandle()) as ICacheManager<string>;

            cache.Should().NotBeNull();
            cache.CacheHandles.Count().Should().Be(1);
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithJsonSerializer()
        {
            var cache = CacheFactory.Build(
                p => p
                    .WithJsonSerializer()
                    .WithSystemRuntimeCacheHandle());

            cache.Configuration.SerializerType.Should().NotBeNull();
            cache.Configuration.SerializerType.Should().Be(typeof(JsonCacheSerializer));
        }

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithJsonSerializerCustomSettings()
        {
            var serializationSettings = new JsonSerializerSettings()
            {
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat
            };

            var deserializationSettings = new JsonSerializerSettings()
            {
                FloatFormatHandling = FloatFormatHandling.Symbol
            };

            var cache = CacheFactory.Build(
                p => p
                    .WithJsonSerializer(serializationSettings, deserializationSettings)
                    .WithSystemRuntimeCacheHandle());
        }

#if !NETCOREAPP2

        [Fact]
        [ReplaceCulture]
        public void CacheFactory_Build_WithSerializer_SimpleBinary()
        {
            var cache = CacheFactory.Build(
                p =>
                p.WithSerializer(typeof(BinaryCacheSerializer))
                    .WithSystemRuntimeCacheHandle());

            cache.Configuration.SerializerType.Should().NotBeNull();
            cache.Configuration.SerializerType.Should().Be(typeof(BinaryCacheSerializer));
        }

#endif
    }
}
