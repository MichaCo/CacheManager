using System;
using System.Runtime.Serialization;

namespace CacheManager.Core
{
    /// <summary>
    /// The data contract cache item will be used to serialize a <see cref="CacheItem{T}"/>.
    /// A <see cref="CacheItem{T}"/> cannot be derserialized by DataContractSerializer because of the private setters.
    /// </summary>
    [DataContract]
    internal class DataContractCacheItem
    {
        [DataMember(Name = "createdUtc")]
        public DateTime CreatedUtc { get; set; }

        [DataMember(Name = "expirationMode")]
        public ExpirationMode ExpirationMode { get; set; }

        [DataMember(Name = "expirationTimeout")]
        public TimeSpan ExpirationTimeout { get; set; }

        [DataMember(Name = "key")]
        public string Key { get; set; }

        [DataMember(Name = "lastAccessedUtc")]
        public DateTime LastAccessedUtc { get; set; }

        [DataMember(Name = "region")]
        public string Region { get; set; }

        [DataMember(Name = "value")]
        public byte[] Value { get; set; }

        public static DataContractCacheItem FromCacheItem<TCacheValue>(CacheItem<TCacheValue> item, byte[] value)
            => new DataContractCacheItem()
            {
                CreatedUtc = item.CreatedUtc,
                ExpirationMode = item.ExpirationMode,
                ExpirationTimeout = item.ExpirationTimeout,
                Key = item.Key,
                LastAccessedUtc = item.LastAccessedUtc,
                Region = item.Region,
                Value = value
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