using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using CacheManager.Core;
using CacheManager.Core.Configuration;
using CacheManager.Tests.TestCommon;
using Xunit.Extensions;

namespace CacheManager.Tests.Core
{
    [ExcludeFromCodeCoverage]
    public class ThreadRandomReadWriteTestBase
    {
        [Theory]
        [ClassData(typeof(CacheManagerTestData))]
        public void Thread_RandomAccess(ICacheManager<object> cache)
        {
            foreach (var handle in cache.CacheHandles)
            {
                Trace.TraceInformation("Using handle {0}", handle.GetType());
            }

            var blob = new byte[4096];

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
                            for (int i = 0; i < 200; i++)
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
                                    item = new CacheItem<object>(key, value, region, ExpirationMode.Absolute, TimeSpan.FromMilliseconds(10));
                                }

                                cache.Put(item);
                                if (!cache.Add(item))
                                {
                                    cache.Put(item);
                                }

                                var cacheItem = cache.GetCacheItem(key);
                                var cacheItemFromRegion = cache.GetCacheItem(key, region);
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
                        Trace.TraceError("{1} Error: {0}", ex.Message, tId);
                        throw;
                    }

                    Trace.TraceInformation("Hits: {0}, Misses: {1}", hits, misses);
                };

                ThreadTestHelper.Run(test, 5, 1);
            }
        }
    }
}