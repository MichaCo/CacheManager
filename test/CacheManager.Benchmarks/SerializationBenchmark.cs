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
        private BinaryCacheSerializer binary;
        private JsonCacheSerializer json;
        private GzJsonCacheSerializer jsonGz;
        private ProtoBufSerializer proto;
        private BondBinaryCacheSerializer bondBinary;
        private readonly Type pocoType = typeof(TestPoco);

        [Setup]
        public void Setup()
        {
            this.binary = new BinaryCacheSerializer();
            this.json = new JsonCacheSerializer();
            this.jsonGz = new GzJsonCacheSerializer();
            this.proto = new ProtoBufSerializer();
            this.bondBinary = new BondBinaryCacheSerializer(1024);

            var rnd = new Random();
            var items = new List<CacheItem<TestPoco>>();
            for (var iter = 0; iter < iterations; iter++)
            {
                var list = new List<string>();
                for (var i = 0; i < 10; i++)
                {
                    list.Add(Guid.NewGuid().ToString());
                }

                var oList = new List<TestSubPoco>();
                for (var i = 0; i < 10; i++)
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

        private static JsonSerializer Serializer = Newtonsoft.Json.JsonSerializer.CreateDefault();
        private static JsonArrayPool CharArrayPool = JsonArrayPool.Instance;

        public class JsonArrayPool : IArrayPool<char>
        {
            public static readonly JsonArrayPool Instance = new JsonArrayPool();

            public char[] Rent(int minimumLength)
            {
                return ArrayPool<char>.Shared.Rent(minimumLength);
            }

            public void Return(char[] array)
            {
                ArrayPool<char>.Shared.Return(array);
            }
        }

        private class JsonTextWriterSerializer
        {
            public byte[] Serialize<T>(T value)
            {
                var stream = new StringBuilder(256);
                using (var writer = new StringWriter(stream))
                using (var jsonWriter = new JsonTextWriter(writer))
                {
                    jsonWriter.ArrayPool = CharArrayPool;
                    Serializer.Serialize(jsonWriter, value);
                    jsonWriter.Flush();
                }

                return Encoding.UTF8.GetBytes(stream.ToString());
            }

            public T Deserialize<T>(byte[] value)
            {
                using (var reader = new StreamReader(new MemoryStream(value)))
                using (var jsonReader = new JsonTextReader(reader))
                {
                    jsonReader.ArrayPool = CharArrayPool;
                    return Serializer.Deserialize<T>(jsonReader);
                }
            }
        }

        public class PooledJsonTextWriterSerializer : IDisposable
        {
            private static ArrayPool<byte> ByteArrayPool = ArrayPool<byte>.Create();
            private static Microsoft.Extensions.ObjectPool.ObjectPool<StringBuilder> BuilderArrayPool = new DefaultObjectPoolProvider().CreateStringBuilderPool(256, 48 * 1024);

            public ArraySegment<byte> Buffer;

            public void Serialize<T>(T value)
            {
                var stringBuilder = BuilderArrayPool.Get();
                using (var jsonWriter = new JsonTextWriter(new StringWriter(stringBuilder)))
                {
                    jsonWriter.ArrayPool = CharArrayPool;
                    Serializer.Serialize(jsonWriter, value);
                    jsonWriter.Flush();
                }

                var charLength = stringBuilder.Length;
                var chars = CharArrayPool.Rent(charLength);
                stringBuilder.CopyTo(0, chars, 0, charLength);
                BuilderArrayPool.Return(stringBuilder);

                var length = Encoding.UTF8.GetByteCount(chars, 0, charLength);
                var bytes = ByteArrayPool.Rent(length);
                Encoding.UTF8.GetBytes(chars, 0, charLength, bytes, 0);
                Buffer = new ArraySegment<byte>(bytes, 0, length);
                CharArrayPool.Return(chars);
            }

            public T Deserialize<T>()
            {
                var stringValue = Encoding.UTF8.GetString(Buffer.Array, Buffer.Offset, Buffer.Count);
                using (var stream = new StringReader(stringValue))
                using (var jsonReader = new JsonTextReader(stream))
                {
                    jsonReader.ArrayPool = CharArrayPool;
                    return Serializer.Deserialize<T>(jsonReader);
                }
            }

            private bool disposedValue = false; // To detect redundant calls

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        if (Buffer.Array != null)
                        {
                            ByteArrayPool.Return(Buffer.Array);
                        }
                    }

                    disposedValue = true;
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }
        }

        private static JsonTextWriterSerializer textWriterSerializer = new JsonTextWriterSerializer();

        //[Benchmark(Baseline = true)]
        public void JsonTextWriter()
        {
            ExecRun((item) =>
            {
                var data = textWriterSerializer.Serialize(item);
                var result = textWriterSerializer.Deserialize<TestPoco>(data);
                if (result == null)
                {
                    throw new Exception();
                }
            });
        }

        //[Benchmark()]
        public void BufferedJsonTextWriter()
        {
            ExecRun((item) =>
            {
                using (var seri = new PooledJsonTextWriterSerializer())
                {
                    seri.Serialize(item);
                    var result = seri.Deserialize<TestPoco>();
                    if (result == null)
                    {
                        throw new Exception();
                    }
                }
            });
        }

        [Benchmark()]
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

        [Benchmark(Baseline = true)]
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

        [Benchmark]
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
    }
}