using System;
using Newtonsoft.Json;

namespace CacheManager.Core
{
    /// <summary>
    /// The json cache item will be used to serialize a <see cref="CacheItem{T}"/>.
    /// A <see cref="CacheItem{T}"/> cannot be derserialized by Newtonsoft.Json because of the private setters.
    /// </summary>
    internal class JsonCacheItem
    {
        [JsonProperty("createdUtc")]
        public DateTime CreatedUtc { get; set; }

        [JsonProperty("expirationMode")]
        public ExpirationMode ExpirationMode { get; set; }

        [JsonProperty("expirationTimeout")]
        public TimeSpan ExpirationTimeout { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("lastAccessedUtc")]
        public DateTime LastAccessedUtc { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }

        [JsonProperty("value")]
        public byte[] Value { get; set; }

        [JsonProperty("ValueType")]
        public string ValueType { get; set; }

        public static JsonCacheItem FromCacheItem<TCacheValue>(CacheItem<TCacheValue> item, byte[] value)
            => new JsonCacheItem()
            {
                CreatedUtc = item.CreatedUtc,
                ExpirationMode = item.ExpirationMode,
                ExpirationTimeout = item.ExpirationTimeout,
                Key = item.Key,
                LastAccessedUtc = item.LastAccessedUtc,
                Region = item.Region,
                Value = value,
                ValueType = item.Value.GetType().AssemblyQualifiedName
            };

        public CacheItem<T> ToCacheItem<T>(object value)
        {
            var item = string.IsNullOrWhiteSpace(this.Region) ?
                new CacheItem<T>(this.Key, (T)value, this.ExpirationMode, this.ExpirationTimeout) :
                new CacheItem<T>(this.Key, this.Region, (T)value, this.ExpirationMode, this.ExpirationTimeout);

            item.LastAccessedUtc = this.LastAccessedUtc;

            return item.WithCreated(this.CreatedUtc);
        }
    }
}