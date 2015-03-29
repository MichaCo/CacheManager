using System.Collections.Generic;
namespace CacheManager.Core.Configuration
{
    public interface ICacheManagerConfiguration
    {
        CacheUpdateMode CacheUpdateMode { get; }

        /// <summary>
        /// Gets the name the cache will be using.
        /// <para>
        /// The name might be used by several components as an identifier or display name.
        /// </para>
        /// </summary>
        string Name { get; }

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
