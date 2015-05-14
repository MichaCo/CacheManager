using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CacheManager.Tests
{
    [ExcludeFromCodeCoverage]
    public class ThreadTestHelper
    {
        public static void Run(Action test, int threads, int iterations)
        {
            var threadList = new List<Thread>();

            Exception exeptionResult = null;
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
                            exeptionResult = ex;
                        }
                    }
                }));
                threadList.Add(t);
            }

            threadList.ForEach(p => p.Start());
            threadList.ForEach(p => p.Join());

            if (exeptionResult != null)
            {
                Trace.TraceError(exeptionResult.Message + "\n\r" + exeptionResult.StackTrace);
                throw exeptionResult;
            }
        }

        public static async Task RunAsync(Func<Task> test, int threads, int iterations)
        {
            var threadList = Enumerable.Repeat(test, threads * iterations);

            try
            {
                await Task.WhenAll(threadList.Select(t => t()));
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message + "\n\r" + ex.StackTrace);
                throw ex;
            }
        }
    }
}