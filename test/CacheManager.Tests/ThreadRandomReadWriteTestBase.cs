using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using CacheManager.Core;
using Xunit;

namespace CacheManager.Tests
{
    [ExcludeFromCodeCoverage]
#if NET40
    [Trait("Framework", "NET40")]
#else
    [Trait("Framework", "NET45")]
#endif

    // This does run on Mono 4.2.x (alpha), disabling it for now TODO: enable if travis ci runs new version of mono
    [Trait("category", "Mono")]
    public class ThreadRandomReadWriteTestBase : BaseCacheManagerTest
    {
        [Theory]
        [MemberData("TestCacheManagers")]
        public void Thread_RandomAccess(ICacheManager<object> cache)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

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
                        Trace.TraceError("{1} Error: {0}", ex.Message, tId);
                        throw;
                    }

                    Trace.TraceInformation("Hits: {0}, Misses: {1}", hits, misses);
                };

                ThreadTestHelper.Run(test, 2, 1);
            }
        }
    }
}