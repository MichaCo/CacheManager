using System;
using System.Linq;
using Bond;
using CacheManager.Core;
using CacheManager.Core.Internal;

namespace CacheManager.Serialization.Bond
{
    [Schema]
    internal class BondCacheItem<T> : SerializerCacheItem<T>
    {
        public BondCacheItem()
        {
        }

        public BondCacheItem(ICacheItemProperties properties, object value) : base(properties, value)
        {
        }

        [Id(1)]
        public override long CreatedUtc { get; set; }

        [Id(2)]
        public override ExpirationMode ExpirationMode { get; set; }

        [Id(3)]
        public override double ExpirationTimeout { get; set; }

        [Id(4)]
        public override string Key { get; set; }

        [Id(5)]
        public override long LastAccessedUtc { get; set; }

        [Id(6)]
        public override string Region { get; set; }

        [Id(7)]
        public override string ValueType { get; set; }

        [Id(8)]
        public override bool UsesExpirationDefaults { get; set; }

        [Id(9)]
        public override T Value { get; set; }
    }
}