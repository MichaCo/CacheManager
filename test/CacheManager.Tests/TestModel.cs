using System;
using System.Diagnostics.CodeAnalysis;

namespace CacheManager.Tests
{
#if !DNXCORE50
    [Serializable]
#endif
    [ExcludeFromCodeCoverage]
    public class RaceConditionTestElement
    {
        public RaceConditionTestElement()
        {
        }

        public long Counter { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class IAmNotSerializable
    {
    }
}
