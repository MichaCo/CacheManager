using System;
using System.Text;
using Bond.IO.Unsafe;
using CacheManager.Core.Internal;
using Microsoft.Extensions.ObjectPool;

namespace CacheManager.Serialization.Bond
{
    /// <summary>
    /// Base class for Bond based serializers
    /// </summary>
    public abstract class BondSerializerBase : CacheSerializer
    {
        private static readonly Type _openItemType = typeof(BondCacheItem<>);

        /// <summary>
        /// Initializes a new instance of the <see cref="BondSerializerBase"/> class.
        /// </summary>
        public BondSerializerBase()
            : this(1024)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BondSerializerBase"/> class.
        /// </summary>
        /// <param name="defaultBufferSize">The default buffer size.</param>
        public BondSerializerBase(int defaultBufferSize)
        {
            OutputBufferPool = new DefaultObjectPool<OutputBuffer>(new OutputBufferPoolPolicy(defaultBufferSize));
            StringBuilderPool = new DefaultObjectPool<StringBuilder>(new StringBuilderPoolPolicy(defaultBufferSize));
        }

        /// <summary>
        /// Gets a pool handling <see cref="OutputBuffer"/>s.
        /// </summary>
        [CLSCompliant(false)]
        protected ObjectPool<OutputBuffer> OutputBufferPool { get; }

        /// <summary>
        /// Gets a pool handling <see cref="StringBuilder"/>s.
        /// </summary>
        [CLSCompliant(false)]
        protected ObjectPool<StringBuilder> StringBuilderPool { get; }

        /// <inheritdoc/>
        protected override Type GetOpenGeneric()
        {
            return _openItemType;
        }

        /// <inheritdoc/>
        protected override object CreateNewItem<TCacheValue>(ICacheItemProperties properties, object value)
        {
            return new BondCacheItem<TCacheValue>(properties, value);
        }

        private class OutputBufferPoolPolicy : IPooledObjectPolicy<OutputBuffer>
        {
            private readonly int _defaultBufferSize;

            public OutputBufferPoolPolicy(int defaultBufferSize)
            {
                _defaultBufferSize = defaultBufferSize;
            }

            public OutputBuffer Create()
            {
                return new OutputBuffer(_defaultBufferSize);
            }

            public bool Return(OutputBuffer value)
            {
                ////if (value.Data.Count > _defaultBufferSize * 1000)
                ////{
                ////    return false;
                ////}

                value.Position = 0;
                return true;
            }
        }

        private class StringBuilderPoolPolicy : IPooledObjectPolicy<StringBuilder>
        {
            private readonly int _defaultBufferSize;

            public StringBuilderPoolPolicy(int defaultBufferSize)
            {
                _defaultBufferSize = defaultBufferSize;
            }

            public StringBuilder Create()
            {
                return new StringBuilder(_defaultBufferSize);
            }

            public bool Return(StringBuilder value)
            {
                ////if (value.Data.Count > _defaultBufferSize * 1000)
                ////{
                ////    return false;
                ////}

                value.Clear();
                return true;
            }
        }
    }
}
