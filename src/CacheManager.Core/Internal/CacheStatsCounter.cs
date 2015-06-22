using System.Threading;

namespace CacheManager.Core.Internal
{
    internal sealed class CacheStatsCounter
    {
        private long[] counters = null;

        public CacheStatsCounter()
        {
            this.counters = new long[9];
        }

        public void Add(CacheStatsCounterType type, long value)
        {
            Interlocked.Add(ref this.counters[(int)type], value);
        }

        public void Decrement(CacheStatsCounterType type)
        {
            Interlocked.Decrement(ref this.counters[(int)type]);
        }

        public long Get(CacheStatsCounterType type)
        {
            var result = this.counters[(int)type];
            return result;
        }

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