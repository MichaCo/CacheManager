using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CacheManager.Core;
using CacheManager.Redis;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CacheManager.Tests
{
    [ExcludeFromCodeCoverage]
    public class MicrosoftConfigurationTests
    {
        [Fact]
        public void Configuration_CacheManager_ComplexSingleManager()
        {
            var key = Guid.NewGuid().ToString();
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "myCacheName"},
                {"cacheManagers:0:maxRetries", "500"},
                {"cacheManagers:0:retryTimeout", "123"},
                {"cacheManagers:0:updateMode", "Up"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:handles:0:enablePerformanceCounters", "true"},
                {"cacheManagers:0:handles:0:enableStatistics", "true"},
                {"cacheManagers:0:handles:0:expirationMode", "Absolute"},
                {"cacheManagers:0:handles:0:expirationTimeout", "0:10:0"},
                {"cacheManagers:0:handles:0:isBackplaneSource", "true"},
                {"cacheManagers:0:handles:0:name", "handleName"},
                {"cacheManagers:0:handles:0:key", key},
                {"cacheManagers:0:handles:1:knownType", "Dictionary"},
                {"cacheManagers:0:handles:1:enablePerformanceCounters", "false"},
                {"cacheManagers:0:handles:1:enableStatistics", "false"},
                {"cacheManagers:0:handles:1:expirationMode", "Sliding"},
                {"cacheManagers:0:handles:1:expirationTimeout", "0:20:0"},
                {"cacheManagers:0:handles:1:isBackplaneSource", "false"},
                {"cacheManagers:0:handles:1:name", "handleName2"},
                {"cacheManagers:0:handles:1:key", key + "2"}
            };

            var config = GetConfiguration(data).GetCacheConfiguration("myCacheName");
            config.Name.Should().Be("myCacheName");
            config.MaxRetries.Should().Be(500);
            config.RetryTimeout.Should().Be(123);
            config.UpdateMode.Should().Be(CacheUpdateMode.Up);
            config.CacheHandleConfigurations.Count.Should().Be(2);
            config.CacheHandleConfigurations[0].Key.Should().Be(key);
            config.CacheHandleConfigurations[1].Key.Should().Be(key + "2");
            config.CacheHandleConfigurations[0].Name.Should().Be("handleName");
            config.CacheHandleConfigurations[1].Name.Should().Be("handleName2");

            var cache = new BaseCacheManager<string>(config);
            cache.Add("key", "value").Should().BeTrue();
        }

        [Fact]
        public void Configuration_CacheManager_ComplexManyManager()
        {
            var key = Guid.NewGuid().ToString();
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "myCacheName1"},
                {"cacheManagers:0:maxRetries", "100"},
                {"cacheManagers:0:retryTimeout", "100"},
                {"cacheManagers:0:updateMode", "None"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:1:name", "myCacheName2"},
                {"cacheManagers:1:maxRetries", "200"},
                {"cacheManagers:1:retryTimeout", "200"},
                {"cacheManagers:1:updateMode", "Up"},
                {"cacheManagers:1:handles:0:knownType", "Dictionary"},
                {"cacheManagers:1:handles:1:knownType", "Dictionary"},
                {"cacheManagers:2:name", "myCacheName3"},
                {"cacheManagers:2:maxRetries", "300"},
                {"cacheManagers:2:retryTimeout", "300"},
                {"cacheManagers:2:updateMode", "Up"},
                {"cacheManagers:2:handles:0:knownType", "Dictionary"},
                {"cacheManagers:2:handles:1:knownType", "Dictionary"},
                {"cacheManagers:2:handles:2:knownType", "Dictionary"},
            };

            var configs = GetConfiguration(data).GetCacheConfigurations().ToArray();
            for (var i = 1; i <= configs.Count(); i++)
            {
                var config = configs[i - 1];
                config.Name.Should().Be("myCacheName" + i);
                config.MaxRetries.Should().Be(i * 100);
                config.RetryTimeout.Should().Be(i * 100);
                config.CacheHandleConfigurations.Count.Should().Be(i);

                var cache = new BaseCacheManager<string>(config);
                cache.Add("key", "value").Should().BeTrue();
            }
        }

        [Fact]
        public void Configuration_CacheManager_ByNameFromMany()
        {
            var key = Guid.NewGuid().ToString();
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "myCacheName1"},
                {"cacheManagers:0:maxRetries", "100"},
                {"cacheManagers:0:retryTimeout", "100"},
                {"cacheManagers:0:updateMode", "None"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:1:name", "myCacheName2"},
                {"cacheManagers:1:maxRetries", "200"},
                {"cacheManagers:1:retryTimeout", "200"},
                {"cacheManagers:1:updateMode", "Up"},
                {"cacheManagers:1:handles:0:knownType", "Dictionary"},
                {"cacheManagers:1:handles:1:knownType", "Dictionary"},
                {"cacheManagers:2:name", "myCacheName3"},
                {"cacheManagers:2:maxRetries", "300"},
                {"cacheManagers:2:retryTimeout", "300"},
                {"cacheManagers:2:updateMode", "Full"},
                {"cacheManagers:2:handles:0:knownType", "Dictionary"},
                {"cacheManagers:2:handles:1:knownType", "Dictionary"},
                {"cacheManagers:2:handles:2:knownType", "Dictionary"},
            };

            var config = GetConfiguration(data).GetCacheConfiguration("myCacheName2");
            config.Name.Should().Be("myCacheName2");
            config.MaxRetries.Should().Be(200);
            config.RetryTimeout.Should().Be(200);
            config.CacheHandleConfigurations.Count.Should().Be(2);

            var cache = new BaseCacheManager<string>(config);
            cache.Add("key", "value").Should().BeTrue();
        }

        [Fact]
        public void Configuration_CacheManager_Empty()
        {
            var data = new Dictionary<string, string>
            {
            };

            var config = GetConfiguration(data);
            Action action = () => config.GetCacheConfiguration("test");
            action.ShouldThrow<InvalidOperationException>().WithMessage("*'cacheManagers' section*");
        }

        [Fact]
        public void Configuration_CacheManager_NoManager()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:test", "test"},
            };

            var config = GetConfiguration(data);
            Action action = () => config.GetCacheConfiguration("something");
            action.ShouldThrow<InvalidOperationException>().WithMessage("*configuration for name 'something' not found*");
        }

        [Fact]
        public void Configuration_CacheManager_NoManagerB()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:test", "test"},
            };

            var config = GetConfiguration(data);
            Action action = () => config.GetCacheConfiguration("something");
            action.ShouldThrow<InvalidOperationException>().WithMessage("*configuration for name 'something' not found*");
        }

        [Fact]
        public void Configuration_CacheManager_NoHandles()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
            };

            var config = GetConfiguration(data);
            Action action = () => config.GetCacheConfiguration("name");
            action.ShouldThrow<InvalidOperationException>().WithMessage("*No cache handles*");
        }

        [Fact]
        public void Configuration_CacheManager_AllProperties()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:maxRetries", "42"},
                {"cacheManagers:0:retryTimeout", "21"},
                {"cacheManagers:0:updateMode", "Up"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
            };

            var config = GetConfiguration(data).GetCacheConfiguration("name");
            config.Name.Should().Be("name");
            config.MaxRetries.Should().Be(42);
            config.RetryTimeout.Should().Be(21);
            config.UpdateMode.Should().Be(CacheUpdateMode.Up);
            var cache = new BaseCacheManager<string>(config);
            cache.Add("key", "value").Should().BeTrue();
        }

        [Fact]
        public void Configuration_CacheManager_InvalidUpdateMode()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:updateMode", "invalid"}
            };

            var config = GetConfiguration(data);
            Action action = () => config.GetCacheConfiguration("name");
            action.ShouldThrow<InvalidOperationException>().WithMessage("*Failed to convert 'invalid'*");
        }

        [Fact]
        public void Configuration_CacheManager_InvalidRetryTimeout()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:retryTimeout", "invalid"}
            };

            var config = GetConfiguration(data);
            Action action = () => config.GetCacheConfiguration("name");
            action.ShouldThrow<InvalidOperationException>().WithMessage("*Failed to convert 'invalid'*");
        }

        [Fact]
        public void Configuration_CacheManager_InvalidMaxRetries()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:maxRetries", "invalid"}
            };

            var config = GetConfiguration(data);
            Action action = () => config.GetCacheConfiguration("name");
            action.ShouldThrow<InvalidOperationException>().WithMessage("*Failed to convert 'invalid'*");
        }

        [Fact]
        public void Configuration_CacheHandle_NoType()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:bla", "blub"},
            };

            var config = GetConfiguration(data);
            Action action = () => config.GetCacheConfiguration("name");
            action.ShouldThrow<InvalidOperationException>().WithMessage("*No 'type' or 'knownType' defined*");
        }

        [Fact]
        [ReplaceCulture]
        public void Configuration_CacheHandle_InvalidType()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:type", "SomeType"},
            };

            var config = GetConfiguration(data);
            Action action = () => config.GetCacheConfiguration("name");
            action.ShouldThrow<TypeLoadException>().WithMessage("*Could not load type 'SomeType'*");
        }

        [Fact]
        [ReplaceCulture]
        public void Configuration_CacheHandle_KnownType_Invalid()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "really"},
            };

            var config = GetConfiguration(data);
            Action action = () => config.GetCacheConfiguration("name");
            action.ShouldThrow<InvalidOperationException>().WithMessage("*Known handle type 'really' is invalid. Check configuration at 'cacheManagers:0:handles:0'*");
        }

        [Fact]
        public void Configuration_CacheHandle_MinimalValid()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:type", "System.Object"},
            };

            var config = GetConfiguration(data).GetCacheConfiguration("name");
            config.CacheHandleConfigurations.Count.Should().Be(1);

            // that's not a valid handle type, but that will be validated later
            config.CacheHandleConfigurations[0].HandleType.Should().Be(typeof(object));
        }

#if !NETCOREAPP

        [Fact]
        public void Configuration_CacheHandle_KnownType_SystemRuntime()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "SystemRuntime"},
            };

            var config = GetConfiguration(data).GetCacheConfiguration("name");
            config.Name.Should().Be("name");
            config.CacheHandleConfigurations.Count.Should().Be(1);
            config.CacheHandleConfigurations[0].HandleType.Should().Be(typeof(SystemRuntimeCaching.MemoryCacheHandle<>));
            var cache = new BaseCacheManager<string>(config);
            cache.Add("key", "value").Should().BeTrue();
        }

#endif

        [Fact]
        public void Configuration_CacheHandle_KnownType_RedisNoKey()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Redis"},
            };

            var config = GetConfiguration(data);
            Action action = () => config.GetCacheConfiguration("name");
            action.ShouldThrow<InvalidOperationException>().WithMessage("*'key' or 'name'*");
        }

        [Fact]
        public void Configuration_CacheHandle_KnownType_Redis()
        {
            var key = Guid.NewGuid().ToString();
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Redis"},
                {"cacheManagers:0:handles:0:key", key},
            };

            var config = GetConfiguration(data).GetCacheConfiguration("name");
            config.Name.Should().Be("name");
            config.CacheHandleConfigurations.Count.Should().Be(1);
            config.CacheHandleConfigurations[0].HandleType.Should().Be(typeof(Redis.RedisCacheHandle<>));
            config.CacheHandleConfigurations[0].Key.Should().Be(key);
            config.CacheHandleConfigurations[0].Name.Should().NotBeNullOrWhiteSpace();  // name is random in this case
        }

        [Fact]
        public void Configuration_CacheHandle_KnownType_RedisB()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Redis"},
                {"cacheManagers:0:handles:0:name", "name"},
            };

            var config = GetConfiguration(data).GetCacheConfiguration("name");
            config.Name.Should().Be("name");
            config.CacheHandleConfigurations.Count.Should().Be(1);
            config.CacheHandleConfigurations[0].HandleType.Should().Be(typeof(Redis.RedisCacheHandle<>));
            config.CacheHandleConfigurations[0].Name.Should().Be("name");
            config.CacheHandleConfigurations[0].Key.Should().Be("name");    // now key gets set to name
        }

#if !NETCOREAPP

        [Fact]
        public void Configuration_CacheHandle_KnownType_CouchbaseNoKey()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Couchbase"},
            };

            var config = GetConfiguration(data);
            Action action = () => config.GetCacheConfiguration("name");
            action.ShouldThrow<InvalidOperationException>().WithMessage("*'key' or 'name'*");
        }

        [Fact]
        public void Configuration_CacheHandle_KnownType_Couchbase()
        {
            var key = Guid.NewGuid().ToString();
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Couchbase"},
                {"cacheManagers:0:handles:0:key", key},
            };

            var config = GetConfiguration(data).GetCacheConfiguration("name");
            config.Name.Should().Be("name");
            config.CacheHandleConfigurations.Count.Should().Be(1);
            config.CacheHandleConfigurations[0].HandleType.Should().Be(typeof(Couchbase.BucketCacheHandle<>));
            config.CacheHandleConfigurations[0].Key.Should().Be(key);
            config.CacheHandleConfigurations[0].Name.Should().NotBeNullOrWhiteSpace();  // name is random in this case

            var cache = new BaseCacheManager<int>(config);
        }

        [Fact]
        public void Configuration_CacheHandle_KnownType_CouchbaseB()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Couchbase"},
                {"cacheManagers:0:handles:0:name", "name"},
            };

            var config = GetConfiguration(data).GetCacheConfiguration("name");
            config.Name.Should().Be("name");
            config.CacheHandleConfigurations.Count.Should().Be(1);
            config.CacheHandleConfigurations[0].HandleType.Should().Be(typeof(Couchbase.BucketCacheHandle<>));
            config.CacheHandleConfigurations[0].Name.Should().Be("name");
            config.CacheHandleConfigurations[0].Key.Should().Be("name");    // now key gets set to name

            var cache = new BaseCacheManager<int>(config);
        }

        [Fact]
        public void Configuration_CacheHandle_KnownType_MemcachedNoKey()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Memcached"},
            };

            var config = GetConfiguration(data);
            Action action = () => config.GetCacheConfiguration("name");
            action.ShouldThrow<InvalidOperationException>().WithMessage("*'key' or 'name'*");
        }

#if MEMCACHEDENABLED

        [Fact]
        [Trait("category", "memcached")]
        public void Configuration_CacheHandle_KnownType_Memcached()
        {
            var key = "default";
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Memcached"},
                {"cacheManagers:0:handles:0:key", key},
            };

            var config = GetConfiguration(data).GetCacheConfiguration("name");
            config.Name.Should().Be("name");
            config.CacheHandleConfigurations.Count.Should().Be(1);
            config.CacheHandleConfigurations[0].HandleType.Should().Be(typeof(Memcached.MemcachedCacheHandle<>));
            config.CacheHandleConfigurations[0].Key.Should().Be(key);
            config.CacheHandleConfigurations[0].Name.Should().NotBeNullOrWhiteSpace();  // name is random in this case

            var cache = new BaseCacheManager<string>(config);
            cache.Add(Guid.NewGuid().ToString(), "value").Should().BeTrue();
        }

#endif

        [Fact]
        public void Configuration_CacheHandle_KnownType_MemcachedB()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Memcached"},
                {"cacheManagers:0:handles:0:name", "name"},
            };

            var config = GetConfiguration(data).GetCacheConfiguration("name");
            config.Name.Should().Be("name");
            config.CacheHandleConfigurations.Count.Should().Be(1);
            config.CacheHandleConfigurations[0].HandleType.Should().Be(typeof(Memcached.MemcachedCacheHandle<>));
            config.CacheHandleConfigurations[0].Name.Should().Be("name");
            config.CacheHandleConfigurations[0].Key.Should().Be("name");    // now key gets set to name
        }

        [Fact]
        public void Configuration_CacheHandle_Type_MemcachedB()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:type", "CacheManager.Memcached.MemcachedCacheHandle`1, CacheManager.Memcached"},
                {"cacheManagers:0:handles:0:name", "name"},
            };

            var config = GetConfiguration(data).GetCacheConfiguration("name");
            config.Name.Should().Be("name");
            config.CacheHandleConfigurations.Count.Should().Be(1);
            config.CacheHandleConfigurations[0].HandleType.Should().Be(typeof(Memcached.MemcachedCacheHandle<>));
            config.CacheHandleConfigurations[0].Name.Should().Be("name");
            config.CacheHandleConfigurations[0].Key.Should().Be("name");    // now key gets set to name
        }

        [Fact]
        public void Configuration_CacheHandle_KnownType_Web()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "SystemWeb"},
            };

            var config = GetConfiguration(data).GetCacheConfiguration("name");
            config.Name.Should().Be("name");
            config.CacheHandleConfigurations.Count.Should().Be(1);
            config.CacheHandleConfigurations[0].HandleType.Should().Be(typeof(Web.SystemWebCacheHandle<>));
        }

#endif

        [Fact]
        public void Configuration_CacheHandle_KnownType_Dictionary()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
            };

            var config = GetConfiguration(data).GetCacheConfiguration("name");
            config.Name.Should().Be("name");
            config.CacheHandleConfigurations.Count.Should().Be(1);
            config.CacheHandleConfigurations[0].HandleType.Should().Be(typeof(Core.Internal.DictionaryCacheHandle<>));

            var cache = new BaseCacheManager<string>(config);
            cache.Add("key", "value").Should().BeTrue();
        }

        [Fact]
        public void Configuration_CacheHandle_KnownType_MsMemory()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "MsMemory"},
            };

            var config = GetConfiguration(data).GetCacheConfiguration("name");
            config.Name.Should().Be("name");
            config.CacheHandleConfigurations.Count.Should().Be(1);
            config.CacheHandleConfigurations[0].HandleType.Should().Be(typeof(CacheManager.MicrosoftCachingMemory.MemoryCacheHandle<>));

            var cache = new BaseCacheManager<string>(config);
            cache.Add("key", "value").Should().BeTrue();
        }

        [Fact]
        public void Configuration_CacheHandle_AllProperties()
        {
            var key = Guid.NewGuid().ToString();
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "cacheName"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:handles:0:enablePerformanceCounters", "true"},
                {"cacheManagers:0:handles:0:enableStatistics", "true"},
                {"cacheManagers:0:handles:0:expirationMode", "Absolute"},
                {"cacheManagers:0:handles:0:expirationTimeout", "0:10:0"},
                {"cacheManagers:0:handles:0:isBackplaneSource", "true"},
                {"cacheManagers:0:handles:0:name", "handleName"},
                {"cacheManagers:0:handles:0:key", key}
            };

            var config = GetConfiguration(data).GetCacheConfiguration("cacheName");
            config.Name.Should().Be("cacheName");
            config.CacheHandleConfigurations[0].EnablePerformanceCounters.Should().BeTrue();
            config.CacheHandleConfigurations[0].EnableStatistics.Should().BeTrue();
            config.CacheHandleConfigurations[0].ExpirationMode.Should().Be(ExpirationMode.Absolute);
            config.CacheHandleConfigurations[0].ExpirationTimeout.Should().Be(TimeSpan.FromMinutes(10));
            config.CacheHandleConfigurations[0].IsBackplaneSource.Should().BeTrue();
            config.CacheHandleConfigurations[0].Name.Should().Be("handleName");
            config.CacheHandleConfigurations[0].Key.Should().Be(key);

            var cache = new BaseCacheManager<string>(config);
            cache.Add("key", "value").Should().BeTrue();
        }

        [Fact]
        [ReplaceCulture]
        public void Configuration_CacheHandle_InvalidBackplaneFlag()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "cacheName"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:handles:0:isBackplaneSource", "invalid"}
            };

            Action act = () => GetConfiguration(data).GetCacheConfiguration("cacheName");
            act.ShouldThrow<InvalidOperationException>().WithMessage("*Failed to convert 'invalid'*");
        }

        [Fact]
        [ReplaceCulture]
        public void Configuration_CacheHandle_InvalidExpirationTimeout()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "cacheName"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:handles:0:expirationTimeout", "invalid"}
            };

            Action act = () => GetConfiguration(data).GetCacheConfiguration("cacheName");
            act.ShouldThrow<InvalidOperationException>().WithMessage("*Failed to convert 'invalid'*");
        }

        [Fact]
        [ReplaceCulture]
        public void Configuration_CacheHandle_InvalidExpirationMode()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "cacheName"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:handles:0:expirationMode", "invalid"}
            };

            Action act = () => GetConfiguration(data).GetCacheConfiguration("cacheName");
            act.ShouldThrow<InvalidOperationException>().WithMessage("*Failed to convert 'invalid'*");
        }

        [Fact]
        [ReplaceCulture]
        public void Configuration_CacheHandle_InvalidStatistics()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "cacheName"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:handles:0:enableStatistics", "invalid"}
            };

            Action act = () => GetConfiguration(data).GetCacheConfiguration("cacheName");
            act.ShouldThrow<InvalidOperationException>().WithMessage("*Failed to convert 'invalid'*");
        }

        [Fact]
        [ReplaceCulture]
        public void Configuration_CacheHandle_InvalidPerCounters()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "cacheName"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:handles:0:enablePerformanceCounters", "invalid"}
            };

            Action act = () => GetConfiguration(data).GetCacheConfiguration("cacheName");
            act.ShouldThrow<InvalidOperationException>().WithMessage("*Failed to convert 'invalid'*");
        }

        [Fact]
        public void Configuration_Backplane_InvalidType()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:backplane:type", ""},
            };

            Action act = () => GetConfiguration(data).GetCacheConfiguration("name");
            act.ShouldThrow<InvalidOperationException>().WithMessage("*No 'type' or 'knownType'*");
        }

        [Fact]
        [ReplaceCulture]
        public void Configuration_Backplane_InvalidSomeType()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:backplane:type", "something"},
            };

            Action act = () => GetConfiguration(data).GetCacheConfiguration("name");
            act.ShouldThrow<TypeLoadException>().WithMessage("*type 'something'*");
        }

        [Fact]
        public void Configuration_Backplane_SomeType()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:backplane:type", "System.Object"},
            };

            var config = GetConfiguration(data).GetCacheConfiguration("name");
            config.BackplaneType.Should().NotBeNull();
        }

        [Fact]
        public void Configuration_Backplane_InvalidKnownType()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:backplane:knownType", ""},
            };

            Action act = () => GetConfiguration(data).GetCacheConfiguration("name");
            act.ShouldThrow<InvalidOperationException>().WithMessage("*No 'type' or 'knownType'*");
        }

        [Fact]
        public void Configuration_Backplane_InvalidKnownTypeB()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:backplane:knownType", "Something"},
            };

            Action act = () => GetConfiguration(data).GetCacheConfiguration("name");
            act.ShouldThrow<InvalidOperationException>().WithMessage("*Known backplane type 'Something' is invalid*");
        }

        [Fact]
        public void Configuration_Backplane_Redis_MissingKey()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:backplane:knownType", "Redis"},
            };

            Action act = () => GetConfiguration(data).GetCacheConfiguration("name");
            act.ShouldThrow<InvalidOperationException>().WithMessage("*The key property is required*");
        }

#if REDISENABLED

        [Fact]
        [Trait("category", "Redis")]
        [Trait("category", "Unreliable")]
        public void Configuration_Backplane_Redis_Valid()
        {
            var key = Guid.NewGuid().ToString();
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Redis"},
                {"cacheManagers:0:handles:0:key", key},
                {"cacheManagers:0:handles:0:isBackplaneSource", "true"},
                {"cacheManagers:0:backplane:knownType", "Redis"},
                {"cacheManagers:0:backplane:channelName", "channelName"},
                {"cacheManagers:0:backplane:key", key},
                {"cacheManagers:0:serializer:knownType", "Json"},
                {"redis:1:connectionString", "127.0.0.1:6379"},
                {"redis:1:key", key}
            };

            var config = GetConfiguration(data).GetCacheConfiguration("name");

            config.BackplaneChannelName.Should().Be("channelName");
            config.BackplaneConfigurationKey.Should().Be(key);
            config.BackplaneType.Should().Be(typeof(Redis.RedisCacheBackplane));
            config.HasBackplane.Should().BeTrue();

            var cache = new BaseCacheManager<string>(config);
            cache.Add(Guid.NewGuid().ToString(), "value").Should().BeTrue();
        }

#endif

        [Fact]
        public void Configuration_Backplane_SomeType_Valid()
        {
            var key = Guid.NewGuid().ToString();
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:backplane:type", "System.Object"},
                {"cacheManagers:0:backplane:channelName", "channelName"},
                {"cacheManagers:0:backplane:key", key},
            };

            var config = GetConfiguration(data).GetCacheConfiguration("name");
            config.BackplaneChannelName.Should().Be("channelName");
            config.BackplaneConfigurationKey.Should().Be(key);
            config.BackplaneType.Should().Be(typeof(object));
            config.HasBackplane.Should().BeTrue();
        }

        [Fact]
        public void Configuration_LoggerFactory_Invalid()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:loggerFactory:knownType", ""},
                {"cacheManagers:0:loggerFactory:type", ""},
            };

            Action act = () => GetConfiguration(data).GetCacheConfiguration("name");
            act.ShouldThrow<InvalidOperationException>().WithMessage("*No 'type' or 'knownType'*");
        }

        [Fact]
        public void Configuration_LoggerFactory_InvalidKnownType()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:loggerFactory:knownType", "something"},
            };

            Action act = () => GetConfiguration(data).GetCacheConfiguration("name");
            act.ShouldThrow<InvalidOperationException>().WithMessage("*known logger factory type 'something'*");
        }

        [Fact]
        [ReplaceCulture]
        public void Configuration_LoggerFactory_InvalidSomeType()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:loggerFactory:type", "something"},
            };

            Action act = () => GetConfiguration(data).GetCacheConfiguration("name");
            act.ShouldThrow<TypeLoadException>().WithMessage("*type 'something'*");
        }

        [Fact]
        public void Configuration_LoggerFactory_SomeType_Valid()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:loggerFactory:type", "System.Object"}
            };

            var config = GetConfiguration(data).GetCacheConfiguration("name");
            config.LoggerFactoryType.Should().Be(typeof(object));
        }

        [Fact]
        public void Configuration_LoggerFactory_KnownType_Valid()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:loggerFactory:knownType", "Microsoft"}
            };

            var config = GetConfiguration(data).GetCacheConfiguration("name");
            config.LoggerFactoryType.Should().Be(typeof(Logging.MicrosoftLoggerFactoryAdapter));

            var cache = new BaseCacheManager<string>(config);
            cache.Add("key", "value").Should().BeTrue();
        }

        [Fact]
        public void Configuration_Serializer_Invalid()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:serializer:knownType", ""},
                {"cacheManagers:0:serializer:type", ""},
            };

            Action act = () => GetConfiguration(data).GetCacheConfiguration("name");
            act.ShouldThrow<InvalidOperationException>().WithMessage("*No 'type' or 'knownType'*");
        }

        [Fact]
        public void Configuration_Serializer_InvalidKnownType()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:serializer:knownType", "something"},
            };

            Action act = () => GetConfiguration(data).GetCacheConfiguration("name");
            act.ShouldThrow<InvalidOperationException>().WithMessage("*known serializer type 'something'*");
        }

        [Fact]
        [ReplaceCulture]
        public void Configuration_Serializer_InvalidSomeType()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:serializer:type", "something"},
            };

            Action act = () => GetConfiguration(data).GetCacheConfiguration("name");
            act.ShouldThrow<TypeLoadException>().WithMessage("*type 'something'*");
        }

        [Fact]
        public void Configuration_Serializer_SomeType_Valid()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:serializer:type", "System.Object"}
            };

            var config = GetConfiguration(data).GetCacheConfiguration("name");
            Action act = () =>
            {
                var cache = new BaseCacheManager<string>(config);
            };

            config.SerializerType.Should().Be(typeof(object));
            act.ShouldThrow<InvalidOperationException>().WithMessage("*ICacheSerializer*");
        }

#if !NETCOREAPP

        [Fact]
        public void Configuration_Serializer_KnownType_Binary()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:serializer:knownType", "Binary"}
            };

            var config = GetConfiguration(data).GetCacheConfiguration("name");
            Action act = () =>
            {
                var cache = new BaseCacheManager<string>(config);
                cache.Add("key", "value");
            };

            config.SerializerType.Should().Be(typeof(Core.Internal.BinaryCacheSerializer));
            act.ShouldNotThrow();
        }

#endif

        [Fact]
        public void Configuration_Serializer_KnownType_Json()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:serializer:knownType", "Json"}
            };

            var config = GetConfiguration(data).GetCacheConfiguration("name");
            Action act = () =>
            {
                var cache = new BaseCacheManager<string>(config);
                cache.Add("key", "value");
            };

            config.SerializerType.Should().Be(typeof(Serialization.Json.JsonCacheSerializer));
            act.ShouldNotThrow();
        }

        [Fact]
        public void Configuration_Serializer_KnownType_GzJson()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:serializer:knownType", "GzJson"}
            };

            var config = GetConfiguration(data).GetCacheConfiguration("name");
            Action act = () =>
            {
                var cache = new BaseCacheManager<string>(config);
                cache.Add("key", "value");
            };

            config.SerializerType.Should().Be(typeof(Serialization.Json.GzJsonCacheSerializer));
            act.ShouldNotThrow();
        }

        [Fact]
        public void Configuration_Serializer_KnownType_Protobuf()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:serializer:knownType", "Protobuf"}
            };

            var config = GetConfiguration(data).GetCacheConfiguration("name");
            Action act = () =>
            {
                var cache = new BaseCacheManager<string>(config);
                cache.Add("key", "value");
            };

            config.SerializerType.Should().Be(typeof(Serialization.ProtoBuf.ProtoBufSerializer));
            act.ShouldNotThrow();
        }

        [Fact]
        public void Configuration_Serializer_Type_Protobuf()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:serializer:type", "CacheManager.Serialization.ProtoBuf.ProtoBufSerializer, CacheManager.Serialization.ProtoBuf"}
            };

            var config = GetConfiguration(data).GetCacheConfiguration("name");
            Action act = () =>
            {
                var cache = new BaseCacheManager<string>(config);
                cache.Add("key", "value");
            };

            config.SerializerType.Should().Be(typeof(Serialization.ProtoBuf.ProtoBufSerializer));
            act.ShouldNotThrow();
        }

        [Fact]
        public void Configuration_Serializer_KnownType_BondCompact()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:serializer:knownType", "BondCompactBinary"}
            };

            var config = GetConfiguration(data).GetCacheConfiguration("name");
            Action act = () =>
            {
                var cache = new BaseCacheManager<string>(config);
                cache.Add("key", "value");
            };

            config.SerializerType.Should().Be(typeof(Serialization.Bond.BondCompactBinaryCacheSerializer));
            act.ShouldNotThrow();
        }

        [Fact]
        public void Configuration_Serializer_KnownType_BondFast()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:serializer:knownType", "BondFastBinary"}
            };

            var config = GetConfiguration(data).GetCacheConfiguration("name");
            Action act = () =>
            {
                var cache = new BaseCacheManager<string>(config);
                cache.Add("key", "value");
            };

            config.SerializerType.Should().Be(typeof(Serialization.Bond.BondFastBinaryCacheSerializer));
            act.ShouldNotThrow();
        }

        [Fact]
        public void Configuration_Serializer_KnownType_BondJosn()
        {
            var data = new Dictionary<string, string>
            {
                {"cacheManagers:0:name", "name"},
                {"cacheManagers:0:handles:0:knownType", "Dictionary"},
                {"cacheManagers:0:serializer:knownType", "BondSimpleJson"}
            };

            var config = GetConfiguration(data).GetCacheConfiguration("name");
            Action act = () =>
            {
                var cache = new BaseCacheManager<string>(config);
                cache.Add("key", "value");
            };

            config.SerializerType.Should().Be(typeof(Serialization.Bond.BondSimpleJsonCacheSerializer));
            act.ShouldNotThrow();
        }

        [Fact]
        public void Configuration_Redis_NothingDefined()
        {
            var key = Guid.NewGuid().ToString();
            var data = new Dictionary<string, string>
            {
            };

            GetConfiguration(data).LoadRedisConfigurations();
            Action act = () => RedisConfigurations.GetConfiguration(key);
            act.ShouldThrow<InvalidOperationException>().WithMessage("*" + key + "*");
        }

        [Fact]
        public void Configuration_Redis_KeyInvalid()
        {
            var data = new Dictionary<string, string>
            {
                {"redis:0:key", ""}
            };

            Action act = () => GetConfiguration(data).LoadRedisConfigurations();
            act.ShouldThrow<InvalidOperationException>().WithMessage("*Key is required*");
        }

        [Fact]
        public void Configuration_Redis_KeyOnly()
        {
            var key = Guid.NewGuid().ToString();
            var data = new Dictionary<string, string>
            {
                {"redis:0:key", key}
            };

            Action act = () => GetConfiguration(data).LoadRedisConfigurations();
            act.ShouldThrow<InvalidOperationException>().WithMessage("*Either connection string or endpoints*");
        }

        [Fact]
        public void Configuration_Redis_Invalid_AllowAdmin()
        {
            var key = Guid.NewGuid().ToString();
            var data = new Dictionary<string, string>
            {
                {"redis:0:key", key},
                {"redis:0:connectionString", "string"},
                {"redis:0:allowAdmin", "invalid"}
            };

            Action act = () => GetConfiguration(data).LoadRedisConfigurations();
            act.ShouldThrow<InvalidOperationException>().WithMessage("*Failed to convert 'invalid'*");
        }

        [Fact]
        public void Configuration_Redis_Invalid_ConnectionTimeout()
        {
            var key = Guid.NewGuid().ToString();
            var data = new Dictionary<string, string>
            {
                {"redis:0:key", key},
                {"redis:0:connectionString", "string"},
                {"redis:0:connectionTimeout", "invalid"}
            };

            Action act = () => GetConfiguration(data).LoadRedisConfigurations();
            act.ShouldThrow<InvalidOperationException>().WithMessage("*Failed to convert 'invalid'*");
        }

        [Fact]
        public void Configuration_Redis_Invalid_Database()
        {
            var key = Guid.NewGuid().ToString();
            var data = new Dictionary<string, string>
            {
                {"redis:0:key", key},
                {"redis:0:connectionString", "string"},
                {"redis:0:database", "invalid"}
            };

            Action act = () => GetConfiguration(data).LoadRedisConfigurations();
            act.ShouldThrow<InvalidOperationException>().WithMessage("*Failed to convert 'invalid'*");
        }

        [Fact]
        public void Configuration_Redis_Invalid_IsSsl()
        {
            var key = Guid.NewGuid().ToString();
            var data = new Dictionary<string, string>
            {
                {"redis:0:key", key},
                {"redis:0:connectionString", "string"},
                {"redis:0:isSsl", "invalid"}
            };

            Action act = () => GetConfiguration(data).LoadRedisConfigurations();
            act.ShouldThrow<InvalidOperationException>().WithMessage("*Failed to convert 'invalid'*");
        }

        [Fact]
        public void Configuration_Redis_Properties()
        {
            var key = Guid.NewGuid().ToString();
            var data = new Dictionary<string, string>
            {
                {"redis:0:allowAdmin", "true"},
                {"redis:0:connectionTimeout", "123"},
                {"redis:0:database", "11"},
                {"redis:0:endpoints:0:host", "HostName"},
                {"redis:0:endpoints:0:port", "1234"},
                {"redis:0:endpoints:1:host", "HostName2"},
                {"redis:0:endpoints:1:port", "2222"},
                {"redis:0:isSsl", "true"},
                {"redis:0:key", key},
                {"redis:0:password", "password"},
                {"redis:0:sslHost", "sslHost"},
                {"redis:0:keyspaceNotificationsEnabled", "TRUE"},
                {"redis:0:twemproxyEnabled", "true" },
                {"redis:0:strictCompatibilityModeVersion", "2.5" }
            };

            GetConfiguration(data).LoadRedisConfigurations();
            var redisConfig = RedisConfigurations.GetConfiguration(key);
            redisConfig.AllowAdmin.Should().BeTrue();
            redisConfig.ConnectionString.Should().BeNullOrWhiteSpace();
            redisConfig.ConnectionTimeout.Should().Be(123);
            redisConfig.Database.Should().Be(11);
            redisConfig.Endpoints[0].Host.Should().Be("HostName");
            redisConfig.Endpoints[0].Port.Should().Be(1234);
            redisConfig.Endpoints[1].Host.Should().Be("HostName2");
            redisConfig.Endpoints[1].Port.Should().Be(2222);
            redisConfig.IsSsl.Should().BeTrue();
            redisConfig.Key.Should().Be(key);
            redisConfig.Password.Should().Be("password");
            redisConfig.SslHost.Should().Be("sslHost");
            redisConfig.KeyspaceNotificationsEnabled.Should().Be(true);
            redisConfig.StrictCompatibilityModeVersion.Should().Be("2.5");
            redisConfig.TwemproxyEnabled.Should().BeTrue();
        }

        [Fact]
        public void Configuration_Redis_ConnectionString()
        {
            var key = Guid.NewGuid().ToString();
            var data = new Dictionary<string, string>
            {
                {"redis:1:connectionString", "localhost,allowAdmin=true,proxy=Twemproxy"},
                {"redis:1:key", key},
                {"redis:1:strictCompatibilityModeVersion", "2.5" },
                {"redis:1:keyspaceNotificationsEnabled", "TRUE"},
                {"redis:1:database", "101"},
            };

            GetConfiguration(data).LoadRedisConfigurations();
            var redisConfig = RedisConfigurations.GetConfiguration(key);
            redisConfig.Key.Should().Be(key);
            redisConfig.AllowAdmin.Should().BeTrue();
            redisConfig.StrictCompatibilityModeVersion.Should().Be("2.5");
            redisConfig.TwemproxyEnabled.Should().BeTrue();
            redisConfig.Database.Should().Be(101);
        }

        private static IConfigurationRoot GetConfiguration(IDictionary<string, string> data)
        {
            var configurationBuilder = new Microsoft.Extensions.Configuration.ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(data);
            return configurationBuilder.Build();
        }
    }
}