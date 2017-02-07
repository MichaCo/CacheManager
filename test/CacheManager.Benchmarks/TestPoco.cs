using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using ProtoBuf;

namespace CacheManager.Benchmarks
{
    [Serializable]
    [JsonObject]
    [ProtoContract]
    [Bond.Schema]
    public class TestPoco
    {
        [JsonProperty]
        [ProtoMember(1)]
        [Bond.Id(1)]
        public long L { get; set; }

        [JsonProperty]
        [ProtoMember(2)]
        [Bond.Id(2)]
        public string S { get; set; }

        [JsonProperty]
        [ProtoMember(3)]
        [Bond.Id(3)]
        public List<string> SList { get; set; }

        [JsonProperty]
        [ProtoMember(4)]
        [Bond.Id(4)]
        public List<TestSubPoco> OList { get; set; }
    }

    [Serializable]
    [ProtoContract]
    [JsonObject]
    [Bond.Schema]
    public class TestSubPoco
    {
        [JsonProperty]
        [ProtoMember(1)]
        [Bond.Id(1)]
        public int Id { get; set; }

        [JsonProperty]
        [ProtoMember(2)]
        [Bond.Id(2)]
        public string Val { get; set; }
    }
}