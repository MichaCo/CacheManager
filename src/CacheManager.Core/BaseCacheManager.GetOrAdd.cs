using System;
using System.Linq;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core
{
    public sealed partial class BaseCacheManager<TCacheValue>
    {
        /// <inheritdoc />
        public TCacheValue GetOrAdd(string key, TCacheValue value)
            => GetOrAdd(key, (k) => value);

        /// <inheritdoc />
        public TCacheValue GetOrAdd(string key, string region, TCacheValue value)
            => GetOrAdd(key, region, (k, r) => value);

        /// <inheritdoc />
        public TCacheValue GetOrAdd(string key, Func<string, TCacheValue> valueFactory)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNull(valueFactory, nameof(valueFactory));

            return GetOrAddInternal(key, null, (k, r) => new CacheItem<TCacheValue>(k, valueFactory(k))).Value;
        }

        /// <inheritdoc />
        public TCacheValue GetOrAdd(string key, string region, Func<string, string, TCacheValue> valueFactory)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNullOrWhiteSpace(region, nameof(region));
            NotNull(valueFactory, nameof(valueFactory));

            return GetOrAddInternal(key, region, (k, r) => new CacheItem<TCacheValue>(k, r, valueFactory(k, r))).Value;
        }

        /// <inheritdoc />
        public CacheItem<TCacheValue> GetOrAdd(string key, Func<string, CacheItem<TCacheValue>> valueFactory)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNull(valueFactory, nameof(valueFactory));

            return GetOrAddInternal(key, null, (k, r) => valueFactory(k));
        }

        /// <inheritdoc />
        public CacheItem<TCacheValue> GetOrAdd(string key, string region, Func<string, string, CacheItem<TCacheValue>> valueFactory)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNullOrWhiteSpace(region, nameof(region));
            NotNull(valueFactory, nameof(valueFactory));

            return GetOrAddInternal(key, region, valueFactory);
        }

        /// <inheritdoc />
        public bool TryGetOrAdd(string key, Func<string, TCacheValue> valueFactory, out TCacheValue value)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNull(valueFactory, nameof(valueFactory));

            if (TryGetOrAddInternal(
                key,
                null,
                (k, r) =>
                {
                    var newValue = valueFactory(k);
                    return newValue == null ? null : new CacheItem<TCacheValue>(k, newValue);
                },
                out CacheItem<TCacheValue> item))
            {
                value = item.Value;
                return true;
            }

            value = default(TCacheValue);
            return false;
        }

        /// <inheritdoc />
        public bool TryGetOrAdd(string key, string region, Func<string, string, TCacheValue> valueFactory, out TCacheValue value)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNullOrWhiteSpace(region, nameof(region));
            NotNull(valueFactory, nameof(valueFactory));

            if (TryGetOrAddInternal(
                key,
                region,
                (k, r) =>
                {
                    var newValue = valueFactory(k, r);
                    return newValue == null ? null : new CacheItem<TCacheValue>(k, r, newValue);
                },
                out CacheItem<TCacheValue> item))
            {
                value = item.Value;
                return true;
            }

            value = default(TCacheValue);
            return false;
        }

        /// <inheritdoc />
        public bool TryGetOrAdd(string key, Func<string, CacheItem<TCacheValue>> valueFactory, out CacheItem<TCacheValue> item)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNull(valueFactory, nameof(valueFactory));

            return TryGetOrAddInternal(key, null, (k, r) => valueFactory(k), out item);
        }

        /// <inheritdoc />
        public bool TryGetOrAdd(string key, string region, Func<string, string, CacheItem<TCacheValue>> valueFactory, out CacheItem<TCacheValue> item)
        {
            NotNullOrWhiteSpace(key, nameof(key));
            NotNullOrWhiteSpace(region, nameof(region));
            NotNull(valueFactory, nameof(valueFactory));

            return TryGetOrAddInternal(key, region, valueFactory, out item);
        }

        private bool TryGetOrAddInternal(string key, string region, Func<string, string, CacheItem<TCacheValue>> valueFactory, out CacheItem<TCacheValue> item)
        {
            item = default(CacheItem<TCacheValue>);
            var tries = 0;
            do
            {
                tries++;
                item = GetCacheItemInternal(key, region);
                if (item != null)
                {
                    return true;
                }

                item = valueFactory(key, region);

                if (item == null)
                {
                    return false;
                }

                if (AddInternal(item))
                {
                    return true;
                }
            }
            while (tries <= Configuration.MaxRetries);

            return false;
        }

        private CacheItem<TCacheValue> GetOrAddInternal(string key, string region, Func<string, string, CacheItem<TCacheValue>> valueFactory)
        {
            var tries = 0;
            do
            {
                tries++;
                var item = GetCacheItemInternal(key, region);
                if (item != null)
                {
                    return item;
                }

                item = valueFactory(key, region);

                // Throw explicit to me more consistent. Otherwise it would throw later eventually...
                if (item == null)
                {
                    throw new InvalidOperationException("The CacheItem which should be added must not be null.");
                }

                if (AddInternal(item))
                {
                    return item;
                }
            }
            while (tries <= Configuration.MaxRetries);

            // should usually never occur, but could if e.g. max retries is 1 and an item gets added between the get and add.
            // pretty unusual, so keep the max tries at least around 50
            throw new InvalidOperationException(
                string.Format("Could not get nor add the item {0} {1}", key, region));
        }
    }
}