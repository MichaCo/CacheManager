using System;
using System.Linq;
using CacheManager.Core.Logging;

namespace CacheManager.Core
{
    public sealed partial class BaseCacheManager<TCacheValue>
    {
        /// <inheritdoc />
        public void Expire(string key, ExpirationMode mode, TimeSpan timeout)
            => ExpireInternal(key, null, mode, timeout);

        /// <inheritdoc />
        public void Expire(string key, string region, ExpirationMode mode, TimeSpan timeout)
            => ExpireInternal(key, region, mode, timeout);

        private void ExpireInternal(string key, string region, ExpirationMode mode, TimeSpan timeout)
        {
            CheckDisposed();

            var item = GetCacheItemInternal(key, region);
            if (item == null)
            {
                Logger.LogTrace("Expire: item not found for key {0}:{1}", key, region);
                return;
            }

            if (_logTrace)
            {
                Logger.LogTrace("Expire [{0}] started.", item);
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

            if (_logTrace)
            {
                Logger.LogTrace("Expire - Expiration of [{0}] has been modified. Using put to store the item...", item);
            }

            PutInternal(item);
        }

        /// <inheritdoc />
        public void Expire(string key, DateTimeOffset absoluteExpiration)
        {
            var timeout = absoluteExpiration.UtcDateTime - DateTime.UtcNow;
            if (timeout <= TimeSpan.Zero)
            {
                throw new ArgumentException("Expiration value must be greater than zero.", nameof(absoluteExpiration));
            }

            Expire(key, ExpirationMode.Absolute, timeout);
        }

        /// <inheritdoc />
        public void Expire(string key, string region, DateTimeOffset absoluteExpiration)
        {
            var timeout = absoluteExpiration.UtcDateTime - DateTime.UtcNow;
            if (timeout <= TimeSpan.Zero)
            {
                throw new ArgumentException("Expiration value must be greater than zero.", nameof(absoluteExpiration));
            }

            Expire(key, region, ExpirationMode.Absolute, timeout);
        }

        /// <inheritdoc />
        public void Expire(string key, TimeSpan slidingExpiration)
        {
            if (slidingExpiration <= TimeSpan.Zero)
            {
                throw new ArgumentException("Expiration value must be greater than zero.", nameof(slidingExpiration));
            }

            Expire(key, ExpirationMode.Sliding, slidingExpiration);
        }

        /// <inheritdoc />
        public void Expire(string key, string region, TimeSpan slidingExpiration)
        {
            if (slidingExpiration <= TimeSpan.Zero)
            {
                throw new ArgumentException("Expiration value must be greater than zero.", nameof(slidingExpiration));
            }

            Expire(key, region, ExpirationMode.Sliding, slidingExpiration);
        }

        /// <inheritdoc />
        public void RemoveExpiration(string key)
        {
            Expire(key, ExpirationMode.None, default(TimeSpan));
        }

        /// <inheritdoc />
        public void RemoveExpiration(string key, string region)
        {
            Expire(key, region, ExpirationMode.None, default(TimeSpan));
        }
    }
}