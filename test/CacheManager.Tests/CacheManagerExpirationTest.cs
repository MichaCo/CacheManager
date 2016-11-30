using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using CacheManager.Core;
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
    public class CacheManagerExpirationTest : BaseCacheManagerTest
    {
        // Issue #97 - Unable to reset expiration to 'None'
        [Fact]
        public void CacheManager_Expire_UnableToResetToNone()
        {
            using (var cache = CacheFactory.Build<string>(
                s => s
                    .WithDictionaryHandle()
                    .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromDays(10))))
            {
                var key = Guid.NewGuid().ToString();
                cache.Add(key, "value");

                cache.Get(key).Should().Be("value");
                cache.GetCacheItem(key).ExpirationMode.Should().Be(ExpirationMode.Sliding);

                var item = cache.GetCacheItem(key);
                var newItem = item.WithNoExpiration();

                cache.Put(newItem);

                cache.GetCacheItem(key).ExpirationMode.Should().Be(ExpirationMode.None);
            }
        }

        // Issue where dictionary cache returned wrong IsExpired results because of the use of DateTimeOffset AND DateTime
        [Fact]
        public void CacheManager_Expire_InheritIsExpiredCheck()
        {
            using (var cache = CacheFactory.Build<string>(
                s => s
                    .WithJsonSerializer()
                    .WithDictionaryHandle("h1")
                    .And
                    .WithRedisConfiguration("redis", "127.0.0.1")
                    .WithRedisCacheHandle("redis")
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(10))))
            {
                var key = Guid.NewGuid().ToString();
                var item = new CacheItem<string>(key, "value");
                cache.Add(item);
                cache.Update(key, v => v + "new");
                var result = cache.Get(key);
                ValidateExistsInAllHandles(cache, key);

                cache.GetCacheItem(key).ExpirationMode.Should().Be(ExpirationMode.Absolute);
                ValidateExistsInAllHandles(cache, key);
            }
        }

        [Fact(Skip = "Bug")]
        public void CacheManager_Expire_DoesNotInheritExpiration()
        {
            using (var cache = CacheFactory.Build<string>(
                s => s
                    .WithDictionaryHandle("h1")
                    .And
                    .WithDictionaryHandle("h2")
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(10))))
            {
                var key = Guid.NewGuid().ToString();
                var item = new CacheItem<string>(key, "value");
                cache.Add(item);
                cache.Update(key, v => v + "new");

                // sets the item on other cache handles
                var result = cache.Get(key);

                for (var i = 0; i < cache.CacheHandles.Count() - 1; i++)
                {
                    var handleItem = cache.CacheHandles.ElementAt(i).GetCacheItem(key);
                    handleItem.ExpirationMode.Should().NotBe(ExpirationMode.Absolute);
                }
            }
        }

        // Issue #57 - Verifying diggits will be ignored and stored as proper milliseconds value (integer).
        [Theory]
        [MemberData("TestCacheManagers")]
        public void CacheManager_Expire_DoesNotBreak_OnVeryPreciseValue<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                var key = Guid.NewGuid().ToString();
                var expiration = TimeSpan.FromTicks(315311111111111);
                Action act = () => cache.Add(new CacheItem<object>(key, "value", ExpirationMode.Sliding, expiration));

                act.ShouldNotThrow();
                var item = cache.GetCacheItem(key);
                item.Should().NotBeNull();
                Math.Ceiling(item.ExpirationTimeout.TotalDays).Should().Be(Math.Ceiling(expiration.TotalDays));
            }
        }

        // Issue #9 - item still expires
        [Theory]
        [MemberData("TestCacheManagers")]
        [Trait("category", "Unreliable")]
        public void CacheManager_RemoveExpiration_DoesNotExpire<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                var key = Guid.NewGuid().ToString();
                cache.Add(new CacheItem<object>(key, "value", ExpirationMode.Absolute, TimeSpan.FromMilliseconds(30)))
                    .Should().BeTrue();

                var item = cache.GetCacheItem(key);
                item.Should().NotBeNull();

                cache.Put(item.WithExpiration(ExpirationMode.None, default(TimeSpan)));

                Thread.Sleep(100);

                cache.Get(key).Should().NotBeNull();
            }
        }

        [Theory]
        [MemberData("TestCacheManagers")]
        [Trait("category", "Unreliable")]
        public void CacheManager_RemoveExpiration_CheckUpdate_Absolut<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                var key = Guid.NewGuid().ToString();
                cache.Add(new CacheItem<object>(key, "value", ExpirationMode.Absolute, TimeSpan.FromMilliseconds(30)))
                    .Should().BeTrue();

                var item = cache.GetCacheItem(key);
                item.Should().NotBeNull();

                cache.Put(item.WithExpiration(ExpirationMode.None, default(TimeSpan)));
                cache.Update(key, (o) => o + "something").Should().NotBeNull();

                Thread.Sleep(100);

                cache.Get(key).Should().NotBeNull();
            }
        }

        [Theory]
        [MemberData("TestCacheManagers")]
        [Trait("category", "Unreliable")]
        public void CacheManager_RemoveExpiration_CheckUpdate_Sliding<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                var key = Guid.NewGuid().ToString();
                cache.Add(new CacheItem<object>(key, "value", ExpirationMode.Sliding, TimeSpan.FromMilliseconds(30)))
                    .Should().BeTrue();

                var item = cache.GetCacheItem(key);
                item.Should().NotBeNull();

                cache.Put(item.WithExpiration(ExpirationMode.None, default(TimeSpan)));
                cache.Update(key, (o) => o + "something").Should().NotBeNull();

                Thread.Sleep(100);

                cache.Get(key).Should().NotBeNull();
            }
        }

        [Theory]
        [MemberData("TestCacheManagers")]
        [Trait("category", "Unreliable")]
        public void CacheManager_RemoveExpiration_Explicit_Absolut<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                var key = Guid.NewGuid().ToString();
                cache.Add(new CacheItem<object>(key, "value", ExpirationMode.Absolute, TimeSpan.FromMilliseconds(30)))
                    .Should().BeTrue();

                var item = cache.GetCacheItem(key);
                item.Should().NotBeNull();

                cache.RemoveExpiration(key);
                cache.Update(key, (o) => o + "something").Should().NotBeNull();

                Thread.Sleep(100);

                cache.Get(key).Should().NotBeNull();
            }
        }

        [Theory]
        [MemberData("TestCacheManagers")]
        [Trait("category", "Unreliable")]
        public void CacheManager_RemoveExpiration_Explicit_Sliding<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                var key = Guid.NewGuid().ToString();
                cache.Add(new CacheItem<object>(key, "value", ExpirationMode.Sliding, TimeSpan.FromMilliseconds(30)))
                    .Should().BeTrue();

                var item = cache.GetCacheItem(key);
                item.Should().NotBeNull();

                cache.RemoveExpiration(key);
                cache.Update(key, (o) => o + "something").Should().NotBeNull();

                Thread.Sleep(100);

                cache.Get(key).Should().NotBeNull();
            }
        }

        [Trait("category", "Unreliable")]
        [Theory]
        [MemberData("TestCacheManagers")]
        public void CacheManager_Sliding_DoesNotExpire_OnGet<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
#if !NET40 && MOCK_HTTPCONTEXT_ENABLED
                var first = cache.CacheHandles.First();
                if (cache.CacheHandles.Count() == 1 && first.GetType() == typeof(SystemWebCacheHandleWrapper<object>))
                {
                    // system.web caching doesn't support short sliding expiration. must be higher than 2000ms for some strange reason...
                    return;
                }
#endif
                var start = Environment.TickCount;
                var key = Guid.NewGuid().ToString();
                cache.Add(new CacheItem<object>(key, "value", ExpirationMode.Sliding, TimeSpan.FromMilliseconds(100)))
                    .Should().BeTrue();

                cache.GetCacheItem(key).Should().NotBeNull("After: " + (Environment.TickCount - start) + ": " + cache.ToString());

                start = Environment.TickCount;
                Thread.Sleep(50);
                cache[key].Should().NotBeNull("After: " + (Environment.TickCount - start) + ": " + cache.ToString());

                start = Environment.TickCount;
                Thread.Sleep(50);
                cache.Get(key).Should().NotBeNull("After: " + (Environment.TickCount - start) + ": " + cache.ToString());

                start = Environment.TickCount;
                Thread.Sleep(110);
                cache.GetCacheItem(key).Should().BeNull("After: " + (Environment.TickCount - start) + ": " + cache.ToString());
            }
        }

        [Trait("category", "Unreliable")]
        [Theory]
        [MemberData("TestCacheManagers")]
        public void CacheManager_Sliding_DoesNotExpire_OnUpdate<T>(T cache)
            where T : ICacheManager<object>
        {
            // see #50, update doesn't copy custom expire settings per item
            using (cache)
            {
#if !NET40 && MOCK_HTTPCONTEXT_ENABLED
                var first = cache.CacheHandles.First();
                if (cache.CacheHandles.Count() == 1 && first.GetType() == typeof(SystemWebCacheHandleWrapper<object>))
                {
                    // system.web caching doesn't support short sliding expiration. must be higher than 2000ms for some strange reason...
                    return;
                }
#endif
                var start = Environment.TickCount;
                var key = Guid.NewGuid().ToString();
                cache.Add(new CacheItem<object>(key, "value", ExpirationMode.Sliding, TimeSpan.FromMilliseconds(100)))
                    .Should().BeTrue();

                cache.AddOrUpdate(key, "value", o => o).Should().NotBeNull("After: " + (Environment.TickCount - start) + ": " + cache.ToString());

                start = Environment.TickCount;
                Thread.Sleep(50);
                object val;
                cache.TryUpdate(key, o => o, out val);
                val.Should().NotBeNull("After: " + (Environment.TickCount - start) + ": " + cache.ToString());

                start = Environment.TickCount;
                Thread.Sleep(50);
                cache.Update(key, o => o).Should().NotBeNull("After: " + (Environment.TickCount - start) + ": " + cache.ToString());

                start = Environment.TickCount;
                Thread.Sleep(110);
                cache.TryUpdate(key, o => o, out val);
                val.Should().BeNull("After: " + (Environment.TickCount - start) + ": " + cache.ToString());
            }
        }

        [Theory]
        [MemberData("TestCacheManagers")]
        public void CacheManager_Expire_Absolute_ForKey_Validate<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                var key = Guid.NewGuid().ToString();
                cache.Add(new CacheItem<object>(key, "value", ExpirationMode.None, default(TimeSpan)))
                    .Should().BeTrue();

                cache.Expire(key, DateTimeOffset.UtcNow.AddMinutes(10));

                var item = cache.GetCacheItem(key);

                item.ExpirationTimeout.Should().BeCloseTo(TimeSpan.FromMinutes(10), precision: 200);
                item.ExpirationMode.Should().Be(ExpirationMode.Absolute);
            }
        }

        [Theory]
        [MemberData("TestCacheManagers")]
        public void CacheManager_Expire_Absolute_ForKeyRegion_Validate<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                var key = Guid.NewGuid().ToString();
                var region = "region";
                cache.Add(new CacheItem<object>(key, region, "value", ExpirationMode.None, default(TimeSpan)))
                    .Should().BeTrue();

                cache.Expire(key, region, DateTimeOffset.UtcNow.AddMinutes(10));

                var item = cache.GetCacheItem(key, region);

                item.ExpirationTimeout.Should().BeCloseTo(TimeSpan.FromMinutes(10), precision: 200);
                item.ExpirationMode.Should().Be(ExpirationMode.Absolute);
            }
        }

        [Theory]
        [MemberData("TestCacheManagers")]
        public void CacheManager_Expire_Sliding_ForKey_Validate<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                var key = Guid.NewGuid().ToString();
                cache.Add(new CacheItem<object>(key, "value", ExpirationMode.None, default(TimeSpan)))
                    .Should().BeTrue();

                cache.Expire(key, TimeSpan.FromMinutes(10));

                var item = cache.GetCacheItem(key);

                item.ExpirationTimeout.Should().BeCloseTo(TimeSpan.FromMinutes(10), precision: 200);
                item.ExpirationMode.Should().Be(ExpirationMode.Sliding);
            }
        }

        [Theory]
        [MemberData("TestCacheManagers")]
        public void CacheManager_Expire_Sliding_ForKeyRegion_Validate<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                var key = Guid.NewGuid().ToString();
                var region = "region";
                cache.Add(new CacheItem<object>(key, region, "value", ExpirationMode.None, default(TimeSpan)))
                    .Should().BeTrue();

                cache.Expire(key, region, TimeSpan.FromMinutes(10));

                var item = cache.GetCacheItem(key, region);

                item.ExpirationTimeout.Should().BeCloseTo(TimeSpan.FromMinutes(10), precision: 200);
                item.ExpirationMode.Should().Be(ExpirationMode.Sliding);
            }
        }

        [Theory]
        [MemberData("TestCacheManagers")]
        public void CacheManager_RemoveExpiration_ForKey_Validate<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                var key = Guid.NewGuid().ToString();
                cache.Add(new CacheItem<object>(key, "value", ExpirationMode.Absolute, TimeSpan.FromMinutes(30)))
                    .Should().BeTrue();

                cache.RemoveExpiration(key);

                var item = cache.GetCacheItem(key);

                item.ExpirationTimeout.Should().Be(default(TimeSpan));
                item.ExpirationMode.Should().Be(ExpirationMode.None);
            }
        }

        [Theory]
        [MemberData("TestCacheManagers")]
        public void CacheManager_RemoveExpiration_ForKeyRegion_Validate<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                var key = Guid.NewGuid().ToString();
                var region = "region";
                cache.Add(new CacheItem<object>(key, region, "value", ExpirationMode.Absolute, TimeSpan.FromMinutes(30)))
                    .Should().BeTrue();

                cache.RemoveExpiration(key, region);

                var item = cache.GetCacheItem(key, region);

                item.ExpirationTimeout.Should().Be(default(TimeSpan));
                item.ExpirationMode.Should().Be(ExpirationMode.None);
            }
        }

#if !DNXCORE50
        [Fact]
        [Trait("category", "Unreliable")]
        [ReplaceCulture]
        public void CacheManager_Configuration_AbsoluteExpires()
        {
            using (var cache = CacheFactory.Build(settings =>
            {
                settings.WithSystemRuntimeCacheHandle()
                        .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMilliseconds(50));
            }))
            {
                var key = Guid.NewGuid().ToString();
                cache.Put(key, "value");

                Thread.Sleep(20);

                cache.Get(key).Should().Be("value");

                Thread.Sleep(40);

                cache.Get(key).Should().BeNull("Should be expired.");
            }
        }

#endif

        [Fact]
        [Trait("category", "Unreliable")]
        [ReplaceCulture]
        public void DictionaryHandle_AbsoluteExpires()
        {
            using (var cache = CacheFactory.Build(settings =>
            {
                settings.WithDictionaryHandle()
                        .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMilliseconds(50));
            }))
            {
                var key = Guid.NewGuid().ToString();
                cache.Put(key, "value");

                Thread.Sleep(20);

                cache.Get(key).Should().Be("value");

                Thread.Sleep(40);

                cache.Get(key).Should().BeNull("Should be expired.");
            }
        }

        [Fact]
        [Trait("category", "Unreliable")]
        [ReplaceCulture]
        public void DictionaryHandle_SlidingExpires()
        {
            using (var cache = CacheFactory.Build(settings =>
            {
                settings.WithDictionaryHandle()
                        .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromMilliseconds(50));
            }))
            {
                var key = Guid.NewGuid().ToString();
                cache.Put(key, "value");

                Thread.Sleep(30);

                cache.Get(key).Should().Be("value");

                Thread.Sleep(30);

                cache.Get(key).Should().Be("value");

                Thread.Sleep(50);

                cache.Get(key).Should().BeNull("Should be expired.");
            }
        }

        [Fact]
        public void CacheItem_WithExpiration()
        {
            var item = new CacheItem<object>("key", "value", ExpirationMode.Absolute, TimeSpan.FromSeconds(1));

            var absolute = item.WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(10));
            var sliding = item.WithExpiration(ExpirationMode.Sliding, TimeSpan.FromMinutes(10));
            var none = item.WithExpiration(ExpirationMode.None, default(TimeSpan));

            absolute.ExpirationMode.Should().Be(ExpirationMode.Absolute);
            absolute.ExpirationTimeout.Should().BeCloseTo(TimeSpan.FromMinutes(10), precision: 200);
            sliding.ExpirationMode.Should().Be(ExpirationMode.Sliding);
            sliding.ExpirationTimeout.Should().BeCloseTo(TimeSpan.FromMinutes(10), precision: 200);
            none.ExpirationMode.Should().Be(ExpirationMode.None);
            none.ExpirationTimeout.Should().BeCloseTo(default(TimeSpan));
        }

        [Fact]
        public void CacheItem_WithAbsoluteExpiration()
        {
            var item = new CacheItem<object>("key", "value", ExpirationMode.Sliding, TimeSpan.FromSeconds(1));

            var absolute = item.WithAbsoluteExpiration(DateTimeOffset.Now.AddMinutes(10));

            absolute.ExpirationMode.Should().Be(ExpirationMode.Absolute);
            absolute.ExpirationTimeout.Should().BeCloseTo(TimeSpan.FromMinutes(10), precision: 200);
        }

        [Fact]
        public void CacheItem_WithSlidingExpiration()
        {
            var item = new CacheItem<object>("key", "value", ExpirationMode.Absolute, TimeSpan.FromSeconds(1));

            var absolute = item.WithSlidingExpiration(TimeSpan.FromMinutes(10));

            absolute.ExpirationMode.Should().Be(ExpirationMode.Sliding);
            absolute.ExpirationTimeout.Should().BeCloseTo(TimeSpan.FromMinutes(10), precision: 200);
        }

        [Fact]
        public void CacheItem_WithNoExpiration()
        {
            var item = new CacheItem<object>("key", "value", ExpirationMode.Absolute, TimeSpan.FromSeconds(1));

            var absolute = item.WithNoExpiration();

            absolute.ExpirationMode.Should().Be(ExpirationMode.None);
            absolute.ExpirationTimeout.Should().BeCloseTo(default(TimeSpan));
        }

#if !DNXCORE50
        [Fact]
        public void BaseCacheHandle_ExpirationInherits_Issue_1()
        {
            using (var cache = CacheFactory.Build(settings =>
            {
                settings.WithSystemRuntimeCacheHandle()
                        .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(10))
                    .And
                    .WithSystemRuntimeCacheHandle();
            }))
            {
                cache.Add("something", "stuip");

                var handles = cache.CacheHandles.ToArray();
                handles[0].GetCacheItem("something").ExpirationMode.Should().Be(ExpirationMode.Absolute);

                // second cache should not inherit the expiration
                handles[1].GetCacheItem("something").ExpirationMode.Should().Be(ExpirationMode.None);
                handles[1].GetCacheItem("something").ExpirationTimeout.Should().Be(default(TimeSpan));
            }
        }

#endif

        private static void ValidateExistsInAllHandles<T>(ICacheManager<T> cache, string key)
        {
            foreach (var handle in cache.CacheHandles)
            {
                var item = handle.GetCacheItem(key);
                if (item == null)
                {
                    throw new InvalidOperationException($"'{key}' Doesn't exist in handle {handle.Configuration.Name}.");
                }
            }
        }

        private static void ValidateExistsInAllHandles<T>(ICacheManager<T> cache, string key, string region)
        {
            foreach (var handle in cache.CacheHandles)
            {
                if (cache.GetCacheItem(key, region) == null)
                {
                    throw new InvalidOperationException($"'{key}:{region}' doesn't exist in handle {handle.Configuration.Name}.");
                }
            }
        }
    }
}