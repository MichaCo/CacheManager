using System;

#if !NETSTANDARD

using System.Runtime.Serialization;

#endif

using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core
{
    /// <summary>
    /// The item which will be stored in the cache holding the cache value and additional
    /// information needed by the cache handles and manager.
    /// </summary>
    /// <typeparam name="T">The type of the cache value.</typeparam>
#if !NETSTANDARD

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
            : this(key, null, value, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheItem{T}"/> class.
        /// </summary>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The cache value.</param>
        /// <param name="region">The cache region.</param>
        /// <exception cref="System.ArgumentNullException">If key, value or region are null.</exception>
        public CacheItem(string key, string region, T value)
            : this(key, region, value, null, null, null)
        {
            NotNullOrWhiteSpace(region, nameof(region));
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
            : this(key, null, value, expiration, timeout, null)
        {
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
        public CacheItem(string key, string region, T value, ExpirationMode expiration, TimeSpan timeout)
            : this(key, region, value, expiration, timeout, null)
        {
            NotNullOrWhiteSpace(region, nameof(region));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheItem{T}"/> class.
        /// </summary>
        protected CacheItem()
        {
        }

#if !NETSTANDARD

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheItem{T}"/> class.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="context">The context.</param>
        /// <exception cref="System.ArgumentNullException">If info is null.</exception>
        protected CacheItem(SerializationInfo info, StreamingContext context)
        {
            NotNull(info, nameof(info));

            this.Key = info.GetString(nameof(this.Key));
            this.Value = (T)info.GetValue(nameof(this.Value), typeof(T));
            this.ValueType = (Type)info.GetValue(nameof(this.ValueType), typeof(Type));
            this.Region = info.GetString(nameof(this.Region));
            this.ExpirationMode = (ExpirationMode)info.GetValue(nameof(this.ExpirationMode), typeof(ExpirationMode));
            this.ExpirationTimeout = (TimeSpan)info.GetValue(nameof(this.ExpirationTimeout), typeof(TimeSpan));
            this.CreatedUtc = info.GetDateTime(nameof(this.CreatedUtc));
            this.LastAccessedUtc = info.GetDateTime(nameof(this.LastAccessedUtc));
        }

#endif

        private CacheItem(string key, string region, T value, ExpirationMode? expiration, TimeSpan? timeout, DateTime? created, DateTime? lastAccessed = null)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNull(value, nameof(value));

            this.Key = key;
            this.Region = region;
            this.Value = value;
            this.ValueType = value.GetType();
            this.ExpirationMode = expiration ?? ExpirationMode.Default;
            this.ExpirationTimeout = (this.ExpirationMode == ExpirationMode.None || this.ExpirationMode == ExpirationMode.Default) ? TimeSpan.Zero : timeout ?? TimeSpan.Zero;

            // validation check for very high expiration time.
            // Otherwise this will lead to all kinds of errors (e.g. adding time to sliding while using a TimeSpan with long.MaxValue ticks)
            if (this.ExpirationTimeout.TotalDays > 365)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), "Expiration timeout must be between 00:00:00 and 365:00:00:00.");
            }
            if (this.ExpirationMode != ExpirationMode.Default && this.ExpirationMode != ExpirationMode.None && this.ExpirationTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), "Expiration timeout must be greater than zero if expiration mode is defined.");
            }

            this.CreatedUtc = created ?? DateTime.UtcNow;
            this.LastAccessedUtc = lastAccessed ?? DateTime.UtcNow;
        }

        /// <summary>
        /// Gets a value indicating whether the item is logically expired or not.
        /// Depending on the cache vendor, the item might still live in the cache although
        /// according to the expiration mode and timeout, the item is already expired.
        /// </summary>
        public bool IsExpired
        {
            get
            {
                DateTime now = DateTime.UtcNow;
                if (this.ExpirationMode == ExpirationMode.Absolute
                    && this.CreatedUtc.Add(this.ExpirationTimeout) < now)
                {
                    return true;
                }
                else if (this.ExpirationMode == ExpirationMode.Sliding
                    && this.LastAccessedUtc.Add(this.ExpirationTimeout) < now)
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the creation date of the cache item.
        /// </summary>
        /// <value>The creation date.</value>
        public DateTime CreatedUtc { get; }

        /// <summary>
        /// Gets the expiration mode.
        /// </summary>
        /// <value>The expiration mode.</value>
        public ExpirationMode ExpirationMode { get; }

        /// <summary>
        /// Gets the expiration timeout.
        /// </summary>
        /// <value>The expiration timeout.</value>
        public TimeSpan ExpirationTimeout { get; }

        /// <summary>
        /// Gets the cache key.
        /// </summary>
        /// <value>The cache key.</value>
        public string Key { get; }

        /// <summary>
        /// Gets or sets the last accessed date of the cache item.
        /// </summary>
        /// <value>The last accessed date.</value>
        public DateTime LastAccessedUtc { get; set; }

        /// <summary>
        /// Gets the cache region.
        /// </summary>
        /// <value>The cache region.</value>
        public string Region { get; }

        /// <summary>
        /// Gets the cache value.
        /// </summary>
        /// <value>The cache value.</value>
        public T Value { get; }

        /// <summary>
        /// Gets the type of the cache value.
        /// <para>This might be used for serialization and deserialization.</para>
        /// </summary>
        /// <value>The type of the cache value.</value>
        public Type ValueType { get; }

#if !NETSTANDARD

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
            NotNull(info, nameof(info));

            info.AddValue(nameof(this.Key), this.Key);
            info.AddValue(nameof(this.Value), this.Value);
            info.AddValue(nameof(this.ValueType), this.ValueType);
            info.AddValue(nameof(this.Region), this.Region);
            info.AddValue(nameof(this.ExpirationMode), this.ExpirationMode);
            info.AddValue(nameof(this.ExpirationTimeout), this.ExpirationTimeout);
            info.AddValue(nameof(this.CreatedUtc), this.CreatedUtc);
            info.AddValue(nameof(this.LastAccessedUtc), this.LastAccessedUtc);
        }

#endif

        /// <inheritdoc />
        public override string ToString()
        {
            return !string.IsNullOrWhiteSpace(this.Region) ?
                $"'{this.Region}:{this.Key}', exp:{this.ExpirationMode.ToString()} {this.ExpirationTimeout}, lastAccess:{this.LastAccessedUtc}"
                : $"'{this.Key}', exp:{this.ExpirationMode.ToString()} {this.ExpirationTimeout}, lastAccess:{this.LastAccessedUtc}";
        }

        /// <summary>
        /// Creates a copy of the current cache item with different expiration options.
        /// This method doesn't change the state of the item in the cache. Use <c>Put</c> or similar methods to update the cache with the returned copy of the item.
        /// </summary>
        /// <remarks>We do not clone the cache item or value.</remarks>
        /// <param name="mode">The expiration mode.</param>
        /// <param name="timeout">The expiration timeout.</param>
        /// <returns>The new instance of the cache item.</returns>
        public CacheItem<T> WithExpiration(ExpirationMode mode, TimeSpan timeout) =>
            new CacheItem<T>(this.Key, this.Region, this.Value, mode, timeout, this.CreatedUtc, this.LastAccessedUtc);

        /// <summary>
        /// Creates a copy of the current cache item and sets a new absolute expiration date.
        /// This method doesn't change the state of the item in the cache. Use <c>Put</c> or similar methods to update the cache with the returned copy of the item.
        /// </summary>
        /// <remarks>We do not clone the cache item or value.</remarks>
        /// <param name="absoluteExpiration">The absolute expiration date.</param>
        /// <returns>The new instance of the cache item.</returns>
        public CacheItem<T> WithAbsoluteExpiration(DateTimeOffset absoluteExpiration)
        {
            TimeSpan timeout = absoluteExpiration - DateTimeOffset.UtcNow;
            if (timeout <= TimeSpan.Zero)
            {
                throw new ArgumentException("Expiration value must be greater than zero.", nameof(absoluteExpiration));
            }

            return new CacheItem<T>(this.Key, this.Region, this.Value, ExpirationMode.Absolute, timeout, this.CreatedUtc, this.LastAccessedUtc);
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
                throw new ArgumentException("Expiration value must be greater than zero.", nameof(slidingExpiration));
            }

            return new CacheItem<T>(this.Key, this.Region, this.Value, ExpirationMode.Sliding, slidingExpiration, this.CreatedUtc, this.LastAccessedUtc);
        }

        /// <summary>
        /// Creates a copy of the current cache item without expiration. Can be used to update the cache
        /// and remove any previously configured expiration of the item.
        /// This method doesn't change the state of the item in the cache. Use <c>Put</c> or similar methods to update the cache with the returned copy of the item.
        /// </summary>
        /// <remarks>We do not clone the cache item or value.</remarks>
        /// <returns>The new instance of the cache item.</returns>
        public CacheItem<T> WithNoExpiration() =>
            new CacheItem<T>(this.Key, this.Region, this.Value, ExpirationMode.None, TimeSpan.Zero, this.CreatedUtc, this.LastAccessedUtc);

        /// <summary>
        /// Creates a copy of the current cache item with no explicit expiration, instructing the cache to use the default defined in the cache handle configuration.
        /// This method doesn't change the state of the item in the cache. Use <c>Put</c> or similar methods to update the cache with the returned copy of the item.
        /// </summary>
        /// <remarks>We do not clone the cache item or value.</remarks>
        /// <returns>The new instance of the cache item.</returns>
        public CacheItem<T> WithDefaultExpiration() =>
            new CacheItem<T>(this.Key, this.Region, this.Value, ExpirationMode.Default, TimeSpan.Zero, this.CreatedUtc, this.LastAccessedUtc);

        /// <summary>
        /// Creates a copy of the current cache item with new value.
        /// This method doesn't change the state of the item in the cache. Use <c>Put</c> or similar methods to update the cache with the returned copy of the item.
        /// </summary>
        /// <remarks>We do not clone the cache item or value.</remarks>
        /// <param name="value">The new value.</param>
        /// <returns>The new instance of the cache item.</returns>
        public CacheItem<T> WithValue(T value) =>
            new CacheItem<T>(this.Key, this.Region, value, this.ExpirationMode, this.ExpirationTimeout, this.CreatedUtc, this.LastAccessedUtc);

        /// <summary>
        /// Creates a copy of the current cache item with a given created date.
        /// This method doesn't change the state of the item in the cache. Use <c>Put</c> or similar methods to update the cache with the returned copy of the item.
        /// </summary>
        /// <remarks>We do not clone the cache item or value.</remarks>
        /// <param name="created">The new created date.</param>
        /// <returns>The new instance of the cache item.</returns>
        public CacheItem<T> WithCreated(DateTime created) =>
            new CacheItem<T>(this.Key, this.Region, this.Value, this.ExpirationMode, this.ExpirationTimeout, created, this.LastAccessedUtc);
    }
}