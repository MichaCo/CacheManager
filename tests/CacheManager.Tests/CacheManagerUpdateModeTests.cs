using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CacheManager.Core;
using CacheManager.Core.Cache;
using CacheManager.Core.Configuration;
using FluentAssertions;
using Moq;
using Xunit;

namespace CacheManager.Tests
{
    [ExcludeFromCodeCoverage]
#if NET40
    [Trait("Framework", "NET40")]
#else
    [Trait("Framework", "NET45")]
#endif
    public class CacheManagerUpdateModeTests
    {
        private Func<CacheUpdateMode, int> testHandleAddCalls = (mode) =>
        {
            var addCalls = 0;
            var value = "something";

            // creating 20 handles, the 10th should return some value for any key, so the cache
            // manager should update all handles (calling addA) depending on the mode, meaning we
            // simply have to count the add calls.
            var handles = new List<BaseCacheHandle<object>>();
            for (int i = 0; i < 20; i++)
            {
                var handleMock = new Mock<BaseCacheHandle<object>>();
                handleMock
                    .Setup(p => p.Add(It.IsAny<CacheItem<object>>()))
                    .Callback(() => addCalls++)
                    .Returns(true);

                handleMock.Setup(p => p.Stats).Returns(new CacheStats<object>("cache", "handle"));
                handleMock.Setup(p => p.Configuration).Returns(new CacheHandleConfiguration("handle"));

                if (i == 10)
                {
                    handleMock
                        .Setup(p => p.GetCacheItem(It.IsAny<string>()))
                        .Returns(new CacheItem<object>("somekey", "something"));
                }

                handles.Add(handleMock.Object);
            }
            var cfg = ConfigurationBuilder.BuildConfiguration(settings => settings.WithUpdateMode(mode));
            var cache = new BaseCacheManager<object>("cacheName", cfg, handles.ToArray());
            cache.Get("somekey").Should().Be(value);

            return addCalls;
        };

        [Fact]
        public void CacheManager_UpdateModeTests_All()
        {
            // act
            var result = this.testHandleAddCalls(CacheUpdateMode.Full);

            // assert
            result.Should().Be(19, " cachemanger should have updated all other 19  handles"); // 19 other handles should be updated.
        }

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