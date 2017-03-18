using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
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
    public class CacheManagerExpirationTest
    {
        public class AllCaches
        {
            [Trait("category", "Unreliable")]
            [Theory]
            [ClassData(typeof(TestCacheManagers))]
            public async Task Expiration_Sliding_DoesNotExpire_OnGet<T>(T cache)
                where T : ICacheManager<object>
            {
                using (cache)
                {
#if !NET40 && MOCK_HTTPCONTEXT_ENABLED
                    if (cache.CacheHandles.OfType<SystemWebCacheHandleWrapper<object>>().Any())
                    {
                        // system.web caching doesn't support short sliding expiration. must be higher than 2000ms for some strange reason...
                        return;
                    }
#endif
                    var timeout = 100;
                    await TestSlidingExpiration(
                        timeout,
                        (key) => cache.Add(new CacheItem<object>(key, "value", ExpirationMode.Sliding, TimeSpan.FromMilliseconds(timeout))),
                        (key) => cache[key]);
                }
            }

            [Trait("category", "Unreliable")]
            [Theory]
            [ClassData(typeof(TestCacheManagers))]
            public async Task Expiration_Sliding_DoesNotExpire_OnUpdate<T>(T cache)
                where T : ICacheManager<object>
            {
                // see #50, update doesn't copy custom expire settings per item
                using (cache)
                {
#if !NET40 && MOCK_HTTPCONTEXT_ENABLED
                    if (cache.CacheHandles.OfType<SystemWebCacheHandleWrapper<object>>().Any())
                    {
                        // system.web caching doesn't support short sliding expiration. must be higher than 2000ms for some strange reason...
                        return;
                    }
#endif
                    var timeout = 100;
                    try
                    {
                        await TestSlidingExpiration(
                            timeout,
                            (key) => cache.Add(new CacheItem<object>(key, "value", ExpirationMode.Sliding, TimeSpan.FromMilliseconds(timeout))),
                            (key) =>
                            {
                                if (cache.TryUpdate(key, o => o, out object value))
                                {
                                    return value;
                                }

                                return null;
                            });
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(cache.ToString(), ex);
                    }
                }
            }
        }

        public class Dictionary
        {
            [Fact]
            [Trait("category", "Unreliable")]
            [ReplaceCulture]
            public async Task DictionaryHandle_AbsoluteExpires()
            {
                using (var cache = CacheFactory.Build(settings =>
                {
                    settings.WithDictionaryHandle()
                            .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMilliseconds(50));
                }))
                {
                    await TestAbsoluteExpiration(
                        50,
                        (key) => cache.Add(key, "value"),
                        (key) => cache.Get(key));
                }
            }

            [Fact]
            [Trait("category", "Unreliable")]
            [ReplaceCulture]
            public async Task DictionaryHandle_SlidingExpires()
            {
                using (var cache = CacheFactory.Build(settings =>
                {
                    settings.WithDictionaryHandle()
                            .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromMilliseconds(50));
                }))
                {
                    await TestSlidingExpiration(
                        50,
                        (key) => cache.Add(key, "value"),
                        (key) => cache.Get(key));
                }
            }
        }
#if MEMCACHEDENABLED

        public class Memcached
        {
            [Fact]
            [Trait("category", "Memcached")]
            [Trait("category", "Unreliable")]
            public async Task Memcached_Absolute_DoesExpire()
            {
                var timeout = 100;
                var cache = CacheFactory.Build(settings =>
                {
                    settings
                        .WithMemcachedCacheHandle(MemcachedTests.Configuration)
                        .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMilliseconds(timeout));
                });

                using (cache)
                {
                    await TestAbsoluteExpiration(
                        timeout,
                        (key) => cache.Add(key, "value"),
                        (key) => cache.Get(key));
                }
            }

            [Fact]
            [Trait("category", "Memcached")]
            [Trait("category", "Unreliable")]
            public async Task Memcached_Sliding_DoesExpire()
            {
                var timeout = 100;
                var cache = CacheFactory.Build(settings =>
                {
                    settings
                        .WithMemcachedCacheHandle(MemcachedTests.Configuration)
                        .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromMilliseconds(timeout));
                });

                using (cache)
                {
                    await TestSlidingExpiration(
                        timeout,
                        (key) => cache.Add(key, "value"),
                        (key) => cache.Get(key));
                }
            }
        }
#endif

        public class MsMemory
        {
            [Fact]
            [Trait("category", "Unreliable")]
            public async Task MsMemory_Absolute_DoesExpire()
            {
                // arrange
                var timeout = 50;

                // act
                using (var cache = CacheFactory.Build(_ => _.WithMicrosoftMemoryCacheHandle()))
                {
                    await TestAbsoluteExpiration(
                        timeout,
                        (key) => cache.Add(new CacheItem<object>(key, "something", ExpirationMode.Absolute, TimeSpan.FromMilliseconds(timeout))),
                        (key) => cache.Get(key));
                }
            }

            [Fact]
            [Trait("category", "Unreliable")]
            public async Task MsMemory_Sliding_DoesExpire()
            {
                // arrange
                var timeout = 50;

                // act
                using (var cache = CacheFactory.Build(_ => _.WithMicrosoftMemoryCacheHandle()))
                {
                    await TestSlidingExpiration(
                        timeout,
                        (key) => cache.Add(new CacheItem<object>(key, "something", ExpirationMode.Sliding, TimeSpan.FromMilliseconds(timeout))),
                        (key) => cache.Get(key));
                }
            }
        }

#if !NETCOREAPP

        public class SysRuntime
        {
            [Fact]
            [Trait("category", "Unreliable")]
            public async Task SysRuntime_Absolute_DoesExpire()
            {
                // arrange
                var timeout = 50;

                // act
                using (var cache = CacheFactory.Build(_ => _.WithSystemRuntimeCacheHandle()))
                {
                    await TestAbsoluteExpiration(
                        timeout,
                        (key) => cache.Add(new CacheItem<object>(key, "something", ExpirationMode.Absolute, TimeSpan.FromMilliseconds(timeout))),
                        (key) => cache.Get(key));
                }
            }

            [Fact]
            [Trait("category", "Unreliable")]
            public async Task SysRuntime_Sliding_DoesExpire()
            {
                // arrange
                var timeout = 50;

                // act
                using (var cache = CacheFactory.Build(_ => _.WithSystemRuntimeCacheHandle()))
                {
                    await TestSlidingExpiration(
                        timeout,
                        (key) => cache.Add(new CacheItem<object>(key, "something", ExpirationMode.Sliding, TimeSpan.FromMilliseconds(timeout))),
                        (key) => cache.Get(key));
                }
            }
        }

#endif

        public class Redis
        {
            [Fact]
            [Trait("category", "Redis")]
            [Trait("category", "Unreliable")]
            public async Task Redis_Absolute_DoesExpire()
            {
                // arrange
                var timeout = 50;
                var cache = TestManagers.CreateRedisCache(1);

                // act/assert
                using (cache)
                {
                    await TestAbsoluteExpiration(
                        timeout,
                        (key) => cache.Add(new CacheItem<object>(key, "something", ExpirationMode.Absolute, TimeSpan.FromMilliseconds(timeout))),
                        (key) => cache.GetCacheItem(key));
                }
            }

            [Fact]
            [Trait("category", "Redis")]
            [Trait("category", "Unreliable")]
            public async Task Redis_Absolute_DoesExpire_MultiClients()
            {
                // arrange
                var timeout = 50;
                var cacheA = TestManagers.CreateRedisCache(2);
                var cacheB = TestManagers.CreateRedisCache(2);

                // act/assert
                using (cacheA)
                using (cacheB)
                {
                    await TestAbsoluteExpiration(
                        timeout,
                        (key) => cacheA.Add(new CacheItem<object>(key, "something", ExpirationMode.Absolute, TimeSpan.FromMilliseconds(timeout))),
                        (key) =>
                        {
                            var a = cacheA.GetCacheItem(key);
                            var b = cacheB.GetCacheItem(key);
                            if (a == null || b == null)
                            {
                                return null;
                            }

                            return a;
                        });
                }
            }

            [Fact]
            [Trait("category", "Redis")]
            [Trait("category", "Unreliable")]
            public async Task Redis_Sliding_DoesExpire()
            {
                // arrange
                var timeout = 50;
                var cache = TestManagers.CreateRedisCache(9);

                // act/assert
                using (cache)
                {
                    await TestSlidingExpiration(
                        timeout,
                        (key) => cache.Add(new CacheItem<object>(key, "something", ExpirationMode.Sliding, TimeSpan.FromMilliseconds(timeout))),
                        (key) => cache.GetCacheItem(key));
                }
            }

            [Fact]
            [Trait("category", "Redis")]
            [Trait("category", "Unreliable")]
            public async Task Redis_Sliding_DoesExpire_MultiClients()
            {
                // arrange
                var timeout = 50;
                var channelName = Guid.NewGuid().ToString();
                var cacheA = TestManagers.CreateRedisAndDicCacheWithBackplane(10, false, channelName);
                var cacheB = TestManagers.CreateRedisAndDicCacheWithBackplane(10, false, channelName);

                // act/assert
                using (cacheA)
                using (cacheB)
                {
                    await TestSlidingExpiration(
                        timeout,
                        (key) => cacheA.Add(new CacheItem<object>(key, "something", ExpirationMode.Sliding, TimeSpan.FromMilliseconds(timeout))),
                        (key) =>
                        {
                            var a = cacheA.GetCacheItem(key);
                            var b = cacheB.GetCacheItem(key);
                            if (a == null || b == null)
                            {
                                return null;
                            }

                            return a;
                        });
                }
            }
        }

        public class ExpireTests
        {
            // Issue #97 - Unable to reset expiration to 'None'
            [Fact]
            public void Expiration_UnableToResetToNone()
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

            [Fact]
            [Trait("category", "Redis")]
            [Trait("category", "Unreliable")]
            public void Expiration_InheritIsExpiredCheck()
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

                    // must respect the first handle because the item will be found in the first one and it should respect the expiration configuration
                    // instead of the individual item
                    cache.GetCacheItem(key).ExpirationMode.Should().Be(ExpirationMode.None);
                    ValidateExistsInAllHandles(cache, key);
                }
            }

            [Fact]
            public void Expiration_DoesNotInheritExpiration()
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

            [Fact]
            public void Expiration_RespectDefaultPerHandle()
            {
                using (var cache = CacheFactory.Build<string>(
                    s => s
                        .WithDictionaryHandle()
                            .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(10))
                        .And
                        .WithDictionaryHandle()
                            .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(10))
                        .And
                        .WithDictionaryHandle()
                            .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromDays(10))))
                {
                    var key = Guid.NewGuid().ToString();
                    cache.Put(key, "value");

                    cache.CacheHandles.ElementAt(0).GetCacheItem(key)
                        .ExpirationMode.Should().Be(ExpirationMode.Absolute);
                    cache.CacheHandles.ElementAt(0).GetCacheItem(key)
                        .ExpirationTimeout.Should().Be(TimeSpan.FromSeconds(10));

                    cache.CacheHandles.ElementAt(1).GetCacheItem(key)
                        .ExpirationMode.Should().Be(ExpirationMode.Absolute);
                    cache.CacheHandles.ElementAt(1).GetCacheItem(key)
                        .ExpirationTimeout.Should().Be(TimeSpan.FromMinutes(10));

                    cache.CacheHandles.ElementAt(2).GetCacheItem(key)
                        .ExpirationMode.Should().Be(ExpirationMode.Sliding);
                    cache.CacheHandles.ElementAt(2).GetCacheItem(key)
                        .ExpirationTimeout.Should().Be(TimeSpan.FromDays(10));
                }
            }

            [Fact]
            public void Expiration_RespectDefaultPerHandleAfterAutofill()
            {
                using (var cache = CacheFactory.Build<string>(
                    s => s
                        .WithDictionaryHandle()
                            .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(10))
                        .And
                        .WithDictionaryHandle()
                            .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(10))
                        .And
                        .WithDictionaryHandle()
                            .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromDays(10))))
                {
                    var key = Guid.NewGuid().ToString();

                    // add into last handle and get it will add the item to all other handles
                    cache.CacheHandles.Last().Add(key, "value");
                    var item = cache.Get(key);

                    cache.CacheHandles.ElementAt(0).GetCacheItem(key)
                        .ExpirationMode.Should().Be(ExpirationMode.Absolute);
                    cache.CacheHandles.ElementAt(0).GetCacheItem(key)
                        .ExpirationTimeout.Should().Be(TimeSpan.FromSeconds(10));

                    cache.CacheHandles.ElementAt(1).GetCacheItem(key)
                        .ExpirationMode.Should().Be(ExpirationMode.Absolute);
                    cache.CacheHandles.ElementAt(1).GetCacheItem(key)
                        .ExpirationTimeout.Should().Be(TimeSpan.FromMinutes(10));

                    cache.CacheHandles.ElementAt(2).GetCacheItem(key)
                        .ExpirationMode.Should().Be(ExpirationMode.Sliding);
                    cache.CacheHandles.ElementAt(2).GetCacheItem(key)
                        .ExpirationTimeout.Should().Be(TimeSpan.FromDays(10));
                }
            }

            [Theory]
            [ClassData(typeof(TestCacheManagers))]
            public void Expiration_ValidateUsesDefaultExpirationFlag<T>(T cache)
                where T : ICacheManager<object>
            {
                using (cache)
                {
                    var key = Guid.NewGuid().ToString();
                    cache.Add(key, key);

                    var item = cache.GetCacheItem(key);

                    item.UsesExpirationDefaults.Should().BeTrue(cache.ToString() + " | " + item.ToString());
                }
            }

            [Theory]
            [ClassData(typeof(TestCacheManagers))]
            public void Expiration_NotDefaultExpirationFlag_Item<T>(T cache)
                where T : ICacheManager<object>
            {
                using (cache)
                {
                    var key = Guid.NewGuid().ToString();
                    cache.Add(new CacheItem<object>(key, key, ExpirationMode.Sliding, TimeSpan.FromMinutes(10)));

                    var item = cache.GetCacheItem(key);

                    item.UsesExpirationDefaults.Should().BeFalse(cache.ToString() + " | " + item.ToString());
                }
            }

            [Theory]
            [ClassData(typeof(TestCacheManagers))]
            public void Expiration_NotDefaultExpirationFlag_Expire<T>(T cache)
                where T : ICacheManager<object>
            {
                using (cache)
                {
                    var key = Guid.NewGuid().ToString();
                    cache.Add(key, key);

                    var item = cache.GetCacheItem(key);
                    item.UsesExpirationDefaults.Should().BeTrue(cache.ToString() + " | " + item.ToString());

                    cache.Expire(key, TimeSpan.FromSeconds(360));
                    item = cache.GetCacheItem(key);

                    item.UsesExpirationDefaults.Should().BeFalse(cache.ToString() + " | " + item.ToString());
                }
            }

            // #74
            [Theory]
            [ClassData(typeof(TestCacheManagers))]
            public void Expiration_DoesNotAcceptExpirationInThePast<T>(T cache)
                where T : ICacheManager<object>
            {
                using (cache)
                {
                    var key = Guid.NewGuid().ToString();
                    var expiration = TimeSpan.FromSeconds(-1);
                    Action act = () => cache.Add(new CacheItem<object>(key, "value", ExpirationMode.Sliding, expiration));

                    act.ShouldThrow<ArgumentOutOfRangeException>()
                        .WithMessage("Expiration timeout must be greater than zero*");
                }
            }

            // Issue #57 - Verifying diggits will be ignored and stored as proper milliseconds value (integer).
            [Theory]
            [ClassData(typeof(TestCacheManagers))]
            public void Expiration_DoesNotBreak_OnVeryPreciseValue<T>(T cache)
                where T : ICacheManager<object>
            {
                using (cache)
                {
                    var key = Guid.NewGuid().ToString();
                    var expiration = TimeSpan.FromTicks(TimeSpan.FromDays(20).Ticks);
                    Action act = () => cache.Add(new CacheItem<object>(key, "value", ExpirationMode.Sliding, expiration));

                    act.ShouldNotThrow();
                    var item = cache.GetCacheItem(key);
                    item.Should().NotBeNull();
                    Math.Ceiling(item.ExpirationTimeout.TotalDays).Should().Be(Math.Ceiling(expiration.TotalDays));
                }
            }
        }

        public class RemoveExpiration
        {
            // Issue #9 - item still expires
            [Theory]
            [ClassData(typeof(TestCacheManagers))]
            [Trait("category", "Unreliable")]
            public async Task Expiration_Remove_DoesNotExpire<T>(T cache)
                where T : ICacheManager<object>
            {
                using (cache)
                {
                    var timeout = 50;
                    await TestRemoveExpiration(
                        timeout,
                        (key) => cache.Add(new CacheItem<object>(key, "value", ExpirationMode.Absolute, TimeSpan.FromMilliseconds(timeout))),
                        (key) =>
                        {
                            var item = cache.GetCacheItem(key);
                            if (item == null)
                            {
                                return false;
                            }
                            cache.Put(item.WithNoExpiration());
                            return true;
                        },
                        (key) => cache.Get(key));
                }
            }

            [Theory]
            [ClassData(typeof(TestCacheManagers))]
            [Trait("category", "Unreliable")]
            public async Task Expiration_Remove_CheckUpdate_Absolut<T>(T cache)
                where T : ICacheManager<object>
            {
                using (cache)
                {
                    var timeout = 50;
                    await TestRemoveExpiration(
                        timeout,
                        (key) => cache.Add(new CacheItem<object>(key, "value", ExpirationMode.Absolute, TimeSpan.FromMilliseconds(timeout))),
                        (key) =>
                        {
                            var item = cache.GetCacheItem(key);
                            if (item == null)
                            {
                                return false;
                            }
                            cache.Put(item.WithNoExpiration());
                            return true;
                        },
                        (key) =>
                        {
                            try
                            {
                                return cache.Update(key, (o) => o + "something");
                            }
                            catch
                            {
                                return null;
                            }
                        });
                }
            }

            [Theory]
            [ClassData(typeof(TestCacheManagers))]
            [Trait("category", "Unreliable")]
            public async Task Expiration_Remove_CheckUpdate_Sliding<T>(T cache)
                where T : ICacheManager<object>
            {
                using (cache)
                {
                    var timeout = 50;
                    await TestRemoveExpiration(
                        timeout,
                        (key) => cache.Add(new CacheItem<object>(key, "value", ExpirationMode.Sliding, TimeSpan.FromMilliseconds(timeout))),
                        (key) =>
                        {
                            var item = cache.GetCacheItem(key);
                            if (item == null)
                            {
                                return false;
                            }
                            cache.Put(item.WithNoExpiration());
                            return true;
                        },
                        (key) =>
                        {
                            try
                            {
                                return cache.Update(key, (o) => o + "something");
                            }
                            catch
                            {
                                return null;
                            }
                        });
                }
            }
        }

        public class RemoveExpirationExplicit
        {
            [Theory]
            [ClassData(typeof(TestCacheManagers))]
            [Trait("category", "Unreliable")]
            public async Task Expiration_Remove_Explicit_Absolut<T>(T cache)
                where T : ICacheManager<object>
            {
                using (cache)
                {
                    var timeout = 50;
                    await TestRemoveExpiration(
                        timeout,
                        (key) => cache.Add(new CacheItem<object>(key, "value", ExpirationMode.Absolute, TimeSpan.FromMilliseconds(timeout))),
                        (key) =>
                        {
                            cache.RemoveExpiration(key);
                            return true;
                        },
                        (key) =>
                        {
                            try
                            {
                                return cache.Update(key, (o) => o + "something");
                            }
                            catch
                            {
                                return null;
                            }
                        });
                }
            }

            [Theory]
            [ClassData(typeof(TestCacheManagers))]
            [Trait("category", "Unreliable")]
            public async Task Expiration_Remove_Explicit_Sliding<T>(T cache)
                where T : ICacheManager<object>
            {
                using (cache)
                {
                    var timeout = 50;
                    await TestRemoveExpiration(
                        timeout,
                        (key) => cache.Add(new CacheItem<object>(key, "value", ExpirationMode.Sliding, TimeSpan.FromMilliseconds(timeout))),
                        (key) =>
                        {
                            cache.RemoveExpiration(key);
                            return true;
                        },
                        (key) =>
                        {
                            try
                            {
                                return cache.Update(key, (o) => o + "something");
                            }
                            catch
                            {
                                return null;
                            }
                        });
                }
            }
        }

        public class ValidateExpire
        {
            [Theory]
            [ClassData(typeof(TestCacheManagers))]
            public void Expiration_Absolute_ForKey_Validate<T>(T cache)
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
            [ClassData(typeof(TestCacheManagers))]
            public void Expiration_Absolute_ForKeyRegion_Validate<T>(T cache)
                where T : ICacheManager<object>
            {
                using (cache)
                {
                    var key = Guid.NewGuid().ToString();
                    var region = Guid.NewGuid().ToString();
                    cache.Add(new CacheItem<object>(key, region, "value", ExpirationMode.None, default(TimeSpan)))
                        .Should().BeTrue();

                    cache.Expire(key, region, DateTimeOffset.UtcNow.AddMinutes(10));

                    var item = cache.GetCacheItem(key, region);

                    item.ExpirationTimeout.Should().BeCloseTo(TimeSpan.FromMinutes(10), precision: 200);
                    item.ExpirationMode.Should().Be(ExpirationMode.Absolute);
                }
            }

            [Theory]
            [ClassData(typeof(TestCacheManagers))]
            public void Expiration_Sliding_ForKey_Validate<T>(T cache)
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
            [ClassData(typeof(TestCacheManagers))]
            public void Expiration_Sliding_ForKeyRegion_Validate<T>(T cache)
                where T : ICacheManager<object>
            {
                using (cache)
                {
                    var key = Guid.NewGuid().ToString();
                    var region = Guid.NewGuid().ToString();
                    cache.Add(new CacheItem<object>(key, region, "value", ExpirationMode.None, default(TimeSpan)))
                        .Should().BeTrue();

                    cache.Expire(key, region, TimeSpan.FromMinutes(10));

                    var item = cache.GetCacheItem(key, region);

                    item.ExpirationTimeout.Should().BeCloseTo(TimeSpan.FromMinutes(10), precision: 200);
                    item.ExpirationMode.Should().Be(ExpirationMode.Sliding);
                }
            }

            [Theory]
            [ClassData(typeof(TestCacheManagers))]
            public void Expiration_Remove_ForKey_Validate<T>(T cache)
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
            [ClassData(typeof(TestCacheManagers))]
            public void Expiration_Remove_ForKeyRegion_Validate<T>(T cache)
                where T : ICacheManager<object>
            {
                using (cache)
                {
                    var key = Guid.NewGuid().ToString();
                    var region = Guid.NewGuid().ToString();
                    cache.Add(new CacheItem<object>(key, region, "value", ExpirationMode.Absolute, TimeSpan.FromMinutes(30)))
                        .Should().BeTrue();

                    cache.RemoveExpiration(key, region);

                    var item = cache.GetCacheItem(key, region);

                    item.ExpirationTimeout.Should().Be(default(TimeSpan));
                    item.ExpirationMode.Should().Be(ExpirationMode.None);
                }
            }
        }

        /* General expiration tests */

        // Related to #136
        [Fact]
        public async Task Expiration_ExtendAbsolut_YieldFalseIsExpired()
        {
            var key = Guid.NewGuid().ToString();

            var item = new CacheItem<string>(key, "value", ExpirationMode.Absolute, TimeSpan.FromMinutes(1));

            await Task.Delay(100);
            item = item.WithAbsoluteExpiration(DateTimeOffset.UtcNow.AddMilliseconds(100));

            // right after changing expiration, should not be expired already. 
            // It might be, if we don't renew the created date... Created date must be updated whenever absolute expiration gets renewed!
            item.IsExpired.Should().BeFalse();

            await Task.Delay(100);
            item.IsExpired.Should().BeTrue();
        }

        [Fact]
        public async Task Expiration_ExtendAbsolut_YieldFalseIsExpired_B()
        {
            var key = Guid.NewGuid().ToString();

            var item = new CacheItem<string>(key, "value", ExpirationMode.Absolute, TimeSpan.FromMinutes(1));

            await Task.Delay(100);
            item = item.WithAbsoluteExpiration(TimeSpan.FromMilliseconds(100));

            // right after changing expiration, should not be expired already. 
            // It might be, if we don't renew the created date... Created date must be updated whenever absolute expiration gets renewed!
            item.IsExpired.Should().BeFalse();

            await Task.Delay(100);
            item.IsExpired.Should().BeTrue();
        }

        [Fact]
        public async Task Expiration_ExtendAbsolut_YieldFalseIsExpired_C()
        {
            var key = Guid.NewGuid().ToString();

            var item = new CacheItem<string>(key, "value", ExpirationMode.Absolute, TimeSpan.FromMinutes(1));

            await Task.Delay(100);
            item = item.WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMilliseconds(100), false);

            // right after changing expiration, should not be expired already. 
            // It might be, if we don't renew the created date... Created date must be updated whenever absolute expiration gets renewed!
            item.IsExpired.Should().BeFalse();

            await Task.Delay(100);
            item.IsExpired.Should().BeTrue();
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        public async Task Expiration_ExtendAbsolut_YieldFalseIsExpired_Expire<T>(T cache)
                where T : ICacheManager<object>
        {
            var key = Guid.NewGuid().ToString();

            var item = new CacheItem<object>(key, "value", ExpirationMode.Absolute, TimeSpan.FromMinutes(1));

            using (cache)
            {
                cache.Add(item);
                await Task.Delay(300);

                cache.Expire(key, DateTimeOffset.UtcNow.AddMilliseconds(300));

                // right after expire, should not be expired already.
                // It might be, if we don't renew the created date... Created date must be updated whenever absolute expiration gets renewed!
                cache.GetCacheItem(key).IsExpired.Should().BeFalse();
            }
        }

        [Fact]
        public void CacheItem_WithExpiration()
        {
            var item = new CacheItem<object>("key", "value", ExpirationMode.Absolute, TimeSpan.FromSeconds(1));

            var absolute = item.WithAbsoluteExpiration(TimeSpan.FromMinutes(10));
            var sliding = item.WithSlidingExpiration(TimeSpan.FromMinutes(10));
            var none = item.WithNoExpiration();

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

#if !NETCOREAPP

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
                var key = Guid.NewGuid().ToString();
                cache.Put(key, "stuip");

                var handles = cache.CacheHandles.ToArray();
                handles[0].GetCacheItem(key).ExpirationMode.Should().Be(ExpirationMode.Absolute);

                // second cache should not inherit the expiration
                handles[1].GetCacheItem(key).ExpirationMode.Should().Be(ExpirationMode.None);
                handles[1].GetCacheItem(key).ExpirationTimeout.Should().Be(default(TimeSpan));
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

        private static async Task TestSlidingExpiration(int timeoutMillis, Func<string, bool> addFunc, Func<string, object> getFunc)
        {
            var tries = 0;
            while (true)
            {
                tries++;
                if (tries > 20)
                {
                    throw new Exception("Timing issues on testing sliding expiration... stopping.");
                }

                var start = Environment.TickCount;
                var key = Guid.NewGuid().ToString();

                /* adding */
                var addResult = addFunc(key);
                if (!addResult)
                {
                    continue;
                }
                addResult.Should().BeTrue("After: " + (Environment.TickCount - start));

                /* testing first iteration */

                try
                {
                    for (var i = 0; i < 3; i++)
                    {
                        start = Environment.TickCount;
                        await Task.Delay((timeoutMillis / 2) + 5);

                        var getResult = getFunc(key);

                        if (getResult == null)
                        {
                            // retry
                            throw new TimeoutException();
                        }

                        getResult.Should().NotBeNull("After: " + (Environment.TickCount - start));
                    }
                }
                catch (TimeoutException)
                {
                    continue;
                }

                /* validate final expiration */
                start = Environment.TickCount;
                await Task.Delay(timeoutMillis + 5);

                var finalResult = getFunc(key);

                finalResult.Should().BeNull("After: " + (Environment.TickCount - start));

                return;
            }
        }

        private static async Task TestAbsoluteExpiration(int timeoutMillis, Func<string, bool> addFunc, Func<string, object> getFunc)
        {
            var tries = 0;
            while (true)
            {
                tries++;
                if (tries > 20)
                {
                    throw new Exception("Timing issues on testing absolute expiration... stopping.");
                }

                var start = Environment.TickCount;
                var key = Guid.NewGuid().ToString();

                /* adding */
                var addResult = addFunc(key);
                if (!addResult)
                {
                    continue;
                }
                addResult.Should().BeTrue("After: " + (Environment.TickCount - start));

                /* testing not expire in time */
                start = Environment.TickCount;
                await Task.Delay((timeoutMillis / 2) + 5);

                var getResult = getFunc(key);

                if (getResult == null)
                {
                    // retry
                    continue;
                }

                getResult.Should().NotBeNull("After: " + (Environment.TickCount - start));

                /* expires after timeout */
                start = Environment.TickCount;
                await Task.Delay((timeoutMillis / 2) + 5);

                var finalResult = getFunc(key);

                finalResult.Should().BeNull("After: " + (Environment.TickCount - start));

                return;
            }
        }

        private static async Task TestRemoveExpiration(int timeoutMillis, Func<string, bool> addFunc, Func<string, bool> removeFunc, Func<string, object> getFunc)
        {
            var tries = 0;
            while (true)
            {
                tries++;
                if (tries > 20)
                {
                    throw new Exception("Timing issues on testing absolute expiration... stopping.");
                }

                var start = Environment.TickCount;
                var key = Guid.NewGuid().ToString();

                /* adding */
                var addResult = addFunc(key);
                if (!addResult)
                {
                    continue;
                }
                addResult.Should().BeTrue("After: " + (Environment.TickCount - start));

                /* testing not expire in time */
                start = Environment.TickCount;
                await Task.Delay((timeoutMillis / 2) + 5);

                var getResult = getFunc(key);

                if (getResult == null)
                {
                    // retry
                    continue;
                }

                if (!removeFunc(key))
                {
                    continue;
                }

                /* expires after timeout */
                start = Environment.TickCount;
                await Task.Delay(timeoutMillis + 5);

                var finalResult = getFunc(key);
                if (finalResult == null)
                {
                    continue;
                }

                finalResult.Should().NotBeNull("After: " + (Environment.TickCount - start));

                return;
            }
        }
    }
}