using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CacheManager.Core;
using FluentAssertions;
using Xunit;

namespace CacheManager.Tests
{
    [ExcludeFromCodeCoverage]
    public class ThreadRandomReadWriteTestBase
    {
        [Theory]
        [Trait("category", "Unreliable")]
        [ClassData(typeof(TestCacheManagers))]
        public async Task Thread_Update(ICacheManager<object> cache)
        {
            using (cache)
            {
                var key = Guid.NewGuid().ToString();
                var handleInfo = string.Join("\nh: ", cache.CacheHandles.Select(p => p.Configuration.Name + ":" + p.GetType().Name));

                cache.Remove(key);
                cache.Add(key, new RaceConditionTestElement() { Counter = 0 });
                int numThreads = 5;
                int iterations = 10;
                int numInnerIterations = 10;
                int countCasModifyCalls = 0;

                // act
                await ThreadTestHelper.RunAsync(
                    async () =>
                    {
                        for (int i = 0; i < numInnerIterations; i++)
                        {
                            cache.Update(
                                key,
                                (value) =>
                                {
                                    var val = value as RaceConditionTestElement;
                                    if (val == null)
                                    {
                                        throw new InvalidOperationException("WTF invalid object result");
                                    }

                                    val.Counter++;
                                    Interlocked.Increment(ref countCasModifyCalls);
                                    return value;
                                },
                                int.MaxValue);
                        }

                        await Task.Delay(0);
                    },
                    numThreads,
                    iterations);

                // assert
                await Task.Delay(10);
                for (var i = 0; i < cache.CacheHandles.Count(); i++)
                {
                    var handle = cache.CacheHandles.ElementAt(i);
                    var result = (RaceConditionTestElement)handle.Get(key);
                    if (i < cache.CacheHandles.Count() - 1)
                    {
                        // only the last one should have the item
                        result.Should().BeNull();
                    }
                    else
                    {
                        result.Should().NotBeNull(handleInfo + "\ncurrent: " + handle.Configuration.Name + ":" + handle.GetType().Name);
                        result.Counter.Should().Be(numThreads * numInnerIterations * iterations, handleInfo + "\ncounter should be exactly the expected value.");
                        countCasModifyCalls.Should().BeGreaterOrEqualTo((int)result.Counter, handleInfo + "\nexpecting no (if synced) or some version collisions.");
                    }
                }
            }
        }
    }
}