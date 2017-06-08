using System.Collections.Generic;
using System.Linq;
using CacheManager.Core.Logging;

namespace CacheManager.Core
{
    public sealed partial class BaseCacheManager<TCacheValue>
    {
        /// <inheritdoc />
        override public IEnumerable<string> Keys(string pattern, string region)
        {
            CheckDisposed();

            var keys = new HashSet<string>();

            if (_logTrace)
            {
                Logger.LogTrace("Keys [{0}:{1}] started.", region, pattern);
            }

            for (var handleIndex = 0; handleIndex < _cacheHandles.Length; handleIndex++)
            {
                var handle = _cacheHandles[handleIndex];
                var handleKeys = handle.Keys(pattern ?? "*", region);
                keys.UnionWith(handleKeys);
            }

            if (_logTrace)
            {
                Logger.LogTrace("Keys [{0}:{1}] found {2} keys.", region, pattern, keys.Count);
            }

            if (region != null)
            {
                var after = region.Length + 1; // +1 for the :
                return keys.Select(k => k.Substring(after));
            }

            return keys;
        }
    }
}