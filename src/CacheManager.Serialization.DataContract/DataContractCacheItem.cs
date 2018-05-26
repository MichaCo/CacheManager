using System;
using System.Runtime.Serialization;
using CacheManager.Core;
using CacheManager.Core.Internal;

namespace CacheManager.Serialization.DataContract
{
    /// <summary>
    /// The data contract cache item will be used to serialize a <see cref="CacheItem{T}"/>.
    /// A <see cref="CacheItem{T}"/> cannot be derserialized by DataContractSerializer because of the private setters.
    /// </summary>
    [DataContract]
    internal class DataContractCacheItem<T> : SerializerCacheItem<T>
    {
        public DataContractCacheItem()
        {
        }

        public DataContractCacheItem(ICacheItemProperties properties, object value) : base(properties, value)
        {
        }

        [DataMember(Name = "createdUtc")]
        public override long CreatedUtc { get; set; }

        [DataMember(Name = "expirationMode")]
        public override ExpirationMode ExpirationMode { get; set; }

        [DataMember(Name = "expirationTimeout")]
        public override double ExpirationTimeout { get; set; }

        [DataMember(Name = "key")]
        public override string Key { get; set; }

        [DataMember(Name = "lastAccessedUtc")]
        public override long LastAccessedUtc { get; set; }

        [DataMember(Name = "region")]
        public override string Region { get; set; }

        [DataMember(Name = "value")]
        public override T Value { get; set; }

        [DataMember(Name = "usesExpirationDefaults")]
        public override bool UsesExpirationDefaults { get; set; }

        [DataMember(Name = "valueType")]
        public override string ValueType { get; set; }
    }
}
