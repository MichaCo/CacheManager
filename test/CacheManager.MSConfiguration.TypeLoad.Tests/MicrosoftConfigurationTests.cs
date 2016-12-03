using System;
using System.Collections.Generic;
using CacheManager.Core;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CacheManager.MSConfiguration.TypeLoad.Tests
{
    public class MicrosoftConfigurationTests
    {
        [Fact]
        public void Configuration_RedisConfig_NotReferenced()
        {
            var key = Guid.NewGuid().ToString();
            var data = new Dictionary<string, string>
            {
                {"redis:0:key", key},
            };

            Action act = () => GetConfiguration(data).LoadRedisConfigurations();
            act.ShouldThrow<InvalidOperationException>("*Redis types could not be loaded*");
        }

        [Fact]
        public void Configuration_CacheHandle_Redis_NotReferenced()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Redis"},
                {"cacheManagers:0:handles:0:key", "key"}
            };

            var config = GetConfiguration(data);
            Action action = () => config.GetCacheConfiguration("name");
            action.ShouldThrow<InvalidOperationException>().WithMessage("*'Redis' could not be loaded*");
        }

        [Fact]
        public void Configuration_CacheHandle_Memcached_NotReferenced()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Memcached"},
                {"cacheManagers:0:handles:0:key", "key"}
            };

            var config = GetConfiguration(data);
            Action action = () => config.GetCacheConfiguration("name");
            action.ShouldThrow<InvalidOperationException>().WithMessage("*'Memcached' could not be loaded*");
        }

        [Fact]
        public void Configuration_CacheHandle_Couchbase_NotReferenced()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Couchbase"},
                {"cacheManagers:0:handles:0:key", "key"}
            };

            var config = GetConfiguration(data);
            Action action = () => config.GetCacheConfiguration("name");
            action.ShouldThrow<InvalidOperationException>().WithMessage("*'Couchbase' could not be loaded*");
        }

        [Fact]
        public void Configuration_CacheHandle_SystemRuntime_NotReferenced()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "SystemRuntime"},
                {"cacheManagers:0:handles:0:key", "key"}
            };

            var config = GetConfiguration(data);
            Action action = () => config.GetCacheConfiguration("name");
            action.ShouldThrow<InvalidOperationException>().WithMessage("*'SystemRuntime' could not be loaded*");
        }

        [Fact]
        public void Configuration_CacheHandle_Web_NotReferenced()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "SystemWeb"},
                {"cacheManagers:0:handles:0:key", "key"}
            };

            var config = GetConfiguration(data);
            Action action = () => config.GetCacheConfiguration("name");
            action.ShouldThrow<InvalidOperationException>().WithMessage("*'SystemWeb' could not be loaded*");
        }

#if NETCOREAPP
        [Fact]
        public void Configuration_Serializer_BinaryInvalidOnCore()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:serializer:knownType", "Binary"},
            };

            Action act = () => GetConfiguration(data).GetCacheConfiguration("name");
            act.ShouldThrow<InvalidOperationException>().WithMessage("*BinaryCacheSerializer is not available*");
        }
#endif

        private static IConfigurationRoot GetConfiguration(IDictionary<string, string> data)
        {
            var configurationBuilder = new Microsoft.Extensions.Configuration.ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(data);
            return configurationBuilder.Build();
        }
    }
}
