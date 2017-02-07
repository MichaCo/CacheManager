using System;
using System.Linq;
#if !NOUNSAFE
using Bond.IO.Unsafe;
#else
using Bond.IO.Safe;
#endif
using CacheManager.Core.Internal;

namespace CacheManager.Serialization.Bond
{
    /// <summary>
    /// Base class for Bond based serializers
    /// </summary>
    public abstract class BondSerializerBase
    {
        private readonly object typesLock = new object();
        private readonly ObjectPool<OutputBuffer> _outputBufferPool;

        /// <summary>
        /// Initializes a new instance of the <see cref="BondSerializerBase"/> class.
        /// </summary>
        public BondSerializerBase(int defaultBufferSize = 1024)
        {
            _outputBufferPool = new ObjectPool<OutputBuffer>(new OutputBufferPoolPolicy(defaultBufferSize));
        }

        internal OutputBuffer LeaseOutputBuffer()
        {
            return _outputBufferPool.Lease();
        }

        internal void ReturnOutputBuffer(OutputBuffer buffer)
        {
            _outputBufferPool.Return(buffer);
        }
    }
}