using System.Collections.Generic;
namespace CacheManager.Core.Configuration
{
    public interface ICacheManagerConfiguration
    {
        CacheUpdateMode CacheUpdateMode { get; }

        string Name { get; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Redis")]
        IList<RedisConfiguration> RedisConfigurations { get; }

        /// <summary>
        /// Gets or sets the limit of the number of retry operations per action.
        /// Default is <see cref="int.MaxValue"/>.
        /// </summary>
        int MaxRetries { get; set; }

        /// <summary>
        /// Gets or sets the number of milliseconds the cache should wait before it 
        /// will retry an action.
        /// </summary>
        int RetryTimeout { get; set; }
    }
}
