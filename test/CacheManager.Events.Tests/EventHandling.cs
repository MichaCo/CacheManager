using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CacheManager.Core;
using CacheManager.Core.Internal;

namespace CacheManager.Events.Tests
{
    public class EventCounter<TCacheValue>
    {
        private object _locki = new object();
        private readonly Dictionary<CacheEvent, int[]> _updates = new Dictionary<CacheEvent, int[]>();

        public EventCounter(ICacheManager<TCacheValue> cache)
        {
            Cache = cache ?? throw new ArgumentNullException(nameof(cache));

            Cache.OnRemove += OnRemove;
            Cache.OnRemoveByHandle += OnRemoveByHandle;
            Cache.OnAdd += OnAdd;
            Cache.OnGet += OnGet;
            Cache.OnPut += OnPut;
            Cache.OnUpdate += OnUpdate;

            _updates.Add(CacheEvent.Add, new int[1]);
            _updates.Add(CacheEvent.Put, new int[1]);
            _updates.Add(CacheEvent.Rem, new int[1]);
            _updates.Add(CacheEvent.Get, new int[1]);
            _updates.Add(CacheEvent.ReH, new int[1]);
            _updates.Add(CacheEvent.Upd, new int[1]);
        }
        

        public Dictionary<CacheEvent, int[]> GetExpectedState()
        {
            var result = new Dictionary<CacheEvent, int[]>();

            foreach (var kv in _updates.ToArray())
            {
                result.Add(kv.Key, new[] { kv.Value[0] });
            }

            return result;
        }

        private void Update(CacheEvent ev, string key)
        {
            Interlocked.Increment(ref _updates[ev][0]);
        }

        public ICacheManager<TCacheValue> Cache { get; }

        private void OnUpdate(object sender, CacheActionEventArgs e)
        {
            Update(CacheEvent.Upd, e.Key);
        }

        private void OnPut(object sender, CacheActionEventArgs e)
        {
            Update(CacheEvent.Put, e.Key);
        }

        private void OnGet(object sender, CacheActionEventArgs e)
        {
            Update(CacheEvent.Get, e.Key);
        }

        private void OnAdd(object sender, CacheActionEventArgs e)
        {
            Update(CacheEvent.Add, e.Key);
        }

        private void OnRemove(object sender, CacheActionEventArgs e)
        {
            Update(CacheEvent.Rem, e.Key);
        }

        private void OnRemoveByHandle(object sender, CacheItemRemovedEventArgs e)
        {
            Update(CacheEvent.ReH, e.Key);
        }
    }
}