using System;
using System.Collections.Generic;
using System.Linq;
using CacheManager.Core.Internal;
using CacheManager.Core.Logging;

namespace CacheManager.Core
{
    public sealed partial class BaseCacheManager<TCacheValue>
    {
        /// <inheritdoc />
        public IEnumerable<string> FindKeys(string pattern)
        {
            CheckDisposed();

            if (_logTrace)
            {
                Logger.LogTrace("FindKeys [{0}] started.", pattern);
            }

            var keys = KeySupplier().FindKeys(pattern);

            if (_logTrace)
            {
                Logger.LogTrace("FindKeys [{0}] completed. [{1}} found.", pattern, keys.Count());
            }

            return keys;
        }

        /// <inheritdoc />
        public IEnumerable<string> FindKeys(string pattern, string region)
        {
            CheckDisposed();

            if (_logTrace)
            {
                Logger.LogTrace("FindKeys [{0}:{1}] started.", region, pattern);
            }

            var keys = KeySupplier().FindKeys(pattern, region);

            if (_logTrace)
            {
                Logger.LogTrace("FindKeys [{0}:{1}] found {2} keys.", region, pattern, keys.Count());
            }

            return keys;
        }

        /// <inheritdoc />
        public IEnumerable<string> GetAllKeys()
        {
            CheckDisposed();

            if (_logTrace)
            {
                Logger.LogTrace("GetAllKeys started.");
            }

            var keys = KeySupplier().GetAllKeys();

            if (_logTrace)
            {
                Logger.LogTrace("GetAllKeys completed. found [{0}]", keys.Count());
            }

            return keys;

        }

        private BaseCacheHandle<TCacheValue> KeySupplier()
        {
            var handle =_cacheHandles.LastOrDefault(h => h.ImplementsKeys);
            if (handle == null)
                throw new InvalidOperationException("No configured implementation supports keys");
            return handle;
        }
    }
}