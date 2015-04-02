using System;
using System.Text;

namespace CacheManager.Core.Cache
{
    public enum BackPlateAction
    {
        Removed,
        Changed,
        Clear,
        ClearRegion
    }

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

        public BackPlateAction Action { get; set; }

        public string Key { get; set; }

        public string OwnerIdentity { get; set; }

        public string Region { get; set; }

        public static BackPlateMessage Deserialize(string msg)
        {
            var tokens = msg.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);

            var ident = tokens[0];
            var action = (BackPlateAction)Int32.Parse(tokens[1]);

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

        public static BackPlateMessage ForChanged(string owner, string key)
        {
            return new BackPlateMessage(owner, BackPlateAction.Changed, key);
        }

        public static BackPlateMessage ForChanged(string owner, string key, string region)
        {
            return new BackPlateMessage(owner, BackPlateAction.Changed, key, region);
        }

        public static BackPlateMessage ForClear(string owner)
        {
            return new BackPlateMessage(owner, BackPlateAction.Clear);
        }

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

        public static BackPlateMessage ForRemoved(string owner, string key)
        {
            return new BackPlateMessage(owner, BackPlateAction.Removed, key);
        }

        public static BackPlateMessage ForRemoved(string owner, string key, string region)
        {
            return new BackPlateMessage(owner, BackPlateAction.Removed, key, region);
        }

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