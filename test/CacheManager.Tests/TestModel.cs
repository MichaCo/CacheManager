using System;
using System.Diagnostics.CodeAnalysis;
using ProtoBuf;

namespace CacheManager.Tests
{
#if !NETCOREAPP
    [Serializable]
#endif
    [ExcludeFromCodeCoverage]
    [ProtoContract]
    [Bond.Schema]
    public class RaceConditionTestElement
    {
        public RaceConditionTestElement()
        {
        }

        [ProtoMember(1)]
        [Bond.Id(1)]
        public long Counter { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class IAmNotSerializable
    {
    }
}