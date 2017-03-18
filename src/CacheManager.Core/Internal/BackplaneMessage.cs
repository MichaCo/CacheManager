using System;
using System.Globalization;
using System.Text;
using static CacheManager.Core.Internal.BackplaneAction;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core.Internal
{
    /// <summary>
    /// Defines the possible actions of the backplane message.
    /// </summary>
    public enum BackplaneAction
    {
        /// <summary>
        /// Default value is invalid to ensure we are not getting wrong results.
        /// </summary>
        Invalid = 0,

        /// <summary>
        /// The changed action.
        /// <see cref="CacheItemChangedEventAction"/>
        /// </summary>
        Changed,

        /// <summary>
        /// The clear action.
        /// </summary>
        Clear,

        /// <summary>
        /// The clear region action.
        /// </summary>
        ClearRegion,

        /// <summary>
        /// If the cache item has been removed.
        /// </summary>
        Removed
    }

    /// <summary>
    /// The enum defines the actual operation used to change the value in the cache.
    /// </summary>
    public enum CacheItemChangedEventAction
    {
        /// <summary>
        /// Default value is invalid to ensure we are not getting wrong results.
        /// </summary>
        Invalid = 0,

        /// <summary>
        /// If Put was used to change the value.
        /// </summary>
        Put,

        /// <summary>
        /// If Add was used to change the value.
        /// </summary>
        Add,

        /// <summary>
        /// If Update was used to change the value.
        /// </summary>
        Update
    }

    /// <summary>
    /// Implements a simple message which can be send as a string to the server.
    /// </summary>
    public sealed class BackplaneMessage
    {
        private BackplaneMessage(string owner, BackplaneAction action)
        {
            NotNullOrWhiteSpace(owner, nameof(owner));

            OwnerIdentity = owner;
            Action = action;
        }

        private BackplaneMessage(string owner, BackplaneAction action, string key)
            : this(owner, action)
        {
            NotNullOrWhiteSpace(key, nameof(key));

            Key = key;
        }

        private BackplaneMessage(string owner, BackplaneAction action, string key, string region)
            : this(owner, action, key)
        {
            NotNullOrWhiteSpace(region, nameof(region));

            Region = region;
        }

        private BackplaneMessage(string owner, BackplaneAction action, string key, CacheItemChangedEventAction changeAction)
            : this(owner, action, key)
        {
            ChangeAction = changeAction;
        }

        private BackplaneMessage(string owner, BackplaneAction action, string key, string region, CacheItemChangedEventAction changeAction)
            : this(owner, action, key, region)
        {
            ChangeAction = changeAction;
        }

        /// <summary>
        /// Gets or sets the action.
        /// </summary>
        /// <value>The action.</value>
        public BackplaneAction Action { get; set; }

        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>The key.</value>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the owner identity.
        /// </summary>
        /// <value>The owner identity.</value>
        public string OwnerIdentity { get; set; }

        /// <summary>
        /// Gets or sets the region.
        /// </summary>
        /// <value>The region.</value>
        public string Region { get; set; }

        /// <summary>
        /// Gets or sets the cache action.
        /// </summary>
        public CacheItemChangedEventAction ChangeAction { get; set; }

        /// <summary>
        /// Deserializes the <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>
        /// The new <see cref="BackplaneMessage" /> instance.
        /// </returns>
        /// <exception cref="System.ArgumentException">If <paramref name="message"/> is null.</exception>
        /// <exception cref="System.InvalidOperationException">If the message is not valid.</exception>
        public static BackplaneMessage Deserialize(string message)
        {
            NotNullOrWhiteSpace(message, nameof(message));

            var tokens = message.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length < 2)
            {
                throw new InvalidOperationException("Received an invalid message.");
            }

            var ident = tokens[0];
            var action = (BackplaneAction)int.Parse(tokens[1], CultureInfo.InvariantCulture);

            if (action == Clear)
            {
                return new BackplaneMessage(ident, Clear);
            }
            else if (action == ClearRegion)
            {
                return new BackplaneMessage(ident, ClearRegion) { Region = Decode(tokens[2]) };
            }
            else if (action == Removed)
            {
                if (tokens.Length < 3)
                {
                    throw new InvalidOperationException("Remove message does not contain valid data.");
                }

                if (tokens.Length == 3)
                {
                    return new BackplaneMessage(ident, Removed, Decode(tokens[2]));
                }

                return new BackplaneMessage(ident, Removed, Decode(tokens[2]), Decode(tokens[3]));
            }

            if (tokens.Length < 4)
            {
                throw new InvalidOperationException("Change message does not contain valid data.");
            }

            var cacheActionVal = tokens[2];
            var changeAction = CacheItemChangedEventAction.Invalid;
            if (cacheActionVal.Equals("Put", StringComparison.OrdinalIgnoreCase))
            {
                changeAction = CacheItemChangedEventAction.Put;
            }

            if (cacheActionVal.Equals("Add", StringComparison.OrdinalIgnoreCase))
            {
                changeAction = CacheItemChangedEventAction.Add;
            }

            if (cacheActionVal.Equals("Update", StringComparison.OrdinalIgnoreCase))
            {
                changeAction = CacheItemChangedEventAction.Update;
            }

            if (changeAction == CacheItemChangedEventAction.Invalid)
            {
                throw new InvalidOperationException("Received message with invalid change action.");
            }

            if (tokens.Length == 4)
            {
                return new BackplaneMessage(ident, action, Decode(tokens[3]), changeAction);
            }

            return new BackplaneMessage(ident, action, Decode(tokens[3]), Decode(tokens[4]), changeAction);
        }

        /// <summary>
        /// Creates a new <see cref="BackplaneMessage"/> for the changed action.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="key">The key.</param>
        /// <param name="changeAction">The cache change action.</param>
        /// <returns>The new <see cref="BackplaneMessage"/> instance.</returns>
        public static BackplaneMessage ForChanged(string owner, string key, CacheItemChangedEventAction changeAction) =>
            new BackplaneMessage(owner, Changed, key, changeAction);

        /// <summary>
        /// Creates a new <see cref="BackplaneMessage"/> for the changed action.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        /// <param name="changeAction">The cache change action.</param>
        /// <returns>The new <see cref="BackplaneMessage"/> instance.</returns>
        public static BackplaneMessage ForChanged(string owner, string key, string region, CacheItemChangedEventAction changeAction) =>
            new BackplaneMessage(owner, Changed, key, region, changeAction);

        /// <summary>
        /// Creates a new <see cref="BackplaneMessage"/> for the clear action.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <returns>The new <see cref="BackplaneMessage"/> instance.</returns>
        public static BackplaneMessage ForClear(string owner) =>
            new BackplaneMessage(owner, Clear);

        /// <summary>
        /// Creates a new <see cref="BackplaneMessage"/> for the clear region action.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="region">The region.</param>
        /// <returns>The new <see cref="BackplaneMessage"/> instance.</returns>
        /// <exception cref="System.ArgumentNullException">If region is null.</exception>
        public static BackplaneMessage ForClearRegion(string owner, string region)
        {
            NotNullOrWhiteSpace(region, nameof(region));

            return new BackplaneMessage(owner, ClearRegion)
            {
                Region = region
            };
        }

        /// <summary>
        /// Creates a new <see cref="BackplaneMessage"/> for the removed action.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="key">The key.</param>
        /// <returns>The new <see cref="BackplaneMessage"/> instance.</returns>
        public static BackplaneMessage ForRemoved(string owner, string key) =>
            new BackplaneMessage(owner, Removed, key);

        /// <summary>
        /// Creates a new <see cref="BackplaneMessage"/> for the removed action.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        /// <returns>The new <see cref="BackplaneMessage"/> instance.</returns>
        public static BackplaneMessage ForRemoved(string owner, string key, string region) =>
            new BackplaneMessage(owner, Removed, key, region);

        /// <summary>
        /// Serializes this instance.
        /// </summary>
        /// <returns>The string representing this message.</returns>
        public string Serialize()
        {
            var action = (int)Action;
            if (Action == Clear)
            {
                return OwnerIdentity + ":" + action;
            }
            else if (Action == ClearRegion)
            {
                return OwnerIdentity + ":" + action + ":" + Encode(Region);
            }
            else if (Action == Removed)
            {
                if (string.IsNullOrWhiteSpace(Region))
                {
                    return OwnerIdentity + ":" + action + ":" + ":" + Encode(Key);
                }

                return OwnerIdentity + ":" + action + ":" + Encode(Key) + ":" + Encode(Region);
            }
            else if (string.IsNullOrWhiteSpace(Region))
            {
                return OwnerIdentity + ":" + action + ":" + ChangeAction + ":" + Encode(Key);
            }

            return OwnerIdentity + ":" + action + ":" + ChangeAction + ":" + Encode(Key) + ":" + Encode(Region);
        }

        private static string Decode(string value)
        {
            var bytes = Convert.FromBase64String(value);
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        private static string Encode(string value) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }
}