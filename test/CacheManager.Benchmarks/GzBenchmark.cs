using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using BenchmarkDotNet.Attributes;
using CacheManager.Serialization.Json;

namespace CacheManager.Benchmarks
{
    [Config(typeof(CacheManagerBenchConfig))]
    public class GzBenchmark
    {
        private static ArrayPool<byte> _pool = ArrayPool<byte>.Create();

        private byte[] _payload;

        [Setup]
        public void Setup()
        {
            var list = new List<string>();

            for (var i = 0; i < 1000; i++)
            {
                list.Add(Guid.NewGuid().ToString());
            }

            _payload = new JsonCacheSerializer().Serialize(list);
        }

        [Benchmark(Baseline = true)]
        public void Naive()
        {
            var compress = new NaiveCompression();
            var a = compress.Compression(_payload);
            var b = compress.Decompression(a);

            if (_payload.Length != b.Length)
            {
                throw new Exception();
            }
        }

        [Benchmark()]
        public void Manuel()
        {
            var compress = new Manual();
            var a = compress.Compression(_payload);
            var b = compress.Decompression(a);

            if (_payload.Length != b.Count)
            {
                throw new Exception();
            }
        }

        [Benchmark()]
        public void ManuelPooled()
        {
            var compress = new ManualPooled();

            var buffer = _pool.Rent((int)(_payload.Length * 1.2));

            var a = compress.Compression(_payload, buffer);

            var b = compress.Decompression(a);

            if (_payload.Length != b.Count)
            {
                throw new Exception();
            }

            _pool.Return(buffer);
        }

        private class ManualPooled
        {
            public ArraySegment<byte> Compression(byte[] data, byte[] buffer)
            {
                using (var bytesBuilder = new MemoryStream(buffer))
                {
                    using (var gzWriter = new GZipStream(bytesBuilder, CompressionLevel.Fastest, true))
                    {
                        gzWriter.Write(data, 0, data.Length);
                    }

                    return new ArraySegment<byte>(buffer, 0, (int)bytesBuilder.Position);
                }
            }

            public ArraySegment<byte> Decompression(ArraySegment<byte> compressedData)
            {
                using (var inputStream = new MemoryStream(compressedData.Array, 0, compressedData.Count))
                using (var gzReader = new GZipStream(inputStream, CompressionMode.Decompress))
                using (var stream = new MemoryStream(compressedData.Count * 2))
                {
                    var buffer = _pool.Rent(compressedData.Count);
                    var readBytes = 0;

                    while ((readBytes = gzReader.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        stream.Write(buffer, 0, readBytes);
                    }

                    _pool.Return(buffer);
                    return new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length);
                }
            }
        }

        private class Manual
        {
            public ArraySegment<byte> Compression(byte[] data)
            {
                var buffer = new byte[data.Length];
                using (var bytesBuilder = new MemoryStream(buffer))
                {
                    using (var gzWriter = new GZipStream(bytesBuilder, CompressionLevel.Fastest, true))
                    {
                        gzWriter.Write(data, 0, data.Length);
                        bytesBuilder.Flush();
                    }

                    return new ArraySegment<byte>(buffer, 0, (int)bytesBuilder.Position);
                }
            }

            public ArraySegment<byte> Decompression(ArraySegment<byte> compressedData)
            {
                var buffer = new byte[compressedData.Count * 2];
                using (var inputStream = new MemoryStream(compressedData.Array, 0, compressedData.Count))
                using (var gzReader = new GZipStream(inputStream, CompressionMode.Decompress))
                using (var stream = new MemoryStream(compressedData.Count * 2))
                {
                    var readBytes = 0;
                    while ((readBytes = gzReader.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        stream.Write(buffer, 0, readBytes);
                    }

                    return new ArraySegment<byte>(stream.GetBuffer(), 0, (int)stream.Length);
                }
            }
        }

        private class NaiveCompression
        {
            public byte[] Compression(byte[] data)
            {
                using (var bytesBuilder = new MemoryStream())
                {
                    using (var gzWriter = new GZipStream(bytesBuilder, CompressionLevel.Fastest, true))
                    {
                        gzWriter.Write(data, 0, data.Length);
                    }

                    return bytesBuilder.ToArray();
                }
            }

            public byte[] Decompression(byte[] compressedData)
            {
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