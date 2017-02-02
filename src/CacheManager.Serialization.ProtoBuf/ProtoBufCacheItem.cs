using System;
using CacheManager.Core;
using ProtoBuf;

namespace CacheManager.Serialization.ProtoBuf
{
    [ProtoContract]
    internal class ProtoBufCacheItem
    {
        [ProtoMember(1)]
        public DateTime CreatedUtc { get; set; }

        [ProtoMember(2)]
        public DateTime LastAccessedUtc { get; set; }

        [ProtoMember(3)]
        public ExpirationMode ExpirationMode { get; set; }

        [ProtoMember(4)]
        public TimeSpan ExpirationTimeout { get; set; }

        [ProtoMember(5)]
        public string Key { get; set; }

        [ProtoMember(6)]
        public string Region { get; set; }

        [ProtoMember(7)]
        public byte[] Value { get; set; }

        [ProtoMember(8)]
        public string ValueType { get; set; }

        [ProtoMember(9)]
        public bool UsesExpirationDefaults { get; set; }

        public static ProtoBufCacheItem FromCacheItem<TValue>(CacheItem<TValue> item, byte[] value)
        {
            return new ProtoBufCacheItem
            {
                CreatedUtc = item.CreatedUtc,
                LastAccessedUtc = item.LastAccessedUtc,
                ExpirationMode = item.ExpirationMode,
                ExpirationTimeout = item.ExpirationTimeout,
                Key = item.Key,
                Region = item.Region,
                Value = value,
                ValueType = item.ValueType.AssemblyQualifiedName,
                UsesExpirationDefaults = item.UsesExpirationDefaults
            };
        }

        public CacheItem<T> ToCacheItem<T>(object value)
        {
            var item = string.IsNullOrWhiteSpace(this.Region) ?
                new CacheItem<T>(this.Key, (T)value) :
                new CacheItem<T>(this.Key, this.Region, (T)value);

            if (!this.UsesExpirationDefaults)
            {
                if (this.ExpirationMode == ExpirationMode.Sliding)
                {
                    item = item.WithSlidingExpiration(this.ExpirationTimeout);
                }
                else if (this.ExpirationMode == ExpirationMode.Absolute)
                {
                    item = item.WithAbsoluteExpiration(this.ExpirationTimeout);
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

            item.LastAccessedUtc = this.LastAccessedUtc;

            return item.WithCreated(this.CreatedUtc);
        }
    }
}