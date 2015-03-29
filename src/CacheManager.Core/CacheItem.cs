using System;
using System.Runtime.Serialization;
using CacheManager.Core.Configuration;

namespace CacheManager.Core
{
    [Serializable]
    public class CacheItem<T> : ISerializable
    {
        protected CacheItem()
        {
        }

        private CacheItem(string key, string region, T value, DateTime created, DateTime lastAccess, ExpirationMode expiration, TimeSpan timeout)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            this.Key = key;
            this.Region = region;
            this.Value = value;
            this.ValueType = value.GetType();
            this.CreatedUtc = created;
            this.LastAccessedUtc = lastAccess;
            this.ExpirationMode = expiration;
            this.ExpirationTimeout = timeout;
        }

        public CacheItem(string key, T value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            this.Key = key;
            this.Value = value;
            this.ValueType = value.GetType();
            this.CreatedUtc = DateTime.UtcNow;
            this.LastAccessedUtc = DateTime.UtcNow;
        }

        public CacheItem(string key, T value, string region)
            : this(key, value)
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                throw new ArgumentNullException("region");
            }

            this.Region = region;
        }

        public CacheItem(string key, T value, ExpirationMode expiration, TimeSpan timeout)
            : this(key, value)
        {
            this.ExpirationMode = expiration;
            this.ExpirationTimeout = timeout;
        }

        public CacheItem(string key, T value, string region, ExpirationMode expiration, TimeSpan timeout)
            : this(key, value, region)
        {
            this.ExpirationMode = expiration;
            this.ExpirationTimeout = timeout;
        }

        protected CacheItem(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            Key = info.GetString("Key");
            Value = (T)info.GetValue("Value", typeof(T));
            ValueType = (Type)info.GetValue("ValueType", typeof(Type));
            Region = info.GetString("Region");
            ExpirationMode = (ExpirationMode)info.GetValue("ExpirationMode", typeof(ExpirationMode));
            ExpirationTimeout = (TimeSpan)info.GetValue("ExpirationTimeout", typeof(TimeSpan));
            CreatedUtc = info.GetDateTime("CreatedUtc");
            LastAccessedUtc = info.GetDateTime("LastAccessedUtc");
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            info.AddValue("Key", Key);
            info.AddValue("Value", Value);
            info.AddValue("ValueType", ValueType);
            info.AddValue("Region", Region);
            info.AddValue("ExpirationMode", ExpirationMode);
            info.AddValue("ExpirationTimeout", ExpirationTimeout);
            info.AddValue("CreatedUtc", CreatedUtc);
            info.AddValue("LastAccessedUtc", LastAccessedUtc);
        }

        public CacheItem<T> WithValue(T value)
        {
            return new CacheItem<T>(this.Key, this.Region, value, this.CreatedUtc, this.LastAccessedUtc, this.ExpirationMode, this.ExpirationTimeout);
        }

        public CacheItem<T> WithExpiration(ExpirationMode mode, TimeSpan timeout)
        {
            return new CacheItem<T>(this.Key, this.Region, this.Value, this.CreatedUtc, this.LastAccessedUtc, mode, timeout);
        }

        public T Value { get; private set; }

        public Type ValueType
        {
            get;
            private set;
        }

        public string Key { get; private set; }

        public string Region { get; private set; }

        public ExpirationMode ExpirationMode { get; internal set; }

        public TimeSpan ExpirationTimeout { get; internal set; }

        public DateTime CreatedUtc { get; set; }

        public DateTime LastAccessedUtc { get; set; }
    }
}