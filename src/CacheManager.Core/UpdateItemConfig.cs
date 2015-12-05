using System;

namespace CacheManager.Core
{
    /// <summary>
    /// Defines the options for handling version conflicts during update operations.
    /// </summary>
    /// <remarks>
    /// The value <c>Ignore</c> should not be used unless you are 100% sure what you are doing.
    /// </remarks>
    public enum VersionConflictHandling
    {
        /// <summary>
        /// Instructs the cache manager to remove the item on all other cache handles, if a version
        /// conflict occurs.
        /// </summary>
        EvictItemFromOtherCaches,

        /// <summary>
        /// Instructs the cache manager to update the other cache handles with the updated item, if
        /// a version conflict occurs.
        /// </summary>
        UpdateOtherCaches,

        /// <summary>
        /// Instructs the cache manager to ignore conflicts.
        /// </summary>
        Ignore
    }

    /// <summary>
    /// The object is used to specify the update operations of the cache manager.
    /// </summary>
    public class UpdateItemConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateItemConfig"/> class with default values.
        /// </summary>
        public UpdateItemConfig()
        {
            this.MaxRetries = int.MaxValue;
            this.VersionConflictOperation = VersionConflictHandling.EvictItemFromOtherCaches;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateItemConfig"/> class with default
        /// value for max retries.
        /// </summary>
        /// <param name="conflictHandling">The conflict handling which should be used.</param>
        public UpdateItemConfig(VersionConflictHandling conflictHandling)
            : this()
        {
            this.VersionConflictOperation = conflictHandling;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateItemConfig"/> class with default
        /// value for version conflict handling.
        /// </summary>
        /// <param name="maxRetries">
        /// The maximum number of retries the update operation should make.
        /// </param>
        public UpdateItemConfig(int maxRetries)
            : this()
        {
            if (maxRetries < 0)
            {
                throw new ArgumentException("maxRetries must be greater than or equal to 0.");
            }

            this.MaxRetries = maxRetries;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateItemConfig"/> class with the
        /// specified values.
        /// </summary>
        /// <param name="maxRetries">
        /// The maximum number of retries the update operation should make.
        /// </param>
        /// <param name="conflictHandling">The conflict handling which should be used.</param>
        public UpdateItemConfig(int maxRetries, VersionConflictHandling conflictHandling)
        {
            if (maxRetries < 0)
            {
                throw new ArgumentException("maxRetries must be greater than or equal to 0.");
            }

            this.MaxRetries = maxRetries;

            this.VersionConflictOperation = conflictHandling;
        }

        /// <summary>
        /// Gets the number of retries the update operation is allowed to make.
        /// <para>Default are <see cref="int.MaxValue"/></para>
        /// </summary>
        /// <value>The maximum retries.</value>
        public int MaxRetries { get; }

        /// <summary>
        /// Gets the <see cref="VersionConflictHandling"/> which drives the cache manager if a
        /// version conflict occurs.
        /// </summary>
        /// <value>The version conflict operation.</value>
        public VersionConflictHandling VersionConflictOperation { get; }
    }
}