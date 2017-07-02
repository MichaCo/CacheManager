using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace CacheManager.Benchmarks
{
    public class PlainDictionaryUpdateBenchmark
    {
        private const int Threads = 6;
        private const int Iterations = 1000;

        private object _locke = new object();
        private ConcurrentDictionary<string, int> _dictionary;

        [Benchmark(Baseline = true)]
        public void UpdateWithoutLock()
        {
            _dictionary = new ConcurrentDictionary<string, int>();
            _dictionary.TryAdd("key", 0);
            RunParallel(() => UpdateImpl((v) => v + 1), Threads, Iterations);

            if (_dictionary["key"] != Threads * Iterations)
            {
                throw new Exception(string.Format("Not updated correctly, expected '{0}' but found '{1}'.", Threads * Iterations, _dictionary["key"]));
            }
        }

        [Benchmark]
        public void UpdateWithLock()
        {
            _dictionary = new ConcurrentDictionary<string, int>();
            _dictionary.TryAdd("key", 0);
            RunParallel(() => UpdateLockedImpl((v) => v + 1), Threads, Iterations);

            if (_dictionary["key"] != Threads * Iterations)
            {
                throw new Exception(string.Format("Not updated correctly, expected '{0}' but found '{1}'.", Threads * Iterations, _dictionary["key"]));
            }
        }

        // unfortunately we cannot use the non-lock implementation as pocos/reference type objects might be modified
        // in memory on each iteration/retry and changes are anyways applied no matter if the "update" was successful or not
        // depending on the version, I'd have to clone the value prior to updating it.
        public int UpdateImpl(Func<int, int> updateFactory)
        {
            int value;
            var success = false;
            var tries = 0;
            do
            {
                tries++;
                if (!_dictionary.TryGetValue("key", out value))
                {
                    throw new InvalidProgramException("Value not found");
                }

                success = _dictionary.TryUpdate("key", updateFactory(value), value);
            } while (!success);

            return value;
        }

        public int UpdateLockedImpl(Func<int, int> updateFactory)
        {
            lock (_locke)
            {
                if (!_dictionary.TryGetValue("key", out int value))
                {
                    throw new InvalidProgramException("Value not found");
                }

                _dictionary["key"] = updateFactory(value);

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