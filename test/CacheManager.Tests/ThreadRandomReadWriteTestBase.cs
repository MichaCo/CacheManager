using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using CacheManager.Core;
using FluentAssertions;
using Xunit;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Tests
{
    [ExcludeFromCodeCoverage]
#if NET40
    [Trait("Framework", "NET40")]
#else
    [Trait("Framework", "NET45")]
#endif

    public class ThreadRandomReadWriteTestBase : BaseCacheManagerTest
    {
        [Theory]
        [MemberData("TestCacheManagers")]
        public void Thread_Update(ICacheManager<object> cache)
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
                ThreadTestHelper.Run(
                    () =>
                    {
                        for (int i = 0; i < numInnerIterations; i++)
                        {
                            cache.Update(key, (value) =>
                            {
                                var val = (RaceConditionTestElement)value;
                                val.Counter++;
                                Interlocked.Increment(ref countCasModifyCalls);
                                return value;
                            });
                        }
                    },
                    numThreads,
                    iterations);

                // assert
                Thread.Sleep(10);
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

        [Theory(Skip = "has no real value")]
        [MemberData("TestCacheManagers")]
        public void Thread_RandomAccess(ICacheManager<object> cache)
        {
            NotNull(cache, nameof(cache));

            foreach (var handle in cache.CacheHandles)
            {
                Trace.TraceInformation("Using handle {0}", handle.GetType());
            }

            var blob = new byte[1024];

            using (cache)
            {
                Action test = () =>
                {
                    var hits = 0;
                    var misses = 0;
                    var tId = Thread.CurrentThread.ManagedThreadId;

                    try
                    {
                        for (var r = 0; r < 5; r++)
                        {
                            for (int i = 0; i < 5; i++)
                            {
                                string key = "key" + i;
                                object value = blob.Clone();
                                string region = "region" + r;

                                CacheItem<object> item = null;
                                if (r % 2 == 0)
                                {
                                    item = new CacheItem<object>(key, value, ExpirationMode.Sliding, TimeSpan.FromMilliseconds(10));
                                }
                                else
                                {
                                    item = new CacheItem<object>(key, region, value, ExpirationMode.Absolute, TimeSpan.FromMilliseconds(10));
                                }

                                cache.Put(item);
                                if (!cache.Add(item))
                                {
                                    cache.Put(item);
                                }

                                var result = cache.Get(key);
                                if (result == null)
                                {
                                    misses++;
                                }
                                else
                                {
                                    hits++;
                                }

                                if (!cache.Remove(key))
                                {
                                    misses++;
                                }
                                else
                                {
                                    hits++;
                                }

                                Thread.Sleep(0);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("{1} Error: {0}", ex.Message, tId);
                        throw;
                    }

                    Debug.WriteLine("Hits: {0}, Misses: {1}", hits, misses);
                };

                ThreadTestHelper.Run(test, 2, 1);
            }
        }
    }
}