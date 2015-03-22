using System;
namespace CacheManager.Core.Configuration
{
    public interface ICacheHandleConfiguration
    {
        string CacheName { get; }

        bool EnablePerformanceCounters { get; }

        bool EnableStatistics { get; }

        ExpirationMode ExpirationMode { get; }

        TimeSpan ExpirationTimeout { get; }

        string HandleName { get; }
    }
}
