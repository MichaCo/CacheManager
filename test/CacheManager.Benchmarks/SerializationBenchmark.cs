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
    public class SerializationBenchmark
    {
        private int _iterations = 1000;
        private Queue<CacheItem<TestPoco>> _payload;
        private BinaryCacheSerializer _binary = new BinaryCacheSerializer();
        private JsonCacheSerializer _json = new JsonCacheSerializer();
        private GzJsonCacheSerializer _jsonGz = new GzJsonCacheSerializer();
        private ProtoBufSerializer _proto = new Serialization.ProtoBuf.ProtoBufSerializer();
        private BondCompactBinaryCacheSerializer _bondBinary = new BondCompactBinaryCacheSerializer(18000);
        private BondFastBinaryCacheSerializer _bondFastBinary = new BondFastBinaryCacheSerializer(18000);
        private BondSimpleJsonCacheSerializer _bondSimpleJson = new BondSimpleJsonCacheSerializer();
        private readonly Type _pocoType = typeof(TestPoco);

        [Setup]
        public void Setup()
        {
            var rnd = new Random();
            var items = new List<CacheItem<TestPoco>>();
            for (var iter = 0; iter < _iterations; iter++)
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

            _payload = new Queue<CacheItem<TestPoco>>(items);
        }

        private void ExecRun(Action<CacheItem<TestPoco>> action)
        {
            var item = _payload.Dequeue();
            action(item);
            _payload.Enqueue(item);
        }

        [Benchmark()]
        public void BinarySerializer()
        {
            ExecRun((item) =>
            {
                var data = _binary.SerializeCacheItem(item);
                var result = _binary.DeserializeCacheItem<TestPoco>(data, _pocoType);
                if (result == null)
                {
                    throw new Exception();
                }
            });
        }

        [Benchmark(Baseline = true)]
        public void JsonSerializer()
        {
            ExecRun((item) =>
            {
                var data = _json.SerializeCacheItem(item);
                var result = _json.DeserializeCacheItem<TestPoco>(data, _pocoType);
                if (result == null)
                {
                    throw new Exception();
                }
            });
        }

        [Benchmark]
        public void JsonGzSerializer()
        {
            ExecRun((item) =>
            {
                var data = _jsonGz.SerializeCacheItem(item);
                var result = _jsonGz.DeserializeCacheItem<TestPoco>(data, _pocoType);
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
                var data = _proto.SerializeCacheItem(item);
                var result = _proto.DeserializeCacheItem<TestPoco>(data, _pocoType);
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
                var data = _bondBinary.SerializeCacheItem(item);
                var result = _bondBinary.DeserializeCacheItem<TestPoco>(data, _pocoType);
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
                var data = _bondFastBinary.SerializeCacheItem(item);
                var result = _bondFastBinary.DeserializeCacheItem<TestPoco>(data, _pocoType);
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
                var data = _bondSimpleJson.SerializeCacheItem(item);
                var result = _bondSimpleJson.DeserializeCacheItem<TestPoco>(data, _pocoType);
                if (result == null)
                {
                    throw new Exception();
                }
            });
        }
    }
}