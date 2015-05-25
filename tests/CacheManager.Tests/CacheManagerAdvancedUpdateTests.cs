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
            Func<UpdateItemResult<object>> act = () => new UpdateItemResult<object>("value", true, true, 1001);

            // assert
            act().ShouldBeEquivalentTo(new { Value = "value", Success = true, NumberOfTriesNeeded = 1001, VersionConflictOccurred = true });
        }

        [Fact]
        public void CacheManager_Update_ValidateConflictHandle_Ignore()
        {
            // arrange
            Func<string, string> updateFunc = s => s;
            int updateCalls = 0;
            int putCalls = 0;
            int removeCalls = 0;

            var cache = MockHandles(
                count: 5,
                updateCalls: Enumerable.Repeat<Action>(() => updateCalls++, 5).ToArray(),
                updateCallResults: new UpdateItemResult<string>[]
                {
                    new UpdateItemResult<string>(string.Empty, false, true, 0),
                    new UpdateItemResult<string>(string.Empty, true, true, 0),
                    new UpdateItemResult<string>(string.Empty, false, true, 0),
                    new UpdateItemResult<string>(string.Empty, true, true, 0),
                    new UpdateItemResult<string>(string.Empty, true, false, 100)
                },
                putCalls: Enumerable.Repeat<Action>(() => putCalls++, 5).ToArray(),
                removeCalls: Enumerable.Repeat<Action>(() => removeCalls++, 5).ToArray());

            cache.Configuration.CacheUpdateMode = CacheUpdateMode.Up;

            // the update config setting it to Ignore
            UpdateItemConfig updateConfig = new UpdateItemConfig(0, VersionConflictHandling.Ignore);

            // act
            using (cache)
            {
                string value;
                var updateResult = cache.TryUpdate("key", updateFunc, updateConfig, out value);

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
            int updateCalls = 0;
            int putCalls = 0;
            int removeCalls = 0;

            var cache = MockHandles(
                count: 5,
                updateCalls: Enumerable.Repeat<Action>(() => updateCalls++, 5).ToArray(),
                updateCallResults: new UpdateItemResult<string>[]
                {
                    new UpdateItemResult<string>(string.Empty, false, true, 0),
                    new UpdateItemResult<string>(string.Empty, false, true, 0),
                    new UpdateItemResult<string>(string.Empty, true, true, 0),    // version conflict but successfully updated
                    new UpdateItemResult<string>(string.Empty, false, true, 0),
                    new UpdateItemResult<string>(string.Empty, true, false, 100)
                },
                putCalls: Enumerable.Repeat<Action>(() => putCalls++, 5).ToArray(),
                removeCalls: Enumerable.Repeat<Action>(() => removeCalls++, 5).ToArray());

            cache.Configuration.CacheUpdateMode = CacheUpdateMode.Up;

            // the update config setting it to Ignore
            UpdateItemConfig updateConfig = new UpdateItemConfig(0, VersionConflictHandling.EvictItemFromOtherCaches);

            // act
            using (cache)
            {
                string value;
                var updateResult = cache.TryUpdate("key", updateFunc, updateConfig, out value);

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
            int updateCalls = 0;
            int putCalls = 0;
            int removeCalls = 0;

            var cache = MockHandles(
                count: 5,
                updateCalls: Enumerable.Repeat<Action>(() => updateCalls++, 5).ToArray(),
                updateCallResults: new UpdateItemResult<string>[]
                {
                    new UpdateItemResult<string>(string.Empty, false, true, 0),
                    new UpdateItemResult<string>(string.Empty, false, true, 0),
                    new UpdateItemResult<string>(string.Empty, true, false, 0),    // version conflict but failed to update
                    new UpdateItemResult<string>(string.Empty, false, true, 0),
                    new UpdateItemResult<string>(string.Empty, true, false, 100)
                },
                putCalls: Enumerable.Repeat<Action>(() => putCalls++, 5).ToArray(),
                removeCalls: Enumerable.Repeat<Action>(() => removeCalls++, 5).ToArray());

            cache.Configuration.CacheUpdateMode = CacheUpdateMode.Up;

            // the update config setting it to EvictItemFromOtherCaches
            UpdateItemConfig updateConfig = new UpdateItemConfig(0, VersionConflictHandling.EvictItemFromOtherCaches);

            // act
            using (cache)
            {
                string value;
                var updateResult = cache.TryUpdate("key", updateFunc, updateConfig, out value);

                // assert
                updateCalls.Should().Be(3, "cache manager should have stopped updating after the first version conflict");
                putCalls.Should().Be(0, "no put calls expected");
                removeCalls.Should().Be(4, "the key should have been removed from the other 4 handles");
                updateResult.Should().BeFalse("the update in handle 3 was not successful.");
            }
        }

        [Fact]
        public void CacheManager_Update_ValidateConflictHandle_UpdateOtherCaches()
        {
            // arrange
            Func<string, string> updateFunc = s => s;
            
            int updateCalls = 0;
            int putCalls = 0;
            int removeCalls = 0;

            var cache = MockHandles(
                count: 5,
                updateCalls: Enumerable.Repeat<Action>(() => updateCalls++, 5).ToArray(),
                updateCallResults: new UpdateItemResult<string>[]
                {
                    new UpdateItemResult<string>(string.Empty, false, true, 0),
                    new UpdateItemResult<string>(string.Empty, false, true, 0),
                    new UpdateItemResult<string>(string.Empty, true, true, 0),    // version conflict but successfully updated
                                                            // this should trigger cache manager to
                                                            // update the other 4 handles with the
                                                            // new version
                    new UpdateItemResult<string>(string.Empty, false, true, 0),
                    new UpdateItemResult<string>(string.Empty, true, false, 100)
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

            cache.Configuration.CacheUpdateMode = CacheUpdateMode.Up;

            // the update config setting it to UpdateOtherCaches
            UpdateItemConfig updateConfig = new UpdateItemConfig(0, VersionConflictHandling.UpdateOtherCaches);

            // act
            using (cache)
            {
                string value;
                var updateResult = cache.TryUpdate("key", updateFunc, updateConfig, out value);

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
            int updateCalls = 0;
            int putCalls = 0;
            int removeCalls = 0;

            var cache = MockHandles(
                count: 5,
                updateCalls: Enumerable.Repeat<Action>(() => updateCalls++, 5).ToArray(),
                updateCallResults: new UpdateItemResult<string>[]
                {
                    new UpdateItemResult<string>(string.Empty, false, true, 0),
                    new UpdateItemResult<string>(string.Empty, false, true, 0),
                    new UpdateItemResult<string>(string.Empty, true, true, 0),    // version conflict but successfully updated
                                                            // this should trigger cache manager to
                                                            // update the other 4 handles with the
                                                            // new version
                    new UpdateItemResult<string>(string.Empty, false, true, 0),
                    new UpdateItemResult<string>(string.Empty, true, false, 100)
                },
                putCalls: Enumerable.Repeat<Action>(() => putCalls++, 5).ToArray(),
                removeCalls: Enumerable.Repeat<Action>(() => removeCalls++, 5).ToArray(),
                getCallValues: new CacheItem<string>[]
                {
                    null,
                    null,
                    new CacheItem<string>("key", "updated value", "region"),
                    null,
                    null
                });

            // act
            using (cache)
            {
                cache.Add("key", "something", "region");

                string value;
                var updateResult = cache.TryUpdate("key", "region", updateFunc, updateConfig, out value);

                var result = cache.Get("key", "region");

                // assert
                cache.CacheHandles.ElementAt(0).Stats.GetStatistic(CacheStatsCounterType.Items).Should().Be(1);
                result.Should().Be("updated value");
                updateCalls.Should().Be(3, "first 3 updates until version conflict");
                putCalls.Should().Be(4, "cache manager should only update the other 4 handles");
                removeCalls.Should().Be(0, "nothing should be removed");
                updateResult.Should().BeTrue("updated successfully.");
            }
        }

        private static ICacheManager<string> MockHandles(int count, Action[] updateCalls, UpdateItemResult<string>[] updateCallResults, Action[] putCalls, Action[] removeCalls, CacheItem<string>[] getCallValues = null)
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

            var manager = CacheFactory.Build<string>(
                cacheName,
                settings =>
            {
                for (int i = 0; i < count; i++)
                {
                    settings
                        .WithHandle(typeof(MockCacheHandle<>), "handle" + i)
                        .EnableStatistics();
                }
            });

            for(var i = 0; i < count; i++)
            {
                var handle = manager.CacheHandles.ElementAt(i) as MockCacheHandle<string>;
                handle.GetCallValue = getCallValues == null ? null : getCallValues[i];
                if (putCalls != null)
                {
                    handle.PutCall = putCalls[i];
                }
                if (removeCalls != null)
                {
                    handle.RemoveCall = removeCalls[i];
                }
                if (updateCalls != null)
                {
                    handle.UpdateCall = updateCalls[i];
                }
                if (updateCallResults != null)
                {
                    handle.UpdateValue = updateCallResults[i];
                }
            }

            ////var handles = new List<BaseCacheHandle<string>>();
            ////for (int i = 0; i < count; i++)
            ////{
            ////    var handleName = "handle" + i;
            ////    var handleMock = new Mock<BaseCacheHandle<string>>();
            ////    handleMock
            ////        .Setup(p => p.Update(It.IsAny<string>(), It.IsAny<Func<string, string>>(), It.IsAny<UpdateItemConfig>()))
            ////        .Callback(updateCalls[i])
            ////        .Returns(updateCallResults[i]);
            ////    handleMock
            ////        .Setup(p => p.Update(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<string, string>>(), It.IsAny<UpdateItemConfig>()))
            ////        .Callback(updateCalls[i])
            ////        .Returns(updateCallResults[i]);
            ////    // we also count the Put calls because second handle returns version conflict=true
            ////    handleMock.Setup(p => p.Put(It.IsAny<string>(), It.IsAny<string>())).Callback(putCalls[i]);
            ////    handleMock.Setup(p => p.Put(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Callback(putCalls[i]);
            ////    handleMock.Setup(p => p.Put(It.IsAny<CacheItem<string>>())).Callback(putCalls[i]);
            ////    handleMock.Setup(p => p.Remove(It.IsAny<string>())).Callback(removeCalls[i]);
            ////    handleMock.Setup(p => p.Remove(It.IsAny<string>(), It.IsAny<string>())).Callback(removeCalls[i]);
            ////    handleMock.Setup(p => p.Stats).Returns(new CacheStats<string>(cacheName, handleName, true, false));
            ////    if (getCallValues != null)
            ////    {
            ////        handleMock.Setup(p => p.GetCacheItem(It.IsAny<string>())).Returns(getCallValues[i]);
            ////        handleMock.Setup(p => p.GetCacheItem(It.IsAny<string>(), It.IsAny<string>())).Returns(getCallValues[i]);
            ////    }
            ////    handles.Add(handleMock.Object);
            ////}

            return manager;
        }
    }

    public class MockCacheHandle<TCacheValue> : BaseCacheHandle<TCacheValue>
    {
        public CacheItem<TCacheValue> GetCallValue { get; set; }

        public Func<bool> AddCall { get; set; }

        public Action PutCall { get; set; }

        public Action RemoveCall { get; set; }

        public Action UpdateCall { get; set; }

        public UpdateItemResult<TCacheValue> UpdateValue { get; set; }

        public MockCacheHandle(ICacheManager<TCacheValue> manager, CacheHandleConfiguration configuration) 
            : base(manager, configuration)
        {
            AddCall = () => true;
            PutCall = () => { };
            RemoveCall = () => { };
            UpdateCall = () => { };
        }

        public override int Count
        {
            get
            {
                return 0;
            }
        }

        public override void Clear()
        {
        }

        public override void ClearRegion(string region)
        {
        }

        protected override bool AddInternalPrepared(CacheItem<TCacheValue> item)
        {
            return AddCall();
        }

        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key)
        {
            return GetCallValue;
        }

        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key, string region)
        {
            return GetCallValue;
        }

        protected override void PutInternalPrepared(CacheItem<TCacheValue> item)
        {
            PutCall();
        }

        protected override bool RemoveInternal(string key)
        {
            RemoveCall();
            return true;
        }

        protected override bool RemoveInternal(string key, string region)
        {
            RemoveCall();
            return true;
        }

        public override UpdateItemResult<TCacheValue> Update(string key, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config)
        {
            UpdateCall();
            return UpdateValue;
        }

        public override UpdateItemResult<TCacheValue> Update(string key, string region, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config)
        {
            UpdateCall();
            return UpdateValue;
        }
    }
}