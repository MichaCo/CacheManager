namespace CacheManager.Core.Internal
{
#if !NETSTANDARD
    using System;
    using System.Diagnostics;
    using System.Threading;
    using static CacheManager.Core.Utility.Guard;

    internal class CachePerformanceCounters<T> : IDisposable
    {
        private const string Category = ".NET CacheManager";
        private const string Entries = "Total cache items";
        private const string HitRatio = "Hit ratio";
        private const string HitRatioBase = "Cache hit ratio Base";
        private const string Hits = "Total hits";
        private const string HitsPerSecond = "Avg hits per second";
        private const string Misses = "Total misses";
        private const string ReadsPerSecond = "Avg gets per second";
        private const string Writes = "Total cache writes";
        private const string WritesPerSecond = "Avg writes per second";
        private static readonly int _numStatsCounters = Enum.GetValues(typeof(CacheStatsCounterType)).Length;

        private readonly Timer _counterTimer;
        private readonly string _instanceName = string.Empty;
        private readonly CacheStats<T> _stats;
        private readonly long[] _statsCounts;
        private PerformanceCounter[] _counters;
        private bool _enabled = true;
        private object _updateLock = new object();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Mobility", "CA1601:DoNotUseTimersThatPreventPowerStateChanges", Justification = "If perfCounters are enabled, we can live with the power consumption...")]
        public CachePerformanceCounters(string cacheName, string handleName, CacheStats<T> stats)
        {
            NotNullOrWhiteSpace(cacheName, nameof(cacheName));

            NotNullOrWhiteSpace(handleName, nameof(handleName));

            var processName = Process.GetCurrentProcess().ProcessName;

            _instanceName = string.Concat(processName + ":" + cacheName + ":" + handleName);

            var invalidInstanceChars = new string[] { "(", ")", "#", "\\", "/" };

            foreach (var ichar in invalidInstanceChars)
            {
                _instanceName = _instanceName.Replace(ichar, string.Empty);
            }

            if (_instanceName.Length > 128)
            {
                _instanceName = _instanceName.Substring(0, 128);
            }

            InitializeCounters();
            _stats = stats;
            _statsCounts = new long[_numStatsCounters];

            if (_enabled)
            {
                _counterTimer = new Timer(new TimerCallback(PerformanceCounterWorker), null, 450L, 450L);
            }
        }

        ~CachePerformanceCounters()
        {
            Dispose(false);
        }

        public void Decrement(CachePerformanceCounterType type)
        {
            GetCounter(type).Decrement();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Increment(CachePerformanceCounterType type)
        {
            GetCounter(type).Increment();
        }

        public void IncrementBy(CachePerformanceCounterType type, long value)
        {
            GetCounter(type).IncrementBy(value);
        }

        public void SetValue(CachePerformanceCounterType type, long value)
        {
            GetCounter(type).RawValue = value < 0 ? 0 : value;
        }

        private static void InitializeCategory()
        {
            if (PerformanceCounterCategory.Exists(Category))
            {
                return;
            }

            PerformanceCounterCategory.Create(
                Category,
                "CacheManager counters per handle",
                PerformanceCounterCategoryType.MultiInstance,
                new CounterCreationDataCollection
                {
                    new CounterCreationData
                    {
                        CounterName = Entries,
                        CounterHelp = "Current number of cache items stored within the cache handle",
                        CounterType = PerformanceCounterType.NumberOfItems64
                    },
                    new CounterCreationData
                    {
                        CounterName = HitRatio,
                        CounterHelp = "Cache hit ratio of the cache handle",
                        CounterType = PerformanceCounterType.AverageCount64
                    },
                    new CounterCreationData
                    {
                        CounterName = HitRatioBase,
                        CounterHelp = HitRatioBase,
                        CounterType = PerformanceCounterType.AverageBase
                    },
                    new CounterCreationData
                    {
                        CounterName = Hits,
                        CounterHelp = "Total number of cache hits of the cache handle",
                        CounterType = PerformanceCounterType.NumberOfItems64
                    },
                    new CounterCreationData
                    {
                        CounterName = Misses,
                        CounterHelp = "Total number of cache misses of the cache handle",
                        CounterType = PerformanceCounterType.NumberOfItems64
                    },
                    new CounterCreationData
                    {
                        CounterName = Writes,
                        CounterHelp = "Total number of cache writes (add,put,remove) of the cache handle",
                        CounterType = PerformanceCounterType.NumberOfItems64
                    },
                    new CounterCreationData
                    {
                        CounterName = WritesPerSecond,
                        CounterHelp = WritesPerSecond,
                        CounterType = PerformanceCounterType.RateOfCountsPerSecond64
                    },
                    new CounterCreationData
                    {
                        CounterName = ReadsPerSecond,
                        CounterHelp = ReadsPerSecond,
                        CounterType = PerformanceCounterType.RateOfCountsPerSecond64
                    },
                    new CounterCreationData
                    {
                        CounterName = HitsPerSecond,
                        CounterHelp = HitsPerSecond,
                        CounterType = PerformanceCounterType.RateOfCountsPerSecond64
                    },
                });
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "On Dispose the catch is just for safety...")]
        private void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                try
                {
                    ResetCounters();

                    if (_counterTimer != null)
                    {
                        _counterTimer.Dispose();
                    }

                    foreach (var counter in _counters)
                    {
                        counter.Dispose();
                    }
                }
                catch
                {
                }
            }
        }

        private PerformanceCounter GetCounter(CachePerformanceCounterType type) => _counters[(int)type];

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "At this point its fine")]
        private void InitializeCounters()
        {
            try
            {
                InitializeCategory();

                _counters = new PerformanceCounter[9];
                _counters[0] = new PerformanceCounter(Category, Entries, _instanceName, false);
                _counters[1] = new PerformanceCounter(Category, HitRatio, _instanceName, false);
                _counters[2] = new PerformanceCounter(Category, HitRatioBase, _instanceName, false);
                _counters[3] = new PerformanceCounter(Category, Hits, _instanceName, false);
                _counters[4] = new PerformanceCounter(Category, Misses, _instanceName, false);
                _counters[5] = new PerformanceCounter(Category, Writes, _instanceName, false);
                _counters[6] = new PerformanceCounter(Category, ReadsPerSecond, _instanceName, false);
                _counters[7] = new PerformanceCounter(Category, WritesPerSecond, _instanceName, false);
                _counters[8] = new PerformanceCounter(Category, HitsPerSecond, _instanceName, false);

                // resetting them cleans up previous runs on the same category, which will otherwise
                // stay in perfmon forever
                ResetCounters();
            }
            catch
            {
                _enabled = false;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Is just fine at that point.")]
        private void PerformanceCounterWorker(object state)
        {
            if (_enabled && Monitor.TryEnter(_updateLock))
            {
                try
                {
                    var previousCounts = new long[_numStatsCounters];
                    Array.Copy(_statsCounts, previousCounts, _numStatsCounters);

                    for (var i = 0; i < _numStatsCounters; i++)
                    {
                        _statsCounts[i] = _stats.GetStatistic((CacheStatsCounterType)i);
                    }

                    var writes = _statsCounts[4] + _statsCounts[5] + _statsCounts[7] + _statsCounts[8];
                    var previousWrites = previousCounts[4] + previousCounts[5] + previousCounts[7] + previousCounts[8];
                    var hits = _statsCounts[0] - previousCounts[0];

                    SetValue(CachePerformanceCounterType.Items, _statsCounts[2]);

                    IncrementBy(CachePerformanceCounterType.HitRatioBase, _statsCounts[6] - previousCounts[6]);
                    IncrementBy(CachePerformanceCounterType.HitRatio, hits);
                    IncrementBy(CachePerformanceCounterType.TotalHits, hits);
                    IncrementBy(CachePerformanceCounterType.TotalMisses, _statsCounts[1] - previousCounts[1]);
                    IncrementBy(CachePerformanceCounterType.TotalWrites, writes - previousWrites);
                    IncrementBy(CachePerformanceCounterType.ReadsPerSecond, _statsCounts[6] - previousCounts[6]);
                    IncrementBy(CachePerformanceCounterType.WritesPerSecond, writes - previousWrites);
                    IncrementBy(CachePerformanceCounterType.HitsPerSecond, hits);
                }
                catch (Exception e)
                {
                    _enabled = false;
                    Trace.TraceError(e.Message + "\n" + e.StackTrace);
                }
                finally
                {
                    Monitor.Exit(_updateLock);
                }
            }
        }

        private void ResetCounters()
        {
            for (var i = 0; i < Enum.GetValues(typeof(CachePerformanceCounterType)).Length; i++)
            {
                SetValue((CachePerformanceCounterType)i, 0L);
            }
        }
    }
#endif
}