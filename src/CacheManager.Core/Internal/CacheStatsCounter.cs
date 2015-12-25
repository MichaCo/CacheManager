using System.Threading;

namespace CacheManager.Core.Internal
{
    internal sealed class CacheStatsCounter
    {
        private volatile long[] counters = new long[9];

        public void Add(CacheStatsCounterType type, long value)
        {
            Interlocked.Add(ref this.counters[(int)type], value);
        }

        public void Decrement(CacheStatsCounterType type)
        {
            Interlocked.Decrement(ref this.counters[(int)type]);
        }

        public long Get(CacheStatsCounterType type) => this.counters[(int)type];

        public void Increment(CacheStatsCounterType type)
        {
            Interlocked.Increment(ref this.counters[(int)type]);
        }

        public void Set(CacheStatsCounterType type, long value)
        {
            Interlocked.Exchange(ref this.counters[(int)type], value);
        }
    }
}