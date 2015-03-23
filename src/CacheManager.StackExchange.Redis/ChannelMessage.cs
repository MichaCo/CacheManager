using System;
using System.Text;

namespace CacheManager.StackExchange.Redis
{
    internal enum ChannelAction
    {
        Removed,
        Changed,
        Clear,
        ClearRegion
    }

    internal class ChannelMessage
    {
        public ChannelMessage(string owner, ChannelAction action)
        {
            this.IdentityOwner = owner;
            this.Action = action;
        }

        public ChannelMessage(string owner, ChannelAction action, string key)
            : this(owner, action)
        {
            this.Key = key;
        }

        public ChannelMessage(string owner, ChannelAction action, string key, string region)
            : this(owner, action, key)
        {
            this.Region = region;
        }

        public static ChannelMessage FromMsg(string msg)
        {
            var tokens = msg.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);

            var ident = tokens[0];
            var action = (ChannelAction)Int32.Parse(tokens[1]);

            if (action == ChannelAction.Clear)
            {
                return new ChannelMessage(ident, ChannelAction.Clear);
            }
            else if (action == ChannelAction.ClearRegion)
            {
                return new ChannelMessage(ident, ChannelAction.ClearRegion) { Region = Decode(tokens[2]) };
            }
            else if (tokens.Length == 3)
            {
                return new ChannelMessage(ident, action, Decode(tokens[2]));
            }

            return new ChannelMessage(ident, action, Decode(tokens[2]), Decode(tokens[3]));
        }

        public string ToMsg()
        {
            var action = (int)this.Action;
            if (this.Action == ChannelAction.Clear)
            {
                return this.IdentityOwner + ":" + action;
            }
            else if (this.Action == ChannelAction.ClearRegion)
            {
                return this.IdentityOwner + ":" + action + ":" + Encode(this.Region);
            }
            else if (string.IsNullOrWhiteSpace(this.Region))
            {
                return this.IdentityOwner + ":" + action + ":" + Encode(this.Key);
            }

            return this.IdentityOwner + ":" + action + ":" + Encode(this.Key) + ":" + Encode(this.Region);
        }

        private static string Encode(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }

        private static string Decode(string value)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }

        public string IdentityOwner { get; set; }

        public ChannelAction Action { get; set; }

        public string Key { get; set; }

        public string Region { get; set; }
    }
}