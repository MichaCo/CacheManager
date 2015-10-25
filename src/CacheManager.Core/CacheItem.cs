﻿using System;
#if !PORTABLE
using System.Runtime.Serialization;
#endif
using CacheManager.Core.Configuration;

namespace CacheManager.Core
{
    /// <summary>
    /// The item which will be stored in the cache holding the cache value and additional
    /// information needed by the cache handles and manager.
    /// </summary>
    /// <typeparam name="T">The type of the cache value.</typeparam>
#if !PORTABLE
    [Serializable]
    public class CacheItem<T> : ISerializable
#else
    public class CacheItem<T>
#endif
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CacheItem{T}"/> class.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The cache value.</param>
        /// <exception cref="System.ArgumentNullException">If key or value are null.</exception>
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
            this.ParentKeys = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheItem{T}"/> class.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The cache value.</param>
        /// <param name="region">The cache region.</param>
        /// <exception cref="System.ArgumentNullException">If key, value or region are null.</exception>
        public CacheItem(string key, T value, string region)
            : this(key, value)
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                throw new ArgumentNullException("region");
            }

            this.Region = region;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheItem{T}"/> class.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The cache value.</param>
        /// <param name="expiration">The expiration mode.</param>
        /// <param name="timeout">The expiration timeout.</param>
        /// <exception cref="System.ArgumentNullException">If key or value are null.</exception>
        public CacheItem(string key, T value, ExpirationMode expiration, TimeSpan timeout)
            : this(key, value)
        {
            this.ExpirationMode = expiration;
            this.ExpirationTimeout = timeout;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheItem{T}"/> class.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The cache value.</param>
        /// <param name="region">The cache region.</param>
        /// <param name="expiration">The expiration mode.</param>
        /// <param name="timeout">The expiration timeout.</param>
        /// <exception cref="System.ArgumentNullException">If key, value or region are null.</exception>
        public CacheItem(string key, T value, string region, ExpirationMode expiration, TimeSpan timeout)
            : this(key, value, region)
        {
            this.ExpirationMode = expiration;
            this.ExpirationTimeout = timeout;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheItem{T}"/> class.
        /// </summary>
        protected CacheItem()
        {
        }

#if !PORTABLE
        /// <summary>
        /// Initializes a new instance of the <see cref="CacheItem{T}"/> class.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="context">The context.</param>
        /// <exception cref="System.ArgumentNullException">If info is null.</exception>
        protected CacheItem(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            this.Key = info.GetString("Key");
            this.Value = (T)info.GetValue("Value", typeof(T));
            this.ValueType = (Type)info.GetValue("ValueType", typeof(Type));
            this.Region = info.GetString("Region");
            this.ExpirationMode = (ExpirationMode)info.GetValue("ExpirationMode", typeof(ExpirationMode));
            this.ExpirationTimeout = (TimeSpan)info.GetValue("ExpirationTimeout", typeof(TimeSpan));
            this.CreatedUtc = info.GetDateTime("CreatedUtc");
            this.LastAccessedUtc = info.GetDateTime("LastAccessedUtc");
        }
#endif

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

        /// <summary>
        /// Gets or sets the creation date of the cache item.
        /// </summary>
        /// <value>The creation date.</value>
        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// Gets the expiration mode.
        /// </summary>
        /// <value>The expiration mode.</value>
        public ExpirationMode ExpirationMode { get; internal set; }

        /// <summary>
        /// Gets the expiration timeout.
        /// </summary>
        /// <value>The expiration timeout.</value>
        public TimeSpan ExpirationTimeout { get; internal set; }

        /// <summary>
        /// Gets the cache key.
        /// </summary>
        /// <value>The cache key.</value>
        public string Key { get; private set; }

        /// <summary>
        /// Gets or sets the last accessed date of the cache item.
        /// </summary>
        /// <value>The last accessed date.</value>
        public DateTime LastAccessedUtc { get; set; }

        /// <summary>
        /// Gets the cache region.
        /// </summary>
        /// <value>The cache region.</value>
        public string Region { get; private set; }

        /// <summary>
        /// Gets the parent keys for this cache item
        /// NOTE: not serialized, only used on cache creation
        /// </summary>
        public string[] ParentKeys { get; set; }

        /// <summary>
        /// Gets the cache value.
        /// </summary>
        /// <value>The cache value.</value>
        public T Value { get; private set; }

        /// <summary>
        /// Gets the type of the cache value.
        /// <para>This might be used for serialization and deserialization.</para>
        /// </summary>
        /// <value>The type of the cache value.</value>
        public Type ValueType
        {
            get;
            private set;
        }

#if !PORTABLE
        /// <summary>
        /// Populates a <see cref="T:System.Runtime.Serialization.SerializationInfo"/> with the data
        /// needed to serialize the target object.
        /// </summary>
        /// <param name="info">
        /// The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> to populate with data.
        /// </param>
        /// <param name="context">
        /// The destination (see <see cref="T:System.Runtime.Serialization.StreamingContext"/>) for
        /// this serialization.
        /// </param>
        /// <exception cref="System.ArgumentNullException">If info is null.</exception>
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            info.AddValue("Key", this.Key);
            info.AddValue("Value", this.Value);
            info.AddValue("ValueType", this.ValueType);
            info.AddValue("Region", this.Region);
            info.AddValue("ExpirationMode", this.ExpirationMode);
            info.AddValue("ExpirationTimeout", this.ExpirationTimeout);
            info.AddValue("CreatedUtc", this.CreatedUtc);
            info.AddValue("LastAccessedUtc", this.LastAccessedUtc);
        }
#endif

        /// <summary>
        /// Creates a copy of the current cache item with different expiration options.
        /// This method doesn't change the state of the item in the cache. Use <c>Put</c> or similar methods to update the cache with the returned copy of the item.
        /// </summary>
        /// <remarks>We do not clone the cache item or value.</remarks>
        /// <param name="mode">The expiration mode.</param>
        /// <param name="timeout">The expiration timeout.</param>
        /// <returns>The new instance of the cache item.</returns>
        public CacheItem<T> WithExpiration(ExpirationMode mode, TimeSpan timeout)
        {
            return new CacheItem<T>(this.Key, this.Region, this.Value, this.CreatedUtc, this.LastAccessedUtc, mode,
                timeout)
            {
                ParentKeys = this.ParentKeys
            };
        }

        /// <summary>
        /// Creates a copy of the current cache item and sets a new absolute expiration date.
        /// This method doesn't change the state of the item in the cache. Use <c>Put</c> or similar methods to update the cache with the returned copy of the item.
        /// </summary>
        /// <remarks>We do not clone the cache item or value.</remarks>
        /// <param name="absoluteExpiration">The absolute expiration date.</param>
        /// <returns>The new instance of the cache item.</returns>
        public CacheItem<T> WithAbsoluteExpiration(DateTimeOffset absoluteExpiration)
        {
            TimeSpan timeout = absoluteExpiration.UtcDateTime - DateTime.UtcNow;
            if (timeout <= TimeSpan.Zero)
            {
                throw new ArgumentException("Expiration value must be greater than zero.", "absoluteExpiration");
            }

            return new CacheItem<T>(this.Key, this.Region, this.Value, this.CreatedUtc, this.LastAccessedUtc, ExpirationMode.Absolute, timeout);
        }

        /// <summary>
        /// Creates a copy of the current cache item and sets a new sliding expiration value.
        /// This method doesn't change the state of the item in the cache. Use <c>Put</c> or similar methods to update the cache with the returned copy of the item.
        /// </summary>
        /// <remarks>We do not clone the cache item or value.</remarks>
        /// <param name="slidingExpiration">The sliding expiration value.</param>
        /// <returns>The new instance of the cache item.</returns>
        public CacheItem<T> WithSlidingExpiration(TimeSpan slidingExpiration)
        {
            if (slidingExpiration <= TimeSpan.Zero)
            {
                throw new ArgumentException("Expiration value must be greater than zero.", "slidingExpiration");
            }

            return new CacheItem<T>(this.Key, this.Region, this.Value, this.CreatedUtc, this.LastAccessedUtc, ExpirationMode.Sliding, slidingExpiration);
        }

        /// <summary>
        /// Creates a copy of the current cache item without expiration. Can be used to update the cache
        /// and remove any previously configured expiration of the item.
        /// This method doesn't change the state of the item in the cache. Use <c>Put</c> or similar methods to update the cache with the returned copy of the item.
        /// </summary>
        /// <remarks>We do not clone the cache item or value.</remarks>
        /// <returns>The new instance of the cache item.</returns>
        public CacheItem<T> WithNoExpiration()
        {
            return new CacheItem<T>(this.Key, this.Region, this.Value, this.CreatedUtc, this.LastAccessedUtc, ExpirationMode.None, default(TimeSpan));
        }

        /// <summary>
        /// Creates a copy of the current cache item with new value.
        /// This method doesn't change the state of the item in the cache. Use <c>Put</c> or similar methods to update the cache with the returned copy of the item.
        /// </summary>
        /// <remarks>We do not clone the cache item or value.</remarks>
        /// <param name="value">The new value.</param>
        /// <returns>The new instance of the cache item.</returns>
        public CacheItem<T> WithValue(T value)
        {
            return new CacheItem<T>(this.Key, this.Region, value, this.CreatedUtc, this.LastAccessedUtc,
                this.ExpirationMode, this.ExpirationTimeout)
            {
                ParentKeys = this.ParentKeys
            };
        }
    }
}