using System;
using CacheManager.Core.Internal;
using Newtonsoft.Json;

namespace CacheManager.Core
{
    internal class JsonCacheItem<T> : SerializerCacheItem<T>
    {
        [JsonConstructor]
        public JsonCacheItem()
        {
        }

        public JsonCacheItem(ICacheItemProperties properties, object value) : base(properties, value)
        {
        }
        
        [JsonProperty("createdUtc")]
        public override long CreatedUtc { get; set; }
        
        [JsonProperty("expirationMode")]
        public override ExpirationMode ExpirationMode { get; set; }
        
        [JsonProperty("expirationTimeout")]
        public override int ExpirationTimeout { get; set; }
        
        [JsonProperty("key")]
        public override string Key { get; set; }
        
        [JsonProperty("lastAccessedUtc")]
        public override long LastAccessedUtc { get; set; }
        
        [JsonProperty("region")]
        public override string Region { get; set; }
        
        [JsonProperty("usesDefaultExpiration")]
        public override bool UsesExpirationDefaults { get; set; }
        
        [JsonProperty("valueType")]
        public override string ValueType { get; set; }
        
        [JsonProperty("value")]
        public override T Value { get; set; }
    }
}