using System;
using System.Linq;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core
{
    public sealed partial class BaseCacheManager<TCacheValue>
    {
        /// <inheritdoc />
        public TCacheValue GetOrAdd(string key, TCacheValue value)
            => this.GetOrAdd(key, (k) => value);

        /// <inheritdoc />
        public TCacheValue GetOrAdd(string key, string region, TCacheValue value)
            => this.GetOrAdd(key, region, (k, r) => value);

        /// <inheritdoc />
        public TCacheValue GetOrAdd(string key, Func<string, TCacheValue> valueFactory)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNull(valueFactory, nameof(valueFactory));

            return this.GetOrAddInternal(key, null, (k, r) => valueFactory(k));
        }

        /// <inheritdoc />
        public TCacheValue GetOrAdd(string key, string region, Func<string, string, TCacheValue> valueFactory)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNullOrWhiteSpace(region, nameof(region));
            NotNull(valueFactory, nameof(valueFactory));

            return this.GetOrAddInternal(key, region, (k, r) => valueFactory(k, r));
        }

        /// <inheritdoc />
        public bool TryGetOrAdd(string key, Func<string, TCacheValue> valueFactory, out TCacheValue value)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNull(valueFactory, nameof(valueFactory));

            return this.TryGetOrAddInternal(key, null, (k, r) => valueFactory(k), out value);
        }

        /// <inheritdoc />
        public bool TryGetOrAdd(string key, string region, Func<string, string, TCacheValue> valueFactory, out TCacheValue value)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNullOrWhiteSpace(region, nameof(region));
            NotNull(valueFactory, nameof(valueFactory));

            return this.TryGetOrAddInternal(key, region, (k, r) => valueFactory(k, r), out value);
        }

        private bool TryGetOrAddInternal(string key, string region, Func<string, string, TCacheValue> valueFactory, out TCacheValue value)
        {
            value = default(TCacheValue);
            var tries = 0;
            do
            {
                tries++;
                var item = this.GetCacheItemInternal(key, region);
                if (item != null)
                {
                    value = item.Value;
                    return true;
                }

                value = valueFactory(key, region);

                if (value == null)
                {
                    return false;
                }

                item = string.IsNullOrWhiteSpace(region) ? new CacheItem<TCacheValue>(key, value) : new CacheItem<TCacheValue>(key, region, value);
                if (this.AddInternal(item))
                {
                    return true;
                }
            }
            while (tries <= this.Configuration.MaxRetries);

            return false;
        }

        private TCacheValue GetOrAddInternal(string key, string region, Func<string, string, TCacheValue> valueFactory)
        {
            var tries = 0;
            do
            {
                tries++;
                var item = this.GetCacheItemInternal(key, region);
                if (item != null)
                {
                    return item.Value;
                }

                var newValue = valueFactory(key, region);

                // Throw explicit to me more consistent. Otherwise it would throw later eventually...
                if (newValue == null)
                {
                    throw new InvalidOperationException("The value which should be added must not be null.");
                }

                item = string.IsNullOrWhiteSpace(region) ? new CacheItem<TCacheValue>(key, newValue) : new CacheItem<TCacheValue>(key, region, newValue);
                if (this.AddInternal(item))
                {
                    return newValue;
                }
            }
            while (tries <= this.Configuration.MaxRetries);

            // should usually never occur, but could if e.g. max retries is 1 and an item gets added between the get and add.
            // pretty unusual, so keep the max tries at least around 50
            throw new InvalidOperationException(
                string.Format("Could not get nor add the item {0} {1}", key, region));
        }
    }
}