using System;
using Newtonsoft.Json;

namespace CacheManager.Core
{
    /// <summary>
    /// The json cache item will be used to serialize a <see cref="CacheItem{T}"/>.
    /// A <see cref="CacheItem{T}"/> cannot be derserialized by Newtonsoft.Json because of the private setters.
    /// </summary>
    /// <typeparam name="T">The type of the cache value.</typeparam>
    internal class JsonCacheItem<T>
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
        public T Value { get; set; }

        public static JsonCacheItem<TCacheValue> FromCacheItem<TCacheValue>(CacheItem<TCacheValue> item)
            => new JsonCacheItem<TCacheValue>()
            {
                CreatedUtc = item.CreatedUtc,
                ExpirationMode = item.ExpirationMode,
                ExpirationTimeout = item.ExpirationTimeout,
                Key = item.Key,
                LastAccessedUtc = item.LastAccessedUtc,
                Region = item.Region,
                Value = item.Value
            };

        public CacheItem<T> ToCacheItem()
        {
            var item = new CacheItem<T>(this.Key, this.Region, this.Value, this.ExpirationMode, this.ExpirationTimeout)
                .WithCreated(this.CreatedUtc);

            item.LastAccessedUtc = this.LastAccessedUtc;
            return item;
        }
    }
}