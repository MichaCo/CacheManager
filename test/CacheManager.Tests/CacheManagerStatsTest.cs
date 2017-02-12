using System;
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
    [ExcludeFromCodeCoverage]
#if NET40
    [Trait("Framework", "NET40")]
#else
    [Trait("Framework", "NET45")]
#endif
    public class CacheManagerStatsTest
    {
        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        [ReplaceCulture]
        public void CacheManager_Stats_AddGet<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var key1 = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();
                var addCalls = cache.CacheHandles.Select(p => p.Stats.GetStatistic(CacheStatsCounterType.AddCalls));
                var getCalls = cache.CacheHandles.Select(p => p.Stats.GetStatistic(CacheStatsCounterType.GetCalls));
                var misses = cache.CacheHandles.Select(p => p.Stats.GetStatistic(CacheStatsCounterType.Misses));
                var hits = cache.CacheHandles.Select(p => p.Stats.GetStatistic(CacheStatsCounterType.Hits));
                var items = cache.CacheHandles.Select(p => p.Stats.GetStatistic(CacheStatsCounterType.Items));

                // act get without region, should not return anything and should not trigger the event
                var a1 = cache.Add(key1, "something");
                var a2 = cache.Add(key1, "something"); // should not increase adds, but evicts the item from the first handle, so miss +1

                // bot gets should increase first handle +1 and hits +1
                var r1 = cache.Get(key1);
                var r2 = cache[key1];

                // should increase all handles get + 1 and misses +1
                cache.Get(key1, Guid.NewGuid().ToString());

                // assert
                a1.Should().BeTrue();
                a2.Should().BeFalse();
                r1.Should().Be("something");
                r2.Should().Be("something");

                // each cache handle stats should have one addCall increase
                addCalls.ShouldAllBeEquivalentTo(Enumerable.Repeat(1, cache.CacheHandles.Count()));

                items.ShouldAllBeEquivalentTo(
                    Enumerable.Repeat(0, cache.CacheHandles.Count() - 1).Concat(new[] { 1 }));
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Stats_Clear()
        {
            using (var cache = TestManagers.WithOneDicCacheHandle)
            {
                // arrange
                var key1 = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();
                var region = Guid.NewGuid().ToString();
                var clears = cache.CacheHandles.Select(p => p.Stats.GetStatistic(CacheStatsCounterType.ClearCalls));
                cache.Add(key1, "something");
                cache.Add(key2, "something");

                // act
                cache.ClearRegion(region); // should not trigger
                cache.Clear();
                cache.Clear();

                // assert all handles should have 2 clear increases.
                clears.ShouldAllBeEquivalentTo(
                    Enumerable.Repeat(2, cache.CacheHandles.Count()));
            }
        }

        [Fact]
        [ReplaceCulture]
        public void CacheManager_Stats_ClearRegion()
        {
            using (var cache = TestManagers.WithOneDicCacheHandle)
            {
                // arrange
                var key1 = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();
                var region = Guid.NewGuid().ToString();
                var clears = cache.CacheHandles.Select(p => p.Stats.GetStatistic(CacheStatsCounterType.ClearRegionCalls));
                cache.Add(key1, "something");
                cache.Add(key2, "something");
                cache.Add(key2, "something", region);

                // act
                cache.ClearRegion(region);
                cache.Clear();  // should not trigger
                cache.ClearRegion(Guid.NewGuid().ToString());

                // assert all handles should have 2 clearRegion increases.
                clears.ShouldAllBeEquivalentTo(
                    Enumerable.Repeat(2, cache.CacheHandles.Count()));
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        [ReplaceCulture]
        public void CacheManager_Stats_Put<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var key1 = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();
                var region = Guid.NewGuid().ToString();
                var puts = cache.CacheHandles.Select(p => p.Stats.GetStatistic(CacheStatsCounterType.PutCalls));

                // act
                cache.Put(key1, "something");
                cache.Put(key2, "something");
                cache.Put(key2, "something", region);

                // assert all handles should have 2 clearRegion increases.
                puts.ShouldAllBeEquivalentTo(
                    Enumerable.Repeat(3, cache.CacheHandles.Count()));
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        [ReplaceCulture]
        public void CacheManager_Stats_Update<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var key1 = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();
                var adds = cache.CacheHandles.Select(p => p.Stats.GetStatistic(CacheStatsCounterType.AddCalls));
                var gets = cache.CacheHandles.Select(p => p.Stats.GetStatistic(CacheStatsCounterType.GetCalls));
                var hits = cache.CacheHandles.Select(p => p.Stats.GetStatistic(CacheStatsCounterType.Hits));
                cache.Add(key1, "something");
                cache.Add(key2, "something");

                // act
                cache.Update(key1, v => "somethingelse");
                cache.Update(key2, v => "somethingelse");

                // assert could be more than 2 adds.ShouldAllBeEquivalentTo( Enumerable.Repeat(0,
                // cache.CacheHandles.Count)); gets.ShouldAllBeEquivalentTo( Enumerable.Repeat(2,
                // cache.CacheHandles.Count)); hits.ShouldAllBeEquivalentTo( Enumerable.Repeat(2, cache.CacheHandles.Count));
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        [ReplaceCulture]
        public void CacheManager_Stats_Remove<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var key1 = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();
                var region = Guid.NewGuid().ToString();
                var adds = cache.CacheHandles.Select(p => p.Stats.GetStatistic(CacheStatsCounterType.AddCalls));
                var removes = cache.CacheHandles.Select(p => p.Stats.GetStatistic(CacheStatsCounterType.RemoveCalls));

                // act
                var r1 = cache.Remove(key2);               // false
                var r2 = cache.Remove(key2, region);        // false

                var a1 = cache.Add(key1, "something");            // true
                var a2 = cache.Add(key2, "something");            // true
                var a3 = cache.Add(key2, "something", region);    // true
                var a4 = cache.Add(key1, "something");            // false
                var r3 = cache.Remove(key2);                      // true
                var r4 = cache.Remove(key2, region);              // true
                var a5 = cache.Add(key2, "something");            // true
                var a6 = cache.Add(key2, "something", region);    // true

                // assert
                (r1 && r2).Should().BeFalse();
                (r3 && r4).Should().BeTrue();
                a4.Should().BeFalse();
                (a1 && a2 && a3 && a5 && a6).Should().BeTrue();

                // all handles should have 5 add increases.
                adds.ShouldAllBeEquivalentTo(
                    Enumerable.Repeat(5, cache.CacheHandles.Count()));
            }
        }

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        [ReplaceCulture]
        [Trait("category", "Unreliable")]
        public async Task CacheManager_Stats_Threaded<T>(T cache)
            where T : ICacheManager<object>
        {
            var puts = cache.CacheHandles.Select(p => p.Stats.GetStatistic(CacheStatsCounterType.PutCalls));
            var adds = cache.CacheHandles.Select(p => p.Stats.GetStatistic(CacheStatsCounterType.AddCalls));
            var threads = 5;
            var iterations = 10;
            var putCounter = 0;

            using (cache)
            {
                var key = Guid.NewGuid().ToString();
                await ThreadTestHelper.RunAsync(
                    async () =>
                    {
                        cache.Add(key, "hi");
                        cache.Put(key, "changed");
                        Interlocked.Increment(ref putCounter);
                        await Task.Delay(0);
                    },
                    threads,
                    iterations);
            }

            await Task.Delay(20);
            putCounter.Should().Be(threads * iterations);
            puts.ShouldAllBeEquivalentTo(
                    Enumerable.Repeat(threads * iterations, cache.CacheHandles.Count()));
        }
    }
}