using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.Serialization.Json;
using CacheManager.Serialization.ProtoBuf;
using FluentAssertions;
using ProtoBuf;
using Xunit;

namespace CacheManager.Tests
{
    [ExcludeFromCodeCoverage]
#if NET40
    [Trait("Framework", "NET40")]
#else
    [Trait("Framework", "NET45")]
#endif
    public class SerializerTests : BaseCacheManagerTest
    {
#if !DNXCORE50
        [Theory]
        [InlineData(true)]
        [InlineData(float.MaxValue)]
        [InlineData(int.MaxValue)]
        [InlineData(long.MaxValue)]
        [InlineData("some string")]
        [ReplaceCulture]
        public void BinarySerializer_Primitives<T>(T value)
        {
            // arrange
            var serializer = new BinaryCacheSerializer();

            // act
            var data = serializer.Serialize(value);
            var result = serializer.Deserialize(data, typeof(T));

            result.Should().Be(value);
        }

        [Fact]
        public void BinarySerializer_Pocco()
        {
            // arrange
            var serializer = new BinaryCacheSerializer();
            var item = SerializerPoccoSerializable.Create();

            // act
            var data = serializer.Serialize(item);
            var result = serializer.Deserialize(data, item.GetType());

            result.ShouldBeEquivalentTo(item);
        }

        [Fact]
        public void BinarySerializer_CacheItemWithPocco()
        {
            // arrange
            var serializer = new BinaryCacheSerializer();
            var pocco = SerializerPoccoSerializable.Create();
            var item = new CacheItem<SerializerPoccoSerializable>("key", "region", pocco, ExpirationMode.Absolute, TimeSpan.FromDays(1));

            // act
            var data = serializer.SerializeCacheItem(item);
            var result = serializer.DeserializeCacheItem<SerializerPoccoSerializable>(data, pocco.GetType());

            result.ShouldBeEquivalentTo(item);
        }

        [Fact]
        public void BinarySerializer_List()
        {
            // arrange
            var serializer = new BinaryCacheSerializer();
            var items = new List<SerializerPoccoSerializable>()
            {
                SerializerPoccoSerializable.Create(),
                SerializerPoccoSerializable.Create(),
                SerializerPoccoSerializable.Create()
            };

            // act
            var data = serializer.Serialize(items);
            var result = serializer.Deserialize(data, items.GetType());

            result.ShouldBeEquivalentTo(items);
        }
#endif

        [Theory]
        [InlineData(true)]
        [InlineData(float.MaxValue)]
        [InlineData(int.MaxValue)]
        [InlineData(long.MaxValue)]
        [InlineData("some string")]
        [ReplaceCulture]
        public void JsonSerializer_Primitives<T>(T value)
        {
            // arrange
            var serializer = new JsonCacheSerializer();

            // act
            var data = serializer.Serialize(value);
            var result = serializer.Deserialize(data, typeof(T));

            result.Should().Be(value);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(float.MaxValue)]
        [InlineData(int.MaxValue)]
        [InlineData(long.MaxValue)]
        [InlineData("some string")]
        [ReplaceCulture]
        public void JsonSerializer_CacheItem_Primitives<T>(T value)
        {
            // arrange
            var serializer = new JsonCacheSerializer();
            var item = new CacheItem<T>("key", value);

            // act
            var data = serializer.SerializeCacheItem(item);
            var result = serializer.DeserializeCacheItem<T>(data, typeof(T));

            result.Value.Should().Be(value);
            result.ValueType.Should().Be(item.ValueType);
            result.CreatedUtc.Should().Be(item.CreatedUtc);
            result.ExpirationMode.Should().Be(item.ExpirationMode);
            result.ExpirationTimeout.Should().Be(item.ExpirationTimeout);
            result.Key.Should().Be(item.Key);
            result.LastAccessedUtc.Should().Be(item.LastAccessedUtc);
            result.Region.Should().Be(item.Region);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(float.MaxValue)]
        [InlineData(int.MaxValue)]
        [InlineData(long.MaxValue)]
        [InlineData("some string")]
        [ReplaceCulture]
        public void JsonSerializer_CacheItemOfObject_Primitives<T>(T value)
        {
            // arrange
            var serializer = new JsonCacheSerializer();
            var item = new CacheItem<object>("key", value);

            // act
            var data = serializer.SerializeCacheItem(item);
            var result = serializer.DeserializeCacheItem<object>(data, typeof(T));

            result.Value.Should().Be(value);
            result.ValueType.Should().Be(item.ValueType);
            result.CreatedUtc.Should().Be(item.CreatedUtc);
            result.ExpirationMode.Should().Be(item.ExpirationMode);
            result.ExpirationTimeout.Should().Be(item.ExpirationTimeout);
            result.Key.Should().Be(item.Key);
            result.LastAccessedUtc.Should().Be(item.LastAccessedUtc);
            result.Region.Should().Be(item.Region);
        }

        [Fact]
        public void JsonSerializer_Pocco()
        {
            // arrange
            var serializer = new JsonCacheSerializer();
            var item = SerializerPoccoSerializable.Create();

            // act
            var data = serializer.Serialize(item);
            var result = serializer.Deserialize(data, item.GetType());

            result.ShouldBeEquivalentTo(item);
        }

        [Fact]
        public void JsonSerializer_CacheItemWithPocco()
        {
            // arrange
            var serializer = new JsonCacheSerializer();
            var pocco = SerializerPoccoSerializable.Create();
            var item = new CacheItem<SerializerPoccoSerializable>("key", "region", pocco, ExpirationMode.Absolute, TimeSpan.FromDays(1));

            // act
            var data = serializer.SerializeCacheItem(item);
            var result = serializer.DeserializeCacheItem<SerializerPoccoSerializable>(data, pocco.GetType());

            result.ShouldBeEquivalentTo(item);
        }

        [Fact]
        public void JsonSerializer_List()
        {
            // arrange
            var serializer = new JsonCacheSerializer();
            var items = new List<SerializerPoccoSerializable>()
            {
                SerializerPoccoSerializable.Create(),
                SerializerPoccoSerializable.Create(),
                SerializerPoccoSerializable.Create()
            };

            // act
            var data = serializer.Serialize(items);
            var result = serializer.Deserialize(data, items.GetType());

            result.ShouldBeEquivalentTo(items);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(float.MaxValue)]
        [InlineData(int.MaxValue)]
        [InlineData(long.MaxValue)]
        [InlineData("some string")]
        [ReplaceCulture]
        public void GzJsonSerializer_Primitives<T>(T value)
        {
            // arrange
            var serializer = new GzJsonCacheSerializer();

            // act
            var data = serializer.Serialize(value);
            var result = serializer.Deserialize(data, typeof(T));

            result.Should().Be(value);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(float.MaxValue)]
        [InlineData(int.MaxValue)]
        [InlineData(long.MaxValue)]
        [InlineData("some string")]
        [ReplaceCulture]
        public void GzJsonSerializer_CacheItem_Primitives<T>(T value)
        {
            // arrange
            var serializer = new GzJsonCacheSerializer();
            var item = new CacheItem<T>("key", value);

            // act
            var data = serializer.SerializeCacheItem(item);
            var result = serializer.DeserializeCacheItem<T>(data, typeof(T));

            result.Value.Should().Be(value);
            result.ValueType.Should().Be(item.ValueType);
            result.CreatedUtc.Should().Be(item.CreatedUtc);
            result.ExpirationMode.Should().Be(item.ExpirationMode);
            result.ExpirationTimeout.Should().Be(item.ExpirationTimeout);
            result.Key.Should().Be(item.Key);
            result.LastAccessedUtc.Should().Be(item.LastAccessedUtc);
            result.Region.Should().Be(item.Region);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(float.MaxValue)]
        [InlineData(int.MaxValue)]
        [InlineData(long.MaxValue)]
        [InlineData("some string")]
        [ReplaceCulture]
        public void GzJsonSerializer_CacheItemOfObject_Primitives<T>(T value)
        {
            // arrange
            var serializer = new GzJsonCacheSerializer();
            var item = new CacheItem<object>("key", value);

            // act
            var data = serializer.SerializeCacheItem(item);
            var result = serializer.DeserializeCacheItem<object>(data, typeof(T));

            result.Value.Should().Be(value);
            result.ValueType.Should().Be(item.ValueType);
            result.CreatedUtc.Should().Be(item.CreatedUtc);
            result.ExpirationMode.Should().Be(item.ExpirationMode);
            result.ExpirationTimeout.Should().Be(item.ExpirationTimeout);
            result.Key.Should().Be(item.Key);
            result.LastAccessedUtc.Should().Be(item.LastAccessedUtc);
            result.Region.Should().Be(item.Region);
        }

        [Fact]
        public void GzJsonSerializer_Pocco()
        {
            // arrange
            var serializer = new GzJsonCacheSerializer();
            var item = SerializerPoccoSerializable.Create();

            // act
            var data = serializer.Serialize(item);
            var result = serializer.Deserialize(data, item.GetType());

            result.ShouldBeEquivalentTo(item);
        }

        [Fact]
        public void GzJsonSerializer_CacheItemWithPocco()
        {
            // arrange
            var serializer = new GzJsonCacheSerializer();
            var pocco = SerializerPoccoSerializable.Create();
            var item = new CacheItem<SerializerPoccoSerializable>("key", "region", pocco, ExpirationMode.Absolute, TimeSpan.FromDays(1));

            // act
            var data = serializer.SerializeCacheItem(item);
            var result = serializer.DeserializeCacheItem<SerializerPoccoSerializable>(data, pocco.GetType());

            result.ShouldBeEquivalentTo(item);
        }

        [Fact]
        public void GzJsonSerializer_List()
        {
            // arrange
            var serializer = new GzJsonCacheSerializer();
            var items = new List<SerializerPoccoSerializable>()
            {
                SerializerPoccoSerializable.Create(),
                SerializerPoccoSerializable.Create(),
                SerializerPoccoSerializable.Create()
            };

            // act
            var data = serializer.Serialize(items);
            var result = serializer.Deserialize(data, items.GetType());

            result.ShouldBeEquivalentTo(items);
        }

        [Theory]
        [MemberData("TestCacheManagers")]
        public void Serializer_FullAddGet<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var key = Guid.NewGuid().ToString();
                var pocco = SerializerPoccoSerializable.Create();

                // act
                Action actSet = () =>
                {
                    cache.Add(key, pocco);
                };

                // assert
                actSet.ShouldNotThrow();
                cache.Get<SerializerPoccoSerializable>(key).ShouldBeEquivalentTo(pocco);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(float.MaxValue)]
        [InlineData(int.MaxValue)]
        [InlineData(long.MaxValue)]
        [InlineData("some string")]
        [ReplaceCulture]
        public void ProtoBufSerializer_Primitives<T>(T value)
        {
            // arrange
            var serializer = new ProtoBufSerializer();

            // act
            var data = serializer.Serialize(value);
            var result = serializer.Deserialize(data, typeof(T));

            result.Should().Be(value);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(float.MaxValue)]
        [InlineData(int.MaxValue)]
        [InlineData(long.MaxValue)]
        [InlineData("some string")]
        [ReplaceCulture]
        public void ProtoBufSerializer_CacheItem_Primitives<T>(T value)
        {
            // arrange
            var serializer = new ProtoBufSerializer();
            var item = new CacheItem<T>("key", value);

            // act
            var data = serializer.SerializeCacheItem(item);
            var result = serializer.DeserializeCacheItem<T>(data, typeof(T));

            result.Value.Should().Be(value);
            result.ValueType.Should().Be(item.ValueType);
            result.CreatedUtc.Should().Be(item.CreatedUtc);
            result.ExpirationMode.Should().Be(item.ExpirationMode);
            result.ExpirationTimeout.Should().Be(item.ExpirationTimeout);
            result.Key.Should().Be(item.Key);
            result.LastAccessedUtc.Should().Be(item.LastAccessedUtc);
            result.Region.Should().Be(item.Region);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(float.MaxValue)]
        [InlineData(int.MaxValue)]
        [InlineData(long.MaxValue)]
        [InlineData("some string")]
        [ReplaceCulture]
        public void ProtoBufSerializer_CacheItemOfObject_Primitives<T>(T value)
        {
            // arrange
            var serializer = new ProtoBufSerializer();
            var item = new CacheItem<object>("key", value);

            // act
            var data = serializer.SerializeCacheItem(item);
            var result = serializer.DeserializeCacheItem<object>(data, typeof(T));

            result.Value.Should().Be(value);
            result.ValueType.Should().Be(item.ValueType);
            result.CreatedUtc.Should().Be(item.CreatedUtc);
            result.ExpirationMode.Should().Be(item.ExpirationMode);
            result.ExpirationTimeout.Should().Be(item.ExpirationTimeout);
            result.Key.Should().Be(item.Key);
            result.LastAccessedUtc.Should().Be(item.LastAccessedUtc);
            result.Region.Should().Be(item.Region);
        }

        [Fact]
        public void ProtoBufSerializer_Pocco()
        {
            // arrange
            var serializer = new ProtoBufSerializer();
            var item = SerializerPoccoSerializable.Create();

            // act
            var data = serializer.Serialize(item);
            var result = serializer.Deserialize(data, item.GetType());

            result.ShouldBeEquivalentTo(item);
        }

        [Fact]
        public void ProtoBufSerializer_CacheItemWithPocco()
        {
            // arrange
            var serializer = new ProtoBufSerializer();
            var pocco = SerializerPoccoSerializable.Create();
            var item = new CacheItem<SerializerPoccoSerializable>("key", "region", pocco, ExpirationMode.Absolute, TimeSpan.FromDays(1));

            // act
            var data = serializer.SerializeCacheItem(item);
            var result = serializer.DeserializeCacheItem<SerializerPoccoSerializable>(data, pocco.GetType());

            result.ShouldBeEquivalentTo(item);
        }

        [Fact]
        public void ProtoBufSerializer_List()
        {
            // arrange
            var serializer = new ProtoBufSerializer();
            var items = new List<SerializerPoccoSerializable>()
            {
                SerializerPoccoSerializable.Create(),
                SerializerPoccoSerializable.Create(),
                SerializerPoccoSerializable.Create()
            };

            // act
            var data = serializer.Serialize(items);
            var result = serializer.Deserialize(data, items.GetType());

            result.ShouldBeEquivalentTo(items);
        }

        [Fact]
        public void ProtoBufSerializer_FullAddGet()
        {
            using (var cache = TestManagers.CreateRedisCache(serializer: Serializer.Proto))
            {
                // arrange
                var key = Guid.NewGuid().ToString();
                var pocco = SerializerPoccoSerializable.Create();

                // act
                Action actSet = () =>
                {
                    cache.Add(key, pocco);
                };

                // assert
                actSet.ShouldNotThrow();
                cache.Get<SerializerPoccoSerializable>(key).ShouldBeEquivalentTo(pocco);
            }
        }
    }

#if !DNXCORE50
    [Serializable]
#endif
    [ProtoContract]
    public class SerializerPoccoSerializable
    {
        [ProtoMember(1)]
        public string StringProperty { get; set; }

        [ProtoMember(2)]
        public int IntProperty { get; set; }

        [ProtoMember(3)]
        public string[] StringArrayProperty { get; set; }

        [ProtoMember(4)]
        public IList<string> StringListProperty { get; set; }

        [ProtoMember(5)]
        public IDictionary<string, ChildPocco> ChildDictionaryProperty { get; set; }

        public static SerializerPoccoSerializable Create()
        {
            var rnd = new Random();
            return new SerializerPoccoSerializable()
            {
                StringProperty = DataGenerator.GetString(),
                IntProperty = DataGenerator.GetInt(),
                StringArrayProperty = DataGenerator.GetStrings(),
                StringListProperty = new List<string>(DataGenerator.GetStrings()),
                ChildDictionaryProperty = new Dictionary<string, ChildPocco>()
                {
                    { DataGenerator.GetString(), new ChildPocco() { StringProperty = DataGenerator.GetString() } },
                    { DataGenerator.GetString(), new ChildPocco() { StringProperty = DataGenerator.GetString() } },
                    { DataGenerator.GetString(), new ChildPocco() { StringProperty = DataGenerator.GetString() } },
                    { DataGenerator.GetString(), new ChildPocco() { StringProperty = DataGenerator.GetString() } },
                }
            };
        }
    }

#if !DNXCORE50
    [Serializable]
#endif
    [ProtoContract]
    public class ChildPocco
    {
        [ProtoMember(1)]
        public string StringProperty { get; set; }
    }

    internal class DataGenerator
    {
        private static Random random = new Random();

        public static string GetString() => Guid.NewGuid().ToString();

        public static int GetInt() => random.Next();

        public static string[] GetStrings() => Enumerable.Repeat(0, 100).Select(p => GetString()).ToArray();
    }
}