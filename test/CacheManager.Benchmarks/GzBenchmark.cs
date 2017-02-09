using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using BenchmarkDotNet.Attributes;
using CacheManager.Core.Utility;
using CacheManager.Serialization.Json;

namespace CacheManager.Benchmarks
{
    [Config(typeof(CacheManagerBenchConfig))]
    public class GzBenchmark
    {
        private byte[] payload;

        [Setup]
        public void Setup()
        {
            var list = new List<string>();

            for (var i = 0; i < 1000; i++)
            {
                list.Add(Guid.NewGuid().ToString());
            }

            payload = new JsonCacheSerializer().Serialize(list);
        }

        [Benchmark()]
        public void NaiveImpl()
        {
            var compress = new NaiveCompression();
            var a = compress.Compression(payload);
            var b = compress.Decompression(a);

            if (payload.Length != b.Length)
            {
                throw new Exception();
            }
        }

        private class NaiveCompression
        {
            public byte[] Compression(byte[] data)
            {
                Guard.NotNull(data, nameof(data));

                using (var bytesBuilder = new MemoryStream())
                {
                    using (var gzWriter = new GZipStream(bytesBuilder, CompressionMode.Compress))
                    {
                        gzWriter.Write(data, 0, data.Length);
                    }

                    return bytesBuilder.ToArray();
                }
            }

            public byte[] Decompression(byte[] compressedData)
            {
                Guard.NotNull(compressedData, nameof(compressedData));

                using (var inputStream = new MemoryStream(compressedData))
                using (var gzReader = new GZipStream(inputStream, CompressionMode.Decompress))
                using (var bytesBuilder = new MemoryStream())
                {
                    gzReader.CopyTo(bytesBuilder);
                    return bytesBuilder.ToArray();
                }
            }
        }
    }
}