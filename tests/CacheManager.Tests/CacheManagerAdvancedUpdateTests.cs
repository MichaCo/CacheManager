using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
    public class CacheManagerAdvancedUpdateTests
    {
        [Fact]
        [ReplaceCulture]
        public void UpdateItemConfig_Default()
        {
            // arrange act
            Func<UpdateItemConfig> act = () => new UpdateItemConfig();

            // assert
            act().ShouldBeEquivalentTo(new { MaxRetries = int.MaxValue, VersionConflictOperation = VersionConflictHandling.EvictItemFromOtherCaches });
        }

        [Fact]
        [ReplaceCulture]
        public void UpdateItemConfig_Ctor()
        {
            // arrange act
            Func<UpdateItemConfig> act = () => new UpdateItemConfig(101, VersionConflictHandling.Ignore);

            // assert
            act().ShouldBeEquivalentTo(new { MaxRetries = 101, VersionConflictOperation = VersionConflictHandling.Ignore });
        }

        [Fact]
        [ReplaceCulture]
        public void UpdateItemConfig_Ctor_InvalidRetries()
        {
            // arrange act
            Action act = () => new UpdateItemConfig(-10, VersionConflictHandling.Ignore);

            // assert
            act.ShouldThrow<ArgumentException>()
                .WithMessage("maxRetries must be *0*");
        }

        [Fact]
        [ReplaceCulture]
        public void UpdateItemResult_Ctor()
        {
            // arrange act
            Func<UpdateItemResult> act = () => new UpdateItemResult(true, true, 1001);

            // assert
            act().ShouldBeEquivalentTo(new { Success = true, NumberOfRetriesNeeded = 1001, VersionConflictOccurred = true });
        }

        [Fact]
        public void CacheManager_Update_ValidateConflictHandle_Ignore()
        {
            // arrange
            Func<string, string> updateFunc = s => s;

            // the update config setting it to Ignore
            UpdateItemConfig updateConfig = new UpdateItemConfig(0, VersionConflictHandling.Ignore);
            var cfg = ConfigurationBuilder.BuildConfiguration(settings => settings.WithUpdateMode(CacheUpdateMode.Up));
            int updateCalls = 0;
            int putCalls = 0;
            int removeCalls = 0;

            var handles = MockHandles(
                count: 5,
                updateCalls: Enumerable.Repeat<Action>(() => updateCalls++, 5).ToArray(),
                updateCallResults: new UpdateItemResult[]
                {
                    new UpdateItemResult(false, true, 0),
                    new UpdateItemResult(true, true, 0),
                    new UpdateItemResult(false, true, 0),
                    new UpdateItemResult(true, true, 0),
                    new UpdateItemResult(true, false, 100)
                },
                putCalls: Enumerable.Repeat<Action>(() => putCalls++, 5).ToArray(),
                removeCalls: Enumerable.Repeat<Action>(() => removeCalls++, 5).ToArray());

            // act
            using (var cache = new BaseCacheManager<string>("cacheName", cfg, handles))
            {
                var updateResult = cache.Update("key", updateFunc, updateConfig);

                // assert
                updateCalls.Should().Be(5, "all handle should have been invoked");
                putCalls.Should().Be(0, "with ignore, the manager should not run put on the other handles");
                removeCalls.Should().Be(0, "no items should have been removed with ignore");
                updateResult.Should().BeTrue("first two handles return true");
            }
        }

        [Fact]
        public void CacheManager_Update_ValidateConflictHandle_Evict()
        {
            // arrange
            Func<string, string> updateFunc = s => s;

            // the update config setting it to Ignore
            UpdateItemConfig updateConfig = new UpdateItemConfig(0, VersionConflictHandling.EvictItemFromOtherCaches);
            var cfg = ConfigurationBuilder.BuildConfiguration(settings => settings.WithUpdateMode(CacheUpdateMode.Up));
            int updateCalls = 0;
            int putCalls = 0;
            int removeCalls = 0;

            var handles = MockHandles(
                count: 5,
                updateCalls: Enumerable.Repeat<Action>(() => updateCalls++, 5).ToArray(),
                updateCallResults: new UpdateItemResult[]
                {
                    new UpdateItemResult(false, true, 0),
                    new UpdateItemResult(false, true, 0),
                    new UpdateItemResult(true, true, 0),    // version conflict but successfully updated
                    new UpdateItemResult(false, true, 0),
                    new UpdateItemResult(true, false, 100)
                },
                putCalls: Enumerable.Repeat<Action>(() => putCalls++, 5).ToArray(),
                removeCalls: Enumerable.Repeat<Action>(() => removeCalls++, 5).ToArray());

            // act
            using (var cache = new BaseCacheManager<string>("cacheName", cfg, handles))
            {
                var updateResult = cache.Update("key", updateFunc, updateConfig);

                // assert
                updateCalls.Should().Be(3, "cache manager should have stopped updating after the first version conflict");
                putCalls.Should().Be(0, "no put calls expected");
                removeCalls.Should().Be(4, "the key should have been removed from the other 4 handles");
                updateResult.Should().BeTrue("we return success in handle 3 allthough there is a version conflict");
            }
        }

        [Fact]
        public void CacheManager_Update_ValidateConflictHandle_EvictWithFailedResult()
        {
            // arrange
            Func<string, string> updateFunc = s => s;

            // the update config setting it to EvictItemFromOtherCaches
            UpdateItemConfig updateConfig = new UpdateItemConfig(0, VersionConflictHandling.EvictItemFromOtherCaches);
            var cfg = ConfigurationBuilder.BuildConfiguration(settings => settings.WithUpdateMode(CacheUpdateMode.Up));
            int updateCalls = 0;
            int putCalls = 0;
            int removeCalls = 0;

            var handles = MockHandles(
                count: 5,
                updateCalls: Enumerable.Repeat<Action>(() => updateCalls++, 5).ToArray(),
                updateCallResults: new UpdateItemResult[]
                {
                    new UpdateItemResult(false, true, 0),
                    new UpdateItemResult(false, true, 0),
                    new UpdateItemResult(true, false, 0),    // version conflict but failed to update
                    new UpdateItemResult(false, true, 0),
                    new UpdateItemResult(true, false, 100)
                },
                putCalls: Enumerable.Repeat<Action>(() => putCalls++, 5).ToArray(),
                removeCalls: Enumerable.Repeat<Action>(() => removeCalls++, 5).ToArray());

            // act
            using (var cache = new BaseCacheManager<string>("cacheName", cfg, handles))
            {
                var updateResult = cache.Update("key", updateFunc, updateConfig);

                // assert
                updateCalls.Should().Be(3, "cache manager should have stopped updating after the first version conflict");
                putCalls.Should().Be(0, "no put calls expected");
                removeCalls.Should().Be(0, "the key should have been removed from the other 4 handles");
                updateResult.Should().BeFalse("the update in handle 3 was not successful.");
            }
        }

        [Fact]
        public void CacheManager_Update_ValidateConflictHandle_UpdateOtherCaches()
        {
            // arrange
            Func<string, string> updateFunc = s => s;

            // the update config setting it to UpdateOtherCaches
            UpdateItemConfig updateConfig = new UpdateItemConfig(0, VersionConflictHandling.UpdateOtherCaches);
            var cfg = ConfigurationBuilder.BuildConfiguration(settings => settings.WithUpdateMode(CacheUpdateMode.Up));
            int updateCalls = 0;
            int putCalls = 0;
            int removeCalls = 0;

            var handles = MockHandles(
                count: 5,
                updateCalls: Enumerable.Repeat<Action>(() => updateCalls++, 5).ToArray(),
                updateCallResults: new UpdateItemResult[]
                {
                    new UpdateItemResult(false, true, 0),
                    new UpdateItemResult(false, true, 0),
                    new UpdateItemResult(true, true, 0),    // version conflict but successfully updated
                                                            // this should trigger cache manager to
                                                            // update the other 4 handles with the
                                                            // new version
                    new UpdateItemResult(false, true, 0),
                    new UpdateItemResult(true, false, 100)
                },
                putCalls: Enumerable.Repeat<Action>(() => putCalls++, 5).ToArray(),
                removeCalls: Enumerable.Repeat<Action>(() => removeCalls++, 5).ToArray(),
                getCallValues: new CacheItem<string>[]
                {
                    null,
                    null,
                    new CacheItem<string>("key", "updated value"),
                    null,
                    null
                });

            // act
            using (var cache = new BaseCacheManager<string>("cacheName", cfg, handles))
            {
                var updateResult = cache.Update("key", updateFunc, updateConfig);

                // assert
                updateCalls.Should().Be(3, "first 3 updates until version conflict");
                putCalls.Should().Be(4, "cache manager should only update the other 4 handles");
                removeCalls.Should().Be(0, "nothing should be removed");
                updateResult.Should().BeTrue("updated successfully.");
            }
        }

        [Fact]
        public void CacheManager_Update_UpdateOtherCaches_ValidateItem()
        {
            // arrange
            Func<string, string> updateFunc = s => "updated value";

            // the update config setting it to UpdateOtherCaches
            UpdateItemConfig updateConfig = new UpdateItemConfig(0, VersionConflictHandling.UpdateOtherCaches);
            var cfg = ConfigurationBuilder.BuildConfiguration(settings => settings.WithUpdateMode(CacheUpdateMode.Up));
            int updateCalls = 0;
            int putCalls = 0;
            int removeCalls = 0;

            var handles = MockHandles(
                count: 5,
                updateCalls: Enumerable.Repeat<Action>(() => updateCalls++, 5).ToArray(),
                updateCallResults: new UpdateItemResult[]
                {
                    new UpdateItemResult(false, true, 0),
                    new UpdateItemResult(false, true, 0),
                    new UpdateItemResult(true, true, 0),    // version conflict but successfully updated
                                                            // this should trigger cache manager to
                                                            // update the other 4 handles with the
                                                            // new version
                    new UpdateItemResult(false, true, 0),
                    new UpdateItemResult(true, false, 100)
                },
                putCalls: Enumerable.Repeat<Action>(() => putCalls++, 5).ToArray(),
                removeCalls: Enumerable.Repeat<Action>(() => removeCalls++, 5).ToArray(),
                getCallValues: new CacheItem<string>[]
                {
                    null,
                    null,
                    new CacheItem<string>("key", "updated value"),
                    null,
                    null
                });

            // act
            using (var cache = CacheFactory.Build<string>("myCache", settings =>
            {
                settings.WithSystemRuntimeCacheHandle("default")
                    .EnableStatistics();
            }))
            {
                var localCache = cache as BaseCacheManager<string>;

                var handle1 = cache.CacheHandles.ElementAt(0);
                foreach (var handle in handles)
                {
                    localCache.AddCacheHandle(handle);
                }
                cache.Add("key", "something", "region");

                var updateResult = cache.Update("key", "region", updateFunc, updateConfig);

                var result = cache.Get("key", "region");

                // assert
                handle1.Stats.GetStatistic(CacheStatsCounterType.Items).Should().Be(1);
                result.Should().Be("updated value");
                updateCalls.Should().Be(3, "first 3 updates until version conflict");
                putCalls.Should().Be(4, "cache manager should only update the other 4 handles");
                removeCalls.Should().Be(0, "nothing should be removed");
                updateResult.Should().BeTrue("updated successfully.");
            }
        }

        private static BaseCacheHandle<string>[] MockHandles(int count, Action[] updateCalls, UpdateItemResult[] updateCallResults, Action[] putCalls, Action[] removeCalls, CacheItem<string>[] getCallValues = null)
        {
            if (count <= 0)
            {
                throw new InvalidOperationException();
            }

            if (updateCalls.Length != count || updateCallResults.Length != count || putCalls.Length != count || removeCalls.Length != count)
            {
                throw new InvalidOperationException("Count and arrays must match");
            }

            var cacheName = "myCache";
            var handles = new List<BaseCacheHandle<string>>();
            for (int i = 0; i < count; i++)
            {
                var handleName = "handle" + i;
                var handleMock = new Mock<BaseCacheHandle<string>>();
                handleMock
                    .Setup(p => p.Update(It.IsAny<string>(), It.IsAny<Func<string, string>>(), It.IsAny<UpdateItemConfig>()))
                    .Callback(updateCalls[i])
                    .Returns(updateCallResults[i]);
                handleMock
                    .Setup(p => p.Update(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<string, string>>(), It.IsAny<UpdateItemConfig>()))
                    .Callback(updateCalls[i])
                    .Returns(updateCallResults[i]);
                // we also count the Put calls because second handle returns version conflict=true
                handleMock.Setup(p => p.Put(It.IsAny<string>(), It.IsAny<string>())).Callback(putCalls[i]);
                handleMock.Setup(p => p.Put(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Callback(putCalls[i]);
                handleMock.Setup(p => p.Put(It.IsAny<CacheItem<string>>())).Callback(putCalls[i]);
                handleMock.Setup(p => p.Remove(It.IsAny<string>())).Callback(removeCalls[i]);
                handleMock.Setup(p => p.Remove(It.IsAny<string>(), It.IsAny<string>())).Callback(removeCalls[i]);
                handleMock.Setup(p => p.Stats).Returns(new CacheStats<string>(cacheName, handleName, true, false));
                if (getCallValues != null)
                {
                    handleMock.Setup(p => p.GetCacheItem(It.IsAny<string>())).Returns(getCallValues[i]);
                    handleMock.Setup(p => p.GetCacheItem(It.IsAny<string>(), It.IsAny<string>())).Returns(getCallValues[i]);
                }
                handles.Add(handleMock.Object);
            }

            return handles.ToArray();
        }
    }
}