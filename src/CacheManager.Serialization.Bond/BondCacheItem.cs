using System;
using System.Linq;
using Bond;
using CacheManager.Core;

namespace CacheManager.Serialization.Bond
{
    [Schema]
    internal class BondCacheItemWrapper
    {
        [Id(1)]
        public byte[] Data { get; set; }

        [Id(2)]
        public string ValueType { get; set; }
    }

    [Schema]
    internal class BondCacheItem<T>
    {
        public static readonly Type OpenItemType = typeof(BondCacheItem<>);

        public BondCacheItem()
        {
        }

        public BondCacheItem(CacheItem<T> from)
        {
            if (from == null)
            {
                throw new ArgumentNullException(nameof(from));
            }

            CreatedUtc = from.CreatedUtc.Ticks;
            ExpirationMode = from.ExpirationMode;
            ExpirationTimeout = from.ExpirationTimeout.TotalMilliseconds;
            Key = from.Key;
            LastAccessedUtc = from.LastAccessedUtc.Ticks;
            Region = from.Region;
            ValueType = from.ValueType.AssemblyQualifiedName;
            UsesExpirationDefaults = from.UsesExpirationDefaults;
            Value = (T)from.Value;
        }

        [Id(1)]
        public long CreatedUtc { get; set; }

        [Id(2)]
        public ExpirationMode ExpirationMode { get; set; }

        [Id(3)]
        public double ExpirationTimeout { get; set; }

        [Id(4)]
        public string Key { get; set; }

        [Id(5)]
        public long LastAccessedUtc { get; set; }

        [Id(6)]
        public string Region { get; set; }

        [Id(7)]
        public string ValueType { get; set; }

        [Id(8)]
        public bool UsesExpirationDefaults { get; set; }

        [Id(9)]
        public T Value { get; set; }

        public CacheItem<T> ToCacheItem()
        {
            var item = string.IsNullOrWhiteSpace(this.Region) ?
                new CacheItem<T>(this.Key, this.Value) :
                new CacheItem<T>(this.Key, this.Region, this.Value);

            if (!this.UsesExpirationDefaults)
            {
                if (this.ExpirationMode == ExpirationMode.Sliding)
                {
                    item = item.WithSlidingExpiration(TimeSpan.FromMilliseconds(this.ExpirationTimeout));
                }
                else if (this.ExpirationMode == ExpirationMode.Absolute)
                {
                    item = item.WithAbsoluteExpiration(TimeSpan.FromMilliseconds(this.ExpirationTimeout));
                }
                else if (this.ExpirationMode == ExpirationMode.None)
                {
                    item = item.WithNoExpiration();
                }
            }
            else
            {
                item = item.WithDefaultExpiration();
            }

            item.LastAccessedUtc = new DateTime(this.LastAccessedUtc);

            return item.WithCreated(new DateTime(this.CreatedUtc));
        }

        // don't remove, referenced via reflection. Needed cuz we cannot cast Item<T> to Item<object>
        public CacheItem<object> ToObjectCacheItem()
        {
            var item = string.IsNullOrWhiteSpace(this.Region) ?
                new CacheItem<object>(this.Key, this.Value) :
                new CacheItem<object>(this.Key, this.Region, this.Value);

            if (!this.UsesExpirationDefaults)
            {
                if (this.ExpirationMode == ExpirationMode.Sliding)
                {
                    item = item.WithSlidingExpiration(TimeSpan.FromMilliseconds(this.ExpirationTimeout));
                }
                else if (this.ExpirationMode == ExpirationMode.Absolute)
                {
                    item = item.WithAbsoluteExpiration(TimeSpan.FromMilliseconds(this.ExpirationTimeout));
                }
                else if (this.ExpirationMode == ExpirationMode.None)
                {
                    item = item.WithNoExpiration();
                }
            }
            else
            {
                item = item.WithDefaultExpiration();
            }

            item.LastAccessedUtc = new DateTime(this.LastAccessedUtc);

            return item.WithCreated(new DateTime(this.CreatedUtc));
        }
    }
}