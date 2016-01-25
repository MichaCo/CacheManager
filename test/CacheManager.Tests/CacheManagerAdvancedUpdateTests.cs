using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.Core.Logging;
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
        public void UpdateItemResult_ForSuccess()
        {
            // arrange act
            Func<UpdateItemResult<object>> act = () => UpdateItemResult.ForSuccess<object>("value", true, 1001);

            // assert
            act().ShouldBeEquivalentTo(new { Value = "value", UpdateState = UpdateItemResultState.Success, NumberOfTriesNeeded = 1001, VersionConflictOccurred = true });
        }

        [Fact]
        [ReplaceCulture]
        public void UpdateItemResult_ForTooManyTries()
        {
            // arrange act
            Func<UpdateItemResult<object>> act = () => UpdateItemResult.ForTooManyRetries<object>(1001);

            // assert
            act().ShouldBeEquivalentTo(new { Value = default(object), UpdateState = UpdateItemResultState.TooManyRetries, NumberOfTriesNeeded = 1001, VersionConflictOccurred = true });
        }

        [Fact]
        [ReplaceCulture]
        public void UpdateItemResult_ForDidNotExist()
        {
            // arrange act
            Func<UpdateItemResult<object>> act = () => UpdateItemResult.ForItemDidNotExist<object>();

            // assert
            act().ShouldBeEquivalentTo(new { Value = default(object), UpdateState = UpdateItemResultState.ItemDidNotExist, NumberOfTriesNeeded = 1, VersionConflictOccurred = false });
        }

        [Fact]
        public void CacheManager_Update_Validate_LowestWins()
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
                    null,
                    null,
                    null,
                    null,
                    UpdateItemResult.ForSuccess<string>(string.Empty, true, 100)
                },
                putCalls: Enumerable.Repeat<Action>(() => putCalls++, 5).ToArray(),
                removeCalls: Enumerable.Repeat<Action>(() => removeCalls++, 5).ToArray());

            cache.Configuration.CacheUpdateMode = CacheUpdateMode.Up;

            // the update config setting it to Ignore: update handling should be ignore, update was success, items shoudl get evicted from others
            UpdateItemConfig updateConfig = new UpdateItemConfig(0, VersionConflictHandling.Ignore);

            // act
            using (cache)
            {
                string value;
                var updateResult = cache.TryUpdate("key", updateFunc, updateConfig, out value);

                // assert
                updateCalls.Should().Be(1, "first handle should have been invoked");
                putCalls.Should().Be(0, "evicted");
                removeCalls.Should().Be(4, "items should have been removed");
                updateResult.Should().BeTrue();
            }
        }

        [Fact]
        public void CacheManager_Update_ItemDoesNotExist()
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
                    UpdateItemResult.ForItemDidNotExist<string>(),
                    UpdateItemResult.ForItemDidNotExist<string>(),
                    UpdateItemResult.ForItemDidNotExist<string>(),
                    UpdateItemResult.ForItemDidNotExist<string>(),
                    UpdateItemResult.ForItemDidNotExist<string>()
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
                updateCalls.Should().Be(5, "should iterate through all of them");
                putCalls.Should().Be(0, "no put calls expected");
                removeCalls.Should().Be(0);
                updateResult.Should().BeFalse();
            }
        }

        [Fact]
        public void CacheManager_Update_ExceededRetryLimit()
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
                    UpdateItemResult.ForSuccess<string>(string.Empty, true, 100),
                    UpdateItemResult.ForSuccess<string>(string.Empty, true, 100),
                    UpdateItemResult.ForSuccess<string>(string.Empty, true, 100),
                    UpdateItemResult.ForTooManyRetries<string>(1000),
                    UpdateItemResult.ForItemDidNotExist<string>(),
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
                updateCalls.Should().Be(2, "bottom to top");
                putCalls.Should().Be(0, "no put calls expected");
                removeCalls.Should().Be(4, "the key should have been removed from the other 4 handles");
                updateResult.Should().BeFalse("the update in handle 4 was not successful.");
            }
        }

        [Fact]
        public void CacheManager_Update_Success_ValidateEvict()
        {
            // arrange
            Func<string, string> updateFunc = s => s;

            int updateCalls = 0;
            int addCalls = 0;
            int putCalls = 0;
            int removeCalls = 0;

            var cache = MockHandles(
                count: 5,
                updateCalls: Enumerable.Repeat<Action>(() => updateCalls++, 5).ToArray(),
                updateCallResults: new UpdateItemResult<string>[]
                {
                    UpdateItemResult.ForItemDidNotExist<string>(),
                    UpdateItemResult.ForItemDidNotExist<string>(),
                    UpdateItemResult.ForItemDidNotExist<string>(),
                    UpdateItemResult.ForSuccess("some value", true, 100),
                    UpdateItemResult.ForItemDidNotExist<string>()
                },
                putCalls: Enumerable.Repeat<Action>(() => putCalls++, 5).ToArray(),
                removeCalls: Enumerable.Repeat<Action>(() => removeCalls++, 5).ToArray(),
                getCallValues: new CacheItem<string>[]
                {
                    null,
                    null,
                    null,
                    new CacheItem<string>("key", "updated value"),  // have to return an item for the second one
                    null
                },
                addCalls: Enumerable.Repeat<Func<bool>>(() => { addCalls++; return true; }, 5).ToArray());

            cache.Configuration.CacheUpdateMode = CacheUpdateMode.Up;

            // the update config setting it to UpdateOtherCaches
            UpdateItemConfig updateConfig = new UpdateItemConfig(0, VersionConflictHandling.UpdateOtherCaches);

            // act
            using (cache)
            {
                string value;
                var updateResult = cache.TryUpdate("key", updateFunc, updateConfig, out value);

                // assert
                updateCalls.Should().Be(2, "second from below did update");
                putCalls.Should().Be(0, "no puts");
                addCalls.Should().Be(1, "one below the one updating");
                removeCalls.Should().Be(3, "3 above");
                updateResult.Should().BeTrue("updated successfully.");
            }
        }

        private static ICacheManager<string> MockHandles(int count, Action[] updateCalls, UpdateItemResult<string>[] updateCallResults, Action[] putCalls, Action[] removeCalls, CacheItem<string>[] getCallValues = null, Func<bool>[] addCalls = null)
        {
            if (count <= 0)
            {
                throw new InvalidOperationException();
            }

            if (updateCalls.Length != count || updateCallResults.Length != count || putCalls.Length != count || removeCalls.Length != count)
            {
                throw new InvalidOperationException("Count and arrays must match");
            }

            var manager = CacheFactory.Build<string>(
                settings =>
                {
                    for (int i = 0; i < count; i++)
                    {
                        settings
                            .WithHandle(typeof(MockCacheHandle<>), "handle" + i)
                            .EnableStatistics();
                    }
                });

            for (var i = 0; i < count; i++)
            {
                var handle = manager.CacheHandles.ElementAt(i) as MockCacheHandle<string>;
                handle.GetCallValue = getCallValues == null ? null : getCallValues[i];
                if (putCalls != null)
                {
                    handle.PutCall = putCalls[i];
                }
                if (addCalls != null)
                {
                    handle.AddCall = addCalls[i];
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

            return manager;
        }
    }

    public class MockCacheHandle<TCacheValue> : BaseCacheHandle<TCacheValue>
    {
        public MockCacheHandle(ICacheManager<TCacheValue> manager, CacheHandleConfiguration configuration)
            : base(manager, configuration)
        {
            this.Logger = manager.Configuration.LoggerFactory.CreateLogger(this);
            this.AddCall = () => true;
            this.PutCall = () => { };
            this.RemoveCall = () => { };
            this.UpdateCall = () => { };
        }

        public CacheItem<TCacheValue> GetCallValue { get; set; }

        public Func<bool> AddCall { get; set; }

        public Action PutCall { get; set; }

        public Action RemoveCall { get; set; }

        public Action UpdateCall { get; set; }

        public UpdateItemResult<TCacheValue> UpdateValue { get; set; }

        public override int Count => 0;

        protected override ILogger Logger { get; }

        public override void Clear()
        {
        }

        public override void ClearRegion(string region)
        {
        }

        public override UpdateItemResult<TCacheValue> Update(string key, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config)
        {
            this.UpdateCall();
            return this.UpdateValue;
        }

        public override UpdateItemResult<TCacheValue> Update(string key, string region, Func<TCacheValue, TCacheValue> updateValue, UpdateItemConfig config)
        {
            this.UpdateCall();
            return this.UpdateValue;
        }

        protected override bool AddInternalPrepared(CacheItem<TCacheValue> item) => this.AddCall();

        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key) => this.GetCallValue;

        protected override CacheItem<TCacheValue> GetCacheItemInternal(string key, string region) => this.GetCallValue;

        protected override void PutInternalPrepared(CacheItem<TCacheValue> item) => this.PutCall();

        protected override bool RemoveInternal(string key)
        {
            this.RemoveCall();
            return true;
        }

        protected override bool RemoveInternal(string key, string region)
        {
            this.RemoveCall();
            return true;
        }
    }
}