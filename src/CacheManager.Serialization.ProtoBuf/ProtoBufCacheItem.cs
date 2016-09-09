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
