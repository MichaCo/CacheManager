using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CacheManager.Tests.TestCommon
{
    [ExcludeFromCodeCoverage]
    public class ThreadTestHelper
    {
        public static void Run(Action test, int threads, int iterations)
        {
            var threadList = new List<Thread>();

            Exception exResult = null;
            for (int i = 0; i < threads; i++)
            {
                var t = new Thread(new ThreadStart(() =>
                {
                    for (var iter = 0; iter < iterations; iter++)
                    {
                        try
                        {
                            test();
                        }
                        catch (Exception ex)
                        {
                            exResult = ex;
                        }
                    }
                }));
                threadList.Add(t);
            }

            threadList.ForEach(p => p.Start());
            threadList.ForEach(p => p.Join());

            if (exResult != null)
            {
                Trace.TraceError(exResult.Message + "\n\r" + exResult.StackTrace);
                throw exResult;
            }
        }

        public static async Task RunAsync(Func<Task> test, int threads, int iterations)
        {
            var threadList = Enumerable.Repeat(test, threads * iterations);

            try
            {
                await Task.WhenAll(threadList.Select(t => t()));
            }
            catch (Exception exResult)
            {
                Trace.TraceError(exResult.Message + "\n\r" + exResult.StackTrace);
                throw exResult;
            }
        }
    }
}