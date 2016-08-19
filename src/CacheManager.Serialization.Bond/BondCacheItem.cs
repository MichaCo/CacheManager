using System;
using CacheManager.Core;
using MSBond = Bond;

namespace CacheManager.Serialization.Bond
{
    [MSBond.Schema]
    public class BondCacheItem
    {
        [MSBond.Id(0)]
        public DateTime CreatedUtc { get; set; }

        [MSBond.Id(1)]
        public DateTime LastAccessedUtc { get; set; }

        [MSBond.Id(2)]
        public ExpirationMode ExpirationMode { get; set; }

        [MSBond.Id(3)]
        public TimeSpan ExpirationTimeout { get; set; }

        [MSBond.Id(4)]
        public string Key { get; set; }

        [MSBond.Id(5)]
        public string Region { get; set; }

        [MSBond.Id(6)]
        public byte[] Value { get; set; }

        public static BondCacheItem FromCacheItem<TValue>(CacheItem<TValue> item, byte[] value)
        {
            return new BondCacheItem
            {
                CreatedUtc = item.CreatedUtc,
                LastAccessedUtc = item.LastAccessedUtc,
                ExpirationMode = item.ExpirationMode,
                ExpirationTimeout = item.ExpirationTimeout,
                Key = item.Key,
                Region = item.Region,
                Value = value
            };
        }

        public CacheItem<T> ToCacheItem<T>(object value)
        {
            var output = string.IsNullOrEmpty(Region) ?
                    new CacheItem<T>(Key, (T)value, ExpirationMode, ExpirationTimeout) :
                    new CacheItem<T>(Key, Region, (T)value, ExpirationMode, ExpirationTimeout);

            output.LastAccessedUtc = LastAccessedUtc;
            return output.WithCreated(CreatedUtc);
        }
    }
}
