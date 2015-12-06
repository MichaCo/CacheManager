using System;
using System.Globalization;
using System.Text;
using static CacheManager.Core.Internal.BackPlateAction;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core.Internal
{
    /// <summary>
    /// Defines the possible actions of the back plate message.
    /// </summary>
    public enum BackPlateAction
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
    public sealed class BackPlateMessage
    {
        private BackPlateMessage(string owner, BackPlateAction action)
        {
            NotNullOrWhiteSpace(owner, nameof(owner));

            this.OwnerIdentity = owner;
            this.Action = action;
        }

        private BackPlateMessage(string owner, BackPlateAction action, string key)
            : this(owner, action)
        {
            NotNullOrWhiteSpace(key, nameof(key));

            this.Key = key;
        }

        private BackPlateMessage(string owner, BackPlateAction action, string key, string region)
            : this(owner, action, key)
        {
            NotNullOrWhiteSpace(region, nameof(region));

            this.Region = region;
        }

        /// <summary>
        /// Gets or sets the action.
        /// </summary>
        /// <value>The action.</value>
        public BackPlateAction Action { get; set; }

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
        /// The <see cref="BackPlateMessage" /> instance.
        /// </returns>
        /// <exception cref="System.ArgumentException">Parameter message cannot be null or empty.</exception>
        public static BackPlateMessage Deserialize(string message)
        {
            NotNullOrWhiteSpace(message, nameof(message));

            var tokens = message.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);

            var ident = tokens[0];
            var action = (BackPlateAction)int.Parse(tokens[1], CultureInfo.InvariantCulture);

            if (action == Clear)
            {
                return new BackPlateMessage(ident, Clear);
            }
            else if (action == ClearRegion)
            {
                return new BackPlateMessage(ident, ClearRegion) { Region = Decode(tokens[2]) };
            }
            else if (tokens.Length == 3)
            {
                return new BackPlateMessage(ident, action, Decode(tokens[2]));
            }

            return new BackPlateMessage(ident, action, Decode(tokens[2]), Decode(tokens[3]));
        }

        /// <summary>
        /// Creates a new <see cref="BackPlateMessage"/> for the changed action.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="key">The key.</param>
        /// <returns>The new <see cref="BackPlateMessage"/> instance.</returns>
        public static BackPlateMessage ForChanged(string owner, string key) =>
            new BackPlateMessage(owner, Changed, key);

        /// <summary>
        /// Creates a new <see cref="BackPlateMessage"/> for the changed action.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        /// <returns>The new <see cref="BackPlateMessage"/> instance.</returns>
        public static BackPlateMessage ForChanged(string owner, string key, string region) =>
            new BackPlateMessage(owner, Changed, key, region);

        /// <summary>
        /// Creates a new <see cref="BackPlateMessage"/> for the clear action.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <returns>The new <see cref="BackPlateMessage"/> instance.</returns>
        public static BackPlateMessage ForClear(string owner) =>
            new BackPlateMessage(owner, Clear);

        /// <summary>
        /// Creates a new <see cref="BackPlateMessage"/> for the clear region action.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="region">The region.</param>
        /// <returns>The new <see cref="BackPlateMessage"/> instance.</returns>
        /// <exception cref="System.ArgumentNullException">If region is null.</exception>
        public static BackPlateMessage ForClearRegion(string owner, string region)
        {
            NotNullOrWhiteSpace(region, nameof(region));

            return new BackPlateMessage(owner, ClearRegion)
            {
                Region = region
            };
        }

        /// <summary>
        /// Creates a new <see cref="BackPlateMessage"/> for the removed action.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="key">The key.</param>
        /// <returns>The new <see cref="BackPlateMessage"/> instance.</returns>
        public static BackPlateMessage ForRemoved(string owner, string key) =>
            new BackPlateMessage(owner, Removed, key);

        /// <summary>
        /// Creates a new <see cref="BackPlateMessage"/> for the removed action.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        /// <returns>The new <see cref="BackPlateMessage"/> instance.</returns>
        public static BackPlateMessage ForRemoved(string owner, string key, string region) =>
            new BackPlateMessage(owner, Removed, key, region);

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