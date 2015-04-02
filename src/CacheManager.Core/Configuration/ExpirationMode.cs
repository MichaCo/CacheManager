namespace CacheManager.Core.Configuration
{
    /// <summary>
    /// Defines the supported expiration modes for cache items.
    /// <para>Value <c>None</c> will indicate that no expiration should be set.</para>
    /// </summary>
    public enum ExpirationMode
    {
        /// <summary>
        /// Defines no expiration.
        /// </summary>
        None,

        /// <summary>
        /// Defines sliding expiration. The expiration timeout will be refreshed on every access.
        /// </summary>
        Sliding,

        /// <summary>
        /// Defines absolute expiration. The item will expire after the expiration timeout.
        /// </summary>
        Absolute
    }
}