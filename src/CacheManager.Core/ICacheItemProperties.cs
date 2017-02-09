using System;

namespace CacheManager.Core
{
    public interface ICacheItemProperties
    {
        DateTime CreatedUtc { get; }

        ExpirationMode ExpirationMode { get; }

        TimeSpan ExpirationTimeout { get; }

        bool IsExpired { get; }

        string Key { get; }

        DateTime LastAccessedUtc { get; set; }

        string Region { get; }

        bool UsesExpirationDefaults { get; }

        Type ValueType { get; }
    }
}