namespace CacheManager.Core
{
    /// <summary>
    /// Internally used entity which lets the cache manager know what happened during an update operation.
    /// </summary>
    public class UpdateItemResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateItemResult"/> class with the
        /// specified properties.
        /// </summary>
        /// <param name="conflictOccurred">Must be <c>True</c> if a conflict occurred.</param>
        /// <param name="success">Must be <c>True</c> only if the item got updated.</param>
        /// <param name="retries">Number of retries taken to update the item.</param>
        public UpdateItemResult(bool conflictOccurred, bool success, int retries)
        {
            this.VersionConflictOccurred = conflictOccurred;
            this.Success = success;
            this.NumberOfRetriesNeeded = retries;
        }

        /// <summary>
        /// Gets the number of tries the cache needed to update the item.
        /// </summary>
        /// <value>The number of retries needed.</value>
        public int NumberOfRetriesNeeded { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the update operation was successful or not.
        /// </summary>
        /// <value><c>true</c> if success; otherwise, <c>false</c>.</value>
        public bool Success { get; private set; }

        /// <summary>
        /// Gets a value indicating whether a version conflict occurred during an update operation.
        /// </summary>
        /// <value><c>true</c> if a version conflict occurred; otherwise, <c>false</c>.</value>
        public bool VersionConflictOccurred { get; private set; }
    }
}