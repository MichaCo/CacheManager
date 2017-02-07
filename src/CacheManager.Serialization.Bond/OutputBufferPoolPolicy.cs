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
    internal class OutputBufferPoolPolicy : IObjectPoolPolicy<OutputBuffer>
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
}