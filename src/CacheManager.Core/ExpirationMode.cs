namespace CacheManager.Core
{
    /// <summary>
    /// Defines the supported expiration modes for cache items.
    /// <para>Value <c>None</c> will indicate that no expiration should be set.</para>
    /// </summary>
    public enum ExpirationMode
    {
        /// <summary>
        /// Default value for the expircation mode enum.
        /// CacheManager will default to <c>None</c>. The <code>Default</code> entry in the enum is used as separation from the other values
        /// and to make it possible to explicitly set the expiration to <c>None</c>.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Defines no expiration.
        /// </summary>
        None = 1,

        /// <summary>
        /// Defines sliding expiration. The expiration timeout will be refreshed on every access.
        /// </summary>
        Sliding = 2,

        /// <summary>
        /// Defines absolute expiration. The item will expire after the expiration timeout.
        /// </summary>
        Absolute = 3
    }
}