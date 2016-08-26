﻿using System;
using System.Diagnostics.CodeAnalysis;
using ProtoBuf;

namespace CacheManager.Tests
{
#if !DNXCORE50
    [Serializable]
#endif
    [ExcludeFromCodeCoverage]
    [ProtoContract]
    public class RaceConditionTestElement
    {
        public RaceConditionTestElement()
        {
        }

        [ProtoMember(1)]
        public long Counter { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class IAmNotSerializable
    {
    }
}