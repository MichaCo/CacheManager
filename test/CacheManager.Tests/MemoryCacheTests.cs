using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using CacheManager.Core;
using CacheManager.SystemRuntimeCaching;
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
    public class MemoryCacheTests
    {
        [Fact]
        [Trait("category", "Unreliable")]
        public void SysRuntime_MemoryCache_Absolute_DoesExpire()
        {
            // arrange
            var key = Guid.NewGuid().ToString();
            var item = new CacheItem<object>(key, "something", ExpirationMode.Absolute, new TimeSpan(0, 0, 0, 0, 300));

            // act
            using (var act = this.GetHandle("Default"))
            {
                // act
                act.Add(item);
                act[key].Should().NotBeNull();

                Thread.Sleep(310);

                // assert
                act[key].Should().BeNull();
            }
        }

#if DNX451
        [Fact(Skip = "DNX doesn't read from app.config")]
#else
        [Fact]
#endif
        public void SysRuntime_MemoryCache_CreateDefaultCache()
        {
            using (var act = this.GetHandle("Default"))
            {
                // arrange
                var settings = ((MemoryCacheHandle<object>)act.CacheHandles.ElementAt(0)).CacheSettings;

                // act assert
                settings["CacheMemoryLimitMegabytes"].Should().Be("42");
                settings["PhysicalMemoryLimitPercentage"].Should().Be("69");
                settings["PollingInterval"].Should().Be("00:10:00");
            }
        }

#if DNX451
        [Fact(Skip = "DNX doesn't read from app.config")]
#else
        [Fact]
#endif
        public void SysRuntime_MemoryCache_CreateNamedCache()
        {
            using (var act = this.GetHandle("NamedTest"))
            {
                // arrange
                var settings = ((MemoryCacheHandle<object>)act.CacheHandles.ElementAt(0)).CacheSettings;

                // act assert
                settings["CacheMemoryLimitMegabytes"].Should().Be("12");
                settings["PhysicalMemoryLimitPercentage"].Should().Be("23");
                settings["PollingInterval"].Should().Be("00:02:00");
            }
        }

        [Fact]
        [Trait("category", "Unreliable")]
        public void SysRuntime_MemoryCache_Sliding_DoesExpire()
        {
            // arrange
            var key = Guid.NewGuid().ToString();
            var item = new CacheItem<object>(key, "something", ExpirationMode.Sliding, new TimeSpan(0, 0, 0, 0, 8));

            // act
            using (var act = this.GetHandle("Default"))
            {
                // act
                act.Add(item);

                Thread.Sleep(15);

                // assert
                act[key].Should().BeNull();
            }
        }

        [Fact]
        [Trait("category", "Unreliable")]
        public void SysRuntime_MemoryCache_Sliding_DoesSlide()
        {
            // arrange
            var key = Guid.NewGuid().ToString();
            var item = new CacheItem<object>(key, "something", ExpirationMode.Sliding, new TimeSpan(0, 0, 0, 0, 50));

            // act
            var act = this.GetHandle("Default");
            {
                // act
                act.Add(item);

                var valid = true;
                var state = 0;
                var t = new Thread(new ThreadStart(() =>
                {
                    // assert trying to get the item 2 times after 15ms which are 10ms more then the
                    // TimeSpan of 20ms. So each hit should extend the timeout for 20ms... if not,
                    // the test will fail.
                    Thread.Sleep(30);
                    valid = act[key] != null;

                    if (valid)
                    {
                        state = 1;
                        Thread.Sleep(30);
                        valid = act[key] != null;
                    }

                    // then test if it expires in time...
                    if (valid)
                    {
                        state = 2;
                        Thread.Sleep(50);
                        valid = act[key] == null;
                    }
                }));

                t.Start();
                t.Join();
                valid.Should().BeTrue("State: " + state);
            }
        }

        private ICacheManager<object> GetHandle(string name)
            => CacheFactory.Build(
                "cache1",
                settings =>
                {
                    settings.WithSystemRuntimeCacheHandle(name);
                });
    }
}