using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.Serialization.Bond;
using CacheManager.Serialization.Json;
using CacheManager.Serialization.ProtoBuf;
using Microsoft.Extensions.ObjectPool;
using Newtonsoft.Json;

namespace CacheManager.Benchmarks
{
    [Config(typeof(CacheManagerBenchConfig))]
    public class SerializationBenchmark
    {
        private int iterations = 1000;
        private Queue<CacheItem<TestPoco>> payload;
        private BinaryCacheSerializer binary = new BinaryCacheSerializer();
        private JsonCacheSerializer json = new JsonCacheSerializer();
        private GzJsonCacheSerializer jsonGz = new GzJsonCacheSerializer();
        private ProtoBufSerializer proto = new Serialization.ProtoBuf.ProtoBufSerializer();
        private BondCompactBinaryCacheSerializer bondBinary = new BondCompactBinaryCacheSerializer(18000);
        private BondFastBinaryCacheSerializer bondFastBinary = new BondFastBinaryCacheSerializer(18000);
        private BondSimpleJsonCacheSerializer bondSimpleJson = new BondSimpleJsonCacheSerializer();
        private readonly Type pocoType = typeof(TestPoco);

        [Setup]
        public void Setup()
        {
            var rnd = new Random();
            var items = new List<CacheItem<TestPoco>>();
            for (var iter = 0; iter < iterations; iter++)
            {
                var list = new List<string>();
                for (var i = 0; i < 300; i++)
                {
                    list.Add(Guid.NewGuid().ToString());
                }

                var oList = new List<TestSubPoco>();
                for (var i = 0; i < 100; i++)
                {
                    oList.Add(new TestSubPoco()
                    {
                        Id = rnd.Next(1, int.MaxValue),
                        Val = Guid.NewGuid().ToString()
                    });
                }

                items.Add(new CacheItem<TestPoco>("key" + iter, new TestPoco()
                {
                    L = rnd.Next(1000, int.MaxValue),
                    S = Guid.NewGuid().ToString(),
                    SList = list,
                    OList = oList
                }));
            }

            payload = new Queue<CacheItem<TestPoco>>(items);
        }
        
        private void ExecRun(Action<CacheItem<TestPoco>> action)
        {
            var item = payload.Dequeue();
            action(item);
            payload.Enqueue(item);
        }
        
        //[Benchmark()]
        public void BinarySerializer()
        {
            ExecRun((item) =>
            {
                var data = binary.SerializeCacheItem(item);
                var result = binary.DeserializeCacheItem<TestPoco>(data, pocoType);
                if (result == null)
                {
                    throw new Exception();
                }
            });
        }

        //[Benchmark(Baseline = true)]
        public void JsonSerializer()
        {
            ExecRun((item) =>
            {
                var data = json.SerializeCacheItem(item);
                var result = json.DeserializeCacheItem<TestPoco>(data, pocoType);
                if (result == null)
                {
                    throw new Exception();
                }
            });
        }

        //[Benchmark]
        public void JsonGzSerializer()
        {
            ExecRun((item) =>
            {
                var data = jsonGz.SerializeCacheItem(item);
                var result = jsonGz.DeserializeCacheItem<TestPoco>(data, pocoType);
                if (result == null)
                {
                    throw new Exception();
                }
            });
        }

        [Benchmark]
        public void ProtoBufSerializer()
        {
            ExecRun((item) =>
            {
                var data = proto.SerializeCacheItem(item);
                var result = proto.DeserializeCacheItem<TestPoco>(data, pocoType);
                if (result == null)
                {
                    throw new Exception();
                }
            });
        }

        [Benchmark]
        public void BondBinarySerializer()
        {
            ExecRun((item) =>
            {
                var data = bondBinary.SerializeCacheItem(item);
                var result = bondBinary.DeserializeCacheItem<TestPoco>(data, pocoType);
                if (result == null)
                {
                    throw new Exception();
                }
            });
        }

        [Benchmark]
        public void BondFastBinarySerializer()
        {
            ExecRun((item) =>
            {
                var data = bondFastBinary.SerializeCacheItem(item);
                var result = bondFastBinary.DeserializeCacheItem<TestPoco>(data, pocoType);
                if (result == null)
                {
                    throw new Exception();
                }
            });
        }

        [Benchmark]
        public void BondSimpleJsonSerializer()
        {
            ExecRun((item) =>
            {
                var data = bondSimpleJson.SerializeCacheItem(item);
                var result = bondSimpleJson.DeserializeCacheItem<TestPoco>(data, pocoType);
                if (result == null)
                {
                    throw new Exception();
                }
            });
        }
    }
}