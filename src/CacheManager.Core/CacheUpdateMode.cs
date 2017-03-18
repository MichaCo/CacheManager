using System;

namespace CacheManager.Core
{
    /// <summary>
    /// Defines the possible update modes of the cache manager.
    /// <para>
    /// Update mode works on Get operations. If the cache manager finds the cache item in one of the
    /// cache handles, and other cache handles do not have that item, it might add the item to the
    /// other cache handles depending on the mode.
    /// </para>
    /// </summary>
    public enum CacheUpdateMode
    {
        /// <summary>
        /// <c>None</c> instructs the cache manager to not set a cache item to other cache handles
        /// at all.
        /// </summary>
        None,

        /////// <summary>
        /////// <c>Full</c> instructs the cache manager to add the cache item found to all cache
        /////// handles, except the one the item was found in.
        /////// </summary>
        ////[Obsolete("Will be removed in 1.0.0. I don't really see any actual value using this setting and it might actually cause issues.")]
        ////Full,

        /// <summary>
        /// <c>Up</c> instructs the cache manager to add the cache item found to cache handles which
        /// are 'above' the one the item was found in. The order of the cache handles is defined by
        /// the configuration (order they have been added). First cache handle added is the top most one.
        /// </summary>
        Up
    }
}