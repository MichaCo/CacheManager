using System;
using System.Globalization;
using System.Text;

namespace CacheManager.Core.Cache
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
            if (string.IsNullOrWhiteSpace(owner))
            {
                throw new ArgumentNullException("owner");
            }

            this.OwnerIdentity = owner;
            this.Action = action;
        }

        private BackPlateMessage(string owner, BackPlateAction action, string key)
            : this(owner, action)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }

            this.Key = key;
        }

        private BackPlateMessage(string owner, BackPlateAction action, string key, string region)
            : this(owner, action, key)
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                throw new ArgumentNullException("region");
            }

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
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Parameter message cannot be null or empty.");
            }

            var tokens = message.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);

            var ident = tokens[0];
            var action = (BackPlateAction)int.Parse(tokens[1], CultureInfo.InvariantCulture);

            if (action == BackPlateAction.Clear)
            {
                return new BackPlateMessage(ident, BackPlateAction.Clear);
            }
            else if (action == BackPlateAction.ClearRegion)
            {
                return new BackPlateMessage(ident, BackPlateAction.ClearRegion) { Region = Decode(tokens[2]) };
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
        public static BackPlateMessage ForChanged(string owner, string key)
        {
            return new BackPlateMessage(owner, BackPlateAction.Changed, key);
        }

        /// <summary>
        /// Creates a new <see cref="BackPlateMessage"/> for the changed action.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        /// <returns>The new <see cref="BackPlateMessage"/> instance.</returns>
        public static BackPlateMessage ForChanged(string owner, string key, string region)
        {
            return new BackPlateMessage(owner, BackPlateAction.Changed, key, region);
        }

        /// <summary>
        /// Creates a new <see cref="BackPlateMessage"/> for the clear action.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <returns>The new <see cref="BackPlateMessage"/> instance.</returns>
        public static BackPlateMessage ForClear(string owner)
        {
            return new BackPlateMessage(owner, BackPlateAction.Clear);
        }

        /// <summary>
        /// Creates a new <see cref="BackPlateMessage"/> for the clear region action.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="region">The region.</param>
        /// <returns>The new <see cref="BackPlateMessage"/> instance.</returns>
        /// <exception cref="System.ArgumentNullException">If region is null.</exception>
        public static BackPlateMessage ForClearRegion(string owner, string region)
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                throw new ArgumentNullException("region");
            }

            return new BackPlateMessage(owner, BackPlateAction.ClearRegion)
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
        public static BackPlateMessage ForRemoved(string owner, string key)
        {
            return new BackPlateMessage(owner, BackPlateAction.Removed, key);
        }

        /// <summary>
        /// Creates a new <see cref="BackPlateMessage"/> for the removed action.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        /// <returns>The new <see cref="BackPlateMessage"/> instance.</returns>
        public static BackPlateMessage ForRemoved(string owner, string key, string region)
        {
            return new BackPlateMessage(owner, BackPlateAction.Removed, key, region);
        }

        /// <summary>
        /// Serializes this instance.
        /// </summary>
        /// <returns>The string representing this message.</returns>
        public string Serialize()
        {
            var action = (int)this.Action;
            if (this.Action == BackPlateAction.Clear)
            {
                return this.OwnerIdentity + ":" + action;
            }
            else if (this.Action == BackPlateAction.ClearRegion)
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
            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }

        private static string Encode(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }
    }
}