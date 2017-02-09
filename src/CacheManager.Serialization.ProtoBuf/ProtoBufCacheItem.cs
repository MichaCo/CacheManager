using System;
using CacheManager.Core;
using CacheManager.Core.Internal;
using ProtoBuf;

namespace CacheManager.Serialization.ProtoBuf
{
    [ProtoContract]
    internal class ProtoBufCacheItem<T> : SerializerCacheItem<T>
    {
        // needed so the serializer can deserialize the item using the empty ctor.
        public ProtoBufCacheItem()
        {
        }

        public ProtoBufCacheItem(ICacheItemProperties properties, object value) : base(properties, value)
        {
        }

        [ProtoMember(1)]
        public override long CreatedUtc { get; set; }

        [ProtoMember(2)]
        public override long LastAccessedUtc { get; set; }

        [ProtoMember(3)]
        public override ExpirationMode ExpirationMode { get; set; }

        [ProtoMember(4)]
        public override double ExpirationTimeout { get; set; }

        [ProtoMember(5)]
        public override string Key { get; set; }

        [ProtoMember(6)]
        public override string Region { get; set; }

        [ProtoMember(7)]
        public override T Value { get; set; }

        [ProtoMember(8)]
        public override string ValueType { get; set; }

        [ProtoMember(9)]
        public override bool UsesExpirationDefaults { get; set; }
    }
}