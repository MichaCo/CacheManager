using System;
using System.Linq;
using CacheManager.Core.Logging;

namespace CacheManager.Core
{
    public sealed partial class BaseCacheManager<TCacheValue>
    {
        /// <inheritdoc />
        public void Expire(string key, ExpirationMode mode, TimeSpan timeout)
            => this.ExpireInternal(key, null, mode, timeout);

        /// <inheritdoc />
        public void Expire(string key, string region, ExpirationMode mode, TimeSpan timeout)
            => this.ExpireInternal(key, region, mode, timeout);

        internal void ExpireInternal(string key, string region, ExpirationMode mode, TimeSpan timeout)
        {
            this.CheckDisposed();
            
            var item = this.GetCacheItemInternal(key, region);
            if (item == null)
            {
                this.Logger.LogTrace("Expire: item not found for key {0}:{1}", key, region);
                return;
            }

            if (this.logTrace)
            {
                this.Logger.LogTrace("Expire [{0}] started.", item);
            }

            if (mode == ExpirationMode.Absolute)
            {
                item = item.WithAbsoluteExpiration(timeout);
            }
            else if (mode == ExpirationMode.Sliding)
            {
                item = item.WithSlidingExpiration(timeout);
            }
            else if (mode == ExpirationMode.None)
            {
                item = item.WithNoExpiration();
            }
            else if (mode == ExpirationMode.Default)
            {
                item = item.WithDefaultExpiration();
            }
            
            if (this.logTrace)
            {
                this.Logger.LogTrace("Expire - Expiration of [{0}] has been modified. Using put to store the item...", item);
            }

            this.PutInternal(item);
        }

        /// <inheritdoc />
        public void Expire(string key, DateTimeOffset absoluteExpiration)
        {
            TimeSpan timeout = absoluteExpiration.UtcDateTime - DateTime.UtcNow;
            if (timeout <= TimeSpan.Zero)
            {
                throw new ArgumentException("Expiration value must be greater than zero.", nameof(absoluteExpiration));
            }

            this.Expire(key, ExpirationMode.Absolute, timeout);
        }

        /// <inheritdoc />
        public void Expire(string key, string region, DateTimeOffset absoluteExpiration)
        {
            TimeSpan timeout = absoluteExpiration.UtcDateTime - DateTime.UtcNow;
            if (timeout <= TimeSpan.Zero)
            {
                throw new ArgumentException("Expiration value must be greater than zero.", nameof(absoluteExpiration));
            }

            this.Expire(key, region, ExpirationMode.Absolute, timeout);
        }

        /// <inheritdoc />
        public void Expire(string key, TimeSpan slidingExpiration)
        {
            if (slidingExpiration <= TimeSpan.Zero)
            {
                throw new ArgumentException("Expiration value must be greater than zero.", nameof(slidingExpiration));
            }

            this.Expire(key, ExpirationMode.Sliding, slidingExpiration);
        }

        /// <inheritdoc />
        public void Expire(string key, string region, TimeSpan slidingExpiration)
        {
            if (slidingExpiration <= TimeSpan.Zero)
            {
                throw new ArgumentException("Expiration value must be greater than zero.", nameof(slidingExpiration));
            }

            this.Expire(key, region, ExpirationMode.Sliding, slidingExpiration);
        }

        /// <inheritdoc />
        public void RemoveExpiration(string key)
        {
            this.Expire(key, ExpirationMode.None, default(TimeSpan));
        }

        /// <inheritdoc />
        public void RemoveExpiration(string key, string region)
        {
            this.Expire(key, region, ExpirationMode.None, default(TimeSpan));
        }
    }
}