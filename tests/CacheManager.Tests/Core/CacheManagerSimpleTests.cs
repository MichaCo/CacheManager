using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CacheManager.Core;
using CacheManager.Core.Cache;
using CacheManager.Core.Configuration;
using CacheManager.Tests.TestCommon;
using FluentAssertions;
using Xunit;

namespace CacheManager.Tests.Core
{
    /// <summary>
    /// Validates that add and put adds a new item to all handles defined.
    /// Validates that remove removes an item from all handles defined.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class CacheManagerSimpleTests : BaseCacheManagerTest
    {
        #region general

        [Fact]
        public void CacheManager_EmptyInitialization()
        {
            var cfg = ConfigurationBuilder.BuildConfiguration<string>("cache1", settings => { });

            // arrange
            using (var cache = new BaseCacheManager<string>(cfg))
            {
                var key = "key";
                // act
                cache.Add(key, "something");
                var result = cache.Get(key);
                // assert
                cache.CacheHandles.Count.Should().Be(0);
                result.Should().Be(null, "No handles in the cache managers should yield to no items in the cache");
            }
        }

        [Fact]
        public void CacheManager_AddCacheItem_WithExpMode_ButWithoutTimeout()
        {
            // arrange
            using (var cache = this.WithManyDictionaryHandles)
            {
                var key = "key";

                // act
                Action act = () => cache.Add(new CacheItem<object>(key, "something", ExpirationMode.Absolute, default(TimeSpan)));

                act.ShouldThrow<InvalidOperationException>()
                    .WithMessage("Expiration mode defined without timeout.");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_ManuallyAddHandler_WithNullValue()
        {
            // arrange
            var cfg = ConfigurationBuilder.BuildConfiguration<string>("cache", settings => { });
            var cache = new BaseCacheManager<string>(cfg);

            // act
            Action act = () => cache.AddCacheHandle(null);

            // assert
            act.ShouldThrow<ArgumentNullException>()
                .WithMessage("*Parameter name: handle");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Ctor_Cfg_WithNullValue()
        {
            // arrange

            // act
            Action act = () => new BaseCacheManager<object>(null);

            // assert
            act.ShouldThrow<ArgumentException>()
                    .WithMessage("*Parameter name: configuration");
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Ctor_Handles_WithNullValue()
        {
            // arrange
            var cfg = ConfigurationBuilder.BuildConfiguration<object>("cache", settings => { });

            // act
            Action act = () => new BaseCacheManager<object>(cfg, null);

            // assert
            act.ShouldThrow<ArgumentException>()
                    .WithMessage("*Parameter name: handles");
        }

        #endregion

        #region put params validation

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Put_InvalidKey()
        {
            using (var cache = CacheFactory.Build("cache", settings =>
            {
                settings.WithHandle<DictionaryCacheHandle>("h1");
            }))
            {
                // arrange
                // act
                Action act = () => cache.Put(null, null);
                Action actR = () => cache.Put(null, null, null);

                // assert
                act.ShouldThrow<ArgumentException>()
                    .WithMessage("*Parameter name: key");

                actR.ShouldThrow<ArgumentException>()
                    .WithMessage("*Parameter name: key");
            }
        }
        
        [Fact]
        [ReplaceCulture]
        public void CacheManager_Put_InvalidValue()
        {
            using (var cache = CacheFactory.Build("cache", settings =>
                {
                    settings.WithHandle<DictionaryCacheHandle>("h1");
                }))
            {
                // arrange
                // act
                Action act = () => cache.Put("key", null);
                Action actR = () => cache.Put("key", null, null);

                // assert
                act.ShouldThrow<ArgumentException>()
                    .WithMessage("*Parameter name: value");

                actR.ShouldThrow<ArgumentException>()
                    .WithMessage("*Parameter name: value");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Put_InvalidCacheItem()
        {
            using (var cache = CacheFactory.Build("cache", settings =>
                {
                    settings.WithHandle<DictionaryCacheHandle>("h1");
                }))
            {
                // act
                Action act = () => cache.Put(null);

                // assert
                act.ShouldThrow<ArgumentNullException>()
                    .WithMessage("*Parameter name: item");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Put_InvalidRegion()
        {
            using (var cache = CacheFactory.Build("cache", settings =>
            {
                settings.WithHandle<DictionaryCacheHandle>("h1");
            }))
            {
                // act
                Action act = () => cache.Put("key", "value", null);

                // assert
                act.ShouldThrow<ArgumentNullException>()
                    .WithMessage("*Parameter name: region");
            }
        }

        [Theory]
        [InlineData(new[] { 12345 })]
        [InlineData("something")]
        [InlineData(true)]
        [InlineData(0.223f)]
        public void CacheManager_Put_CacheItem_Positive<T>(T value)
        {
            using (var cache = CacheFactory.Build("cache", settings =>
                {
                    settings.WithHandle<DictionaryCacheHandle>("h1");
                }))
            {
                // arrange
                var key = "my key";
                var item = new CacheItem<object>(key, value);
                var itemRegion = new CacheItem<object>(key, value, "region");

                // act
                Action act = () => cache.Put(item);
                Action actRegion = () => cache.Put(itemRegion);

                // assert
                act.ShouldNotThrow();
                actRegion.ShouldNotThrow();
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
            using (var cache = CacheFactory.Build("cache", settings =>
                {
                    settings.WithHandle<DictionaryCacheHandle>("h1");
                }))
            {
                // arrange
                var key = "my key";

                // act
                Action act = () => cache.Put(key, value);
                Action actRegion = () => cache.Put(key, value, "region");

                // assert
                act.ShouldNotThrow();
                actRegion.ShouldNotThrow();
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
            using (var cache = CacheFactory.Build("cache", settings =>
            {
                settings.WithHandle<DictionaryCacheHandle>("h1");
            }))
            {
                // arrange
                // act
                Action act = () => cache.Update(null, null);
                Action actR = () => cache.Update(null, "r", null);
                Action actU = () => cache.Update(null, (o) => o, null);
                Action actRU = () => cache.Update(null, null, null, null);

                // assert
                act.ShouldThrow<ArgumentException>()
                    .WithMessage("*Parameter name: key*");

                actR.ShouldThrow<ArgumentException>()
                    .WithMessage("*Parameter name: key*");

                actU.ShouldThrow<ArgumentException>()
                    .WithMessage("*Parameter name: key*");

                actRU.ShouldThrow<ArgumentException>()
                    .WithMessage("*Parameter name: key*");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Update_InvalidUpdateFunc()
        {
            using (var cache = CacheFactory.Build("cache", settings =>
            {
                settings.WithHandle<DictionaryCacheHandle>("h1");
            }))
            {
                // arrange
                // act
                Action act = () => cache.Update("key", null);
                Action actR = () => cache.Update("key", "region", null);
                Action actU = () => cache.Update("key", null, new UpdateItemConfig());
                Action actRU = () => cache.Update("key", "region", null, new UpdateItemConfig());

                // assert
                act.ShouldThrow<ArgumentException>()
                    .WithMessage("*Parameter name: updateValue*");

                actR.ShouldThrow<ArgumentException>()
                    .WithMessage("*Parameter name: updateValue*");

                actU.ShouldThrow<ArgumentException>()
                    .WithMessage("*Parameter name: updateValue*");

                actRU.ShouldThrow<ArgumentException>()
                    .WithMessage("*Parameter name: updateValue*");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Update_InvalidRegion()
        {
            using (var cache = CacheFactory.Build("cache", settings =>
            {
                settings.WithHandle<DictionaryCacheHandle>("h1");
            }))
            {
                // arrange
                // act
                Action actR = () => cache.Update("key", null, a => a);
                Action actRU = () => cache.Update("key", null, a => a, new UpdateItemConfig());

                // assert
                actR.ShouldThrow<ArgumentException>()
                    .WithMessage("*Parameter name: region*");

                actRU.ShouldThrow<ArgumentException>()
                    .WithMessage("*Parameter name: region*");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Update_InvalidConfig()
        {
            using (var cache = CacheFactory.Build("cache", settings =>
            {
                settings.WithHandle<DictionaryCacheHandle>("h1");
            }))
            {
                // arrange
                // act
                Action act = () => cache.Update("key", a => a, null);
                Action actR = () => cache.Update("key", "region", a => a, null);

                // assert
                act.ShouldThrow<ArgumentException>()
                    .WithMessage("*Parameter name: config*");

                actR.ShouldThrow<ArgumentException>()
                    .WithMessage("*Parameter name: config*");
            }
        }

        [Fact]
        public void CacheManager_Update_ItemNotAdded()
        {
            using (var cache = CacheFactory.Build("cache", settings =>
            {
                settings.WithHandle<DictionaryCacheHandle>("h1");
            }))
            {
                // arrange
                // act
                Func<bool> act = () => cache.Update("key", item => item);

                // assert
                act().Should().BeFalse("Item has not been added to the cache");
            }
        }

        [Fact]
        public void CacheManager_Update_Simple()
        {
            using (var cache = CacheFactory.Build("cache", settings =>
            {
                settings.WithHandle<DictionaryCacheHandle>("h1");
            }))
            {
                // arrange
                cache.Add("mykey", "something");
                // act
                Func<bool> act = () => cache.Update("mykey", item => item + " awesome");
                Func<string> act2 = () => cache.Get<string>("mykey");

                // assert
                act().Should().BeTrue();
                act2().Should().Be("something awesome");
            }
        }
        
        #endregion

        #region Add validation

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Add_InvalidKey()
        {
            // arrange
            using (var cache = CacheFactory.Build("cache", settings =>
                {
                    settings.WithHandle<DictionaryCacheHandle>("h1");
                }))
            {
                // act
                Action act = () => cache.Add(null, null);
                Action actR = () => cache.Add(null, null, null);

                // assert
                act.ShouldThrow<ArgumentException>()
                    .WithMessage("*Parameter name: key");

                actR.ShouldThrow<ArgumentException>()
                    .WithMessage("*Parameter name: key");
            }
        }
        
        [Fact]
        [ReplaceCulture]
        public void CacheManager_Add_InvalidValue()
        {
            // arrange
            using (var cache = CacheFactory.Build("cache", 
                settings =>
                {
                    settings.WithHandle<DictionaryCacheHandle>("h1");
                }))
            {
                // act
                Action act = () => cache.Add("key", null);
                Action actR = () => cache.Add("key", null, "region");

                // assert
                act.ShouldThrow<ArgumentException>()
                    .WithMessage("*Parameter name: value");

                actR.ShouldThrow<ArgumentException>()
                    .WithMessage("*Parameter name: value");
            }
        }
        
        [Fact]
        [ReplaceCulture]
        public void CacheManager_Add_InvalidRegion()
        {
            // arrange
            using (var cache = CacheFactory.Build("cache",
                settings =>
                {
                    settings.WithHandle<DictionaryCacheHandle>("h1");
                }))
            {
                // act
                Action actR = () => cache.Add("key", "value", null);

                // assert
                actR.ShouldThrow<ArgumentException>()
                    .WithMessage("*Parameter name: region");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Add_InvalidCacheItem()
        {
            // arrange
            using (var cache = CacheFactory.Build("cache", settings =>
                {
                    settings.WithHandle<DictionaryCacheHandle>("h1");
                }))
            {
                // act
                Action act = () => cache.Add(null);

                // assert
                act.ShouldThrow<ArgumentNullException>()
                    .WithMessage("*Parameter name: item");
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
            using (var cache = CacheFactory.Build("cache", settings =>
                {
                    settings.WithHandle<DictionaryCacheHandle>("h1");
                }))
            {
                var key = "my key";
                var item = new CacheItem<object>(key, value);

                // act
                Action act = () => cache.Add(item);

                // assert
                act.ShouldNotThrow();
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
            using (var cache = CacheFactory.Build("cache", settings =>
                {
                    settings.WithHandle<DictionaryCacheHandle>("h1");
                }))
            {
                var key = "my key";

                // act
                Action act = () => cache.Add(key, value);

                // assert
                act.ShouldNotThrow();
                cache.Get(key).Should().Be(value);
            }
        }

        #endregion Add validation

        #region get validation

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Get_InvalideKey()
        {
            // arrange
            using (var cache = CacheFactory.Build("cache", settings =>
            {
                settings.WithHandle<DictionaryCacheHandle>("h1");
            }))
            {
                // act
                Action act = () => cache.Get(null);
                Action actR = () => cache.Get(null, "region");

                // assert
                act.ShouldThrow<ArgumentNullException>()
                    .WithMessage("*Parameter name: key");

                actR.ShouldThrow<ArgumentNullException>()
                    .WithMessage("*Parameter name: key");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Get_InvalideRegion()
        {
            // arrange
            using (var cache = CacheFactory.Build("cache", settings =>
            {
                settings.WithHandle<DictionaryCacheHandle>("h1");
            }))
            {
                // act
                Action act = () => cache.Get("key", null);

                // assert
                act.ShouldThrow<ArgumentNullException>()
                    .WithMessage("*Parameter name: region");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_GetItem_InvalideKey()
        {
            // arrange
            using (var cache = CacheFactory.Build("cache", settings =>
            {
                settings.WithHandle<DictionaryCacheHandle>("h1");
            }))
            {
                // act
                Action act = () => cache.GetCacheItem(null);
                Action actR = () => cache.GetCacheItem(null, "region");

                // assert
                act.ShouldThrow<ArgumentNullException>()
                    .WithMessage("*Parameter name: key");

                actR.ShouldThrow<ArgumentNullException>()
                    .WithMessage("*Parameter name: key");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_GetItem_InvalideRegion()
        {
            // arrange
            using (var cache = CacheFactory.Build("cache", settings =>
            {
                settings.WithHandle<DictionaryCacheHandle>("h1");
            }))
            {
                // act
                Action act = () => cache.GetCacheItem("key", null);

                // assert
                act.ShouldThrow<ArgumentNullException>()
                    .WithMessage("*Parameter name: region");
            }
        }
        
        [Fact]
        [ReplaceCulture]
        public void CacheManager_GetT_InvalideKey()
        {
            // arrange
            using (var cache = CacheFactory.Build("cache", settings =>
            {
                settings.WithHandle<DictionaryCacheHandle>("h1");
            }))
            {
                // act
                Action act = () => cache.Get<string>(null);
                Action actR = () => cache.Get<string>(null, "region");

                // assert
                act.ShouldThrow<ArgumentNullException>()
                    .WithMessage("*Parameter name: key");

                actR.ShouldThrow<ArgumentNullException>()
                    .WithMessage("*Parameter name: key");
            }
        }
        
        [Fact]
        [ReplaceCulture]
        public void CacheManager_GetT_InvalideRegion()
        {
            // arrange
            using (var cache = CacheFactory.Build("cache", settings =>
            {
                settings.WithHandle<DictionaryCacheHandle>("h1");
            }))
            {
                // act
                Action act = () => cache.Get<string>("key", null);

                // assert
                act.ShouldThrow<ArgumentNullException>()
                    .WithMessage("*Parameter name: region");
            }
        }
        
        [Fact]
        [ReplaceCulture]
        public void CacheManager_Get_KeyNotAvailable()
        {
            // arrange
            using (var cache = CacheFactory.Build("cache", settings =>
                {
                    settings.WithHandle<DictionaryCacheHandle>("h1");
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
            using (var cache = CacheFactory.Build("cache", settings =>
                {
                    settings.WithHandle<DictionaryCacheHandle>("h1");
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
            using (var cache = CacheFactory.Build("cache", settings =>
            {
                settings.WithHandle<DictionaryCacheHandle>("h1");
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
                    .And.ShouldBeEquivalentTo(new { Key = key, Value = value }, p => p.ExcludingMissingProperties());
            }
        }

        #endregion get validation

        #region remove

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Remove_InvalideKey()
        {
            // arrange
            using (var cache = CacheFactory.Build("cache", settings =>
                {
                    settings.WithHandle<DictionaryCacheHandle>("h1");
                }))
            {
                // act
                Action act = () => cache.Remove(null);
                Action actR = () => cache.Remove(null, "region");

                // assert
                act.ShouldThrow<ArgumentException>()
                    .WithMessage("*Parameter name: key");

                actR.ShouldThrow<ArgumentException>()
                    .WithMessage("*Parameter name: key");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Remove_InvalideRegion()
        {
            // arrange
            using (var cache = CacheFactory.Build("cache", settings =>
            {
                settings.WithHandle<DictionaryCacheHandle>("h1");
            }))
            {
                // act
                Action act = () => cache.Remove("key", null);

                // assert
                act.ShouldThrow<ArgumentException>()
                    .WithMessage("*Parameter name: region");
            }
        }
        
        [Fact]
        [ReplaceCulture]
        public void CacheManager_Remove_KeyEmpty()
        {
            // arrange
            using (var cache = CacheFactory.Build("cache", settings =>
                {
                    settings.WithHandle<DictionaryCacheHandle>("h1");
                }))
            {
                string key = string.Empty;

                // act
                Action act = () => cache.Remove(key);

                // assert
                act.ShouldThrow<ArgumentException>()
                    .WithMessage("*Parameter name: key");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Remove_KeyWhiteSpace()
        {
            // arrange
            using (var cache = CacheFactory.Build("cache", settings =>
                {
                    settings.WithHandle<DictionaryCacheHandle>("h1");
                }))
            {
                string key = "                ";

                // act
                Action act = () => cache.Remove(key);

                // assert
                act.ShouldThrow<ArgumentException>()
                    .WithMessage("*Parameter name: key");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Remove_KeyNotAvailable()
        {
            // arrange
            using (var cache = CacheFactory.Build("cache", settings =>
                {
                    settings.WithHandle<DictionaryCacheHandle>("h1");
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
            using (var cache = CacheFactory.Build("cache", settings =>
                {
                    settings.WithHandle<DictionaryCacheHandle>("h1");
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
        public void CacheManager_Index_InvalideKey()
        {
            // arrange
            using (var cache = CacheFactory.Build("cache", settings =>
                {
                    settings.WithHandle<DictionaryCacheHandle>("h1");
                }))
            {
                string key = null;

                // act
                object result;
                Action act = () => result = cache[key];
                Action actR = () => result = cache[key, "region"];

                // assert
                act.ShouldThrow<ArgumentNullException>()
                    .WithMessage("*Parameter name: key");

                actR.ShouldThrow<ArgumentNullException>()
                    .WithMessage("*Parameter name: key");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Index_Key_RegionEmpty()
        {
            // arrange
            using (var cache = CacheFactory.Build("cache", settings =>
            {
                settings.WithHandle<DictionaryCacheHandle>("h1");
            }))
            {
                // act
                object result;
                Action act = () => result = cache["key", ""];

                // assert
                act.ShouldThrow<ArgumentNullException>()
                    .WithMessage("*Parameter name: region");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Index_KeyNotAvailable()
        {
            // arrange
            using (var cache = CacheFactory.Build("cache", settings =>
                {
                    settings.WithHandle<DictionaryCacheHandle>("h1");
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

        [Theory]
        [MemberData("GetCacheManagers")]
        [ReplaceCulture]
        public void CacheManager_CastGet_Region<T>(T cache) where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                // act
                Func<bool> actA = () => cache.Add("key", "some value", "region");
                Func<string> act = () => cache.Get<string>("key", "region");

                // assert
                actA().Should().BeTrue();
                act().Should().Be("some value");
            }
        }

        [Theory]
        [MemberData("GetCacheManagers")]
        [ReplaceCulture]
        public void CacheManager_CastGet<T>(T cache) where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var keys = new List<string>() { "key1", "key2", "key3", "key4", "key5", "key6", "key7", "key8" };
                var values = new List<object>() 
                {
                    "string", 33293, 0.123f, 0.324d, 123311L, true,
                    new ComplexType(){ Name="name", SomeBool=false, SomeId= 213},
                    new DateTime(2014,1,3)
                };

                // act
                PopulateCache(cache, keys, values, 1);
                string strSomething = cache.Get<string>("key1");
                int iSomething = cache.Get<int>("key2");
                float fSomething = cache.Get<float>("key3");
                double dSomething = cache.Get<double>("key4");
                long lSomething = cache.Get<long>("key5");
                bool bSomething = cache.Get<bool>("key6");
                ComplexType obj = cache.Get<ComplexType>("key7");
                DateTime date = cache.Get<DateTime>("key8");
                object o = cache.Get<object>("nonexistent");

                // assert
                ValidateCacheValues(cache, keys, values);
                strSomething.ShouldBeEquivalentTo(values[0]);
                iSomething.ShouldBeEquivalentTo(values[1]);
                fSomething.ShouldBeEquivalentTo(values[2]);
                dSomething.ShouldBeEquivalentTo(values[3]);
                lSomething.ShouldBeEquivalentTo(values[4]);
                bSomething.ShouldBeEquivalentTo(values[5]);
                obj.ShouldBeEquivalentTo(values[6]);
                date.ShouldBeEquivalentTo(values[7]);
                o.Should().Be(null);
            }
        }

        [Theory]
        [MemberData("GetCacheManagers")]
        [ReplaceCulture]
        public void CacheManager_CastGet_InvalidTypeThrows<T>(T cache) where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                cache.Add("someint", 123456);

                // act
                Action act = () => cache.Get<string>("someint");

                // assert
                act.ShouldThrow<InvalidCastException>();
            }
        }

        [Theory]
        [MemberData("GetCacheManagers")]
        [ReplaceCulture]
        public void CacheManager_SimplePut<T>(T cache) where T : ICacheManager<object>
        {
            using (cache)
            {
                cache.Clear();
                // arrange
                var keys = new List<string>() { "key1", "key2", "key3" };
                var values = new List<object>() { new DateTime(2014, 1, 1), 234, "test string" };

                // act
                Action actPut = () =>
                {
                    PopulateCache(cache, keys, values, 0);
                };

                // assert
                actPut.ShouldNotThrow();
                ValidateCacheValues(cache, keys, values);
            }
        }
        
        [Theory]
        [MemberData("GetCacheManagers")]
        public void CacheManager_SimpleAdd<T>(T cache) where T : ICacheManager<object>
        {
            using (cache)
            {
                cache.Clear();
                // arrange
                var keys = new List<string>() { "key1", "key2", "key3", "key4" };
                var values = new List<object>() { new DateTime(2014, 1, 1), 234, "test string"};

                // act
                Action actSet = () =>
                {
                    PopulateCache(cache, keys, values, 1);
                };

                // assert
                actSet.ShouldNotThrow();
                ValidateCacheValues(cache, keys, values);
            }
        }

        [Theory]
        [MemberData("GetCacheManagers")]
        public void CacheManager_SimpleIndexPut<T>(T cache) where T : ICacheManager<object>
        {
            using (cache)
            {
                cache.Clear();
                // arrange
                var keys = new List<string>() { "key1", "key2", "key3" };
                var values = new List<object>() { new DateTime(2014, 1, 1), 234, "test string" };

                // act
                Action actSet = () =>
                {
                    PopulateCache(cache, keys, values, 2);
                };

                // assert
                actSet.ShouldNotThrow();
                ValidateCacheValues(cache, keys, values);
            }
        }

        [Theory]
        [MemberData("GetCacheManagers")]
        public void CacheManager_SimpleRemove<T>(T cache) where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var keys = new List<string>() { "key1", "key2", "key3" };
                var values = new List<object>() { new DateTime(2014, 1, 1), 234, "test string" };
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

        [Theory]
        [MemberData("GetCacheManagers")]
        public void CacheManager_Clear_AllItemsRemoved<T>(T cache) where T : ICacheManager<object>
        {
            // arrange
            // act
            using (cache)
            {
                // act
                cache.Add("key1", "value1");
                cache.Add("key2", "value2");
                cache.Clear();

                // assert
                cache["key1"].Should().BeNull();
                cache["key2"].Should().BeNull();
            }
        }

        [Theory]
        [MemberData("GetCacheManagers")]
        public void CacheManager_SimpleUpdate<T>(T cache) where T : ICacheManager<object>
        {
            using (cache)
            {
                cache.Clear();
                // arrange
                var keys = new List<string>() { "key1", "key2", "key3" };
                var values = new List<object>() { 10, 20, 30 };
                var newValues = new List<object>() { 11, 21, 31 };

                // act
                Action actSet = () =>
                {
                    PopulateCache(cache, keys, values, 1);

                    foreach (var key in keys)
                    {
                        int oldValue = cache.Get<int>(key);
                        cache.Update(key, item =>
                        {
                            int val = (int)item + 1;
                            return val;
                        });
                    }
                };

                // assert
                actSet.ShouldNotThrow();
                ValidateCacheValues(cache, keys, newValues);
            }
        }
        
        private void PopulateCache<T>(ICacheManager<T> cache, IList<string> keys, IList<T> values, int mode)
        {
            cache.Clear();
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

        private void ValidateCacheValues<T>(ICacheManager<T> cache, IList<string> keys, IList<T> values)
        {
            var cacheCfgText = "Cache: " + cache.Configuration.Name;
            cacheCfgText += ", Handles: " + string.Join(
                ",",
                cache.CacheHandles.Select(p => p.Configuration.HandleName).ToArray());

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
        public class ComplexType
        {
            public string Name { get; set; }

            public long SomeId { get; set; }

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

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }
    }
}