namespace CacheManager.Core
{
    /// <summary>
    /// Internally used entity which lets the cache manager know what happened during an update operation.
    /// </summary>
    /// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
    public class UpdateItemResult<TCacheValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateItemResult{TCacheValue}" /> class with the
        /// specified properties.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="conflictOccurred">Must be <c>True</c> if a conflict occurred.</param>
        /// <param name="success">Must be <c>True</c> only if the item got updated.</param>
        /// <param name="triesNeeded">Number of retries taken to update the item.</param>
        public UpdateItemResult(TCacheValue value, bool conflictOccurred, bool success, int triesNeeded)
        {
            this.VersionConflictOccurred = conflictOccurred;
            this.Success = success;
            this.NumberOfTriesNeeded = triesNeeded;
            this.Value = value;
        }

        /// <summary>
        /// Gets the number of tries the cache needed to update the item.
        /// </summary>
        /// <value>The number of retries needed.</value>
        public int NumberOfTriesNeeded { get; private set; }

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

        /// <summary>
        /// Gets the updated value.
        /// </summary>
        /// <value>
        /// The updated value.
        /// </value>
        public TCacheValue Value { get; private set; }
    }
}