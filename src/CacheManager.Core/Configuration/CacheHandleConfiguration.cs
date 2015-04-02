using System;

namespace CacheManager.Core.Configuration
{
    public sealed class CacheHandleConfiguration<TCacheValue> : ICacheHandleConfiguration
    {
        internal CacheHandleConfiguration(string cacheName, string handleName)
        {
            if (string.IsNullOrWhiteSpace(cacheName))
            {
                throw new ArgumentNullException("cacheName");
            }
            if (string.IsNullOrWhiteSpace(handleName))
            {
                throw new ArgumentNullException("handleName");
            }

            this.CacheName = cacheName;
            this.HandleName = handleName;
        }

        /// <summary>
        /// Gets the name of the cache the handle got assigned to.
        /// </summary>
        public string CacheName { get; private set; }

        public bool EnablePerformanceCounters { get; internal set; }

        public bool EnableStatistics { get; internal set; }

        public ExpirationMode ExpirationMode { get; internal set; }

        public TimeSpan ExpirationTimeout { get; internal set; }

        /// <summary>
        /// Gets the name for the cache handle which is also the identifier of the configuration.
        /// </summary>
        public string HandleName { get; private set; }

        public bool IsBackPlateSource { get; internal set; }

        internal Type HandleType { get; set; }

        internal static CacheHandleConfiguration<TCacheValue> Create<TCacheHandle>(string cacheName, string handleName) where TCacheHandle : ICacheHandle<TCacheValue>
        {
            return new CacheHandleConfiguration<TCacheValue>(cacheName, handleName)
            {
                HandleType = typeof(TCacheHandle)
            };
        }
    }
}