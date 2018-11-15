using System;
using System.Linq;
using System.Text;
using Bond.IO.Unsafe;
using CacheManager.Core.Internal;
using CacheManager.Core.Utility;

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
            OutputBufferPool = new ObjectPool<OutputBuffer>(new OutputBufferPoolPolicy(defaultBufferSize));
            StringBuilderPool = new ObjectPool<StringBuilder>(new StringBuilderPoolPolicy(defaultBufferSize));
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
                ////if (value.Data.Count > _defaultBufferSize * 1000)
                ////{
                ////    return false;
                ////}

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
