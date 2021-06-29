using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            act.Should().Throw<InvalidOperationException>("*Redis types could not be loaded*");
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
            action.Should().Throw<InvalidOperationException>().WithMessage("*'Redis' could not be loaded*");
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
            action.Should().Throw<InvalidOperationException>().WithMessage("*'Memcached' could not be loaded*");
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
            action.Should().Throw<InvalidOperationException>().WithMessage("*'Couchbase' could not be loaded*");
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
            action.Should().Throw<InvalidOperationException>().WithMessage("*'SystemRuntime' could not be loaded*");
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
            action.Should().Throw<InvalidOperationException>().WithMessage("*'SystemWeb' could not be loaded*");
        }

        [Fact]
        public void Configuration_Serializer_Json_NotReferenced()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:serializer:knownType", "Json"}
            };

            var config = GetConfiguration(data);
            Action action = () => config.GetCacheConfiguration("name");
            action.Should().Throw<InvalidOperationException>().WithMessage("*serializer type 'Json' could not be loaded*");
        }

        [Fact]
        public void Configuration_Serializer_GzJson_NotReferenced()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:serializer:knownType", "GzJson"}
            };

            var config = GetConfiguration(data);
            Action action = () => config.GetCacheConfiguration("name");
            action.Should().Throw<InvalidOperationException>().WithMessage("*serializer type 'GzJson' could not be loaded*");
        }

        [Fact]
        public void Configuration_Serializer_Protobuf_NotReferenced()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:serializer:knownType", "Protobuf"}
            };

            var config = GetConfiguration(data);
            Action action = () => config.GetCacheConfiguration("name");
            action.Should().Throw<InvalidOperationException>().WithMessage("*serializer type 'Protobuf' could not be loaded*");
        }

        [Fact]
        public void Configuration_Serializer_BondCompactBinary_NotReferenced()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:serializer:knownType", "BondCompactBinary"}
            };

            var config = GetConfiguration(data);
            Action action = () => config.GetCacheConfiguration("name");
            action.Should().Throw<InvalidOperationException>().WithMessage("*serializer type 'BondCompactBinary' could not be loaded*");
        }

        [Fact]
        public void Configuration_Serializer_BondFastBinary_NotReferenced()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:serializer:knownType", "BondFastBinary"}
            };

            var config = GetConfiguration(data);
            Action action = () => config.GetCacheConfiguration("name");
            action.Should().Throw<InvalidOperationException>().WithMessage("*serializer type 'BondFastBinary' could not be loaded*");
        }

        [Fact]
        public void Configuration_Serializer_BondSimpleJson_NotReferenced()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:serializer:knownType", "BondSimpleJson"}
            };

            var config = GetConfiguration(data);
            Action action = () => config.GetCacheConfiguration("name");
            action.Should().Throw<InvalidOperationException>().WithMessage("*serializer type 'BondSimpleJson' could not be loaded*");
        }

        private static IConfigurationRoot GetConfiguration(IDictionary<string, string> data)
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(data);
            return configurationBuilder.Build();
        }
    }
}
