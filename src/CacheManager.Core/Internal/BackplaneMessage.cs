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
        /// The remove action.
        /// </summary>
        Removed,

        /// <summary>
        /// The changed action.
        /// </summary>
        Changed,

        /// <summary>
        /// The clear action.
        /// </summary>
        Clear,

        /// <summary>
        /// The clear region action.
        /// </summary>
        ClearRegion
    }

    /// <summary>
    /// Implements a simple message which can be send as a string to the server.
    /// </summary>
    public sealed class BackplaneMessage
    {
        private BackplaneMessage(string owner, BackplaneAction action)
        {
            NotNullOrWhiteSpace(owner, nameof(owner));

            this.OwnerIdentity = owner;
            this.Action = action;
        }

        private BackplaneMessage(string owner, BackplaneAction action, string key)
            : this(owner, action)
        {
            NotNullOrWhiteSpace(key, nameof(key));

            this.Key = key;
        }

        private BackplaneMessage(string owner, BackplaneAction action, string key, string region)
            : this(owner, action, key)
        {
            NotNullOrWhiteSpace(region, nameof(region));

            this.Region = region;
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
        /// Deserializes the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>
        /// The <see cref="BackplaneMessage" /> instance.
        /// </returns>
        /// <exception cref="System.ArgumentException">Parameter message cannot be null or empty.</exception>
        public static BackplaneMessage Deserialize(string message)
        {
            NotNullOrWhiteSpace(message, nameof(message));

            var tokens = message.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);

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
            else if (tokens.Length == 3)
            {
                return new BackplaneMessage(ident, action, Decode(tokens[2]));
            }

            return new BackplaneMessage(ident, action, Decode(tokens[2]), Decode(tokens[3]));
        }

        /// <summary>
        /// Creates a new <see cref="BackplaneMessage"/> for the changed action.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="key">The key.</param>
        /// <returns>The new <see cref="BackplaneMessage"/> instance.</returns>
        public static BackplaneMessage ForChanged(string owner, string key) =>
            new BackplaneMessage(owner, Changed, key);

        /// <summary>
        /// Creates a new <see cref="BackplaneMessage"/> for the changed action.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        /// <returns>The new <see cref="BackplaneMessage"/> instance.</returns>
        public static BackplaneMessage ForChanged(string owner, string key, string region) =>
            new BackplaneMessage(owner, Changed, key, region);

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
            var action = (int)this.Action;
            if (this.Action == Clear)
            {
                return this.OwnerIdentity + ":" + action;
            }
            else if (this.Action == ClearRegion)
            {
                return this.OwnerIdentity + ":" + action + ":" + Encode(this.Region);
            }
            else if (string.IsNullOrWhiteSpace(this.Region))
            {
                return this.OwnerIdentity + ":" + action + ":" + Encode(this.Key);
            }

            return this.OwnerIdentity + ":" + action + ":" + Encode(this.Key) + ":" + Encode(this.Region);
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