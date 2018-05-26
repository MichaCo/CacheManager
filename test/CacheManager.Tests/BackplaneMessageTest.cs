using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CacheManager.Core.Internal;
using FluentAssertions;
using Xunit;

namespace CacheManager.Tests
{
    [ExcludeFromCodeCoverage]
    public class BackplaneMessageTest
    {
        [Fact]
        public void BackplaneMessage_ChangeAddKey()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };
            var key = Guid.NewGuid().ToString();
            var msg = BackplaneMessage.ForChanged(owner, key, CacheItemChangedEventAction.Add);

            // act
            var serialized = BackplaneMessage.Serialize(msg);
            var deserialized = BackplaneMessage.Deserialize(serialized).First();

            // assert
            deserialized.Key.Should().Be(key);
            deserialized.Region.Should().Be(null);
            deserialized.ChangeAction.Should().Be(CacheItemChangedEventAction.Add);
            deserialized.Action.Should().Be(BackplaneAction.Changed);
            deserialized.OwnerIdentity.Should().BeEquivalentTo(owner);
            deserialized.Should().BeEquivalentTo(msg);
        }

        [Fact]
        public void BackplaneMessage_ChangeAddKeyRegion()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };
            var key = Guid.NewGuid().ToString();
            var region = Guid.NewGuid().ToString();
            var msg = BackplaneMessage.ForChanged(owner, key, region, CacheItemChangedEventAction.Add);

            // act
            var serialized = BackplaneMessage.Serialize(msg);
            var deserialized = BackplaneMessage.Deserialize(serialized).First();

            // assert
            deserialized.Key.Should().Be(key);
            deserialized.Region.Should().Be(region);
            deserialized.ChangeAction.Should().Be(CacheItemChangedEventAction.Add);
            deserialized.Action.Should().Be(BackplaneAction.Changed);
            deserialized.OwnerIdentity.Should().BeEquivalentTo(owner);
            deserialized.Should().BeEquivalentTo(msg);
        }

        [Fact]
        public void BackplaneMessage_ChangePutKey()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };
            var key = Guid.NewGuid().ToString();
            var msg = BackplaneMessage.ForChanged(owner, key, CacheItemChangedEventAction.Put);

            // act
            var serialized = BackplaneMessage.Serialize(msg);
            var deserialized = BackplaneMessage.Deserialize(serialized).First();

            // assert
            deserialized.Key.Should().Be(key);
            deserialized.Region.Should().Be(null);
            deserialized.ChangeAction.Should().Be(CacheItemChangedEventAction.Put);
            deserialized.Action.Should().Be(BackplaneAction.Changed);
            deserialized.OwnerIdentity.Should().BeEquivalentTo(owner);
            deserialized.Should().BeEquivalentTo(msg);
        }

        [Fact]
        public void BackplaneMessage_ChangePutKeyRegion()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };
            var key = Guid.NewGuid().ToString();
            var region = Guid.NewGuid().ToString();
            var msg = BackplaneMessage.ForChanged(owner, key, region, CacheItemChangedEventAction.Put);

            // act
            var serialized = BackplaneMessage.Serialize(msg);
            var deserialized = BackplaneMessage.Deserialize(serialized).First();

            // assert
            deserialized.Key.Should().Be(key);
            deserialized.Region.Should().Be(region);
            deserialized.ChangeAction.Should().Be(CacheItemChangedEventAction.Put);
            deserialized.Action.Should().Be(BackplaneAction.Changed);
            deserialized.OwnerIdentity.Should().BeEquivalentTo(owner);
            deserialized.Should().BeEquivalentTo(msg);
        }

        [Fact]
        public void BackplaneMessage_ChangeUpdateKey()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };
            var key = Guid.NewGuid().ToString();
            var msg = BackplaneMessage.ForChanged(owner, key, CacheItemChangedEventAction.Update);

            // act
            var serialized = BackplaneMessage.Serialize(msg);
            var deserialized = BackplaneMessage.Deserialize(serialized).First();

            // assert
            deserialized.Key.Should().Be(key);
            deserialized.Region.Should().Be(null);
            deserialized.ChangeAction.Should().Be(CacheItemChangedEventAction.Update);
            deserialized.Action.Should().Be(BackplaneAction.Changed);
            deserialized.OwnerIdentity.Should().BeEquivalentTo(owner);
            deserialized.Should().BeEquivalentTo(msg);
        }

        [Fact]
        public void BackplaneMessage_ChangePutUpdateRegion()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };
            var key = Guid.NewGuid().ToString();
            var region = Guid.NewGuid().ToString();
            var msg = BackplaneMessage.ForChanged(owner, key, region, CacheItemChangedEventAction.Update);

            // act
            var serialized = BackplaneMessage.Serialize(msg);
            var deserialized = BackplaneMessage.Deserialize(serialized).First();

            // assert
            deserialized.Key.Should().Be(key);
            deserialized.Region.Should().Be(region);
            deserialized.ChangeAction.Should().Be(CacheItemChangedEventAction.Update);
            deserialized.Action.Should().Be(BackplaneAction.Changed);
            deserialized.OwnerIdentity.Should().BeEquivalentTo(owner);
            deserialized.Should().BeEquivalentTo(msg);
        }

        [Fact]
        public void BackplaneMessage_RemovedKey()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };
            var key = Guid.NewGuid().ToString();
            var msg = BackplaneMessage.ForRemoved(owner, key);

            // act
            var serialized = BackplaneMessage.Serialize(msg);
            var deserialized = BackplaneMessage.Deserialize(serialized).First();

            // assert
            deserialized.Key.Should().Be(key);
            deserialized.Region.Should().Be(null);
            deserialized.ChangeAction.Should().Be(CacheItemChangedEventAction.Invalid);
            deserialized.Action.Should().Be(BackplaneAction.Removed);
            deserialized.OwnerIdentity.Should().BeEquivalentTo(owner);
            deserialized.Should().BeEquivalentTo(msg);
        }

        [Fact]
        public void BackplaneMessage_RemovedKeyRegion()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };
            var key = Guid.NewGuid().ToString();
            var region = Guid.NewGuid().ToString();
            var msg = BackplaneMessage.ForRemoved(owner, key, region);

            // act
            var serialized = BackplaneMessage.Serialize(msg);
            var deserialized = BackplaneMessage.Deserialize(serialized).First();

            // assert
            deserialized.Key.Should().Be(key);
            deserialized.Region.Should().Be(region);
            deserialized.ChangeAction.Should().Be(CacheItemChangedEventAction.Invalid);
            deserialized.Action.Should().Be(BackplaneAction.Removed);
            deserialized.OwnerIdentity.Should().BeEquivalentTo(owner);
            deserialized.Should().BeEquivalentTo(msg);
        }

        [Fact]
        public void BackplaneMessage_Clear()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };
            var msg = BackplaneMessage.ForClear(owner);

            // act
            var serialized = BackplaneMessage.Serialize(msg);
            var deserialized = BackplaneMessage.Deserialize(serialized).First();

            // assert
            deserialized.Key.Should().Be(null);
            deserialized.Region.Should().Be(null);
            deserialized.ChangeAction.Should().Be(CacheItemChangedEventAction.Invalid);
            deserialized.Action.Should().Be(BackplaneAction.Clear);
            deserialized.OwnerIdentity.Should().BeEquivalentTo(owner);
            deserialized.Should().BeEquivalentTo(msg);
        }

        [Fact]
        public void BackplaneMessage_ClearRegion()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };

            var region = Guid.NewGuid().ToString();
            var msg = BackplaneMessage.ForClearRegion(owner, region);

            // act
            var serialized = BackplaneMessage.Serialize(msg);
            var deserialized = BackplaneMessage.Deserialize(serialized).First();

            // assert
            deserialized.Key.Should().Be(null);
            deserialized.Region.Should().Be(region);
            deserialized.ChangeAction.Should().Be(CacheItemChangedEventAction.Invalid);
            deserialized.Action.Should().Be(BackplaneAction.ClearRegion);
            deserialized.OwnerIdentity.Should().BeEquivalentTo(owner);
            deserialized.Should().BeEquivalentTo(msg);
        }

        [Fact]
        public void BackplaneMessage_RefEquals()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };

            var region = Guid.NewGuid().ToString();
            var msg = BackplaneMessage.ForClearRegion(owner, region);

            // act
            msg.Equals(msg).Should().BeTrue();
        }

        [Fact]
        public void BackplaneMessage_EqualsOtherChangedKey()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };

            var key = Guid.NewGuid().ToString();
            var msg = BackplaneMessage.ForChanged(owner, key, CacheItemChangedEventAction.Update);
            var msg2 = BackplaneMessage.ForChanged(owner, key, CacheItemChangedEventAction.Update);

            // act
            msg.Equals(msg2).Should().BeTrue();
        }

        [Fact]
        public void BackplaneMessage_EqualsOtherChangedKeyRegion()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };

            var key = Guid.NewGuid().ToString();
            var region = Guid.NewGuid().ToString();
            var msg = BackplaneMessage.ForChanged(owner, key, region, CacheItemChangedEventAction.Update);
            var msg2 = BackplaneMessage.ForChanged(owner, key, region, CacheItemChangedEventAction.Update);

            // act
            msg.Equals(msg2).Should().BeTrue();
        }

        [Fact]
        public void BackplaneMessage_EqualsOtherRemovedKey()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };

            var key = Guid.NewGuid().ToString();
            var msg = BackplaneMessage.ForRemoved(owner, key);
            var msg2 = BackplaneMessage.ForRemoved(owner, key);

            // act
            msg.Equals(msg2).Should().BeTrue();
        }

        [Fact]
        public void BackplaneMessage_EqualsOtherRemovedKeyRegion()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };

            var key = Guid.NewGuid().ToString();
            var region = Guid.NewGuid().ToString();
            var msg = BackplaneMessage.ForRemoved(owner, key, region);
            var msg2 = BackplaneMessage.ForRemoved(owner, key, region);

            // act
            msg.Equals(msg2).Should().BeTrue();
        }

        [Fact]
        public void BackplaneMessage_EqualsOtherClearRegion()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };

            var region = Guid.NewGuid().ToString();
            var msg = BackplaneMessage.ForClearRegion(owner, region);
            var msg2 = BackplaneMessage.ForClearRegion(owner, region);

            // act
            msg.Equals(msg2).Should().BeTrue();
        }

        [Fact]
        public void BackplaneMessage_EqualsOtherClear()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };
            var msg = BackplaneMessage.ForClear(owner);
            var msg2 = BackplaneMessage.ForClear(owner);

            // act
            msg.Equals(msg2).Should().BeTrue();
        }

        [Fact]
        public void BackplaneMessage_NotEqualNull()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };

            var region = Guid.NewGuid().ToString();
            var msg = BackplaneMessage.ForClearRegion(owner, region);

            // act
            msg.Equals(null).Should().BeFalse();
        }

        [Fact]
        public void BackplaneMessage_NotEqualOther()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };

            var region = Guid.NewGuid().ToString();
            var msg = BackplaneMessage.ForClearRegion(owner, region);

            // act
            msg.Equals("hello").Should().BeFalse();
        }

        [Fact]
        public void BackplaneMessage_HashChangedKey()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };

            var key = Guid.NewGuid().ToString();
            var msg = BackplaneMessage.ForChanged(owner, key, CacheItemChangedEventAction.Update);
            var msg2 = BackplaneMessage.ForChanged(owner, key, CacheItemChangedEventAction.Update);

            // act
            msg.GetHashCode().Should().Be(msg2.GetHashCode());
        }

        [Fact]
        public void BackplaneMessage_HashChangedKeyRegion()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };

            var key = Guid.NewGuid().ToString();
            var region = Guid.NewGuid().ToString();
            var msg = BackplaneMessage.ForChanged(owner, key, region, CacheItemChangedEventAction.Update);
            var msg2 = BackplaneMessage.ForChanged(owner, key, region, CacheItemChangedEventAction.Update);

            // act
            msg.GetHashCode().Should().Be(msg2.GetHashCode());
        }

        [Fact]
        public void BackplaneMessage_HashRemovedKey()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };

            var key = Guid.NewGuid().ToString();
            var msg = BackplaneMessage.ForRemoved(owner, key);
            var msg2 = BackplaneMessage.ForRemoved(owner, key);

            // act
            msg.GetHashCode().Should().Be(msg2.GetHashCode());
        }

        [Fact]
        public void BackplaneMessage_HashRemovedKeyRegion()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };

            var key = Guid.NewGuid().ToString();
            var region = Guid.NewGuid().ToString();
            var msg = BackplaneMessage.ForRemoved(owner, key, region);
            var msg2 = BackplaneMessage.ForRemoved(owner, key, region);

            // act
            msg.GetHashCode().Should().Be(msg2.GetHashCode());
        }

        [Fact]
        public void BackplaneMessage_HashClearRegion()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };

            var region = Guid.NewGuid().ToString();
            var msg = BackplaneMessage.ForClearRegion(owner, region);
            var msg2 = BackplaneMessage.ForClearRegion(owner, region);

            // act
            msg.GetHashCode().Should().Be(msg2.GetHashCode());
        }

        [Fact]
        public void BackplaneMessage_HashClear()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };
            var msg = BackplaneMessage.ForClear(owner);
            var msg2 = BackplaneMessage.ForClear(owner);

            // act
            msg.GetHashCode().Should().Be(msg2.GetHashCode());
        }

        [Fact]
        public void BackplaneMessage_Hashset_AllEqual()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };

            var key = Guid.NewGuid().ToString();
            var region = Guid.NewGuid().ToString();
            var msg = BackplaneMessage.ForChanged(owner, key, region, CacheItemChangedEventAction.Update);
            var msg2 = BackplaneMessage.ForChanged(owner, key, region, CacheItemChangedEventAction.Update);

            var hashset = new HashSet<BackplaneMessage>();

            // act
            hashset.Add(msg);
            hashset.Add(msg);
            hashset.Count.Should().Be(1);
            hashset.Add(msg2);
            hashset.Add(msg2);
            hashset.Count.Should().Be(1);
        }

        [Fact]
        public void BackplaneMessage_Hashset_DifferentChangeAction()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };

            var key = Guid.NewGuid().ToString();
            var region = Guid.NewGuid().ToString();
            var msg = BackplaneMessage.ForChanged(owner, key, region, CacheItemChangedEventAction.Update);
            var msg2 = BackplaneMessage.ForChanged(owner, key, region, CacheItemChangedEventAction.Add);

            var hashset = new HashSet<BackplaneMessage>();

            // act
            hashset.Add(msg);
            hashset.Add(msg);
            hashset.Count.Should().Be(1);
            hashset.Add(msg2);
            hashset.Add(msg2);
            hashset.Count.Should().Be(2);
        }

        [Fact]
        public void BackplaneMessage_Hashset_DifferentKey()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };

            var key = Guid.NewGuid().ToString();
            var key2 = Guid.NewGuid().ToString();
            var region = Guid.NewGuid().ToString();
            var msg = BackplaneMessage.ForChanged(owner, key, region, CacheItemChangedEventAction.Update);
            var msg2 = BackplaneMessage.ForChanged(owner, key2, region, CacheItemChangedEventAction.Update);

            var hashset = new HashSet<BackplaneMessage>();

            // act
            hashset.Add(msg);
            hashset.Add(msg);
            hashset.Count.Should().Be(1);
            hashset.Add(msg2);
            hashset.Add(msg2);
            hashset.Count.Should().Be(2);
        }

        [Fact]
        public void BackplaneMessage_Hashset_DifferentRegion()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };

            var key = Guid.NewGuid().ToString();
            var region = Guid.NewGuid().ToString();
            var region2 = Guid.NewGuid().ToString();
            var msg = BackplaneMessage.ForChanged(owner, key, region, CacheItemChangedEventAction.Update);
            var msg2 = BackplaneMessage.ForChanged(owner, key, region2, CacheItemChangedEventAction.Update);

            var hashset = new HashSet<BackplaneMessage>();

            // act
            hashset.Add(msg);
            hashset.Add(msg);
            hashset.Count.Should().Be(1);
            hashset.Add(msg2);
            hashset.Add(msg2);
            hashset.Count.Should().Be(2);
        }

        [Fact]
        public void BackplaneMessage_Hashset_DifferentMessage()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };

            var key = Guid.NewGuid().ToString();
            var region = Guid.NewGuid().ToString();
            var msg = BackplaneMessage.ForChanged(owner, key, region, CacheItemChangedEventAction.Update);
            var msg2 = BackplaneMessage.ForRemoved(owner, key, region);

            var hashset = new HashSet<BackplaneMessage>();

            // act
            hashset.Add(msg);
            hashset.Add(msg);
            hashset.Count.Should().Be(1);
            hashset.Add(msg2);
            hashset.Add(msg2);
            hashset.Count.Should().Be(2);
        }

        [Fact]
        public void BackplaneMessage_Multiple()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };

            var messages = CreateMany(owner);

            var serialized = BackplaneMessage.Serialize(messages.ToArray());
            var deserialized = BackplaneMessage.Deserialize(serialized).ToArray();

            messages.Count.Should().Be(41);
            deserialized.Should().BeEquivalentTo(messages);
        }

        [Fact]
        public void BackplaneMessage_Multiple_IgnoreOwner()
        {
            // arrange
            var owner = new byte[] { 1, 2, 3, 4 };

            var messages = CreateMany(owner);

            var serialized = BackplaneMessage.Serialize(messages.ToArray());
            var deserialized = BackplaneMessage.Deserialize(serialized, skipOwner: owner).ToArray();

            messages.Count.Should().Be(41);
            deserialized.Length.Should().Be(0);
        }

        private static ISet<BackplaneMessage> CreateMany(byte[] owner)
        {
            var messages = new HashSet<BackplaneMessage>();
            var key = Guid.NewGuid().ToString();
            var region = Guid.NewGuid().ToString();

            // test hash compare works too, result hashset should still have 41 messages only!
            for (var m = 0; m < 100; m++)
            {
                for (var i = 0; i < 10; i++)
                {
                    messages.Add(BackplaneMessage.ForChanged(owner, key + i, CacheItemChangedEventAction.Update));
                    messages.Add(BackplaneMessage.ForChanged(owner, key + i, region, CacheItemChangedEventAction.Add));
                }

                messages.Add(BackplaneMessage.ForClear(owner));

                for (var i = 0; i < 10; i++)
                {
                    messages.Add(BackplaneMessage.ForClearRegion(owner, region + i));
                }
                for (var i = 0; i < 10; i++)
                {
                    messages.Add(BackplaneMessage.ForRemoved(owner, key + i, region));
                }
            }

            return messages;
        }

        [Fact]
        public void BackplaneMessage_DeserializeInvalidBytes_WrongOwnerLen()
        {
            // arrange
            var data = new byte[] { 0, 118, 50, 0, 4, 0, 1, 2, 3, 4 };

            Action act = () => BackplaneMessage.Deserialize(data).First();
            act.Should().Throw<IndexOutOfRangeException>().WithMessage("*Cannot read bytes,*");
        }

        [Fact]
        public void BackplaneMessage_DeserializeInvalidBytes_NoAction()
        {
            // arrange
            var data = new byte[] { 0, 118, 50, 0, 4, 0, 0, 0, 1, 2, 3, 4 };

            Action act = () => BackplaneMessage.Deserialize(data).First();
            act.Should().Throw<IndexOutOfRangeException>().WithMessage("*Cannot read byte,*");
        }

        [Fact]
        public void BackplaneMessage_DeserializeInvalidBytes_WrongAction()
        {
            // arrange
            var data = new byte[] { 0, 118, 50, 0, 4, 0, 0, 0, 1, 2, 3, 4, 255 };

            Action act = () => BackplaneMessage.Deserialize(data).First();
            act.Should().Throw<ArgumentException>().WithMessage("*invalid message type*");
        }

        [Fact]
        public void BackplaneMessage_DeserializeInvalidBytes_InvalidString()
        {
            // arrange                                      clear region with wrong region string
            var data = new byte[] { 0, 118, 50, 0, 4, 0, 0, 0, 1, 2, 3, 4, 3, 10, 0, 0, 0, 42 };

            Action act = () => BackplaneMessage.Deserialize(data).First();
            act.Should().Throw<IndexOutOfRangeException>().WithMessage("*Cannot read string,*");
        }
    }
}
