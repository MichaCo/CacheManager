﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CacheManager.Core;
using CacheManager.Core.Internal;
using FluentAssertions;
using Xunit;

namespace CacheManager.Tests
{
    /// <summary>
    /// Validates that add and put adds a new item to all handles defined. Validates that remove
    /// removes an item from all handles defined.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class CacheManagerSimpleTests : IClassFixture<RedisTestFixture>
    {
        private static object runLock = new object();

        #region general

        [Fact]
        public void CacheManager_NullableTypes_ShouldNotAllowNulls()
        {
            var manager = new BaseCacheManager<DateTime?>(CacheConfigurationBuilder.BuildConfiguration(s => s.WithDictionaryHandle()));

            DateTime? value = new Nullable<DateTime>();
            Assert.Null(value);
            Action act = () => manager.Add(Guid.NewGuid().ToString(), value);

            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Equals("value");
        }

        [Fact]
        public void CacheManager_AddCacheItem_WithExpMode_ButWithoutTimeout()
        {
            // arrange
            var cache = TestManagers.WithManyDictionaryHandles;
            var key = "key";

            // act
            Action act = () => cache.Add(new CacheItem<object>(key, "something", ExpirationMode.Absolute, default(TimeSpan)));

            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithMessage("Expiration timeout must be greater than zero*");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_CtorA_NoConfig()
        {
            Action act = () => new BaseCacheManager<object>(null);
            act.Should().Throw<ArgumentException>()
                .And.ParamName.Equals("configuration");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_CtorA_ConfigNoName()
        {
            // name should be set from config and default is a Guid
            var manager = new BaseCacheManager<object>(CacheConfigurationBuilder.BuildConfiguration(s => s.WithDictionaryHandle()));
            manager.Name.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_CtorA_ConfigWithName()
        {
            // name should be implicitly set
            var manager = new BaseCacheManager<object>(
                CacheConfigurationBuilder.BuildConfiguration("newName", s => s.WithDictionaryHandle()));

            manager.Name.Should().Be("newName");
        }

        #endregion general

        #region exists

        [Theory]
        [ReplaceCulture]
        [ClassData(typeof(TestCacheManagers))]
        public void CacheManager_Exists_InvalidKey(ICacheManager<object> cache)
        {
            using (cache)
            {
                // arrange act
                Action act = () => cache.Exists(null);
                Action actB = () => cache.Exists(null, "region");
                Action actR = () => cache.Exists("key", null);

                // assert
                act.Should().Throw<ArgumentException>(cache.Configuration.ToString())
                    .And.ParamName.Equals("key");

                actB.Should().Throw<ArgumentException>(cache.ToString())
                    .And.ParamName.Equals("key");

                actR.Should().Throw<ArgumentException>(cache.ToString())
                    .And.ParamName.Equals("region");
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        public void CacheManager_Exists_KeyDoesExist(ICacheManager<object> cache)
        {
            using (cache)
            {
                // arrange
                var key = Guid.NewGuid().ToString();
                var value = ComplexType.Create();

                // act
                cache.Add(key, value);

                // assert
                cache.Exists(key).Should().BeTrue(cache.Configuration.ToString());
                cache.Get(key).Should().Be(value, cache.Configuration.ToString());
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        public void CacheManager_Exists_KeyRegionDoesExist(ICacheManager<object> cache)
        {
            using (cache)
            {
                // arrange
                var key = Guid.NewGuid().ToString();
                var region = Guid.NewGuid().ToString();
                var value = ComplexType.Create();

                // act
                cache.Add(key, value, region);

                // assert
                cache.Exists(key, region).Should().BeTrue(cache.Configuration.ToString());
                cache.Get(key, region).Should().Be(value, cache.Configuration.ToString());
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        public void CacheManager_Exists_KeyDoesNotExist(ICacheManager<object> cache)
        {
            using (cache)
            {
                // arrange
                var key = Guid.NewGuid().ToString();

                // act
                // assert
                cache.Exists(key).Should().BeFalse(cache.Configuration.ToString());
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        public void CacheManager_Exists_KeyRegionDoesNotExist(ICacheManager<object> cache)
        {
            using (cache)
            {
                // arrange
                var key = Guid.NewGuid().ToString();
                var region = Guid.NewGuid().ToString();

                // act
                // assert
                cache.Exists(key, region).Should().BeFalse(cache.Configuration.ToString());
            }
        }

        #endregion

        #region put params validation

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Put_InvalidKey()
        {
            using (var cache = CacheFactory.Build(settings =>
            {
                settings.WithDictionaryHandle("h1");
            }))
            {
                // arrange act
                Action act = () => cache.Put(null, null);
                Action actR = () => cache.Put(null, null, null);

                // assert
                act.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("key");

                actR.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("key");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Put_InvalidValue()
        {
            using (var cache = CacheFactory.Build(settings =>
                {
                    settings.WithDictionaryHandle("h1");
                }))
            {
                // arrange act
                Action act = () => cache.Put("key", null);
                Action actR = () => cache.Put("key", null, null);

                // assert
                act.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("value");

                actR.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("value");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Put_InvalidCacheItem()
        {
            using (var cache = CacheFactory.Build(settings =>
                {
                    settings.WithDictionaryHandle("h1");
                }))
            {
                // act
                Action act = () => cache.Put(null);

                // assert
                act.Should().Throw<ArgumentNullException>()
                    .And.ParamName.Equals("item");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Put_InvalidRegion()
        {
            using (var cache = CacheFactory.Build(settings =>
            {
                settings.WithDictionaryHandle("h1");
            }))
            {
                // act
                Action act = () => cache.Put("key", "value", null);

                // assert
                act.Should().Throw<ArgumentNullException>()
                    .And.ParamName.Equals("region");
            }
        }

        [Theory]
        [InlineData(new[] { 12345 })]
        [InlineData("something")]
        [InlineData(true)]
        [InlineData(0.223f)]
        public void CacheManager_Put_CacheItem_Positive<T>(T value)
        {
            using (var cache = CacheFactory.Build(settings =>
                {
                    settings.WithDictionaryHandle("h1");
                }))
            {
                // arrange
                var key = "my key";
                var item = new CacheItem<object>(key, value);
                var itemRegion = new CacheItem<object>(key, "region", value);

                // act
                Action act = () => cache.Put(item);
                Action actRegion = () => cache.Put(itemRegion);

                // assert
                act.Should().NotThrow();
                actRegion.Should().NotThrow();
                cache.Get(key).Should().Be(value);
                cache.Get(key, "region").Should().Be(value);
            }
        }

        [Theory]
        [InlineData(12345)]
        [InlineData("something")]
        [InlineData(true)]
        [InlineData(0.223f)]
        public void CacheManager_Put_KeyValue_Positive<T>(T value)
        {
            using (var cache = CacheFactory.Build(settings =>
                {
                    settings.WithDictionaryHandle("h1");
                }))
            {
                // arrange
                var key = "my key";

                // act
                Action act = () => cache.Put(key, value);
                Action actRegion = () => cache.Put(key, value, "region");

                // assert
                act.Should().NotThrow();
                actRegion.Should().NotThrow();
                cache.Get(key).Should().Be(value);
                cache.Get(key, "region").Should().Be(value);
            }
        }

        #endregion put params validation

        #region update call validation

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Update_InvalidKey()
        {
            using (var cache = CacheFactory.Build(settings =>
            {
                settings.WithDictionaryHandle("h1");
            }))
            {
                // arrange act
                Action act = () => cache.Update(null, null);
                Action actR = () => cache.Update(null, "r", null);
                Action actU = () => cache.Update(null, (o) => o, 33);
                Action actRU = () => cache.Update(null, null, null, 33);

                object val = null;
                Action actT = () => cache.TryUpdate(null, null, out val);
                Action actTR = () => cache.TryUpdate(null, "r", null, out val);
                Action actTU = () => cache.TryUpdate(null, (o) => o, 33, out val);
                Action actTRU = () => cache.TryUpdate(null, null, null, 33, out val);

                // assert
                act.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("key");

                actR.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("key");

                actU.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("key");

                actRU.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("key");

                actT.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("key");

                actTR.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("key");

                actTU.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("key");

                actTRU.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("key");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Update_InvalidUpdateFunc()
        {
            using (var cache = CacheFactory.Build(settings =>
            {
                settings.WithDictionaryHandle("h1");
            }))
            {
                // arrange act
                Action act = () => cache.Update("key", null);
                Action actR = () => cache.Update("key", "region", null);
                Action actU = () => cache.Update("key", null, 33);
                Action actRU = () => cache.Update("key", "region", null, 33);

                object val = null;
                Action actT = () => cache.TryUpdate("key", null, out val);
                Action actTR = () => cache.TryUpdate("key", "r", null, out val);
                Action actTU = () => cache.TryUpdate("key", null, 33, out val);
                Action actTRU = () => cache.TryUpdate("key", "r", null, 33, out val);

                // assert
                act.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("updateValue");

                actR.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("updateValue");

                actU.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("updateValue");

                actRU.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("updateValue");

                actT.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("updateValue");

                actTR.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("updateValue");

                actTU.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("updateValue");

                actTRU.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("updateValue");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Update_InvalidRegion()
        {
            using (var cache = CacheFactory.Build(settings =>
            {
                settings.WithDictionaryHandle("h1");
            }))
            {
                // arrange act
                Action actR = () => cache.Update("key", null, a => a);
                Action actRU = () => cache.Update("key", null, a => a, 33);

                object val = null;
                Action actTR = () => cache.TryUpdate("key", null, null, out val);
                Action actTRU = () => cache.TryUpdate("key", null, null, 33, out val);

                // assert
                actR.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("region");

                actRU.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("region");

                actTR.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("region");

                actTRU.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("region");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Update_InvalidConfig()
        {
            using (var cache = CacheFactory.Build(settings =>
            {
                settings.WithDictionaryHandle("h1");
            }))
            {
                // arrange act
                Action act = () => cache.Update("key", a => a, -1);
                Action actR = () => cache.Update("key", "region", a => a, -1);

                object val = null;
                Action actTU = () => cache.TryUpdate("key", a => a, -1, out val);
                Action actTRU = () => cache.TryUpdate("key", "region", a => a, -1, out val);

                // assert
                act.Should().Throw<InvalidOperationException>()
                    .WithMessage("*retries must be greater than*");

                actR.Should().Throw<InvalidOperationException>()
                    .WithMessage("*retries must be greater than*");

                actTU.Should().Throw<InvalidOperationException>()
                    .WithMessage("*retries must be greater than*");

                actTRU.Should().Throw<InvalidOperationException>()
                    .WithMessage("*retries must be greater than*");
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        public void CacheManager_Update_ItemNotAdded<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var key = Guid.NewGuid().ToString();

                // act
                Action act = () => cache.Update(key, item => item);
                Action actR = () => cache.Update(key, "region", item => item);

                object value;
                Func<bool> act2 = () => cache.TryUpdate(key, item => item, out value);
                Func<bool> act2R = () => cache.TryUpdate(key, "region", item => item, out value);

                // assert
                act.Should().Throw<InvalidOperationException>("*failed*");
                actR.Should().Throw<InvalidOperationException>("*failed*");
                act2().Should().BeFalse("Item has not been added to the cache");
                act2R().Should().BeFalse("Item has not been added to the cache");
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        public void CacheManager_Update_ValueFactoryReturnsNull<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var key = Guid.NewGuid().ToString();
                var region = Guid.NewGuid().ToString();

                cache.Add(key, "value");
                cache.Add(key, "value", region);

                // act
                Action act = () => cache.Update(key, (v) => null);
                Action actR = () => cache.Update(key, region, (v) => null);
                Action actU = () => cache.Update(key, (v) => null, 33);
                Action actRU = () => cache.Update(key, region, (v) => null, 33);

                object val = null;
                Func<bool> actT = () => cache.TryUpdate(key, (v) => null, out val);
                Func<bool> actTR = () => cache.TryUpdate(key, region, (v) => null, out val);
                Func<bool> actTU = () => cache.TryUpdate(key, (v) => null, 33, out val);
                Func<bool> actTRU = () => cache.TryUpdate(key, region, (v) => null, 33, out val);

                // assert
                act.Should().Throw<InvalidOperationException>().WithMessage("*value factory returned null*");
                actR.Should().Throw<InvalidOperationException>().WithMessage("*value factory returned null*");
                actU.Should().Throw<InvalidOperationException>().WithMessage("*value factory returned null*");
                actRU.Should().Throw<InvalidOperationException>().WithMessage("*value factory returned null*");

                actT().Should().BeFalse();
                actTR().Should().BeFalse();
                actTU().Should().BeFalse();
                actTRU().Should().BeFalse();
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        public void CacheManager_Update_Simple<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var key = Guid.NewGuid().ToString();
                var region = Guid.NewGuid().ToString();
                cache.Add(key, "something");
                cache.Add(key, "something", region);

                // act
                Func<object> act = () => cache.Update(key, item => item + " more");
                Func<object> actR = () => cache.Update(key, region, item => item + " more");

                object value = string.Empty;
                object value2 = string.Empty;
                Func<bool> actT = () => cache.TryUpdate(key, item => item + " awesome", out value);
                Func<bool> actTR = () => cache.TryUpdate(key, region, item => item + " awesome", out value2);
                Func<string> act2 = () => cache.Get<string>(key);

                // assert
                act().Should().Be("something more");
                actR().Should().Be("something more");
                actT().Should().BeTrue();
                actTR().Should().BeTrue();
                value.Should().Be("something more awesome");
                value2.Should().Be("something more awesome");
                act2().Should().Be("something more awesome");
            }
        }

        #endregion update call validation

        #region add or update call validation

        [Fact]
        [ReplaceCulture]
        public void CacheManager_AddOrUpdate_InvalidKey()
        {
            using (var cache = CacheFactory.Build(settings =>
            {
                settings.WithDictionaryHandle("h1");
            }))
            {
                // arrange act
                Action act = () => cache.AddOrUpdate(null, null, (o) => o);
                Action actR = () => cache.AddOrUpdate(null, "r", null, (o) => o);
                Action actU = () => cache.AddOrUpdate(null, null, (o) => o, 33);
                Action actRU = () => cache.AddOrUpdate(null, "r", null, null, 33);

                // assert
                act.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("key");

                actR.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("key");

                actU.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("key");

                actRU.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("key");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_AddOrUpdate_InvalidUpdateFunc()
        {
            using (var cache = CacheFactory.Build(settings =>
            {
                settings.WithDictionaryHandle("h1");
            }))
            {
                // arrange act
                Action act = () => cache.AddOrUpdate("key", "value", null);
                Action actR = () => cache.AddOrUpdate("key", "region", "value", null);
                Action actU = () => cache.AddOrUpdate("key", "value", null, 1);
                Action actRU = () => cache.AddOrUpdate("key", "region", "value", null, 1);
                Action actI = () => cache.AddOrUpdate(new CacheItem<object>("k", "v"), null);
                Action actIU = () => cache.AddOrUpdate(new CacheItem<object>("k", "v"), null, 1);

                // assert
                act.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("updateValue");

                actR.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("updateValue");

                actU.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("updateValue");

                actRU.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("updateValue");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_AddOrUpdate_InvalidRegion()
        {
            using (var cache = CacheFactory.Build(settings =>
            {
                settings.WithDictionaryHandle("h1");
            }))
            {
                // arrange act
                Action actR = () => cache.AddOrUpdate("key", null, "value", a => a);
                Action actRU = () => cache.AddOrUpdate("key", null, "value", a => a, 1);

                // assert
                actR.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("region");

                actRU.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("region");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_AddOrUpdate_InvalidConfig()
        {
            using (var cache = CacheFactory.Build(settings =>
            {
                settings.WithDictionaryHandle("h1");
            }))
            {
                // arrange act
                Action actU = () => cache.AddOrUpdate("key", "value", (o) => o, -1);
                Action actRU = () => cache.AddOrUpdate("key", "region", "value", (o) => o, -1);
                Action actIU = () => cache.AddOrUpdate(new CacheItem<object>("k", "v"), (o) => o, -1);

                // assert
                actU.Should().Throw<InvalidOperationException>()
                    .WithMessage("*retries must be greater than*");

                actRU.Should().Throw<InvalidOperationException>()
                    .WithMessage("*retries must be greater than*");

                actIU.Should().Throw<InvalidOperationException>()
                    .WithMessage("*retries must be greater than*");
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        public void CacheManager_AddOrUpdate_ItemNotAdded<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var key = Guid.NewGuid().ToString();
                object value = "value";

                // act
                Func<object> act = () => cache.AddOrUpdate(key, value, item => "not this value");

                // assert
                act().Should().Be(value, $"{key} {value} {cache}");

                var addCalls = cache.CacheHandles.Select(h => h.Stats.GetStatistic(CacheStatsCounterType.AddCalls)).Sum();
                addCalls.Should().Be(1, "Item should be added to last handle only");
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        public void CacheManager_AddOrUpdate_Update_Simple<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var key = Guid.NewGuid().ToString();
                cache.Add(key, "something");

                // act
                Func<object> act = () => cache.AddOrUpdate(key, "does exist", item =>
                {
                    item.Should().Be("something");
                    return item + " more";
                });
                Func<string> act2 = () => cache.Get<string>(key);

                // assert
                act().Should().Be("something more");
                act2().Should().Be("something more");
            }
        }

        #endregion add or update call validation

        #region get or add

        // validates #268
        [Fact]
        public void CacheManager_GetOrAdd_Concurrent_SameKey()
        {
            var rnd = new Random();
            var cache = CacheFactory.Build<object>(settings => settings
                .WithMicrosoftMemoryCacheHandle());

            CacheItem<object> GenerateValue(string key)
            {
                Thread.Sleep(400);
                return new CacheItem<object>(key, $"{rnd.Next()} {DateTime.Now.ToLongTimeString()} : HALLO WORLD FOR " + key);
            }

            object v1 = null;
            object v2 = null;

            const string sameKey = "Test 1 ";

            Parallel.Invoke(
                () => { v1 = cache.GetOrAddCacheItem(sameKey, GenerateValue).Value; },
                () => { v2 = cache.GetOrAddCacheItem(sameKey, GenerateValue).Value; }
            );

            Assert.True(v1 == v2); // FAILS
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_GetOrAdd_InvalidKey()
        {
            using (var cache = CacheFactory.Build(settings =>
            {
                settings.WithDictionaryHandle("h1");
            }))
            {
                // arrange act
                Action actA = () => cache.GetOrAdd(null, "value");
                Action actB = () => cache.GetOrAdd(null, "region", "value");
                Action actC = () => cache.GetOrAdd(null, (k) => "value");
                Action actD = () => cache.GetOrAdd(null, "region", (k, r) => "value");
                Action actE = () => cache.GetOrAddCacheItem(null, (k) => new CacheItem<object>(k, "value"));
                Action actF = () => cache.GetOrAddCacheItem(null, "region", (k, r) => new CacheItem<object>(k, "value"));

                // assert
                actA.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("key");

                actB.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("key");

                actC.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("key");

                actD.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("key");

                actE.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("key");

                actF.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("key");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_TryGetOrAdd_InvalidKey()
        {
            using (var cache = CacheFactory.Build(settings =>
            {
                settings.WithDictionaryHandle("h1");
            }))
            {
                // arrange act
                object val;
                Action actC = () => cache.TryGetOrAdd(null, (k) => "value", out val);
                Action actD = () => cache.TryGetOrAdd(null, "region", (k, r) => "value", out val);
                Action actE = () => cache.TryGetOrAdd(null, (k) => new CacheItem<object>(k, "value"), out val);
                Action actF = () => cache.TryGetOrAdd(null, "region", (k, r) => new CacheItem<object>(k, "value"), out val);

                // assert
                actC.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("key");

                actD.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("key");

                actE.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("key");

                actF.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("key");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_GetOrAdd_InvalidRegion()
        {
            using (var cache = CacheFactory.Build(settings =>
            {
                settings.WithDictionaryHandle("h1");
            }))
            {
                // arrange act
                Action actA = () => cache.GetOrAdd("key", " ", "value");
                Action actB = () => cache.GetOrAdd("key", null, (k, r) => "value");

                // assert
                actA.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("region");

                actB.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("region");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_TryGetOrAdd_InvalidRegion()
        {
            using (var cache = CacheFactory.Build(settings =>
            {
                settings.WithDictionaryHandle("h1");
            }))
            {
                // arrange act
                object val;
                Action actB = () => cache.TryGetOrAdd("key", null, (k, r) => "value", out val);

                // assert
                actB.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("region");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_GetOrAdd_InvalidFactory()
        {
            using (var cache = CacheFactory.Build(settings =>
            {
                settings.WithDictionaryHandle("h1");
            }))
            {
                // arrange act
                Action actA = () => cache.GetOrAddCacheItem("key", null);
                Action actB = () => cache.GetOrAddCacheItem("key", "region", null);

                // assert
                actA.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("valueFactory");

                actB.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("valueFactory");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_TryGetOrAdd_InvalidFactory()
        {
            using (var cache = CacheFactory.Build(settings =>
            {
                settings.WithDictionaryHandle("h1");
            }))
            {
                // arrange act
                object val;
                Action actA = () => cache.TryGetOrAdd("key", null, out val);
                Action actB = () => cache.TryGetOrAdd("key", "region", null, out val);

                // assert
                actA.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("valueFactory");

                actB.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("valueFactory");
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        public void CacheManager_GetOrAdd_SimpleAdd<T>(T cache)
            where T : ICacheManager<object>
        {
            // arrange
            var key = Guid.NewGuid().ToString();
            var keyF = Guid.NewGuid().ToString();
            var keyG = Guid.NewGuid().ToString();
            var region = Guid.NewGuid().ToString();
            var val = Guid.NewGuid().ToString();

            using (cache)
            {
                // act
                cache.GetOrAdd(key, val);
                cache.GetOrAdd(key, region, val);
                cache.GetOrAdd(keyF, (k) => val);
                cache.GetOrAdd(keyF, region, (k, r) => val);
                cache.GetOrAddCacheItem(keyG, (k) => new CacheItem<object>(keyG, val));
                cache.GetOrAddCacheItem(keyG, region, (k, r) => new CacheItem<object>(keyG, region, val, ExpirationMode.Absolute, TimeSpan.FromMinutes(42)));

                // assert
                cache[key].Should().Be(val);
                cache[key, region].Should().Be(val);
                cache[keyF].Should().Be(val);
                cache[keyF, region].Should().Be(val);
                cache[keyG].Should().Be(val);
                cache[keyG, region].Should().Be(val);
                var item = cache.GetCacheItem(keyG, region);
                item.ExpirationMode.Should().Be(ExpirationMode.Absolute);
                item.ExpirationTimeout.Should().Be(TimeSpan.FromMinutes(42));
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        public void CacheManager_TryGetOrAdd_SimpleAdd<T>(T cache)
            where T : ICacheManager<object>
        {
            // arrange
            var key = Guid.NewGuid().ToString();
            var key2 = Guid.NewGuid().ToString();
            var val = Guid.NewGuid().ToString();
            var region = Guid.NewGuid().ToString();
            object valueA = null;
            object valueB = null;
            CacheItem<object> valueC = null;
            CacheItem<object> valueD = null;

            using (cache)
            {
                // act
                Func<bool> actA = () => cache.TryGetOrAdd(key, k => val, out valueA);
                Func<bool> actB = () => cache.TryGetOrAdd(key, region, (k, r) => val, out valueB);
                var valC = new CacheItem<object>(key2, val);
                Func<bool> actC = () => cache.TryGetOrAddCacheItem(key2, k => valC, out valueC);
                var valD = new CacheItem<object>(key2, region, val, ExpirationMode.Absolute, TimeSpan.FromMinutes(42));
                Func<bool> actD = () => cache.TryGetOrAddCacheItem(key2, region, (k, r) => valD, out valueD);

                // assert
                actA().Should().BeTrue();
                actB().Should().BeTrue();
                actC().Should().BeTrue();
                actD().Should().BeTrue();
                valueA.Should().Be(val);
                valueB.Should().Be(val);
                valueC.Should().Be(valC);
                valueD.Should().Be(valD);
                cache[key].Should().Be(val);
                cache[key, region].Should().Be(val);
                cache[key2].Should().Be(val);
                cache[key2, region].Should().Be(val);
                var item = cache.GetCacheItem(key2, region);
                item.ExpirationMode.Should().Be(ExpirationMode.Absolute);
                item.ExpirationTimeout.Should().Be(TimeSpan.FromMinutes(42));
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        public void CacheManager_GetOrAdd_FactoryReturnsNull<T>(T cache)
            where T : ICacheManager<object>
        {
            // arrange
            var key = Guid.NewGuid().ToString();

            using (cache)
            {
                // act
                Action act = () => cache.GetOrAddCacheItem(key, (k) => null);

                // assert
                act.Should().Throw<InvalidOperationException>("added");
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        public void CacheManager_TryGetOrAdd_FactoryReturnsNull<T>(T cache)
            where T : ICacheManager<object>
        {
            // arrange
            var key = Guid.NewGuid().ToString();

            using (cache)
            {
                // act
                object val = null;
                CacheItem<object> val2 = null;
                Func<bool> act = () => cache.TryGetOrAdd(key, (k) => null, out val);
                Func<bool> actB = () => cache.TryGetOrAddCacheItem(key, (k) => null, out val2);

                // assert
                act().Should().BeFalse();
                actB().Should().BeFalse();
                val.Should().BeNull();
                val2.Should().BeNull();
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        public void CacheManager_GetOrAdd_AddNull<T>(T cache)
            where T : ICacheManager<object>
        {
            // arrange
            var key = Guid.NewGuid().ToString();

            using (cache)
            {
                // act
                Action act = () => cache.GetOrAdd(key, (object)null);

                // assert
                act.Should().Throw<ArgumentNullException>("added");
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        public void CacheManager_GetOrAdd_SimpleGet<T>(T cache)
            where T : ICacheManager<object>
        {
            // arrange
            var key = Guid.NewGuid().ToString();
            var keyF = Guid.NewGuid().ToString();
            var region = Guid.NewGuid().ToString();
            var val = Guid.NewGuid().ToString();
            Func<string, object> add = (k) => { throw new InvalidOperationException(); };
            Func<string, string, object> addRegion = (k, r) => { throw new InvalidOperationException(); };

            using (cache)
            {
                cache.Add(key, val);
                cache.Add(key, val, region);
                cache.Add(keyF, val);
                cache.Add(keyF, val, region);

                // act
                var result = cache.GetOrAdd(key, val);
                var resultB = cache.GetOrAdd(key, region, val);
                var resultC = cache.GetOrAddCacheItem(key, (k) => new CacheItem<object>(key, val));
                var resultD = cache.GetOrAddCacheItem(key, region, (k, r) => new CacheItem<object>(key, val));
                Action act = () => cache.GetOrAdd(keyF, add);
                Action actB = () => cache.GetOrAdd(keyF, region, addRegion);

                // assert
                result.Should().Be(val);
                resultB.Should().Be(val);
                resultC.Value.Should().Be(val);
                resultD.Value.Should().Be(val);
                act.Should().NotThrow();
                actB.Should().NotThrow();
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        public void CacheManager_TryGetOrAdd_SimpleGet<T>(T cache)
            where T : ICacheManager<object>
        {
            // arrange
            var key = Guid.NewGuid().ToString();
            var val = Guid.NewGuid().ToString();
            var region = Guid.NewGuid().ToString();

            // the factories should not get invoked because the item exists.
            Func<string, object> add = (k) => { throw new InvalidOperationException(); };
            Func<string, string, object> addRegion = (k, r) => { throw new InvalidOperationException(); };
            object resultA = null;
            object resultB = null;
            object resultC = null;

            using (cache)
            {
                cache.Add(key, val);
                cache.Add(key, val, region);
                var cacheItem = new CacheItem<object>(key, val);
                cache.Add(cacheItem);

                // act
                Func<bool> actA = () => cache.TryGetOrAdd(key, add, out resultA);
                Func<bool> actB = () => cache.TryGetOrAdd(key, region, addRegion, out resultB);
                Func<bool> actC = () => cache.TryGetOrAdd(key, region, (k, r) => cacheItem, out resultC);

                // assert
                actA().Should().BeTrue();
                actB().Should().BeTrue();
                actC().Should().BeTrue();
                resultA.Should().Be(val);
                resultB.Should().Be(val);
                resultC.Should().Be(val);
            }
        }

        [Theory()]
        [Trait("category", "Unreliable")]
        [ClassData(typeof(TestCacheManagers))]
        public async Task CacheManager_GetOrAdd_ForceRace<T>(T cache)
            where T : ICacheManager<object>
        {
            // arrange
            var key = Guid.NewGuid().ToString();
            var val = Guid.NewGuid().ToString();
            var counter = 0;
            var runs = 6;

            using (cache)
            {
                Func<CacheItem<object>> action = () =>
                {
                    var tries = 0;
                    var created = cache.GetOrAddCacheItem(key, (k) =>
                    {
                        tries++;
                        Interlocked.Increment(ref counter);

                        // force collision so that multiple threads try to add... yea thats long, but parallel should be fine
                        Task.Delay(1).Wait();
                        return new CacheItem<object>(k, counter, ExpirationMode.Absolute, TimeSpan.FromMinutes(tries));
                    });

                    cache.Remove(key);
                    return created;
                };

                var tasks = new List<Task<CacheItem<object>>>();
                for (var i = 0; i < runs; i++)
                {
                    tasks.Add(Task.Run(action));
                }

                var results = await Task.WhenAll(tasks.ToArray());

                await Task.Delay(0);

                // tries inside the factory counts how often the factory is being called, then we use that value as timeout
                // should be one as the factory should run only once
                results.Max(p => p.ExpirationTimeout.Minutes).Should().Be(1);

                // even with retries, the factory should not get invoked more than once per call!
                counter.Should().BeLessOrEqualTo(runs);
            }
        }

        [Theory()]
        [Trait("category", "Unreliable")]
        [ClassData(typeof(TestCacheManagers))]
        public async Task CacheManager_TryGetOrAdd_ForceRace<T>(T cache)
            where T : ICacheManager<object>
        {
            // arrange
            var key = Guid.NewGuid().ToString();
            var val = Guid.NewGuid().ToString();
            var counter = 0;
            var runs = 6;

            using (cache)
            {
                Func<CacheItem<object>> action = () =>
                {
                    var tries = 0;
                    CacheItem<object> result = null;
                    while (!cache.TryGetOrAddCacheItem(
                        key, (k) =>
                        {
                            tries++;
                            Interlocked.Increment(ref counter);

                            // force collision so that multiple threads try to add... yea thats long, but parallel should be fine
                            Task.Delay(1).Wait();
                            return new CacheItem<object>(k, counter, ExpirationMode.Absolute, TimeSpan.FromMinutes(tries));
                        },
                        out result))
                    { }

                    cache.Remove(key);
                    return result;
                };

                var tasks = new List<Task<CacheItem<object>>>();
                for (var i = 0; i < runs; i++)
                {
                    tasks.Add(Task.Run(action));
                }

                var results = await Task.WhenAll(tasks.ToArray());

                await Task.Delay(0);

                // tries inside the factory counts how often the factory is being called, then we use that value as timeout
                // should be one as the factory should run only once
                results.Max(p => p.ExpirationTimeout.Minutes).Should().Be(1);

                // even with retries, the factory should not get invoked more than once per call!
                counter.Should().BeLessOrEqualTo(runs);
            }
        }

        #endregion get or add

        #region Add validation

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Add_InvalidKey()
        {
            // arrange
            using (var cache = CacheFactory.Build(settings =>
                {
                    settings.WithDictionaryHandle("h1");
                }))
            {
                // act
                Action act = () => cache.Add(null, null);
                Action actR = () => cache.Add(null, null, null);

                // assert
                act.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("key");

                actR.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("key");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Add_InvalidValue()
        {
            // arrange
            using (var cache = CacheFactory.Build(
                settings =>
                {
                    settings.WithDictionaryHandle("h1");
                }))
            {
                // act
                Action act = () => cache.Add("key", null);
                Action actR = () => cache.Add("key", null, "region");

                // assert
                act.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("value");

                actR.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("value");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Add_InvalidRegion()
        {
            // arrange
            using (var cache = CacheFactory.Build(
                settings =>
                {
                    settings.WithDictionaryHandle("h1");
                }))
            {
                // act
                Action actR = () => cache.Add("key", "value", null);

                // assert
                actR.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("region");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Add_InvalidCacheItem()
        {
            // arrange
            using (var cache = CacheFactory.Build(settings =>
                {
                    settings.WithDictionaryHandle("h1");
                }))
            {
                // act
                Action act = () => cache.Add(null);

                // assert
                act.Should().Throw<ArgumentNullException>()
                    .And.ParamName.Equals("item");
            }
        }

        [Theory]
        [InlineData(12345)]
        [InlineData("something")]
        [InlineData(true)]
        [InlineData(0.223f)]
        public void CacheManager_Add_CacheItem_Positive<T>(T value)
        {
            // arrange
            using (var cache = CacheFactory.Build(settings =>
                {
                    settings.WithDictionaryHandle("h1");
                }))
            {
                var key = "my key";
                var item = new CacheItem<object>(key, value);

                // act
                Action act = () => cache.Add(item);

                // assert
                act.Should().NotThrow();
                cache.Get(key).Should().Be(value);
            }
        }

        [Theory]
        [InlineData(12345)]
        [InlineData("something")]
        [InlineData(true)]
        [InlineData(0.223f)]
        public void CacheManager_Add_KeyValue_Positive<T>(T value)
        {
            // arrange
            using (var cache = CacheFactory.Build(settings =>
                {
                    settings.WithDictionaryHandle("h1");
                }))
            {
                var key = "my key";

                // act
                Action act = () => cache.Add(key, value);

                // assert
                act.Should().NotThrow();
                cache.Get(key).Should().Be(value);
            }
        }

        #endregion Add validation

        #region get validation

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Get_InvalidKey()
        {
            // arrange
            using (var cache = CacheFactory.Build(settings =>
            {
                settings.WithDictionaryHandle("h1");
            }))
            {
                // act
                Action act = () => cache.Get(null);
                Action actR = () => cache.Get(null, "region");

                // assert
                act.Should().Throw<ArgumentNullException>()
                    .And.ParamName.Equals("key");

                actR.Should().Throw<ArgumentNullException>()
                    .And.ParamName.Equals("key");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Get_InvalidRegion()
        {
            // arrange
            using (var cache = CacheFactory.Build(settings =>
            {
                settings.WithDictionaryHandle("h1");
            }))
            {
                // act
                Action act = () => cache.Get("key", null);

                // assert
                act.Should().Throw<ArgumentNullException>()
                    .And.ParamName.Equals("region");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_GetItem_InvalidKey()
        {
            // arrange
            using (var cache = CacheFactory.Build(settings =>
            {
                settings.WithDictionaryHandle("h1");
            }))
            {
                // act
                Action act = () => cache.GetCacheItem(null);
                Action actR = () => cache.GetCacheItem(null, "region");

                // assert
                act.Should().Throw<ArgumentNullException>()
                    .And.ParamName.Equals("key");

                actR.Should().Throw<ArgumentNullException>()
                    .And.ParamName.Equals("key");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_GetItem_InvalidRegion()
        {
            // arrange
            using (var cache = CacheFactory.Build(settings =>
            {
                settings.WithDictionaryHandle("h1");
            }))
            {
                // act
                Action act = () => cache.GetCacheItem("key", null);

                // assert
                act.Should().Throw<ArgumentNullException>()
                    .And.ParamName.Equals("region");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_GetT_InvalidKey()
        {
            // arrange
            using (var cache = CacheFactory.Build(settings =>
            {
                settings.WithDictionaryHandle("h1");
            }))
            {
                // act
                Action act = () => cache.Get<string>(null);
                Action actR = () => cache.Get<string>(null, "region");

                // assert
                act.Should().Throw<ArgumentNullException>()
                    .And.ParamName.Equals("key");

                actR.Should().Throw<ArgumentNullException>()
                    .And.ParamName.Equals("key");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_GetT_InvalidRegion()
        {
            // arrange
            using (var cache = CacheFactory.Build(settings =>
            {
                settings.WithDictionaryHandle("h1");
            }))
            {
                // act
                Action act = () => cache.Get<string>("key", null);

                // assert
                act.Should().Throw<ArgumentNullException>()
                    .And.ParamName.Equals("region");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Get_KeyNotAvailable()
        {
            // arrange
            using (var cache = CacheFactory.Build(settings =>
                {
                    settings.WithDictionaryHandle("h1");
                }))
            {
                string key = "some key";

                // act
                Func<object> act = () => cache.Get(key);

                // assert
                act().Should().BeNull("no object added");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_GetAdd_Positive()
        {
            // arrange
            using (var cache = CacheFactory.Build(settings =>
                {
                    settings.WithDictionaryHandle("h1");
                }))
            {
                string key = "some key";
                string value = "some value";

                // act
                Func<bool> actAdd = () => cache.Add(key, value);
                Func<object> actGet = () => cache.Get(key);

                // assert
                actAdd().Should().BeTrue("the cache should add the key/value");
                actGet().Should()
                    .NotBeNull("object was added")
                    .And.Be(value);
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_GetCacheItem_Positive()
        {
            // arrange
            using (var cache = CacheFactory.Build(settings =>
            {
                settings.WithDictionaryHandle("h1");
            }))
            {
                string key = "some key";
                string value = "some value";

                // act
                Func<bool> actAdd = () => cache.Add(key, value);
                Func<CacheItem<object>> actGet = () => cache.GetCacheItem(key);

                // assert
                actAdd().Should().BeTrue("the cache should add the key/value");
                actGet().Should()
                    .NotBeNull("object was added")
                    .And.Should().BeEquivalentTo(new { Key = key, Value = value }, p => p.ExcludingMissingMembers());
            }
        }

        #endregion get validation

        #region remove

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Remove_InvalidKey()
        {
            // arrange
            using (var cache = CacheFactory.Build(settings =>
                {
                    settings.WithDictionaryHandle("h1");
                }))
            {
                // act
                Action act = () => cache.Remove(null);
                Action actR = () => cache.Remove(null, "region");

                // assert
                act.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("key");

                actR.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("key");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Remove_InvalidRegion()
        {
            // arrange
            using (var cache = CacheFactory.Build(settings =>
            {
                settings.WithDictionaryHandle("h1");
            }))
            {
                // act
                Action act = () => cache.Remove("key", null);

                // assert
                act.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("region");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Remove_KeyEmpty()
        {
            // arrange
            using (var cache = CacheFactory.Build(settings =>
                {
                    settings.WithDictionaryHandle("h1");
                }))
            {
                string key = string.Empty;

                // act
                Action act = () => cache.Remove(key);

                // assert
                act.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("key");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Remove_KeyWhiteSpace()
        {
            // arrange
            using (var cache = CacheFactory.Build(settings =>
                {
                    settings.WithDictionaryHandle("h1");
                }))
            {
                string key = "                ";

                // act
                Action act = () => cache.Remove(key);

                // assert
                act.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("key");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Remove_KeyNotAvailable()
        {
            // arrange
            using (var cache = CacheFactory.Build(settings =>
                {
                    settings.WithDictionaryHandle("h1");
                }))
            {
                string key = "some key";

                // act
                Func<bool> act = () => cache.Remove(key);
                Func<bool> actR = () => cache.Remove(key, "region");

                // assert
                act().Should().BeFalse("key should not be present");
                actR().Should().BeFalse("key should not be present");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Remove_Positive()
        {
            // arrange
            using (var cache = CacheFactory.Build(settings =>
                {
                    settings.WithDictionaryHandle("h1");
                }))
            {
                string key = "some key";
                cache.Add(key, "something"); // add something to be removed

                // act
                var result = cache.Remove(key);
                var item = cache[key];

                // assert
                result.Should().BeTrue("key should be present");
                item.Should().BeNull();
            }
        }

        #endregion remove

        #region indexer

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Index_InvalidKey()
        {
            // arrange
            using (var cache = CacheFactory.Build(settings =>
                {
                    settings.WithDictionaryHandle("h1");
                }))
            {
                string key = null;

                // act
                object result;
                Action act = () => result = cache[key];
                Action actR = () => result = cache[key, "region"];

                // assert
                act.Should().Throw<ArgumentNullException>()
                    .And.ParamName.Equals("key");

                actR.Should().Throw<ArgumentNullException>()
                    .And.ParamName.Equals("key");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Index_Key_RegionEmpty()
        {
            // arrange
            using (var cache = CacheFactory.Build(settings =>
            {
                settings.WithDictionaryHandle("h1");
            }))
            {
                // act
                object result;
                Action act = () => result = cache["key", string.Empty];

                // assert
                act.Should().Throw<ArgumentException>()
                    .And.ParamName.Equals("region");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Index_KeyNotAvailable()
        {
            // arrange
            using (var cache = CacheFactory.Build(settings =>
                {
                    settings.WithDictionaryHandle("h1");
                }))
            {
                string key = "some key";

                // act
                Func<object> act = () => cache[key];

                // assert
                act().Should().BeNull("no object added for key");
            }
        }

        #endregion indexer

        #region testing empty handle list

        [Fact]
        public void CacheManager_NoCacheHandles()
        {
            // arrange
            // act
            Action act = () => new BaseCacheManager<string>(new CacheManagerConfiguration() { MaxRetries = 1000 });

            // assert
            act.Should().Throw<InvalidOperationException>().WithMessage("*no cache handles*");
        }

        #endregion testing empty handle list

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        [ReplaceCulture]
        public void CacheManager_CastGet_Region<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var key = Guid.NewGuid().ToString();
                var region = Guid.NewGuid().ToString();

                // act
                Func<bool> actA = () => cache.Add(key, "some value", region);
                Func<string> act = () => cache.Get<string>(key, region);

                // assert
                actA().Should().BeTrue();
                act().Should().Be("some value");
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        [ReplaceCulture]
        public void CacheManager_CastGet<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var keys = new List<string>() { Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
                var values = new List<object>()
                {
                    "string", 33293, 0.123f, 0.324d, 123311L, true,
                    new ComplexType() { Name = "name", SomeBool = false, SomeId = 213 }
                };

                // act
                PopulateCache(cache, keys, values, 1);
                object strSomething = cache.Get<string>(keys[0]);
                object someNumber = cache.Get<int>(keys[1]);
                object someFloating = cache.Get<float>(keys[2]);
                object someDoubling = cache.Get<double>(keys[3]);
                object someLonging = cache.Get<long>(keys[4]);
                object someBooling = cache.Get<bool>(keys[5]);
                object obj = cache.Get<ComplexType>(keys[6]);
                object someObject = cache.Get<object>("nonexistent");

                // assert
                ValidateCacheValues(cache, keys, values);
                strSomething.Should().BeEquivalentTo(values[0]);
                someNumber.Should().BeEquivalentTo(values[1]);
                someFloating.Should().BeEquivalentTo(values[2]);
                someDoubling.Should().BeEquivalentTo(values[3]);
                someLonging.Should().BeEquivalentTo(values[4]);
                someBooling.Should().BeEquivalentTo(values[5]);
                obj.Should().BeEquivalentTo(values[6]);
                someObject.Should().Be(null);
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        [ReplaceCulture]
        public void CacheManager_CastGet_ICanHazString<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                var key = Guid.NewGuid().ToString();

                // arrange
                cache.Add(key, 123456);

                // act
                var val = cache.Get<string>(key);

                // assert
                val.Should().Be("123456");
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        [ReplaceCulture]
        public void CacheManager_SimplePut<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var keys = new List<string>() { Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
                var values = new List<object>() { true, 234, "test string" };

                // act
                Action actPut = () =>
                {
                    PopulateCache(cache, keys, values, 0);
                };

                // assert
                actPut.Should().NotThrow();
                ValidateCacheValues(cache, keys, values);
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        public void CacheManager_SimpleAdd<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var keys = new List<string>() { Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
                var values = new List<object>() { true, 234, "test string" };

                // act
                Action actSet = () =>
                {
                    PopulateCache(cache, keys, values, 1);
                };

                // assert
                actSet.Should().NotThrow();
                ValidateCacheValues(cache, keys, values);
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        public void CacheManager_SimpleIndexPut<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var keys = new List<string>() { Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
                var values = new List<object>() { true, 234, "test string" };

                // act
                Action actSet = () =>
                {
                    PopulateCache(cache, keys, values, 2);
                };

                // assert
                actSet.Should().NotThrow();
                ValidateCacheValues(cache, keys, values);
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        public void CacheManager_SimpleRemove<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var keys = new List<string>() { Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
                var values = new List<object>() { true, 234, "test string" };
                var nulls = new List<object>() { null, null, null };

                // act
                PopulateCache(cache, keys, values, 0);

                for (var i = 0; i < keys.Count; i++)
                {
                    cache.Remove(keys[i]);
                }

                // assert
                ValidateCacheValues(cache, keys, nulls);
            }
        }

        [Fact]
        public void CacheManager_Clear_AllItemsRemoved()
        {
            // arrange act
            using (var cache = TestManagers.WithOneDicCacheHandle)
            {
                // arrange
                var key1 = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();

                // act
                cache.Add(key1, "value1");
                cache.Add(key2, "value2");
                cache.Clear();

                // assert
                cache[key1].Should().BeNull();
                cache[key2].Should().BeNull();
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        public void CacheManager_SimpleUpdate<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                if (cache.Configuration.UpdateMode == CacheUpdateMode.None)
                {
                    // skip for none because we want to test the update mode
                    return;
                }

                // arrange
                var keys = new List<string>() { Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
                var values = new List<object>() { 10, 20, 30 };
                var newValues = new List<object>() { 11, 21, 31 };

                // act
                Action actSet = () =>
                {
                    PopulateCache(cache, keys, values, 1);

                    foreach (var key in keys)
                    {
                        var result = cache.Update(key, item =>
                        {
                            int val = (int)item + 1;
                            return val;
                        });

                        var value = cache.Get(key);
                        value.Should().NotBeNull();
                    }
                };

                // assert
                actSet.Should().NotThrow();
                ValidateCacheValues(cache, keys, newValues);
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        public void CacheManager_IsCaseSensitive_Key<T>(T cache)
            where T : ICacheManager<object>
        {
            var key = "A" + Guid.NewGuid().ToString().ToUpper();
            using (cache)
            {
                cache.Add(key, "some value");

                var result = cache.Get(key.ToLower());

                result.Should().BeNull();
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        public void CacheManager_IsCaseSensitive_Region<T>(T cache)
            where T : ICacheManager<object>
        {
            var key = "A" + Guid.NewGuid().ToString().ToUpper();
            var region = "A" + Guid.NewGuid().ToString().ToUpper();
            using (cache)
            {
                cache.Add(key, "some value", region);

                var result = cache.Get(key, region.ToLower());

                result.Should().BeNull();
            }
        }

        private static void PopulateCache<T>(ICacheManager<T> cache, IList<string> keys, IList<T> values, int mode)
        {
            // let us make this safe per run so cache doesn't get cleared/populated from multiple tests
            lock (runLock)
            {
                foreach (var key in keys)
                {
                    cache.Remove(key);
                }

                for (int i = 0; i < values.Count; i++)
                {
                    var val = cache.Get(keys[i]);
                    if (val != null)
                    {
                        throw new InvalidOperationException("cache already contains this element");
                    }

                    if (mode == 0)
                    {
                        cache.Put(keys[i], values[i]);
                    }
                    else if (mode == 1)
                    {
                        cache.Add(keys[i], values[i]).Should().BeTrue();
                    }
                    else if (mode == 2)
                    {
                        cache[keys[i]] = values[i];
                    }
                }
            }
        }

        private static void ValidateCacheValues<T>(ICacheManager<T> cache, IList<string> keys, IList<T> values)
        {
            var cacheCfgText = cache.ToString();

            Debug.WriteLine("Validating for cache: " + cacheCfgText);
            values.Select((value, index) =>
            {
                var val = cache.Get(keys[index]);
                val.Should().Be(value, cacheCfgText)
                    .And.Be(cache[keys[index]], cacheCfgText);

                return cache.CacheHandles
                        .All(p =>
                        {
                            p.Get(keys[index])
                                .Should().Be(value, cacheCfgText)
                                .And.Be(p[keys[index]], cacheCfgText);
                            return true;
                        });
            }).ToList();
        }

        [Serializable]
        [ProtoBuf.ProtoContract]
        [Bond.Schema]
        public class ComplexType
        {
            public static ComplexType Create()
            {
                return new ComplexType()
                {
                    Name = Guid.NewGuid().ToString(),
                    SomeId = long.MaxValue,
                    SomeBool = true
                };
            }

            [ProtoBuf.ProtoMember(1)]
            [Bond.Id(1)]
            public string Name { get; set; }

            [ProtoBuf.ProtoMember(2)]
            [Bond.Id(2)]
            public long SomeId { get; set; }

            [ProtoBuf.ProtoMember(3)]
            [Bond.Id(3)]
            public bool SomeBool { get; set; }

            public override bool Equals(object obj)
            {
                var target = obj as ComplexType;
                if (target == null)
                {
                    return false;
                }

                return this.Name.Equals(target.Name) && this.SomeBool.Equals(target.SomeBool) && this.SomeId.Equals(target.SomeId);
            }

            public override int GetHashCode() => base.GetHashCode();
        }
    }
}
