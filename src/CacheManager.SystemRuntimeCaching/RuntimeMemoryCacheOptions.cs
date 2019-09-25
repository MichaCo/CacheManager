using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CacheManager.SystemRuntimeCaching
{
    /// <summary>
    /// <see cref="System.Runtime.Caching.MemoryCache"/> configuration options
    /// </summary>
    public class RuntimeMemoryCacheOptions
    {
        /// <summary>
        /// An integer value that specifies the maximum allowable size, in megabytes, that an instance of a MemoryCache can grow to. The default value is 0, which means that the autosizing heuristics of the MemoryCache class are used by default.
        /// </summary>
        public int CacheMemoryLimitMegabytes { get; set; } = 0;

        /// <summary>
        /// An integer value between 0 and 100 that specifies the maximum percentage of physically installed computer memory that can be consumed by the cache. The default value is 0, which means that the autosizing heuristics of the MemoryCache class are used by default.
        /// </summary>
        public int PhysicalMemoryLimitPercentage { get; set; } = 0;

        /// <summary>
        /// A value that indicates the time interval after which the cache implementation compares the current memory load against the absolute and percentage-based memory limits that are set for the cache instance.
        /// </summary>
        public TimeSpan PollingInterval { get; set; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Gets the configuration as a <see cref="NameValueCollection"/>
        /// </summary>
        /// <returns>A <see cref="NameValueCollection"/> with the current configuration.</returns>
        public NameValueCollection AsNameValueCollection()
        {
            return new NameValueCollection(3)
            {
                { nameof(CacheMemoryLimitMegabytes), CacheMemoryLimitMegabytes.ToString(CultureInfo.InvariantCulture) },
                { nameof(PhysicalMemoryLimitPercentage), PhysicalMemoryLimitPercentage.ToString(CultureInfo.InvariantCulture) },
                { nameof(PollingInterval), PollingInterval.ToString("c") }
            };
        }
    }
}
