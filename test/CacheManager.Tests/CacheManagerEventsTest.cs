using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.Core.Logging;
using CacheManager.Core.Utility;
using FluentAssertions;
using Xunit;

namespace CacheManager.Tests
{
    [ExcludeFromCodeCoverage]
    public class CacheManagerEventsTest
    {
        [Fact]
        [ReplaceCulture]
        public void Events_CacheActionEventArgsCtor()
        {
            // arrange
            string key = null;
            string region = null;

            // act
            Action act = () => new CacheActionEventArgs(key, region);

            // assert
            act.ShouldThrow<ArgumentNullException>()
                .WithMessage("*Parameter name: key*");
        }

        [Fact]
        [ReplaceCulture]
        public void Events_CacheActionEventArgsCtor_Valid()
        {
            // arrange
            string key = "key";
            string region = null;

            // act
            Func<CacheActionEventArgs> act = () => new CacheActionEventArgs(key, region);

            // assert
            act().ShouldBeEquivalentTo(new { Region = (string)null, Key = key, Origin = CacheActionEventArgOrigin.Local });
        }

        [Fact]
        [ReplaceCulture]
        public void Events_CacheItemRemovedEventArgsCtor()
        {
            // arrange
            string key = null;
            string region = null;

            // act
            Action act = () => new CacheItemRemovedEventArgs(key, region, CacheItemRemovedReason.Expired);

            // assert
            act.ShouldThrow<ArgumentNullException>()
                .WithMessage("*Parameter name: key*");
        }

        [Fact]
        [ReplaceCulture]
        public void Events_CacheItemRemovedEventArgsCtor_Valid()
        {
            // arrange
            string key = "key";
            string region = null;

            // act
            Func<CacheItemRemovedEventArgs> act = () => new CacheItemRemovedEventArgs(key, region, CacheItemRemovedReason.Expired, 2);

            // assert
            act().ShouldBeEquivalentTo(new { Region = (string)null, Key = key, Reason = CacheItemRemovedReason.Expired, Level = 2 });
        }

        [Fact]
        [ReplaceCulture]
        public void Events_CacheItemRemovedEventArgsCtor_ValidB()
        {
            // arrange
            string key = "key";
            string region = "region";

            // act
            Func<CacheItemRemovedEventArgs> act = () => new CacheItemRemovedEventArgs(key, region, CacheItemRemovedReason.Expired, 2);

            // assert
            act().ShouldBeEquivalentTo(new { Region = region, Key = key, Reason = CacheItemRemovedReason.Expired, Level = 2 });
        }

        [Fact]
        [ReplaceCulture]
        public void Events_CacheItemRemovedEventArgsCtor_ValidC()
        {
            // arrange
            string key = "key";
            string region = "region";

            // act
            Func<CacheItemRemovedEventArgs> act = () => new CacheItemRemovedEventArgs(key, region, CacheItemRemovedReason.Evicted, 0);

            // assert
            act().ShouldBeEquivalentTo(new { Region = region, Key = key, Reason = CacheItemRemovedReason.Evicted, Level = 0 });
        }

        [Fact]
        public void Events_CacheClearEventArgsCtor()
        {
            // arrange act
            Action act = () => new CacheClearEventArgs();

            // assert
            act.ShouldNotThrow();
        }

        [Fact]
        [ReplaceCulture]
        public void Events_CacheClearRegionEventArgsCtor()
        {
            // arrange
            string region = null;

            // act
            Action act = () => new CacheClearRegionEventArgs(region);

            // assert
            act.ShouldThrow<ArgumentNullException>()
                .WithMessage("*Parameter name: region*");
        }

        [Fact]
        [ReplaceCulture]
        public void Events_CacheClearRegionEventArgsCtor_Valid()
        {
            // arrange
            string region = Guid.NewGuid().ToString();

            // act
            Func<CacheClearRegionEventArgs> act = () => new CacheClearRegionEventArgs(region);

            // assert
            act().ShouldBeEquivalentTo(new { Region = region, Origin = CacheActionEventArgOrigin.Local });
        }

        public class LongRunningEventTestBase
        {
            public async Task<CacheItemRemovedEventArgs> RunTest(ICacheManagerConfiguration configuration, string useKey, string useRegion, bool endGetShouldBeNull = true, bool runGetWhileWaiting = true)
            {
                var triggered = false;
                CacheItemRemovedEventArgs resultArgs = null;

                var cache = new BaseCacheManager<string>(configuration);
                cache.OnRemoveByHandle += (sender, args) =>
                {
                    triggered = true;
                    resultArgs = args;
                };

                if (useRegion == null)
                {
                    cache.Add(useKey, "value");
                    cache.Get(useKey).Should().NotBeNull();
                }
                else
                {
                    cache.Add(useKey, "value", useRegion);
                    cache.Get(useKey, useRegion).Should().NotBeNull();
                }

                // sys runtime checks roughly every 10 seconds, there is no other way to test this quicker I think
                var count = 0;
                while (count < 30 && !triggered)
                {
                    if (runGetWhileWaiting)
                    {
                        if (useRegion == null)
                        {
                            cache.CacheHandles.ToList().ForEach(p => p.Get(useKey));
                        }
                        else
                        {
                            cache.CacheHandles.ToList().ForEach(p => p.Get(useKey, useRegion));
                        }
                    }

                    await Task.Delay(1000);
                    count++;
                }

                if (!triggered)
                {
                    throw new Exception("Waited pretty long, no events triggered...");
                }

                // validate on Up update mode, the handles above have been cleaned up for example
                if (endGetShouldBeNull)
                {
                    if (useRegion == null)
                    {
                        cache.Get(useKey).Should().BeNull();
                    }
                    else
                    {
                        cache.Get(useKey, useRegion).Should().BeNull();
                    }
                }

                return resultArgs;
            }
        }

#if !NETCOREAPP

        // exclusive inner class for parallel exec of this long running test
        public class SystemRuntimeSpecific : LongRunningEventTestBase
        {
            [Fact]
            public async Task Events_SysRuntime_ExpireTriggers()
            {
                var cfg = new ConfigurationBuilder()
                    .WithSystemRuntimeCacheHandle()
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(1))
                    .Build();

                string useKey = Guid.NewGuid().ToString();
                string useRegion = "@_@23@_!!";
                var result = await this.RunTest(cfg, useKey, useRegion);

                result.Reason.Should().Be(CacheItemRemovedReason.Expired);
                result.Level.Should().Be(1);
                result.Key.Should().Be(useKey);
                result.Region.Should().Be(useRegion);
            }

            [Fact]
            public async Task Events_SysRuntime_ExpireEvictsAbove()
            {
                var cfg = new ConfigurationBuilder()
                    .WithDictionaryHandle()
                    .And
                    .WithSystemRuntimeCacheHandle()
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(1))
                    .Build();

                string useKey = Guid.NewGuid().ToString();

                var result = await this.RunTest(cfg, useKey, null);

                result.Reason.Should().Be(CacheItemRemovedReason.Expired);
                result.Level.Should().Be(2);
                result.Key.Should().Be(useKey);
                result.Region.Should().BeNull();
            }
        }
#endif

        // exclusive inner class for parallel exec of this long running test
        public class DictionarySpecific : LongRunningEventTestBase
        {
            [Fact]
            public async Task Events_Dic_ExpireTriggers()
            {
                var cfg = new ConfigurationBuilder()
                    .WithDictionaryHandle()
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(1))
                    .Build();

                string useKey = Guid.NewGuid().ToString();
                string useRegion = "@_@23@_!!";
                var result = await this.RunTest(cfg, useKey, useRegion);

                result.Reason.Should().Be(CacheItemRemovedReason.Expired);
                result.Level.Should().Be(1);
                result.Key.Should().Be(useKey);
                result.Region.Should().Be(useRegion);
            }

            [Fact]
            public async Task Events_Dic_ExpireEvictsAbove()
            {
                var cfg = new ConfigurationBuilder()
                    .WithDictionaryHandle()
                    .And
                    .WithDictionaryHandle()
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(1))
                    .Build();

                string useKey = Guid.NewGuid().ToString();

                var result = await this.RunTest(cfg, useKey, null);

                result.Reason.Should().Be(CacheItemRemovedReason.Expired);
                result.Level.Should().Be(2);
                result.Key.Should().Be(useKey);
                result.Region.Should().BeNull();
            }
        }

        // exclusive inner class for parallel exec of this long running test
        public class MsMemorySpecific : LongRunningEventTestBase
        {
            [Fact]
            public async Task Events_MsMemory_ExpireTriggers()
            {
                var cfg = new ConfigurationBuilder()
                    .WithMicrosoftMemoryCacheHandle()
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(1))
                    .Build();

                string useKey = Guid.NewGuid().ToString();
                string useRegion = "@_@23@_!!";
                var result = await this.RunTest(cfg, useKey, useRegion);

                result.Reason.Should().Be(CacheItemRemovedReason.Expired);
                result.Level.Should().Be(1);
                result.Key.Should().Be(useKey);
                result.Region.Should().Be(useRegion);
            }

            [Fact]
            public async Task Events_MsMemory_ExpireEvictsAbove()
            {
                var cfg = new ConfigurationBuilder()
                    .WithDictionaryHandle()
                    .And
                    .WithMicrosoftMemoryCacheHandle()
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(1))
                    .Build();

                string useKey = Guid.NewGuid().ToString();

                var result = await this.RunTest(cfg, useKey, null);

                result.Reason.Should().Be(CacheItemRemovedReason.Expired);
                result.Level.Should().Be(2);
                result.Key.Should().Be(useKey);
                result.Region.Should().BeNull();
            }
        }

#if MOCK_HTTPCONTEXT_ENABLED

        // exclusive inner class for parallel exec of this long running test
        public class WebCacheSpecific : LongRunningEventTestBase
        {
            [Fact]
            public async Task Events_WebCache_ExpireTriggers()
            {
                var cfg = new ConfigurationBuilder()
                    .WithHandle(typeof(SystemWebCacheHandleWrapper<>))
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(1))
                    .Build();

                string useKey = Guid.NewGuid().ToString();
                string useRegion = "@_@23@_!!";
                var result = await this.RunTest(cfg, useKey, useRegion);

                result.Reason.Should().Be(CacheItemRemovedReason.Expired);
                result.Level.Should().Be(1);
                result.Key.Should().Be(useKey);
                result.Region.Should().Be(useRegion);
            }

            [Fact]
            public async Task Events_WebCache_ExpireEvictsAbove()
            {
                var cfg = new ConfigurationBuilder()
                    .WithDictionaryHandle()
                    .And
                    .WithHandle(typeof(SystemWebCacheHandleWrapper<>))
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(1))
                    .Build();

                string useKey = Guid.NewGuid().ToString();

                var result = await this.RunTest(cfg, useKey, null);

                result.Reason.Should().Be(CacheItemRemovedReason.Expired);
                result.Level.Should().Be(2);
                result.Key.Should().Be(useKey);
                result.Region.Should().BeNull();
            }
        }
#endif

        // exclusive inner class for parallel exec of this long running test
        public class RedisSpecific : LongRunningEventTestBase
        {
            [Fact]
            [Trait("category", "Redis")]
            [Trait("category", "Unreliable")]
            public async Task Events_Redis_ExpireTriggers()
            {
                var cfg = new ConfigurationBuilder()
                    .WithRedisConfiguration("redis", "localhost, allowAdmin=true", 0, true)
                    .WithJsonSerializer()
                    .WithRedisCacheHandle("redis")
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(1))
                    .Build();

                string useKey = Guid.NewGuid().ToString();
                string useRegion = "@_@23@_!!";
                var result = await this.RunTest(cfg, useKey, useRegion);

                result.Reason.Should().Be(CacheItemRemovedReason.Expired);
                result.Level.Should().Be(1);
                result.Key.Should().Be(useKey);
                result.Region.Should().Be(useRegion);
            }

            [Fact]
            [Trait("category", "Redis")]
            [Trait("category", "Unreliable")]
            public async Task Events_Redis_ExpireEvictsAbove()
            {
                var cfg = new ConfigurationBuilder()
                    .WithDictionaryHandle()
                    .And
                    .WithRedisConfiguration("redis", "localhost, allowAdmin=true", 0, true)
                    .WithJsonSerializer()
                    .WithRedisCacheHandle("redis")
                    .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(1))
                    .Build();

                string useKey = Guid.NewGuid().ToString();

                var result = await this.RunTest(cfg, useKey, null);

                result.Reason.Should().Be(CacheItemRemovedReason.Expired);
                result.Level.Should().Be(2);
                result.Key.Should().Be(useKey);
                result.Region.Should().BeNull();
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        [ReplaceCulture]
        public void Events_OnGet<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var key1 = Guid.NewGuid().ToString();
                var region1 = Guid.NewGuid().ToString();
                var data = new EventCallbackData();
                cache.OnGet += (sender, args) => data.AddCall(args, key1);
                cache.Add(key1, "something");

                // act get without region, should not return anything and should not trigger the event
                var result = cache.Get(key1);
                var resultWithRegion = cache.Get(key1, region1);

                // assert
                result.Should().Be("something");
                resultWithRegion.Should().BeNull("the key was not set with a region");
                data.Calls.Should().Be(1, "we expect only one hit");
                data.Keys.ShouldAllBeEquivalentTo(new[] { key1 }, "we expect one call");
                data.Regions.Should().BeEmpty();
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        [ReplaceCulture]
        public void Events_OnGetWithRegion<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var key1 = Guid.NewGuid().ToString();
                var region1 = Guid.NewGuid().ToString();
                var data = new EventCallbackData();
                cache.OnGet += (sender, args) => data.AddCall(args, key1);
                cache.Add(key1, "something", region1);

                // act get without region, should not return anything and should not trigger the event
                var resultWithoutRegion = cache.Get(key1);
                var result = cache.Get(key1, region1);

                // assert
                resultWithoutRegion.Should().BeNull("the key was not set without a region");
                result.Should().Be("something");
                data.Calls.Should().Be(1, "we expect only one hit");
                data.Keys.ShouldAllBeEquivalentTo(new[] { key1 });
                data.Regions.ShouldAllBeEquivalentTo(new[] { region1 });
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        [ReplaceCulture]
        public void Events_OnGetMiss<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var key1 = Guid.NewGuid().ToString();
                var region = Guid.NewGuid().ToString();
                var data = new EventCallbackData();
                cache.OnGet += (sender, args) => data.AddCall(args);

                // act
                var result = cache.Get(key1);
                var resultWithRegion = cache.Get(key1, region);

                // assert
                result.Should().BeNull("the key was not set without region");
                resultWithRegion.Should().BeNull("the key was not set with a region");
                data.Calls.Should().Be(0, "we expect only one hit");
                data.Keys.ShouldAllBeEquivalentTo(new string[] { }, "we expect no calls");
                data.Regions.ShouldAllBeEquivalentTo(new string[] { }, "we expect no calls");
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        [ReplaceCulture]
        public void Events_OnGetManyHandles<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var key1 = Guid.NewGuid().ToString();
                var region1 = Guid.NewGuid().ToString();
                var data = new EventCallbackData();

                // all callbacks should be triggered, so result count should be 4
                cache.OnGet += (sender, args) => data.AddCall(args, key1);
                cache.OnGet += (sender, args) => data.AddCall(args, key1);
                cache.OnGet += (sender, args) => data.AddCall(args, key1);
                cache.OnGet += (sender, args) => data.AddCall(args, key1);
                cache.Add(key1, "something", region1);

                // act get without region, should not return anything and should not trigger the event
                var result = cache.Get(key1, region1);

                // assert
                result.Should().Be("something");
                data.Calls.Should().Be(4, "we expect 4 hits");
                data.Keys.ShouldAllBeEquivalentTo(Enumerable.Repeat(key1, 4), "we expect 4 hits");
                data.Regions.ShouldAllBeEquivalentTo(Enumerable.Repeat(region1, 4), "we expect 4 hits");
            }
        }

        /// <summary>
        /// Validates that many event subscriptions all get called Validates that remove misses do
        /// not trigger Validates that other events do not trigger Validates that it works with and
        /// without region.
        /// </summary>
        /// <typeparam name="T">The cache type.</typeparam>
        /// <param name="cache">The cache instance.</param>
        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        [Trait("category", "Unreliable")]
        [ReplaceCulture]
        public void Events_OnRemoveMany<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                var mgr = cache as BaseCacheManager<T>;

                // arrange
                var key1 = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();
                var region1 = Guid.NewGuid().ToString();
                var region2 = Guid.NewGuid().ToString();
                var data = new EventCallbackData();

                // all callbacks should be triggered, so result count should be 4
                cache.OnRemove += (sender, args) => data.AddCall(args, key1, key2);
                cache.OnRemove += (sender, args) => data.AddCall(args, key1, key2);
                cache.OnRemove += (sender, args) => data.AddCall(args, key1, key2);
                cache.OnRemove += (sender, args) => data.AddCall(args, key1, key2);
                cache.OnGet += (sender, args) => data.AddCall(args, key1, key2);  // this should not trigger
                cache.Add(key1, "something", region1);
                cache.Add(key2, "something", region2);

                // act get without region, should not return anything and should not trigger the event
                var r1 = cache.Remove(key1);              // false
                var r2 = cache.Remove(key1, region1);    // true
                var r3 = cache.Remove(key2, Guid.NewGuid().ToString());   // false
                var r4 = cache.Remove(key2, region2);   // true

                // assert
                r1.Should().BeFalse(key1 + cache.ToString());
                r2.Should().BeTrue($"{key1} {region1}" + cache.ToString());
                r3.Should().BeFalse($"{key2} random region." + cache.ToString());
                r4.Should().BeTrue($"{key2} {region2}" + cache.ToString());

                data.Calls.Should().Be(8, $"we expect 8 hits for {key1} and {key2} \n-> keys: " + string.Join(", ", data.Keys));
                data.Keys.ShouldAllBeEquivalentTo(
                    Enumerable.Repeat(key1, 4).Concat(Enumerable.Repeat(key2, 4)),
                    cfg => cfg.WithStrictOrdering(),
                    "we expect 8 hits");

                data.Regions.ShouldAllBeEquivalentTo(
                    Enumerable.Repeat(region1, 4).Concat(Enumerable.Repeat(region2, 4)),
                    cfg => cfg.WithStrictOrdering(),
                    "we expect 8 hits");
            }
        }

        /// <summary>
        /// Validates that many event subscriptions all get called Validates that add misses do not
        /// trigger Validates that other events do not trigger Validates that it works with and
        /// without region.
        /// </summary>
        /// <typeparam name="T">The cache type.</typeparam>
        /// <param name="cache">The cache instance.</param>
        ////[Theory(Skip = "Doesn't work well in parallel")]
        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        [Trait("category", "Unreliable")]
        [ReplaceCulture]
        public void Events_OnAddMany<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var key1 = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();
                var region1 = Guid.NewGuid().ToString();
                var region2 = Guid.NewGuid().ToString();
                var data = new EventCallbackData();

                // all callbacks should be triggered, so result count should be 4
                cache.OnAdd += (sender, args) => data.AddCall(args, key1, key2);
                cache.OnAdd += (sender, args) => data.AddCall(args, key1, key2);
                cache.OnAdd += (sender, args) => data.AddCall(args, key1, key2);
                cache.OnGet += (sender, args) => data.AddCall(args, key1, key2);  // this should not trigger
                cache.OnRemove += (sender, args) => data.AddCall(args, key1, key2);  // this should not trigger

                // act get without region, should not return anything and should not trigger the event
                var r1 = cache.Add(key1, "something", region1);  // true
                var r2 = cache.Add(key2, "something", region2); // true
                var r3 = cache.Add(key1, "something", region1);  // false
                var r4 = cache.Add(key2, "something", region2); // false
                var r5 = cache.Add(key1, "something");            // true
                var r6 = cache.Add(key1, "something");            // false

                // assert
                (r1 && r2 && r5).Should().BeTrue();
                (r3 && r4 && r6).Should().BeFalse();

                // 3x true x 3 event handles = 9 calls
                data.Calls.Should().Be(9, "we expect 9 hits");
                data.Keys.ShouldAllBeEquivalentTo(
                    Enumerable.Repeat(key1, 3)
                        .Concat(Enumerable.Repeat(key2, 3))
                        .Concat(Enumerable.Repeat(key1, 3)),
                    cfg => cfg.WithStrictOrdering(),
                    "we expect 9 hits");

                data.Regions.ShouldAllBeEquivalentTo(
                    Enumerable.Repeat(region1, 3)                      // 3 times region
                        .Concat(Enumerable.Repeat(region2, 3)),       // 3 times region2
                    cfg => cfg.WithStrictOrdering(),
                    "we expect 6 hits");
            }
        }

        /// <summary>
        /// Validates that many event subscriptions all get called Validates that other events do
        /// not trigger Validates that it works with and without region.
        /// </summary>
        /// <typeparam name="T">The cache type.</typeparam>
        /// <param name="cache">The cache instance.</param>
        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        [Trait("category", "Unreliable")]
        [ReplaceCulture]
        public void Events_OnPutMany<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var key1 = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();
                var region1 = Guid.NewGuid().ToString();
                var region2 = Guid.NewGuid().ToString();
                var data = new EventCallbackData();

                // all callbacks should be triggered, so result count should be 4
                cache.OnPut += (sender, args) => data.AddCall(args, key1, key2);
                cache.OnPut += (sender, args) => data.AddCall(args, key1, key2);
                cache.OnPut += (sender, args) => data.AddCall(args, key1, key2);
                cache.OnAdd += (sender, args) => data.AddCall(args, key1, key2);  // this should not trigger
                cache.OnGet += (sender, args) => data.AddCall(args, key1, key2);  // this should not trigger
                cache.OnRemove += (sender, args) => data.AddCall(args, key1, key2);  // this should not trigger

                // act get without region, should not return anything and should not trigger the event
                cache.Put(key1, "something", region1);
                cache.Put(key2, "something", region2);
                cache.Put(key1, "something", region1);
                cache.Put(key1, "something");

                // assert 4x Put calls x 3 event handles = 12 calls
                data.Calls.Should().Be(12, $"we expect 12 hits for {key1} and {key2} \n-> keys: " + string.Join(", ", data.Keys));
                data.Keys.ShouldAllBeEquivalentTo(
                    Enumerable.Repeat(key1, 3)
                        .Concat(Enumerable.Repeat(key2, 3))
                        .Concat(Enumerable.Repeat(key1, 6)),
                    cfg => cfg.WithStrictOrdering(),
                    "we expect 12 hits");

                data.Regions.ShouldAllBeEquivalentTo(
                    Enumerable.Repeat(region1, 3)                      // 3 times region
                        .Concat(Enumerable.Repeat(region2, 3))        // 3 times region2
                        .Concat(Enumerable.Repeat(region1, 3)),         // 3 times region
                    cfg => cfg.WithStrictOrdering(),
                    "we expect 12 hits");
            }
        }

        /// <summary>
        /// Validates that many event subscriptions all get called Validates that other events do
        /// not trigger Validates that it works with and without region.
        /// </summary>
        /// <typeparam name="T">The cache type.</typeparam>
        /// <param name="cache">The cache instance.</param>
        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        [Trait("category", "Unreliable")]
        [ReplaceCulture]
        public void Events_OnUpdate<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var data = new EventCallbackData();
                var key1 = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();
                var region1 = Guid.NewGuid().ToString();
                var region2 = Guid.NewGuid().ToString();

                cache.OnUpdate += (sender, args) => data.AddCall(args, key1, key2);
                cache.OnPut += (sender, args) => data.AddCall(args, key1, key2);    // this should not trigger
                cache.OnAdd += (sender, args) => data.AddCall(args, key1, key2);    // we should have 3times add
                cache.OnGet += (sender, args) => data.AddCall(args, key1, key2);    // this should not trigger
                cache.OnRemove += (sender, args) => data.AddCall(args, key1, key2);  // this should not trigger

                // act get without region, should not return anything and should not trigger the event
                cache.Add(key1, 1, region1).Should().BeTrue("add key1 to region");
                cache.Add(key2, 1, region2).Should().BeTrue("add key2 to region2");
                cache.Add(key1, 1).Should().BeTrue("add key1");

                object val;
                cache.TryUpdate(key1, region1, o => ((int)o) + 1, out val).Should().BeTrue();
                cache.TryUpdate(key2, region2, o => ((int)o) + 1, out val).Should().BeTrue();
                cache.TryUpdate(key1, o => ((int)o) + 1, out val).Should().BeTrue();

                // assert 4x Put calls x 3 event handles = 12 calls
                data.Calls.Should().Be(6, "we expect 6 hits");
                data.Keys.ShouldAllBeEquivalentTo(
                    new string[] { key1, key2, key1, key1, key2, key1 },
                    cfg => cfg.WithStrictOrdering(),
                    "we expect 3 adds and 3 updates in exact order");

                data.Regions.ShouldAllBeEquivalentTo(
                    new string[] { region1, region2, region1, region2, },
                    cfg => cfg.WithStrictOrdering(),
                    "we expect 4 region hits");
            }
        }

        /// <summary>
        /// Validates that many event subscriptions all get called Validates that other events do
        /// not trigger Validates that it works with and without region.
        /// </summary>
        /// <typeparam name="T">The cache type.</typeparam>
        /// <param name="cache">The cache instance.</param>
        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        [Trait("category", "Unreliable")]
        [ReplaceCulture]
        public void Events_OnClearRegion<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var key1 = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();
                var region1 = Guid.NewGuid().ToString();
                var region2 = Guid.NewGuid().ToString();
                var data = new EventCallbackData();

                // all callbacks should be triggered, so result count should be 6
                cache.OnClearRegion += (sender, args) => data.AddCall(args, region1, region2);
                cache.OnClearRegion += (sender, args) => data.AddCall(args, region1, region2);
                cache.OnClearRegion += (sender, args) => data.AddCall(args, region1, region2);
                cache.OnClear += (sender, args) => data.AddCall();                // this should not trigger
                cache.OnGet += (sender, args) => data.AddCall(args, key1, key2);  // this should not trigger

                // on remove now triggeres per cache handle eventually
                cache.OnRemove += (sender, args) => data.AddCall(args, key1, key2);  // this should not trigger
                cache.Put(key1, "something", region1);
                cache.Put(key2, "something", region2);
                cache.Put(key1, "something", region1);
                cache.Put(key1, "something");

                // act get without region, should not return anything and should not trigger the event
                cache.ClearRegion(region1);
                cache.ClearRegion(region2);

                // assert 2x calls x 3 event handles = 6 calls
                data.Calls.Should().Be(6, $"we expect 6 hits for {key1} and {key2} \n-> keys: " + string.Join(", ", data.Keys));

                data.Regions.ShouldAllBeEquivalentTo(
                    Enumerable.Repeat(region1, 3)                  // 3 times region
                        .Concat(Enumerable.Repeat(region2, 3)),    // 3 times region2
                    cfg => cfg.WithStrictOrdering(),
                    "we expect 6 hits");
            }
        }

        [Fact]
        [ReplaceCulture]
        public void Events_OnClear()
        {
            using (var cache = TestManagers.WithOneDicCacheHandle)
            {
                // arrange
                var key1 = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();
                var region1 = Guid.NewGuid().ToString();
                var region2 = Guid.NewGuid().ToString();
                var data = new EventCallbackData();

                // all callbacks should be triggered, so result count should be 4
                cache.OnClear += (sender, args) => data.AddCall();
                cache.OnClear += (sender, args) => data.AddCall();
                cache.OnClear += (sender, args) => data.AddCall();
                cache.OnClearRegion += (sender, args) => data.AddCall(args, key1, key2); // this should not trigger
                cache.OnGet += (sender, args) => data.AddCall(args, key1, key2);  // this should not trigger
                cache.OnRemove += (sender, args) => data.AddCall(args, key1, key2);  // this should not trigger
                cache.Put(key1, "something", region1);
                cache.Put(key2, "something", region2);
                cache.Put(key1, "something", region1);
                cache.Put(key1, "something");

                // act
                cache.Clear();
                cache.Clear();

                // assert 2x calls x 3 event handles = 6 calls
                data.Calls.Should().Be(6, "we expect 6 hits");
            }
        }

        [Fact]
        public void Events_MockedCustomRemove_Evicted()
        {
            using (var cache = CacheFactory.Build<string>(
                s => s.WithHandle(typeof(CustomRemoveEventTestHandle))))
            {
                CacheItemRemovedReason reason = 0;
                string key = string.Empty;
                string region = string.Empty;

                cache.OnRemoveByHandle += (sender, args) =>
                {
                    reason = args.Reason;
                    key = args.Key;
                    region = args.Region;
                };

                var handle = cache.CacheHandles.OfType<CustomRemoveEventTestHandle>().First();
                handle.TestTrigger("key", "region", CacheItemRemovedReason.Evicted);

                reason.Should().Be(CacheItemRemovedReason.Evicted);
                key.Should().Be("key");
                region.Should().Be("region");
            }
        }

        [Fact]
        public void Events_MockedCustomRemove_Expired()
        {
            using (var cache = CacheFactory.Build<string>(
                s => s.WithHandle(typeof(CustomRemoveEventTestHandle))))
            {
                CacheItemRemovedReason reason = 0;
                string key = string.Empty;
                string region = string.Empty;

                cache.OnRemoveByHandle += (sender, args) =>
                {
                    reason = args.Reason;
                    key = args.Key;
                    region = args.Region;
                };

                var handle = cache.CacheHandles.OfType<CustomRemoveEventTestHandle>().First();
                handle.TestTrigger("key", "region", CacheItemRemovedReason.Expired);

                reason.Should().Be(CacheItemRemovedReason.Expired);
                key.Should().Be("key");
                region.Should().Be("region");
            }
        }

        [Fact]
        public void Events_MockedCustomRemove_TestLevel()
        {
            using (var cache = CacheFactory.Build<string>(
                s => s.WithHandle(typeof(CustomRemoveEventTestHandle))))
            {
                int? level = null;

                cache.OnRemoveByHandle += (sender, args) =>
                {
                    level = args.Level;
                };

                var handle = cache.CacheHandles.OfType<CustomRemoveEventTestHandle>().First();
                handle.TestTrigger("key", null, CacheItemRemovedReason.Expired);

                level.Should().Be(1);
            }
        }

        [Fact]
        public void Events_MockedCustomRemove_TestMultiLevelA()
        {
            using (var cache = CacheFactory.Build<string>(
                s => s.WithHandle(typeof(CustomRemoveEventTestHandle))
                    .And.WithHandle(typeof(CustomRemoveEventTestHandle))
                    .And.WithHandle(typeof(CustomRemoveEventTestHandle))))
            {
                int? level = null;

                cache.OnRemoveByHandle += (sender, args) =>
                {
                    level = args.Level;
                };

                // tests if triggereing the first one really triggers the correct level
                var handle = cache.CacheHandles.OfType<CustomRemoveEventTestHandle>().First();
                handle.TestTrigger("key", null, CacheItemRemovedReason.Expired);

                level.Should().Be(1);
            }
        }

        [Fact]
        public void Events_MockedCustomRemove_TestMultiLevelB()
        {
            using (var cache = CacheFactory.Build<string>(
                s => s
                    .WithUpdateMode(CacheUpdateMode.None) // prevent trigger cleanup above (not implemented in the mock handle)
                    .WithHandle(typeof(CustomRemoveEventTestHandle))
                    .And.WithHandle(typeof(CustomRemoveEventTestHandle))
                    .And.WithHandle(typeof(CustomRemoveEventTestHandle))))
            {
                int? level = null;

                cache.OnRemoveByHandle += (sender, args) =>
                {
                    level = args.Level;
                };

                // tests if triggereing the last one really triggers the correct level
                var handle = cache.CacheHandles.OfType<CustomRemoveEventTestHandle>().Last();
                handle.TestTrigger("key", null, CacheItemRemovedReason.Expired);

                level.Should().Be(3);
            }
        }

        private class EventCallbackData
        {
            public EventCallbackData()
            {
                this.Keys = new List<string>();
                this.Regions = new List<string>();
                this.Results = new List<UpdateItemResult<object>>();
            }

            public int Calls { get; set; }

            public List<string> Keys { get; set; }

            public List<string> Regions { get; set; }

            public List<UpdateItemResult<object>> Results { get; set; }

            internal void AddCall(CacheActionEventArgs args, params string[] validKeys)
            {
                Guard.NotNullOrEmpty(validKeys, nameof(validKeys));
                if (validKeys.Contains(args.Key))
                {
                    this.Calls++;
                    this.Keys.Add(args.Key);
                    if (!string.IsNullOrWhiteSpace(args.Region))
                    {
                        this.Regions.Add(args.Region);
                    }
                }
            }

            internal void AddCall(CacheItemRemovedEventArgs args, params string[] validKeys)
            {
                Guard.NotNullOrEmpty(validKeys, nameof(validKeys));
                if (validKeys.Contains(args.Key))
                {
                    this.Calls++;
                    this.Keys.Add(args.Key);
                    if (!string.IsNullOrWhiteSpace(args.Region))
                    {
                        this.Regions.Add(args.Region);
                    }
                }
            }

            internal void AddCall(CacheClearRegionEventArgs args, params string[] validKeys)
            {
                Guard.NotNullOrEmpty(validKeys, nameof(validKeys));
                if (validKeys.Contains(args.Region))
                {
                    this.Calls++;
                    this.Regions.Add(args.Region);
                }
            }

            internal void AddCall()
            {
                this.Calls++;
            }
        }

        private class CustomRemoveEventTestHandle : BaseCacheHandle<string>
        {
            public CustomRemoveEventTestHandle(ICacheManagerConfiguration managerConfiguration, CacheHandleConfiguration configuration)
                : base(managerConfiguration, configuration)
            {
            }

            public void TestTrigger(string key, string region, CacheItemRemovedReason reason)
            {
                this.TriggerCacheSpecificRemove(key, region, reason);
            }

            public override int Count
            {
                get
                {
                    return 0;
                }
            }

            protected override ILogger Logger
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override void Clear()
            {
                throw new NotImplementedException();
            }

            public override void ClearRegion(string region)
            {
                throw new NotImplementedException();
            }

            public override bool Exists(string key)
            {
                throw new NotImplementedException();
            }

            public override bool Exists(string key, string region)
            {
                throw new NotImplementedException();
            }

            protected override bool AddInternalPrepared(CacheItem<string> item)
            {
                throw new NotImplementedException();
            }

            protected override CacheItem<string> GetCacheItemInternal(string key)
            {
                throw new NotImplementedException();
            }

            protected override CacheItem<string> GetCacheItemInternal(string key, string region)
            {
                throw new NotImplementedException();
            }

            protected override void PutInternalPrepared(CacheItem<string> item)
            {
                throw new NotImplementedException();
            }

            protected override bool RemoveInternal(string key)
            {
                throw new NotImplementedException();
            }

            protected override bool RemoveInternal(string key, string region)
            {
                throw new NotImplementedException();
            }
        }
    }
}