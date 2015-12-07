using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.Serialization.Json;
using FluentAssertions;
using Xunit;

namespace CacheManager.Tests
{
    [ExcludeFromCodeCoverage]
#if NET40
    [Trait("Framework", "NET40")]
#else
    [Trait("Framework", "NET45")]
#endif
    public class SerializerTests
    {
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
    }

    [Serializable]
    public class SerializerPoccoSerializable
    {
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

        public string StringProperty { get; set; }
        public int IntProperty { get; set; }
        public string[] StringArrayProperty { get; set; }
        public IList<string> StringListProperty { get; set; }
        public IDictionary<string, ChildPocco> ChildDictionaryProperty { get; set; }
    }

    [Serializable]
    public class ChildPocco
    {
        public string StringProperty { get; set; }
    }

    class DataGenerator
    {
        static Random random = new Random();

        public static string GetString() => Guid.NewGuid().ToString();
        public static int GetInt() => random.Next();
        public static string[] GetStrings() => Enumerable.Repeat(0, 100).Select(p => GetString()).ToArray();
    }
}
