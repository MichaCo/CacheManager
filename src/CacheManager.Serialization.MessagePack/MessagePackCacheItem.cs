using CacheManager.Core.Internal;
using MessagePack;

namespace CacheManager.Core
{
    [MessagePackObject]
    internal class MessagePackCacheItem<T> : SerializerCacheItem<T>
    {
        public MessagePackCacheItem()
        {
        }

        public MessagePackCacheItem(ICacheItemProperties properties, object value) : base(properties, value)
        {
        }

        [Key(0)]
        public override long CreatedUtc { get; set; }
        [Key(1)]
        public override ExpirationMode ExpirationMode { get; set; }
        [Key(2)]
        public override double ExpirationTimeout { get; set; }
        [Key(3)]
        public override string Key { get; set; }
        [Key(4)]
        public override long LastAccessedUtc { get; set; }
        [Key(5)]
        public override string Region { get; set; }
        [Key(6)]
        public override bool UsesExpirationDefaults { get; set; }
        [Key(7)]
        public override string ValueType { get; set; }
        [Key(8)]
        public override T Value { get; set; }
    }
}
