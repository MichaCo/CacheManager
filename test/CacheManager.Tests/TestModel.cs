using System;
using System.Diagnostics.CodeAnalysis;
using ProtoBuf;

namespace CacheManager.Tests
{
    [Serializable]
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
