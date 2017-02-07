using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace CacheManager.Benchmarks
{
    [Config(typeof(CacheManagerBenchConfig))]
    public class PlainDictionaryUpdateBenchmark
    {
        private const int Threads = 6;
        private const int Iterations = 1000;
        
        private object locke = new object();
        private ConcurrentDictionary<string, int> dictionary;

        [Benchmark(Baseline = true)]
        public void UpdateWithoutLock()
        {
            dictionary = new ConcurrentDictionary<string, int>();
            dictionary.TryAdd("key", 0);
            RunParallel(() => UpdateImpl((v) => v + 1), Threads, Iterations);

            if (dictionary["key"] != Threads * Iterations)
            {
                throw new Exception(string.Format("Not updated correctly, expected '{0}' but found '{1}'.", Threads * Iterations, dictionary["key"]));
            }
        }

        [Benchmark]
        public void UpdateWithLock()
        {
            dictionary = new ConcurrentDictionary<string, int>();
            dictionary.TryAdd("key", 0);
            RunParallel(() => UpdateLockedImpl((v) => v + 1), Threads, Iterations);

            if (dictionary["key"] != Threads * Iterations)
            {
                throw new Exception(string.Format("Not updated correctly, expected '{0}' but found '{1}'.", Threads * Iterations, dictionary["key"]));
            }
        }

        // unfortunately we cannot use the non-lock implementation as pocos/reference type objects might be modified
        // in memory on each iteration/retry and changes are anyways applied no matter if the "update" was successful or not 
        // depending on the version, I'd have to clone the value prior to updating it.
        public int UpdateImpl(Func<int, int> updateFactory)
        {
            int value;
            bool success = false;
            int tries = 0;
            do
            {
                tries++;
                if (!dictionary.TryGetValue("key", out value))
                {
                    throw new InvalidProgramException("Value not found");
                }

                success = dictionary.TryUpdate("key", updateFactory(value), value);
            } while (!success);

            //if (tries > 10)
            //    Console.WriteLine("updated after {0} tries.", tries);

            return value;
        }

        public int UpdateLockedImpl(Func<int, int> updateFactory)
        {
            lock (locke)
            {
                int value;

                if (!dictionary.TryGetValue("key", out value))
                {
                    throw new InvalidProgramException("Value not found");
                }

                dictionary["key"] = updateFactory(value);

                return value;
            }
        }

        private void RunParallel(Action act, int threads, int iterations)
        {
            Action iter = () =>
            {
                for (var i = 0; i < iterations; i++)
                {
                    act();
                }
            };

            var tasks = Enumerable.Repeat(iter, threads);

            Parallel.Invoke(tasks.ToArray());
        }
    }
}
