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
        public static JsonCacheItem<TCacheValue> FromCacheItem<TCacheValue>(CacheItem<TCacheValue> item)
        {
            return new JsonCacheItem<TCacheValue>()
            {
                CreatedUtc = item.CreatedUtc,
                ExpirationMode = item.ExpirationMode,
                ExpirationTimeout = item.ExpirationTimeout,
                Key = item.Key,
                LastAccessedUtc = item.LastAccessedUtc,
                Region = item.Region,
                Value = item.Value
            };
        }

        public CacheItem<T> ToCacheItem()
        {
            var item = new CacheItem<T>(this.Key, this.Region, this.Value, this.ExpirationMode, this.ExpirationTimeout)
                .WithCreated(this.CreatedUtc);

            item.LastAccessedUtc = this.LastAccessedUtc;
            return item;
        }

        /// <summary>
        /// Gets the creation date of the cache item.
        /// </summary>
        /// <value>The creation date.</value>
        [JsonProperty("createdUtc")]
        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// Gets the expiration mode.
        /// </summary>
        /// <value>The expiration mode.</value>
        [JsonProperty("expirationMode")]
        public ExpirationMode ExpirationMode { get; set; }

        /// <summary>
        /// Gets the expiration timeout.
        /// </summary>
        /// <value>The expiration timeout.</value>
        [JsonProperty("expirationTimeout")]
        public TimeSpan ExpirationTimeout { get; set; }

        /// <summary>
        /// Gets the cache key.
        /// </summary>
        /// <value>The cache key.</value>
        [JsonProperty("key")]
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the last accessed date of the cache item.
        /// </summary>
        /// <value>The last accessed date.</value>
        [JsonProperty("lastAccessedUtc")]
        public DateTime LastAccessedUtc { get; set; }

        /// <summary>
        /// Gets the cache region.
        /// </summary>
        /// <value>The cache region.</value>
        [JsonProperty("region")]
        public string Region { get; set; }

        /// <summary>
        /// Gets the cache value.
        /// </summary>
        /// <value>The cache value.</value>
        [JsonProperty("value")]
        public T Value { get; set; }
    }
}