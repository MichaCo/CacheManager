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
        public static async Task RunAsync(Func<Task> test, int tasks, int iterations)
        {
            var threadList = new List<Task>();

            var exceptions = new List<Exception>();
            for (int i = 0; i < tasks; i++)
            {
                threadList.Add(Task.Run(async () =>
                {
                    for (var iter = 0; iter < iterations; iter++)
                    {
                        try
                        {
                            await test();
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add(ex);
                        }
                    }

                    await Task.Delay(1);
                }));
            }

            await Task.WhenAll(threadList.ToArray());

            if (exceptions.Count > 0)
            {
                throw new Exception(exceptions.Count + " Exceptions thrown", exceptions.First());
            }
        }

        public static void Run(Action test, int threads, int iterations)
        {
            var threadList = new List<Thread>();

            Exception exceptionResult = null;
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
                            exceptionResult = ex;
                        }
                    }
                }));
                threadList.Add(t);
            }

            threadList.ForEach(p => p.Start());
            threadList.ForEach(p => p.Join());

            if (exceptionResult != null)
            {
                throw exceptionResult;
            }
        }
    }
}