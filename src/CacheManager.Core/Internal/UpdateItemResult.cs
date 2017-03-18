using System;

namespace CacheManager.Core.Internal
{
    /// <summary>
    /// Represents that state of an update operation.
    /// </summary>
    public enum UpdateItemResultState
    {
        /// <summary>
        /// The state represents a successful update operation.
        /// </summary>
        Success,

        /// <summary>
        /// The state represents a failed attempt. The retries limit had been reached.
        /// </summary>
        TooManyRetries,

        /// <summary>
        /// The state represents a failed attempt. The cache item did not exist, so no update could
        /// be made.
        /// </summary>
        ItemDidNotExist,

        /// <summary>
        /// Internal state representing a failure where the factory function returns null instead of a valid value.
        /// </summary>
        FactoryReturnedNull,
    }

    /// <summary>
    /// Helper class to create correct instances.
    /// </summary>
    public static class UpdateItemResult
    {
        /// <summary>
        /// Creates a new instance of the <see cref="UpdateItemResult{TCacheValue}"/> class with
        /// properties typical for the case where the cache item did not exist for an update operation.
        /// </summary>
        /// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
        /// <returns>The item result.</returns>
        public static UpdateItemResult<TCacheValue> ForItemDidNotExist<TCacheValue>() =>
            new UpdateItemResult<TCacheValue>(null, UpdateItemResultState.ItemDidNotExist, false, 1);

        /// <summary>
        /// Creates a new instance of the <see cref="UpdateItemResult{TCacheValue}"/> indicating that the
        /// cache value factory returned null instead of a valid value.
        /// </summary>
        /// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
        /// <returns>The item result.</returns>
        public static UpdateItemResult<TCacheValue> ForFactoryReturnedNull<TCacheValue>() =>
            new UpdateItemResult<TCacheValue>(null, UpdateItemResultState.FactoryReturnedNull, false, 1);

        /// <summary>
        /// Creates a new instance of the <see cref="UpdateItemResult{TCacheValue}"/> class with
        /// properties typical for a successful update operation.
        /// </summary>
        /// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="conflictOccurred">Set to <c>true</c> if a conflict occurred.</param>
        /// <param name="triesNeeded">The tries needed.</param>
        /// <returns>The item result.</returns>
        public static UpdateItemResult<TCacheValue> ForSuccess<TCacheValue>(CacheItem<TCacheValue> value, bool conflictOccurred = false, int triesNeeded = 1) =>
            new UpdateItemResult<TCacheValue>(value, UpdateItemResultState.Success, conflictOccurred, triesNeeded);

        /// <summary>
        /// Creates a new instance of the <see cref="UpdateItemResult{TCacheValue}"/> class with
        /// properties typical for an update operation which failed because it exceeded the limit of tries.
        /// </summary>
        /// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
        /// <param name="triesNeeded">The tries needed.</param>
        /// <returns>The item result.</returns>
        public static UpdateItemResult<TCacheValue> ForTooManyRetries<TCacheValue>(int triesNeeded) =>
            new UpdateItemResult<TCacheValue>(null, UpdateItemResultState.TooManyRetries, true, triesNeeded);
    }

    /// <summary>
    /// Used by cache handle implementations to let the cache manager know what happened during an
    /// update operation.
    /// </summary>
    /// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
    public class UpdateItemResult<TCacheValue>
    {
        internal UpdateItemResult(CacheItem<TCacheValue> value, UpdateItemResultState state, bool conflictOccurred, int triesNeeded)
        {
            if (triesNeeded == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(triesNeeded), "Value must be higher than 0.");
            }

            VersionConflictOccurred = conflictOccurred;
            UpdateState = state;
            NumberOfTriesNeeded = triesNeeded;
            Value = value;
        }

        /// <summary>
        /// Gets the number of tries the cache needed to update the item.
        /// </summary>
        /// <value>The number of retries needed.</value>
        public int NumberOfTriesNeeded { get; }

        /// <summary>
        /// Gets a value indicating whether the update operation was successful or not.
        /// </summary>
        /// <value>The current <see cref="UpdateItemResultState"/>.</value>
        public UpdateItemResultState UpdateState { get; }

        /// <summary>
        /// Gets the updated value.
        /// </summary>
        /// <value>The updated value.</value>
        public CacheItem<TCacheValue> Value { get; }

        /// <summary>
        /// Gets a value indicating whether a version conflict occurred during an update operation.
        /// </summary>
        /// <value><c>true</c> if a version conflict occurred; otherwise, <c>false</c>.</value>
        public bool VersionConflictOccurred { get; }
    }
}