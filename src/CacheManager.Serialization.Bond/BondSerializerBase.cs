using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
#if !NOUNSAFE
using Bond.IO.Unsafe;
#else
using Bond.IO.Safe;
#endif
using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.Core.Utility;

namespace CacheManager.Serialization.Bond
{
    /// <summary>
    /// Base class for Bond based serializers
    /// </summary>
    public abstract class BondSerializerBase : ICacheSerializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BondSerializerBase"/> class.
        /// </summary>
        public BondSerializerBase(int defaultBufferSize = 1024)
        {
            OutputBufferPool = new ObjectPool<OutputBuffer>(new OutputBufferPoolPolicy(defaultBufferSize));
            StringBuilderPool = new ObjectPool<StringBuilder>(new StringBuilderPoolPolicy(defaultBufferSize));
        }

        /// <inheritdoc/>
        public byte[] Serialize<T>(T value)
        {
            return Serialize(value, value.GetType());
        }

        /// <summary>
        /// Gets a pool handling <see cref="OutputBuffer"/>s.
        /// </summary>
        [CLSCompliant(false)]
        protected ObjectPool<OutputBuffer> OutputBufferPool { get; }

        /// <summary>
        /// Gets a pool handling <see cref="StringBuilder"/>s.
        /// </summary>
        protected ObjectPool<StringBuilder> StringBuilderPool { get; }

        /// <summary>
        /// Serializes stuff
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="type">The actual <c>Type</c> to serialize to.</param>
        /// <returns>Byte array of serialized data.</returns>
        protected abstract byte[] Serialize(object value, Type type);

        /// <inheritdoc/>
        public abstract object Deserialize(byte[] data, Type target);

        /// <inheritdoc/>
        public byte[] SerializeCacheItem<T>(CacheItem<T> value)
        {
            if (typeof(T) == TypeCache.ObjectType)
            {
                var valueBytes = this.Serialize(value.Value, value.ValueType);
                var itemType = BondCacheItem<object>.OpenItemType.MakeGenericType(value.ValueType);
                var item = Activator.CreateInstance(itemType, value);

                var wrapper = new BondCacheItemWrapper()
                {
                    ValueType = value.ValueType.AssemblyQualifiedName,
                    Data = this.Serialize(item, itemType)
                };

                return this.Serialize(wrapper);
            }
            else
            {
                var bondCacheItem = new BondCacheItem<T>(value);
                return this.Serialize(bondCacheItem);
            }
        }

        /// <inheritdoc/>
        public CacheItem<T> DeserializeCacheItem<T>(byte[] value, Type valueType = null)
        {
            if (typeof(T) == TypeCache.ObjectType)
            {
                var wrapper = (BondCacheItemWrapper)this.Deserialize(value, typeof(BondCacheItemWrapper));
                var targetValueType = valueType ?? TypeCache.GetType(wrapper.ValueType);
                var itemType = BondCacheItem<object>.OpenItemType.MakeGenericType(targetValueType);
                var obj = Deserialize(wrapper.Data, itemType);

#if NET40
                var methodInfo = itemType.GetMethod("ToObjectCacheItem");
#else
                var methodInfo = itemType.GetTypeInfo().GetDeclaredMethod("ToObjectCacheItem");
#endif

                var cacheItem = (CacheItem<T>)methodInfo.Invoke(obj, new object[0]);
                return cacheItem;
            }
            else
            {
                var bondCacheItem = (BondCacheItem<T>)Deserialize(value, typeof(BondCacheItem<T>));

                return bondCacheItem.ToCacheItem();
            }
        }

        private class OutputBufferPoolPolicy : IObjectPoolPolicy<OutputBuffer>
        {
            private readonly int _defaultBufferSize;

            public OutputBufferPoolPolicy(int defaultBufferSize)
            {
                _defaultBufferSize = defaultBufferSize;
            }

            public OutputBuffer CreateNew()
            {
                return new OutputBuffer(_defaultBufferSize);
            }

            public bool Return(OutputBuffer value)
            {
                //if (value.Data.Count > _defaultBufferSize * 1000)
                //{
                //    return false;
                //}

                value.Position = 0;
                return true;
            }
        }

        private class StringBuilderPoolPolicy : IObjectPoolPolicy<StringBuilder>
        {
            private readonly int _defaultBufferSize;

            public StringBuilderPoolPolicy(int defaultBufferSize)
            {
                _defaultBufferSize = defaultBufferSize;
            }

            public StringBuilder CreateNew()
            {
                return new StringBuilder(_defaultBufferSize);
            }

            public bool Return(StringBuilder value)
            {
                //if (value.Data.Count > _defaultBufferSize * 1000)
                //{
                //    return false;
                //}

                value.Clear();
                return true;
            }
        }
    }
}