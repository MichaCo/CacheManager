using System;
using CacheManager.Core;
using CacheManager.Core.Internal;
using MessagePack;
using MessagePack.Resolvers;

namespace CacheManager.Serialization.MessagePack
{
    /// <summary>
    /// Implements the <c>ICacheSerializer</c> contract using <c>MessagePack</c>.
    /// </summary>
    public class MessagePackCacheSerializer : CacheSerializer
    {
        private static readonly Type _openGenericItemType = typeof(MessagePackCacheItem<>);

        /// <inheritdoc/>
        public override object Deserialize(byte[] data, Type target)
        {
            return MessagePackSerializer.NonGeneric.Deserialize(target, data, ContractlessStandardResolver.Instance);
        }

        /// <inheritdoc/>
        public override byte[] Serialize<T>(T value)
        {
            return MessagePackSerializer.Serialize(value, ContractlessStandardResolver.Instance);
        }

        /// <inheritdoc/>
        protected override object CreateNewItem<TCacheValue>(ICacheItemProperties properties, object value)
        {
            return new MessagePackCacheItem<TCacheValue>(properties, value);
        }

        /// <inheritdoc/>
        protected override Type GetOpenGeneric()
        {
            return _openGenericItemType;
        }
    }
}
