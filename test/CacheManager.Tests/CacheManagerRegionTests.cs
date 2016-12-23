using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CacheManager.Core;
using FluentAssertions;
using Xunit;

namespace CacheManager.Tests
{
    /// <summary>
    /// Validates that add and put adds a new item to all handles defined. Validates that remove
    /// removes an item from all handles defined.
    /// </summary>
    [ExcludeFromCodeCoverage]
#if NET40
    [Trait("Framework", "NET40")]
#else
    [Trait("Framework", "NET45")]
#endif
    public class CacheManagerRegionTests : BaseCacheManagerTest
    {
        [Theory]
        [MemberData("TestCacheManagers")]
        public void CacheManager_Region_AddItems_UseSameKeys<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                List<Tuple<string, string, string>> keys;
                List<string> regions;

                // act
                AddRegionData(cache, 23, 3, true, out keys, out regions);

                // assert
                foreach (var item in keys)
                {
                    var region = item.Item1;
                    var key = item.Item2;
                    var value = item.Item3;
                    cache[key].Should().BeNull("the cache should not find the item without region specified");
                    cache[key, region].Should().Be(value, "item should be in cache for given region and key");
                    var otherRegions = regions.Where(p => p != region).ToList();
                    foreach (var otherRegion in otherRegions)
                    {
                        var reason = @"the cache should find the item in other regions,
                            because the keys are the same,
                            but the value must be different";

                        var val = cache.Get(key, otherRegion);
                        val.Should()
                            .BeOfType<string>(reason)
                            .And
                            .NotBe(value, reason);
                    }
                }
            }
        }

        [Theory]
        [MemberData("TestCacheManagers")]
        public void CacheManager_Region_AddItems_UseDifferentKeys<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                List<Tuple<string, string, string>> keys;
                List<string> regions;

                // act
                AddRegionData(cache, 23, 3, false, out keys, out regions);

                // assert
                foreach (var item in keys)
                {
                    var region = item.Item1;
                    var key = item.Item2;
                    var value = item.Item3;
                    cache[key].Should().BeNull("the cache should not find the item without region specified");
                    var val = cache[key, region];
                    val.Should().Be(value, "item should be in cache for given region and key");
                    var otherRegions = regions.Where(p => p != region).ToList();
                    foreach (var otherRegion in otherRegions)
                    {
                        cache.Get(key, otherRegion).Should().Be(null, "the cache should not find the item in other regions");
                    }
                }
            }
        }
        
        [Theory]
        [MemberData("TestCacheManagers")]
        public void CacheManager_Region_ClearRegion<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // Thread.Sleep(1000); arrange
                List<Tuple<string, string, string>> keys;
                List<string> regions;

                // act
                AddRegionData(cache, 20, 17, true, out keys, out regions);

                try
                {
                    var clearedRegion = regions.ElementAt((int)Math.Ceiling(regions.Count / 2d));
                    cache.ClearRegion(clearedRegion);

                    cache.Add("SomeNewItem", "Should be added to the region and the region should be re added to the cache.", clearedRegion);

                    // assert
                    foreach (var item in keys)
                    {
                        var region = item.Item1;
                        var key = item.Item2;
                        var value = item.Item3;

                        cache[key].Should().BeNull("the cache should not find the item without region specified");
                        if (region == clearedRegion)
                        {
                            cache[key, region].Should().BeNull("we cleared the region");
                            cache["SomeNewItem", region].Should().NotBeNull();
                        }
                        else
                        {
                            cache[key, region].Should().Be(value, "item should be in cache for given region and key");
                        }
                    }
                }
                catch
                {
                    throw;
                }
            }
        }

        // Validates #64, Put has a different code path, at least in redis
        [Theory]
        [MemberData("TestCacheManagers")]
        public void CacheManager_Region_Put_ClearRegion<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                var key = Guid.NewGuid().ToString();
                var region = Guid.NewGuid().ToString();

                cache.Put(key, "put value", region);

                cache.Get<string>(key, region).Should().NotBeNull();

                cache.ClearRegion(region);

                cache.Get(key, region).Should().BeNull();
            }
        }

        [Theory]
        [MemberData("TestCacheManagers")]
        public void CacheManager_Region_Put_ModifySomeItems<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                List<Tuple<string, string, string>> keys;
                List<string> regions;
                var itemsPerRegion = 20;
                var regionCount = 17;

                // act
                AddRegionData(cache, itemsPerRegion, regionCount, true, out keys, out regions);

                // lets find 5 random items and modify them, then store the modified keys to be able
                // to assert correctly
                var modifiedKeys = new List<Tuple<string, string>>();
                var rnd = new Random();
                for (int i = 0; i < 5; i++)
                {
                    var item = keys.ElementAt(rnd.Next(0, itemsPerRegion * regionCount));
                    var region = item.Item1;
                    var key = item.Item2;
                    var value = item.Item3;

                    // modifying the item
                    cache[key, region] = "new value";
                    modifiedKeys.Add(new Tuple<string, string>(region, key));
                }

                // assert
                foreach (var item in keys)
                {
                    var region = item.Item1;
                    var key = item.Item2;
                    var value = item.Item3;

                    cache[key].Should().BeNull("the cache should not find the item without region specified");
                    if (modifiedKeys.Any(p => p.Item1 == region && p.Item2 == key))
                    {
                        cache[key, region].Should().Be("new value", "we modified this value");
                    }
                    else
                    {
                        cache[key, region].Should().Be(value, "item should be in cache for given region and key");
                    }
                }
            }
        }

        [Theory]
        [MemberData("TestCacheManagers")]
        public void CacheManager_Region_RemoveItem_RandomRemoveSomeItems<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                List<Tuple<string, string, string>> keys;
                List<string> regions;
                var itemsPerRegion = 20;
                var regionCount = 17;

                // act
                AddRegionData(cache, itemsPerRegion, regionCount, true, out keys, out regions);

                // lets find 5 random items and remove them, then store the removed keys to be able
                // to assert correctly
                var removedKeys = new List<Tuple<string, string>>();
                var rnd = new Random();
                for (int i = 0; i < 5; i++)
                {
                    var item = keys.ElementAt(rnd.Next(0, itemsPerRegion * regionCount));
                    var region = item.Item1;
                    var key = item.Item2;
                    var value = item.Item3;

                    // remove the item
                    cache.Remove(key, region);
                    removedKeys.Add(new Tuple<string, string>(region, key));
                }

                // assert
                foreach (var item in keys)
                {
                    var region = item.Item1;
                    var key = item.Item2;
                    var value = item.Item3;

                    cache[key].Should().BeNull("the cache should not find the item without region specified");
                    if (removedKeys.Any(p => p.Item1 == region && p.Item2 == key))
                    {
                        cache[key, region].Should().BeNull("we removed this value");
                    }
                    else
                    {
                        cache[key, region].Should().Be(value, "item should be in cache for given region and key");
                    }
                }
            }
        }

        private static void AddRegionData(ICache<object> cache, int numItems, int numRegions, bool sameKey, out List<Tuple<string, string, string>> keys, out List<string> regions)
        {
            keys = new List<Tuple<string, string, string>>();
            regions = new List<string>();
            var sameKeyAllRegions = Guid.NewGuid().ToString();

            for (var r = 0; r < numRegions; r++)
            {
                var region = Guid.NewGuid().ToString();

                for (var i = 0; i < numItems; i++)
                {
                    var key = sameKey ? sameKeyAllRegions + i : Guid.NewGuid().ToString();
                    var value = "Value in region " + r + ": " + i;

                    if (!cache.Add(key, value, region))
                    {
                        throw new InvalidOperationException("Adding key " + key + ":" + value + " didn't work.");
                    }

                    keys.Add(new Tuple<string, string, string>(region, key, value));
                }

                regions.Add(region);
            }
        }
    }
}