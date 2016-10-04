using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.Core.Utility;
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
    public class CacheManagerEventsTest : BaseCacheManagerTest
    {
        [Fact]
        [ReplaceCulture]
        public void CacheManager_Events_CacheActionEventArgsCtor()
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
        public void CacheManager_Events_CacheActionEventArgsCtor_Valid()
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
        public void CacheManager_Events_CacheClearEventArgsCtor()
        {
            // arrange act
            Action act = () => new CacheClearEventArgs();

            // assert
            act.ShouldNotThrow();
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Events_CacheClearRegionEventArgsCtor()
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
        public void CacheManager_Events_CacheClearRegionEventArgsCtor_Valid()
        {
            // arrange
            string region = Guid.NewGuid().ToString();

            // act
            Func<CacheClearRegionEventArgs> act = () => new CacheClearRegionEventArgs(region);

            // assert
            act().ShouldBeEquivalentTo(new { Region = region, Origin = CacheActionEventArgOrigin.Local });
        }

        [Theory]
        [MemberData("TestCacheManagers")]
        [ReplaceCulture]
        public void CacheManager_Events_OnGet<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var key1 = Guid.NewGuid().ToString();
                var data = new EventCallbackData();
                cache.OnGet += (sender, args) => data.AddCall(args, key1);
                cache.Add(key1, "something");

                // act get without region, should not return anything and should not trigger the event
                var result = cache.Get(key1);
                var resultWithRegion = cache.Get(key1, "region");

                // assert
                result.Should().Be("something");
                resultWithRegion.Should().BeNull("the key was not set with a region");
                data.Calls.Should().Be(1, "we expect only one hit");
                data.Keys.ShouldAllBeEquivalentTo(new[] { key1 }, "we expect one call");
                data.Regions.Should().BeEmpty();
            }
        }

        [Theory]
        [MemberData("TestCacheManagers")]
        [ReplaceCulture]
        public void CacheManager_Events_OnGetWithRegion<T>(T cache)
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
        [MemberData("TestCacheManagers")]
        [ReplaceCulture]
        public void CacheManager_Events_OnGetMiss<T>(T cache)
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
        [MemberData("TestCacheManagers")]
        [ReplaceCulture]
        public void CacheManager_Events_OnGetManyHandles<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var key1 = Guid.NewGuid().ToString();
                var data = new EventCallbackData();

                // all callbacks should be triggered, so result count should be 4
                cache.OnGet += (sender, args) => data.AddCall(args, key1);
                cache.OnGet += (sender, args) => data.AddCall(args, key1);
                cache.OnGet += (sender, args) => data.AddCall(args, key1);
                cache.OnGet += (sender, args) => data.AddCall(args, key1);
                cache.Add(key1, "something", "region");

                // act get without region, should not return anything and should not trigger the event
                var result = cache.Get(key1, "region");

                // assert
                result.Should().Be("something");
                data.Calls.Should().Be(4, "we expect 4 hits");
                data.Keys.ShouldAllBeEquivalentTo(Enumerable.Repeat(key1, 4), "we expect 4 hits");
                data.Regions.ShouldAllBeEquivalentTo(Enumerable.Repeat("region", 4), "we expect 4 hits");
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
        [MemberData("TestCacheManagers")]
        [Trait("category", "Unreliable")]
        [ReplaceCulture]
        public void CacheManager_Events_OnRemoveMany<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var key1 = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();
                var data = new EventCallbackData();

                // all callbacks should be triggered, so result count should be 4
                cache.OnRemove += (sender, args) => data.AddCall(args, key1, key2);
                cache.OnRemove += (sender, args) => data.AddCall(args, key1, key2);
                cache.OnRemove += (sender, args) => data.AddCall(args, key1, key2);
                cache.OnRemove += (sender, args) => data.AddCall(args, key1, key2);
                cache.OnGet += (sender, args) => data.AddCall(args, key1, key2);  // this should not trigger
                cache.Add(key1, "something", "region");
                cache.Add(key2, "something", "region2");

                // act get without region, should not return anything and should not trigger the event
                var r1 = cache.Remove(key1);              // false
                var r2 = cache.Remove(key1, "region");    // true
                var r3 = cache.Remove(key2, "region3");   // false
                var r4 = cache.Remove(key2, "region2");   // true

                // assert
                (r1 && r3).Should().BeFalse();
                (r2 && r4).Should().BeTrue();

                data.Calls.Should().Be(8, $"we expect 8 hits for {key1} and {key2} \n-> keys: " + string.Join(", ", data.Keys));
                data.Keys.ShouldAllBeEquivalentTo(
                    Enumerable.Repeat(key1, 4).Concat(Enumerable.Repeat(key2, 4)),
                    cfg => cfg.WithStrictOrdering(),
                    "we expect 8 hits");

                data.Regions.ShouldAllBeEquivalentTo(
                    Enumerable.Repeat("region", 4).Concat(Enumerable.Repeat("region2", 4)),
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
        [MemberData("TestCacheManagers")]
        [ReplaceCulture]
        public void CacheManager_Events_OnAddMany<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var key1 = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();
                var data = new EventCallbackData();

                // all callbacks should be triggered, so result count should be 4
                cache.OnAdd += (sender, args) => data.AddCall(args, key1, key2);
                cache.OnAdd += (sender, args) => data.AddCall(args, key1, key2);
                cache.OnAdd += (sender, args) => data.AddCall(args, key1, key2);
                cache.OnGet += (sender, args) => data.AddCall(args, key1, key2);  // this should not trigger
                cache.OnRemove += (sender, args) => data.AddCall(args, key1, key2);  // this should not trigger

                // act get without region, should not return anything and should not trigger the event
                var r1 = cache.Add(key1, "something", "region");  // true
                var r2 = cache.Add(key2, "something", "region2"); // true
                var r3 = cache.Add(key1, "something", "region");  // false
                var r4 = cache.Add(key2, "something", "region2"); // false
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
                    Enumerable.Repeat("region", 3)                      // 3 times region
                        .Concat(Enumerable.Repeat("region2", 3)),       // 3 times region2
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
        [MemberData("TestCacheManagers")]
        [ReplaceCulture]
        public void CacheManager_Events_OnPutMany<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var key1 = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();
                var data = new EventCallbackData();

                // all callbacks should be triggered, so result count should be 4
                cache.OnPut += (sender, args) => data.AddCall(args, key1, key2);
                cache.OnPut += (sender, args) => data.AddCall(args, key1, key2);
                cache.OnPut += (sender, args) => data.AddCall(args, key1, key2);
                cache.OnAdd += (sender, args) => data.AddCall(args, key1, key2);  // this should not trigger
                cache.OnGet += (sender, args) => data.AddCall(args, key1, key2);  // this should not trigger
                cache.OnRemove += (sender, args) => data.AddCall(args, key1, key2);  // this should not trigger

                // act get without region, should not return anything and should not trigger the event
                cache.Put(key1, "something", "region");
                cache.Put(key2, "something", "region2");
                cache.Put(key1, "something", "region");
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
                    Enumerable.Repeat("region", 3)                      // 3 times region
                        .Concat(Enumerable.Repeat("region2", 3))        // 3 times region2
                        .Concat(Enumerable.Repeat("region", 3)),         // 3 times region
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
        [MemberData("TestCacheManagers")]
        [ReplaceCulture]
        public void CacheManager_Events_OnUpdate<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var data = new EventCallbackData();
                var key1 = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();

                cache.OnUpdate += (sender, args) => data.AddCall(args, key1, key2);
                cache.OnPut += (sender, args) => data.AddCall(args, key1, key2);    // this should not trigger
                cache.OnAdd += (sender, args) => data.AddCall(args, key1, key2);    // we should have 3times add
                cache.OnGet += (sender, args) => data.AddCall(args, key1, key2);    // this should not trigger
                cache.OnRemove += (sender, args) => data.AddCall(args, key1, key2);  // this should not trigger

                // act get without region, should not return anything and should not trigger the event
                cache.Add(key1, 1, "region").Should().BeTrue("add key1 to region");
                cache.Add(key2, 1, "region2").Should().BeTrue("add key2 to region2");
                cache.Add(key1, 1).Should().BeTrue("add key1");

                object val;
                cache.TryUpdate(key1, "region", o => ((int)o) + 1, out val).Should().BeTrue();
                cache.TryUpdate(key2, "region2", o => ((int)o) + 1, out val).Should().BeTrue();
                cache.TryUpdate(key1, o => ((int)o) + 1, out val).Should().BeTrue();

                // assert 4x Put calls x 3 event handles = 12 calls
                data.Calls.Should().Be(6, "we expect 6 hits");
                data.Keys.ShouldAllBeEquivalentTo(
                    new string[] { key1, key2, key1, key1, key2, key1 },
                    cfg => cfg.WithStrictOrdering(),
                    "we expect 3 adds and 3 updates in exact order");

                data.Regions.ShouldAllBeEquivalentTo(
                    new string[] { "region", "region2", "region", "region2", },
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
        [MemberData("TestCacheManagers")]
        [Trait("category", "Unreliable")]
        [ReplaceCulture]
        public void CacheManager_Events_OnClearRegion<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var key1 = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();
                var region1 = "region1";
                var region2 = "region2";
                var data = new EventCallbackData();

                // all callbacks should be triggered, so result count should be 6
                cache.OnClearRegion += (sender, args) => data.AddCall(args, region1, region2);
                cache.OnClearRegion += (sender, args) => data.AddCall(args, region1, region2);
                cache.OnClearRegion += (sender, args) => data.AddCall(args, region1, region2);
                cache.OnClear += (sender, args) => data.AddCall();                // this should not trigger
                cache.OnGet += (sender, args) => data.AddCall(args, key1, key2);  // this should not trigger
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
        public void CacheManager_Events_OnClear()
        {
            using (var cache = TestManagers.WithOneDicCacheHandle)
            {
                // arrange
                var key1 = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();
                var data = new EventCallbackData();

                // all callbacks should be triggered, so result count should be 4
                cache.OnClear += (sender, args) => data.AddCall();
                cache.OnClear += (sender, args) => data.AddCall();
                cache.OnClear += (sender, args) => data.AddCall();
                cache.OnClearRegion += (sender, args) => data.AddCall(args, key1, key2); // this should not trigger
                cache.OnGet += (sender, args) => data.AddCall(args, key1, key2);  // this should not trigger
                cache.OnRemove += (sender, args) => data.AddCall(args, key1, key2);  // this should not trigger
                cache.Put(key1, "something", "region");
                cache.Put(key2, "something", "region2");
                cache.Put(key1, "something", "region");
                cache.Put(key1, "something");

                // act
                cache.Clear();
                cache.Clear();

                // assert 2x calls x 3 event handles = 6 calls
                data.Calls.Should().Be(6, "we expect 6 hits");
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
    }
}