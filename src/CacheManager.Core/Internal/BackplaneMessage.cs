using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static CacheManager.Core.Internal.BackplaneAction;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core.Internal
{
    /// <summary>
    /// Defines the possible actions of the backplane message.
    /// </summary>
    public enum BackplaneAction : byte
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
    public enum CacheItemChangedEventAction : byte
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
        private BackplaneMessage(byte[] owner, BackplaneAction action)
        {
            NotNull(owner, nameof(owner));

            OwnerIdentity = owner;
            Action = action;
        }

        private BackplaneMessage(byte[] owner, BackplaneAction action, string key)
            : this(owner, action)
        {
            NotNullOrWhiteSpace(key, nameof(key));

            Key = key;
        }

        private BackplaneMessage(byte[] owner, BackplaneAction action, string key, string region)
            : this(owner, action, key)
        {
            NotNullOrWhiteSpace(region, nameof(region));

            Region = region;
        }

        private BackplaneMessage(byte[] owner, BackplaneAction action, string key, CacheItemChangedEventAction changeAction)
            : this(owner, action, key)
        {
            ChangeAction = changeAction;
        }

        private BackplaneMessage(byte[] owner, BackplaneAction action, string key, string region, CacheItemChangedEventAction changeAction)
            : this(owner, action, key, region)
        {
            ChangeAction = changeAction;
        }

        /// <summary>
        /// Gets or sets the action.
        /// </summary>
        /// <value>The action.</value>
        public BackplaneAction Action { get; }

        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>The key.</value>
        public string Key { get; }

        /// <summary>
        /// Gets or sets the owner identity.
        /// </summary>
        /// <value>The owner identity.</value>
        public byte[] OwnerIdentity { get; }

        /// <summary>
        /// Gets or sets the region.
        /// </summary>
        /// <value>The region.</value>
        public string Region { get; private set; }

        /// <summary>
        /// Gets or sets the cache action.
        /// </summary>
        public CacheItemChangedEventAction ChangeAction { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            switch (Action)
            {
                case Changed:
                    return $"{Action} {Region}:{Key} {ChangeAction}";

                case Removed:
                    return $"{Action} {Region}:{Key}";

                case ClearRegion:
                    return $"{Action} {Region}";

                case Clear:
                    return $"{Action}";
            }

            return string.Empty;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (object.ReferenceEquals(obj, this))
            {
                return true;
            }

            var objCast = obj as BackplaneMessage;
            if (objCast == null)
            {
                return false;
            }

            return Action == objCast.Action
                && Key == objCast.Key
                && ChangeAction == objCast.ChangeAction
                && Region == objCast.Region;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;

                hash = hash * 23 + Action.GetHashCode();
                hash = hash * 23 + ChangeAction.GetHashCode();
                hash = hash * 23 + (Region?.GetHashCode() ?? 17);
                hash = hash * 23 + (Key?.GetHashCode() ?? 17);
                return hash;
            }
        }

        /// <summary>
        /// Creates a new <see cref="BackplaneMessage"/> for the changed action.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="key">The key.</param>
        /// <param name="changeAction">The cache change action.</param>
        /// <returns>The new <see cref="BackplaneMessage"/> instance.</returns>
        public static BackplaneMessage ForChanged(byte[] owner, string key, CacheItemChangedEventAction changeAction) =>
            new BackplaneMessage(owner, Changed, key, changeAction);

        /// <summary>
        /// Creates a new <see cref="BackplaneMessage"/> for the changed action.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        /// <param name="changeAction">The cache change action.</param>
        /// <returns>The new <see cref="BackplaneMessage"/> instance.</returns>
        public static BackplaneMessage ForChanged(byte[] owner, string key, string region, CacheItemChangedEventAction changeAction) =>
            new BackplaneMessage(owner, Changed, key, region, changeAction);

        /// <summary>
        /// Creates a new <see cref="BackplaneMessage"/> for the clear action.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <returns>The new <see cref="BackplaneMessage"/> instance.</returns>
        public static BackplaneMessage ForClear(byte[] owner) =>
            new BackplaneMessage(owner, Clear);

        /// <summary>
        /// Creates a new <see cref="BackplaneMessage"/> for the clear region action.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="region">The region.</param>
        /// <returns>The new <see cref="BackplaneMessage"/> instance.</returns>
        /// <exception cref="System.ArgumentNullException">If region is null.</exception>
        public static BackplaneMessage ForClearRegion(byte[] owner, string region)
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
        public static BackplaneMessage ForRemoved(byte[] owner, string key) =>
            new BackplaneMessage(owner, Removed, key);

        /// <summary>
        /// Creates a new <see cref="BackplaneMessage"/> for the removed action.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="key">The key.</param>
        /// <param name="region">The region.</param>
        /// <returns>The new <see cref="BackplaneMessage"/> instance.</returns>
        public static BackplaneMessage ForRemoved(byte[] owner, string key, string region) =>
            new BackplaneMessage(owner, Removed, key, region);

        /// <summary>
        /// Serializes this instance.
        /// </summary>
        /// <returns>The string representing this message.</returns>
        public static byte[] Serialize(params BackplaneMessage[] messages)
        {
            NotNullOrEmpty(messages, nameof(messages));

            // calc size
            var size = 0;
            for (var i = 0; i < messages.Length; i++)
            {
                size += MessageWriter.GetEstimatedSize(messages[i], i != 0);
            }

            var writer = new MessageWriter(size);

            for (var i = 0; i < messages.Length; i++)
            {
                SerializeMessage(writer, messages[i], i != 0);
            }

            return writer.GetBytes();
        }

        private static void SerializeMessage(MessageWriter writer, BackplaneMessage message, bool skipOwner)
        {
            if (!skipOwner)
            {
                writer.WriteInt(message.OwnerIdentity.Length);
                writer.WriteBytes(message.OwnerIdentity);
            }

            writer.WriteByte((byte)message.Action);
            switch (message.Action)
            {
                case Changed:
                    writer.WriteByte((byte)message.ChangeAction);
                    if (!string.IsNullOrEmpty(message.Region))
                    {
                        writer.WriteByte(2);
                        writer.WriteString(message.Region);
                    }
                    else
                    {
                        writer.WriteByte(1);
                    }
                    writer.WriteString(message.Key);

                    break;

                case Removed:
                    if (!string.IsNullOrEmpty(message.Region))
                    {
                        writer.WriteByte(2);
                        writer.WriteString(message.Region);
                    }
                    else
                    {
                        writer.WriteByte(1);
                    }
                    writer.WriteString(message.Key);

                    break;

                case ClearRegion:
                    writer.WriteString(message.Region);
                    break;

                case Clear:
                    break;
            }
        }

        /// <summary>
        /// Deserializes the <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="skipOwner">If specified, if the first received message has the same owner, all messages will be skipped.</param>
        /// <returns>
        /// The new <see cref="BackplaneMessage" /> instance.
        /// </returns>
        /// <exception cref="System.ArgumentException">If <paramref name="message"/> is null.</exception>
        /// <exception cref="System.ArgumentException">If the message is not valid.</exception>
        public static IEnumerable<BackplaneMessage> Deserialize(byte[] message, byte[] skipOwner = null)
        {
            NotNull(message, nameof(message));
            if (message.Length < 5)
            {
                throw new ArgumentException("Invalid message");
            }
            var reader = new MessageReader(message);

            var first = DeserializeMessage(reader, null);

            if (skipOwner != null)
            {
                if (first.OwnerIdentity.SequenceEqual(skipOwner))
                {
                    yield break;
                }
            }

            yield return first;

            while (reader.HasMore())
            {
                yield return DeserializeMessage(reader, first.OwnerIdentity);
            }
        }

        private static BackplaneMessage DeserializeMessage(MessageReader reader, byte[] existingOwner)
        {
            var owner = existingOwner ?? reader.ReadBytes(reader.ReadInt());
            var action = (BackplaneAction)reader.ReadByte();

            switch (action)
            {
                case Changed:
                    var changeAction = (CacheItemChangedEventAction)reader.ReadByte();
                    if (reader.ReadByte() == 2)
                    {
                        var r = reader.ReadString();
                        return ForChanged(owner, reader.ReadString(), r, changeAction);
                    }

                    return ForChanged(owner, reader.ReadString(), changeAction);

                case Removed:
                    if (reader.ReadByte() == 2)
                    {
                        var r = reader.ReadString();
                        return ForRemoved(owner, reader.ReadString(), r);
                    }

                    return ForRemoved(owner, reader.ReadString());

                case ClearRegion:
                    return ForClearRegion(owner, reader.ReadString());

                case Clear:
                    return ForClear(owner);

                default:
                    throw new ArgumentException("Invalid message type");
            }
        }

        private class MessageWriter
        {
            private static Encoding _encoding = Encoding.UTF8;
            private readonly byte[] _buffer;
            private int _position = 0;

            public MessageWriter(int size)
            {
                _buffer = new byte[size + 4];
                _position = 4;

                // header v2
                _buffer[0] = 0;
                _buffer[1] = 118;
                _buffer[2] = 50;
                _buffer[3] = 0;
            }

            public byte[] GetBytes()
            {
                var result = new byte[_position];
                Buffer.BlockCopy(_buffer, 0, result, 0, _position);
                return result;
            }

            public void WriteInt(int number)
            {
                var bytes = BitConverter.GetBytes(number);
                WriteBytes(bytes);
            }

            public void WriteString(string value)
            {
                var len = _encoding.GetByteCount(value);
                WriteInt(len);

                _encoding.GetBytes(value, 0, value.Length, _buffer, _position);
                _position += len;
            }

            public void WriteBytes(byte[] bytes)
            {
                Buffer.BlockCopy(bytes, 0, _buffer, _position, bytes.Length);
                _position += bytes.Length;
            }

            public void WriteByte(byte b)
            {
                _buffer[_position] = b;
                _position++;
            }

            public static int GetEstimatedSize(BackplaneMessage msg, bool skipOwner)
            {
                // this is only a rough size multiplied by two for getting a roughly sized buffer
                int size = 2; // two enums
                if (!skipOwner)
                {
                    size += msg.OwnerIdentity.Length * 4;
                }

                size += msg.Key?.Length * 4 ?? 0;
                size += msg.Region?.Length * 4 ?? 0;
                return size * 2;
            }
        }

        private class MessageReader
        {
            private static Encoding _encoding = Encoding.UTF8;
            private readonly byte[] _data;
            private int _position = 0;

            public MessageReader(byte[] bytes)
            {
                _data = bytes;
                _position = 4;

                // check v2 header
                if (_data.Length < 4
                 || _data[0] != 0 || _data[1] != 118 || _data[2] != 50 || _data[3] != 0)
                {
                    throw new InvalidOperationException("Invalid v2 backplane message");
                }
            }

            public bool HasMore()
            {
                return _data.Length > _position;
            }

            public int ReadInt()
            {
                var pos = (_position += 4);
                if (pos > _data.Length)
                {
                    throw new IndexOutOfRangeException("Cannot read INT32, no additional bytes available.");
                }

                return BitConverter.ToInt32(_data, pos - 4);
            }

            public byte ReadByte()
            {
                if (_position >= _data.Length)
                {
                    throw new IndexOutOfRangeException("Cannot read byte, no additional bytes available.");
                }

                return _data[_position++];
            }

            public byte[] ReadBytes(int length)
            {
                var pos = (_position += length);
                if (pos > _data.Length)
                {
                    throw new IndexOutOfRangeException("Cannot read bytes, no additional bytes available.");
                }

                // fix: length check before aloc
                var result = new byte[length];
                Buffer.BlockCopy(_data, pos - length, result, 0, length);
                return result;
            }

            public string ReadString()
            {
                var len = ReadInt();
                if (len <= 0)
                {
                    throw new IndexOutOfRangeException("Invalid length for string");
                }

                var pos = (_position += len);
                if (pos > _data.Length)
                {
                    throw new IndexOutOfRangeException("Cannot read string, no additional bytes available.");
                }

                return _encoding.GetString(_data, pos - len, len);
            }
        }
    }
}