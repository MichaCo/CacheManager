using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CacheManager.Core;
using CacheManager.Core.Internal;
using FluentAssertions;
using Xunit;

namespace CacheManager.Tests
{
    [ExcludeFromCodeCoverage]
    public class CacheManagerUpdateModeTests
    {
        private Func<CacheUpdateMode, int> testHandleAddCalls = (mode) =>
        {
            var addCalls = 0;
            var key = "somekey";
            var value = "something";

            // creating 20 handles, the 10th should return some value for any key, so the cache
            // manager should update all handles (calling addA) depending on the mode, meaning we
            // simply have to count the add calls.
            var handles = new List<BaseCacheHandle<object>>();

            var cache = CacheFactory.Build<object>(
                settings =>
                {
                    settings.WithUpdateMode(mode);
                    for (int i = 0; i < 20; i++)
                    {
                        settings.WithHandle(typeof(MockCacheHandle<>), "handle" + i);
                    }
                });

            var count = 0;
            foreach (var handle in cache.CacheHandles)
            {
                var mockHandle = handle as MockCacheHandle<object>;
                mockHandle.AddCall = () =>
                {
                    addCalls++;
                    return true;
                };

                if (count == 10)
                {
                    mockHandle.GetCallValue = new CacheItem<object>(key, value);
                }

                count++;
            }

            cache.Get(key).Should().Be(value);

            return addCalls;
        };
        
        [Fact]
        public void CacheManager_UpdateModeTests_Up()
        {
            // act
            var result = this.testHandleAddCalls(CacheUpdateMode.Up);

            // assert
            result.Should().Be(10, " cachemanger should have updated all 10 handles above");
        }

        [Fact]
        public void CacheManager_UpdateModeTests_None()
        {
            // act
            var result = this.testHandleAddCalls(CacheUpdateMode.None);

            // assert
            result.Should().Be(0, " cachemanger should not have updated any handles");
        }
    }
}