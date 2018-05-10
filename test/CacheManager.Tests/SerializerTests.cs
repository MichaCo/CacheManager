﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.Core.Logging;
using CacheManager.Serialization.Json;
using CacheManager.Serialization.ProtoBuf;
using FluentAssertions;
using Newtonsoft.Json;
using ProtoBuf;
using Xunit;
using CacheManager.Serialization.Bond;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using CacheManager.Serialization.MessagePack;
using MessagePack;

#if !NETCOREAPP1

using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using CacheManager.Serialization.DataContract;

#endif

namespace CacheManager.Tests
{
    [ExcludeFromCodeCoverage]
    public class SerializerTests
    {
        #region binary serializer

#if !NETCOREAPP1 && !NETCOREAPP2

        [Fact]
        public void BinarySerializer_RespectBinarySerializerSettings()
        {
            var serializationSettings = new BinaryFormatter()
            {
                AssemblyFormat = FormatterAssemblyStyle.Simple,
                FilterLevel = TypeFilterLevel.Low,
                TypeFormat = FormatterTypeStyle.TypesWhenNeeded
            };

            var deserializationSettings = new BinaryFormatter()
            {
                AssemblyFormat = FormatterAssemblyStyle.Full,
                FilterLevel = TypeFilterLevel.Full,
                TypeFormat = FormatterTypeStyle.TypesAlways
            };

            var cache = CacheFactory.Build<string>(
                p => p
                    .WithBinarySerializer(serializationSettings, deserializationSettings)
                    .WithHandle(typeof(SerializerTestCacheHandle)));

            var handle = cache.CacheHandles.ElementAt(0) as SerializerTestCacheHandle;
            var serializer = handle.Serializer as BinaryCacheSerializer;

            serializer.SerializationFormatter.AssemblyFormat.Should().Be(FormatterAssemblyStyle.Simple);
            serializer.SerializationFormatter.FilterLevel.Should().Be(TypeFilterLevel.Low);
            serializer.SerializationFormatter.TypeFormat.Should().Be(FormatterTypeStyle.TypesWhenNeeded);
            serializer.DeserializationFormatter.AssemblyFormat.Should().Be(FormatterAssemblyStyle.Full);
            serializer.DeserializationFormatter.FilterLevel.Should().Be(TypeFilterLevel.Full);
            serializer.DeserializationFormatter.TypeFormat.Should().Be(FormatterTypeStyle.TypesAlways);

            cache.Configuration.SerializerTypeArguments.Length.Should().Be(2);
        }

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

            result.Should().BeEquivalentTo(item);
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

            result.Should().BeEquivalentTo(item);
        }

        [Fact]
        public void BinarySerializer_ObjectCacheItemWithPocco()
        {
            // arrange
            var serializer = new BinaryCacheSerializer();
            var pocco = SerializerPoccoSerializable.Create();
            var item = new CacheItem<object>("key", "region", pocco, ExpirationMode.Absolute, TimeSpan.FromDays(1));

            // act
            var data = serializer.SerializeCacheItem(item);
            var result = serializer.DeserializeCacheItem<object>(data, pocco.GetType());

            result.Should().BeEquivalentTo(item);
        }

        [Fact]
        public void BinarySerializer_CacheItemWithDerivedPocco()
        {
            // arrange
            var serializer = new BinaryCacheSerializer();
            var pocco = DerivedPocco.CreateDerived();
            var item = new CacheItem<SerializerPoccoSerializable>("key", "region", pocco, ExpirationMode.Absolute, TimeSpan.FromDays(1));

            // act
            var data = serializer.SerializeCacheItem(item);
            var result = serializer.DeserializeCacheItem<SerializerPoccoSerializable>(data, pocco.GetType());

            result.Should().BeEquivalentTo(item);
            pocco.Should().BeEquivalentTo(item.Value);
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

            result.Should().BeEquivalentTo(items);
        }

#endif

        #endregion binary serializer

        #region newtonsoft json serializer

        [Fact]
        public void JsonSerializer_RespectJsonSerializerSettings()
        {
            var serializationSettings = new JsonSerializerSettings()
            {
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                FloatFormatHandling = FloatFormatHandling.String
            };

            var deserializationSettings = new JsonSerializerSettings()
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                FloatFormatHandling = FloatFormatHandling.Symbol
            };

            var cache = CacheFactory.Build<string>(
                p => p
                    .WithJsonSerializer(serializationSettings, deserializationSettings)
                    .WithHandle(typeof(SerializerTestCacheHandle)));

            var handle = cache.CacheHandles.ElementAt(0) as SerializerTestCacheHandle;
            var serializer = handle.Serializer as JsonCacheSerializer;

            serializer.SerializationSettings.DateFormatHandling.Should().Be(DateFormatHandling.MicrosoftDateFormat);
            serializer.SerializationSettings.FloatFormatHandling.Should().Be(FloatFormatHandling.String);
            serializer.DeserializationSettings.DateFormatHandling.Should().Be(DateFormatHandling.IsoDateFormat);
            serializer.DeserializationSettings.FloatFormatHandling.Should().Be(FloatFormatHandling.Symbol);

            cache.Configuration.SerializerTypeArguments.Length.Should().Be(2);
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
        [InlineData(long.MaxValue)]
        [InlineData(true)]
        [InlineData(float.MaxValue)]
        [InlineData(int.MaxValue)]
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

            result.Should().BeEquivalentTo(item);
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

            result.Should().BeEquivalentTo(item);
        }

        [Fact]
        public void JsonSerializer_ObjectCacheItemWithPocco()
        {
            // arrange
            var serializer = new JsonCacheSerializer();
            var pocco = SerializerPoccoSerializable.Create();
            var item = new CacheItem<object>("key", "region", pocco, ExpirationMode.Absolute, TimeSpan.FromDays(1));

            // act
            var data = serializer.SerializeCacheItem(item);
            var result = serializer.DeserializeCacheItem<object>(data, pocco.GetType());

            result.Should().BeEquivalentTo(item);
        }

        [Fact]
        public void JsonSerializer_CacheItemWithDerivedPocco()
        {
            // arrange
            var serializer = new JsonCacheSerializer();
            var pocco = DerivedPocco.CreateDerived();
            var item = new CacheItem<SerializerPoccoSerializable>("key", "region", pocco, ExpirationMode.Absolute, TimeSpan.FromDays(1));

            // act
            var data = serializer.SerializeCacheItem(item);
            var result = serializer.DeserializeCacheItem<SerializerPoccoSerializable>(data, pocco.GetType());

            result.Should().BeEquivalentTo(item);
            pocco.Should().BeEquivalentTo(item.Value);
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

            result.Should().BeEquivalentTo(items);
        }

        #endregion newtonsoft json serializer

        #region newtonsoft json with GZ serializer

        [Fact]
        public void GzJsonSerializer_RespectJsonSerializerSettings()
        {
            var serializationSettings = new JsonSerializerSettings()
            {
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                FloatFormatHandling = FloatFormatHandling.String
            };

            var deserializationSettings = new JsonSerializerSettings()
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                FloatFormatHandling = FloatFormatHandling.Symbol
            };

            var cache = CacheFactory.Build<string>(
                p => p
                    .WithGzJsonSerializer(serializationSettings, deserializationSettings)
                    .WithHandle(typeof(SerializerTestCacheHandle)));

            var handle = cache.CacheHandles.ElementAt(0) as SerializerTestCacheHandle;
            var serializer = handle.Serializer as JsonCacheSerializer;

            serializer.SerializationSettings.DateFormatHandling.Should().Be(DateFormatHandling.MicrosoftDateFormat);
            serializer.SerializationSettings.FloatFormatHandling.Should().Be(FloatFormatHandling.String);
            serializer.DeserializationSettings.DateFormatHandling.Should().Be(DateFormatHandling.IsoDateFormat);
            serializer.DeserializationSettings.FloatFormatHandling.Should().Be(FloatFormatHandling.Symbol);

            cache.Configuration.SerializerTypeArguments.Length.Should().Be(2);
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

            result.Should().BeEquivalentTo(item);
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

            result.Should().BeEquivalentTo(item);
        }

        [Fact]
        public void GzJsonSerializer_ObjectCacheItemWithPocco()
        {
            // arrange
            var serializer = new GzJsonCacheSerializer();
            var pocco = SerializerPoccoSerializable.Create();
            var item = new CacheItem<object>("key", "region", pocco, ExpirationMode.Absolute, TimeSpan.FromDays(1));

            // act
            var data = serializer.SerializeCacheItem(item);
            var result = serializer.DeserializeCacheItem<object>(data, pocco.GetType());

            result.Should().BeEquivalentTo(item);
        }

        [Fact]
        public void GzJsonSerializer_CacheItemWithDerivedPocco()
        {
            // arrange
            var serializer = new GzJsonCacheSerializer();
            var pocco = DerivedPocco.CreateDerived();
            var item = new CacheItem<SerializerPoccoSerializable>("key", "region", pocco, ExpirationMode.Absolute, TimeSpan.FromDays(1));

            // act
            var data = serializer.SerializeCacheItem(item);
            var result = serializer.DeserializeCacheItem<SerializerPoccoSerializable>(data, pocco.GetType());

            result.Should().BeEquivalentTo(item);
            pocco.Should().BeEquivalentTo(item.Value);
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

            result.Should().BeEquivalentTo(items);
        }

        #endregion newtonsoft json with GZ serializer

        #region messagepack serializer

        [Fact]
        public void MessagePackSerializer()
        {
            var cache = CacheFactory.Build<string>(
                p => p
                    .WithMessagePackSerializer()
                    .WithHandle(typeof(SerializerTestCacheHandle)));

            var handle = cache.CacheHandles.ElementAt(0) as SerializerTestCacheHandle;
            var serializer = handle.Serializer as MessagePackCacheSerializer;

            cache.Configuration.SerializerTypeArguments.Length.Should().Be(0);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(float.MaxValue)]
        [InlineData(int.MaxValue)]
        [InlineData(long.MaxValue)]
        [InlineData("some string")]
        [ReplaceCulture]
        public void MessagePackSerializer_Primitives<T>(T value)
        {
            // arrange
            var serializer = new MessagePackCacheSerializer();

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
        public void MessagePackSerializer_CacheItem_Primitives<T>(T value)
        {
            // arrange
            var serializer = new MessagePackCacheSerializer();
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
        [InlineData(long.MaxValue)]
        [InlineData(true)]
        [InlineData(float.MaxValue)]
        [InlineData(int.MaxValue)]
        [InlineData("some string")]
        [ReplaceCulture]
        public void MessagePackSerializer_CacheItemOfObject_Primitives<T>(T value)
        {
            // arrange
            var serializer = new MessagePackCacheSerializer();
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
        public void MessagePackSerializer_Pocco()
        {
            // arrange
            var serializer = new MessagePackCacheSerializer();
            var item = SerializerPoccoSerializable.Create();

            // act
            var data = serializer.Serialize(item);
            var result = serializer.Deserialize(data, item.GetType());

            result.Should().BeEquivalentTo(item);
        }

        [Fact]
        public void MessagePackSerializer_CacheItemWithPocco()
        {
            // arrange
            var serializer = new MessagePackCacheSerializer();
            var pocco = SerializerPoccoSerializable.Create();
            var item = new CacheItem<SerializerPoccoSerializable>("key", "region", pocco, ExpirationMode.Absolute, TimeSpan.FromDays(1));

            // act
            var data = serializer.SerializeCacheItem(item);
            var result = serializer.DeserializeCacheItem<SerializerPoccoSerializable>(data, pocco.GetType());

            result.Should().BeEquivalentTo(item);
        }

        [Fact]
        public void MessagePackSerializer_ObjectCacheItemWithPocco()
        {
            // arrange
            var serializer = new MessagePackCacheSerializer();
            var pocco = SerializerPoccoSerializable.Create();
            var item = new CacheItem<object>("key", "region", pocco, ExpirationMode.Absolute, TimeSpan.FromDays(1));

            // act
            var data = serializer.SerializeCacheItem(item);
            var result = serializer.DeserializeCacheItem<object>(data, pocco.GetType());

            result.Should().BeEquivalentTo(item);
        }

        [Fact]
        public void MessagePackSerializer_CacheItemWithDerivedPocco()
        {
            // arrange
            var serializer = new MessagePackCacheSerializer();
            var pocco = DerivedPocco.CreateDerived();
            var item = new CacheItem<SerializerPoccoSerializable>("key", "region", pocco, ExpirationMode.Absolute, TimeSpan.FromDays(1));

            // act
            var data = serializer.SerializeCacheItem(item);
            var result = serializer.DeserializeCacheItem<SerializerPoccoSerializable>(data, pocco.GetType());

            result.Should().BeEquivalentTo(item);
            pocco.Should().BeEquivalentTo(item.Value);
        }

        [Fact]
        public void MessagePackSerializer_List()
        {
            // arrange
            var serializer = new MessagePackCacheSerializer();
            var items = new List<SerializerPoccoSerializable>()
            {
                SerializerPoccoSerializable.Create(),
                SerializerPoccoSerializable.Create(),
                SerializerPoccoSerializable.Create()
            };

            // act
            var data = serializer.Serialize(items);
            var result = serializer.Deserialize(data, items.GetType());

            result.Should().BeEquivalentTo(items);
        }

        #endregion

        #region messagepack with LZ4 serializer

        [Fact]
        public void LZ4MessagePackSerializer()
        {
            var cache = CacheFactory.Build<string>(
                p => p
                    .WithLZ4MessagePackSerializer()
                    .WithHandle(typeof(SerializerTestCacheHandle)));

            var handle = cache.CacheHandles.ElementAt(0) as SerializerTestCacheHandle;
            var serializer = handle.Serializer as MessagePackCacheSerializer;

            cache.Configuration.SerializerTypeArguments.Length.Should().Be(0);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(float.MaxValue)]
        [InlineData(int.MaxValue)]
        [InlineData(long.MaxValue)]
        [InlineData("some string")]
        [ReplaceCulture]
        public void LZ4MessagePackSerializer_Primitives<T>(T value)
        {
            // arrange
            var serializer = new LZ4MessagePackCacheSerializer();

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
        public void LZ4MessagePackSerializer_CacheItem_Primitives<T>(T value)
        {
            // arrange
            var serializer = new LZ4MessagePackCacheSerializer();
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
        [InlineData(long.MaxValue)]
        [InlineData(true)]
        [InlineData(float.MaxValue)]
        [InlineData(int.MaxValue)]
        [InlineData("some string")]
        [ReplaceCulture]
        public void LZ4MessagePackSerializer_CacheItemOfObject_Primitives<T>(T value)
        {
            // arrange
            var serializer = new LZ4MessagePackCacheSerializer();
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
        public void LZ4MessagePackSerializer_Pocco()
        {
            // arrange
            var serializer = new LZ4MessagePackCacheSerializer();
            var item = SerializerPoccoSerializable.Create();

            // act
            var data = serializer.Serialize(item);
            var result = serializer.Deserialize(data, item.GetType());

            result.Should().BeEquivalentTo(item);
        }

        [Fact]
        public void LZ4MessagePackSerializer_CacheItemWithPocco()
        {
            // arrange
            var serializer = new LZ4MessagePackCacheSerializer();
            var pocco = SerializerPoccoSerializable.Create();
            var item = new CacheItem<SerializerPoccoSerializable>("key", "region", pocco, ExpirationMode.Absolute, TimeSpan.FromDays(1));

            // act
            var data = serializer.SerializeCacheItem(item);
            var result = serializer.DeserializeCacheItem<SerializerPoccoSerializable>(data, pocco.GetType());

            result.Should().BeEquivalentTo(item);
        }

        [Fact]
        public void LZ4MessagePackSerializer_ObjectCacheItemWithPocco()
        {
            // arrange
            var serializer = new LZ4MessagePackCacheSerializer();
            var pocco = SerializerPoccoSerializable.Create();
            var item = new CacheItem<object>("key", "region", pocco, ExpirationMode.Absolute, TimeSpan.FromDays(1));

            // act
            var data = serializer.SerializeCacheItem(item);
            var result = serializer.DeserializeCacheItem<object>(data, pocco.GetType());

            result.Should().BeEquivalentTo(item);
        }

        [Fact]
        public void LZ4MessagePackSerializer_CacheItemWithDerivedPocco()
        {
            // arrange
            var serializer = new LZ4MessagePackCacheSerializer();
            var pocco = DerivedPocco.CreateDerived();
            var item = new CacheItem<SerializerPoccoSerializable>("key", "region", pocco, ExpirationMode.Absolute, TimeSpan.FromDays(1));

            // act
            var data = serializer.SerializeCacheItem(item);
            var result = serializer.DeserializeCacheItem<SerializerPoccoSerializable>(data, pocco.GetType());

            result.Should().BeEquivalentTo(item);
            pocco.Should().BeEquivalentTo(item.Value);
        }

        [Fact]
        public void LZ4MessagePackSerializer_List()
        {
            // arrange
            var serializer = new LZ4MessagePackCacheSerializer();
            var items = new List<SerializerPoccoSerializable>()
            {
                SerializerPoccoSerializable.Create(),
                SerializerPoccoSerializable.Create(),
                SerializerPoccoSerializable.Create()
            };

            // act
            var data = serializer.Serialize(items);
            var result = serializer.Deserialize(data, items.GetType());

            result.Should().BeEquivalentTo(items);
        }

        #endregion

#if !NETCOREAPP1

        #region data contract serializer common

        [Fact]
        public void DataContractSerializer_RespectSerializerSettings()
        {
            var serializationSettings = new DataContractSerializerSettings()
            {
                KnownTypes = new[] { typeof(string) }
            };

            var cache = CacheFactory.Build<string>(
                p => p
                    .WithDataContractSerializer(serializationSettings)
                    .WithHandle(typeof(SerializerTestCacheHandle)));

            var handle = cache.CacheHandles.ElementAt(0) as SerializerTestCacheHandle;
            var serializer = handle.Serializer as DataContractCacheSerializer;

            serializer.SerializerSettings.KnownTypes.Should().BeEquivalentTo(new[] { typeof(string) });

            cache.Configuration.SerializerTypeArguments.Length.Should().Be(1);
        }

        [Fact]
        public void DataContractSerializer_Json_RespectSerializerSettings()
        {
            var serializationSettings = new DataContractJsonSerializerSettings()
            {
                KnownTypes = new[] { typeof(string) }
            };

            var cache = CacheFactory.Build<string>(
                p => p
                    .WithDataContractJsonSerializer(serializationSettings)
                    .WithHandle(typeof(SerializerTestCacheHandle)));

            var handle = cache.CacheHandles.ElementAt(0) as SerializerTestCacheHandle;
            var serializer = handle.Serializer as DataContractJsonCacheSerializer;

            serializer.SerializerSettings.KnownTypes.Should().BeEquivalentTo(new[] { typeof(string) });

            cache.Configuration.SerializerTypeArguments.Length.Should().Be(1);
        }

        [Fact]
        public void DataContractSerializer_GzJson_RespectSerializerSettings()
        {
            var serializationSettings = new DataContractJsonSerializerSettings()
            {
                KnownTypes = new[] { typeof(string) }
            };

            var cache = CacheFactory.Build<string>(
                p => p
                    .WithDataContractGzJsonSerializer(serializationSettings)
                    .WithHandle(typeof(SerializerTestCacheHandle)));

            var handle = cache.CacheHandles.ElementAt(0) as SerializerTestCacheHandle;
            var serializer = handle.Serializer as DataContractGzJsonCacheSerializer;

            serializer.SerializerSettings.KnownTypes.Should().BeEquivalentTo(new[] { typeof(string) });

            cache.Configuration.SerializerTypeArguments.Length.Should().Be(1);
        }

        // this test actually failed because the DataContractBinaryCacheSerializer didn't had that ctor for it...
        [Fact]
        public void DataContractSerializer_Binary_RespectSerializerSettings()
        {
            var serializationSettings = new DataContractSerializerSettings()
            {
                KnownTypes = new[] { typeof(string) }
            };

            var cache = CacheFactory.Build<string>(
                p => p
                    .WithDataContractBinarySerializer(serializationSettings)
                    .WithHandle(typeof(SerializerTestCacheHandle)));

            var handle = cache.CacheHandles.ElementAt(0) as SerializerTestCacheHandle;
            var serializer = handle.Serializer as DataContractBinaryCacheSerializer;

            serializer.SerializerSettings.KnownTypes.Should().BeEquivalentTo(new[] { typeof(string) });

            cache.Configuration.SerializerTypeArguments.Length.Should().Be(1);
        }

        #endregion data contract serializer common

        #region data contract serializer

        [Theory]
        [InlineData(true)]
        [InlineData(float.MaxValue)]
        [InlineData(int.MaxValue)]
        [InlineData(long.MaxValue)]
        [InlineData("some string")]
        [ReplaceCulture]
        public void DataContractSerializer_Primitives<T>(T value)
        {
            // arrange
            var serializer = new DataContractCacheSerializer();

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
        public void DataContractSerializer_CacheItem_Primitives<T>(T value)
        {
            // arrange
            var serializer = new DataContractCacheSerializer();
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
        public void DataContractSerializer_CacheItemOfObject_Primitives<T>(T value)
        {
            // arrange
            var serializer = new DataContractCacheSerializer();
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
        public void DataContractSerializer_Pocco()
        {
            // arrange
            var serializer = new DataContractCacheSerializer();
            var item = SerializerPoccoSerializable.Create();

            // act
            var data = serializer.Serialize(item);
            var result = serializer.Deserialize(data, item.GetType());

            result.Should().BeEquivalentTo(item);
        }

        [Fact]
        public void DataContractSerializer_CacheItemWithPocco()
        {
            // arrange
            var serializer = new DataContractCacheSerializer();
            var pocco = SerializerPoccoSerializable.Create();
            var item = new CacheItem<SerializerPoccoSerializable>("key", "region", pocco, ExpirationMode.Absolute, TimeSpan.FromDays(1));

            // act
            var data = serializer.SerializeCacheItem(item);
            var result = serializer.DeserializeCacheItem<SerializerPoccoSerializable>(data, pocco.GetType());

            result.Should().BeEquivalentTo(item);
        }

        [Fact]
        public void DataContractSerializer_List()
        {
            // arrange
            var serializer = new DataContractCacheSerializer();
            var items = new List<SerializerPoccoSerializable>()
            {
                SerializerPoccoSerializable.Create(),
                SerializerPoccoSerializable.Create(),
                SerializerPoccoSerializable.Create()
            };

            // act
            var data = serializer.Serialize(items);
            var result = serializer.Deserialize(data, items.GetType());

            result.Should().BeEquivalentTo(items);
        }

        [Fact]
        public void DataContractSerializer_FullAddGet()
        {
            FullAddGetWithSerializer(Serializer.DataContract);
        }

        #endregion data contract serializer

        #region data contract serializer binary

        [Theory]
        [InlineData(true)]
        [InlineData(float.MaxValue)]
        [InlineData(int.MaxValue)]
        [InlineData(long.MaxValue)]
        [InlineData("some string")]
        [ReplaceCulture]
        public void DataContractBinarySerializer_Primitives<T>(T value)
        {
            // arrange
            var serializer = new DataContractBinaryCacheSerializer();

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
        public void DataContractBinarySerializer_CacheItem_Primitives<T>(T value)
        {
            // arrange
            var serializer = new DataContractBinaryCacheSerializer();
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
        public void DataContractBinarySerializer_CacheItemOfObject_Primitives<T>(T value)
        {
            // arrange
            var serializer = new DataContractBinaryCacheSerializer();
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
        public void DataContractBinarySerializer_Pocco()
        {
            // arrange
            var serializer = new DataContractBinaryCacheSerializer();
            var item = SerializerPoccoSerializable.Create();

            // act
            var data = serializer.Serialize(item);
            var result = serializer.Deserialize(data, item.GetType());

            result.Should().BeEquivalentTo(item);
        }

        [Fact]
        public void DataContractBinarySerializer_CacheItemWithPocco()
        {
            // arrange
            var serializer = new DataContractBinaryCacheSerializer();
            var pocco = SerializerPoccoSerializable.Create();
            var item = new CacheItem<SerializerPoccoSerializable>("key", "region", pocco, ExpirationMode.Absolute, TimeSpan.FromDays(1));

            // act
            var data = serializer.SerializeCacheItem(item);
            var result = serializer.DeserializeCacheItem<SerializerPoccoSerializable>(data, pocco.GetType());

            result.Should().BeEquivalentTo(item);
        }

        [Fact]
        public void DataContractBinarySerializer_List()
        {
            // arrange
            var serializer = new DataContractBinaryCacheSerializer();
            var items = new List<SerializerPoccoSerializable>()
            {
                SerializerPoccoSerializable.Create(),
                SerializerPoccoSerializable.Create(),
                SerializerPoccoSerializable.Create()
            };

            // act
            var data = serializer.Serialize(items);
            var result = serializer.Deserialize(data, items.GetType());

            result.Should().BeEquivalentTo(items);
        }

        [Fact]
        public void DataContractBinarySerializer_FullAddGet()
        {
            FullAddGetWithSerializer(Serializer.DataContractBinary);
        }

        #endregion data contract serializer binary

        #region data contract serializer json

        [Theory]
        [InlineData(true)]
        [InlineData(float.MaxValue)]
        [InlineData(int.MaxValue)]
        [InlineData(long.MaxValue)]
        [InlineData("some string")]
        [ReplaceCulture]
        public void DataContractJsonSerializer_Primitives<T>(T value)
        {
            // arrange
            var serializer = new DataContractJsonCacheSerializer();

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
        public void DataContractJsonSerializer_CacheItem_Primitives<T>(T value)
        {
            // arrange
            var serializer = new DataContractJsonCacheSerializer(new DataContractJsonSerializerSettings()
            {
                //DataContractJsonSerializer serializes DateTime values as Date(1231231313) instead of "2017-11-07T13:09:39.7079187Z".
                //So, I've changed the format to make the test pass.
                DateTimeFormat = new DateTimeFormat("O")
            });
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
        public void DataContractJsonSerializer_CacheItemOfObject_Primitives<T>(T value)
        {
            // arrange
            var serializer = new DataContractJsonCacheSerializer(new DataContractJsonSerializerSettings()
            {
                //DataContractJsonSerializer serializes DateTime values as Date(1231231313) instead of "2017-11-07T13:09:39.7079187Z".
                //So, I've changed the format to make the test pass.
                DateTimeFormat = new DateTimeFormat("O")
            });
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
        public void DataContractJsonSerializer_Pocco()
        {
            // arrange
            var serializer = new DataContractJsonCacheSerializer();
            var item = SerializerPoccoSerializable.Create();

            // act
            var data = serializer.Serialize(item);
            var result = serializer.Deserialize(data, item.GetType());

            result.Should().BeEquivalentTo(item);
        }

        [Fact]
        public void DataContractJsonSerializer_CacheItemWithPocco()
        {
            // arrange
            var serializer = new DataContractJsonCacheSerializer(new DataContractJsonSerializerSettings()
            {
                //DataContractJsonSerializer serializes DateTime values as Date(1231231313) instead of "2017-11-07T13:09:39.7079187Z".
                //So, I've changed the format to make the test pass.
                DateTimeFormat = new DateTimeFormat("O")
            });
            var pocco = SerializerPoccoSerializable.Create();
            var item = new CacheItem<SerializerPoccoSerializable>("key", "region", pocco, ExpirationMode.Absolute, TimeSpan.FromDays(1));

            // act
            var data = serializer.SerializeCacheItem(item);
            var result = serializer.DeserializeCacheItem<SerializerPoccoSerializable>(data, pocco.GetType());

            result.Should().BeEquivalentTo(item);
        }

        [Fact]
        public void DataContractJsonSerializer_List()
        {
            // arrange
            var serializer = new DataContractJsonCacheSerializer();
            var items = new List<SerializerPoccoSerializable>()
            {
                SerializerPoccoSerializable.Create(),
                SerializerPoccoSerializable.Create(),
                SerializerPoccoSerializable.Create()
            };

            // act
            var data = serializer.Serialize(items);
            var result = serializer.Deserialize(data, items.GetType());

            result.Should().BeEquivalentTo(items);
        }

        [Fact]
        public void DataContractJsonSerializer_FullAddGet()
        {
            FullAddGetWithSerializer(Serializer.DataContractJson);
        }

        #endregion data contract serializer json

        #region data contract serializer gz json

        [Theory]
        [InlineData(true)]
        [InlineData(float.MaxValue)]
        [InlineData(int.MaxValue)]
        [InlineData(long.MaxValue)]
        [InlineData("some string")]
        [ReplaceCulture]
        public void DataContractGzJsonSerializer_Primitives<T>(T value)
        {
            // arrange
            var serializer = new DataContractGzJsonCacheSerializer();

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
        public void DataContractGzJsonSerializer_CacheItem_Primitives<T>(T value)
        {
            // arrange
            var serializer = new DataContractGzJsonCacheSerializer(new DataContractJsonSerializerSettings()
            {
                //DataContractJsonSerializer serializes DateTime values as Date(1231231313) instead of "2017-11-07T13:09:39.7079187Z".
                //So, I've changed the format to make the test pass.
                DateTimeFormat = new DateTimeFormat("O")
            });
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
        public void DataContractGzJsonSerializer_CacheItemOfObject_Primitives<T>(T value)
        {
            // arrange
            var serializer = new DataContractGzJsonCacheSerializer(new DataContractJsonSerializerSettings()
            {
                //DataContractJsonSerializer serializes DateTime values as Date(1231231313) instead of "2017-11-07T13:09:39.7079187Z".
                //So, I've changed the format to make the test pass.
                DateTimeFormat = new DateTimeFormat("O")
            });
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
        public void DataContractGzJsonSerializer_Pocco()
        {
            // arrange
            var serializer = new DataContractGzJsonCacheSerializer();
            var item = SerializerPoccoSerializable.Create();

            // act
            var data = serializer.Serialize(item);
            var result = serializer.Deserialize(data, item.GetType());

            result.Should().BeEquivalentTo(item);
        }

        [Fact]
        public void DataContractGzJsonSerializer_CacheItemWithPocco()
        {
            // arrange
            var serializer = new DataContractGzJsonCacheSerializer(new DataContractJsonSerializerSettings()
            {
                //DataContractJsonSerializer serializes DateTime values as Date(1231231313) instead of "2017-11-07T13:09:39.7079187Z".
                //So, I've changed the format to make the test pass.
                DateTimeFormat = new DateTimeFormat("O")
            });
            var pocco = SerializerPoccoSerializable.Create();
            var item = new CacheItem<SerializerPoccoSerializable>("key", "region", pocco, ExpirationMode.Absolute, TimeSpan.FromDays(1));

            // act
            var data = serializer.SerializeCacheItem(item);
            var result = serializer.DeserializeCacheItem<SerializerPoccoSerializable>(data, pocco.GetType());

            result.Should().BeEquivalentTo(item);
        }

        [Fact]
        public void DataContractGzJsonSerializer_List()
        {
            // arrange
            var serializer = new DataContractGzJsonCacheSerializer();
            var items = new List<SerializerPoccoSerializable>()
            {
                SerializerPoccoSerializable.Create(),
                SerializerPoccoSerializable.Create(),
                SerializerPoccoSerializable.Create()
            };

            // act
            var data = serializer.Serialize(items);
            var result = serializer.Deserialize(data, items.GetType());

            result.Should().BeEquivalentTo(items);
        }

        [Fact]
        public void DataContractGzJsonSerializer_FullAddGet()
        {
            FullAddGetWithSerializer(Serializer.DataContractGzJson);
        }

        #endregion data contract serializer gz json

#endif

        #region protobuf serializer

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

            result.Should().BeEquivalentTo(item);
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

            result.Should().BeEquivalentTo(item);
        }

        [Fact]
        public void ProtoBufSerializer_ObjectCacheItemWithPocco()
        {
            // arrange
            var serializer = new ProtoBufSerializer();
            var pocco = SerializerPoccoSerializable.Create();
            var item = new CacheItem<object>("key", "region", pocco, ExpirationMode.Absolute, TimeSpan.FromDays(1));

            // act
            var data = serializer.SerializeCacheItem(item);
            var result = serializer.DeserializeCacheItem<object>(data, pocco.GetType());

            result.Should().BeEquivalentTo(item);
        }

        [Fact]
        public void ProtoBufSerializer_CacheItemWithDerivedPocco()
        {
            // arrange
            var serializer = new ProtoBufSerializer();
            var pocco = DerivedPocco.CreateDerived();
            var item = new CacheItem<SerializerPoccoSerializable>("key", "region", pocco, ExpirationMode.Absolute, TimeSpan.FromDays(1));

            // act
            var data = serializer.SerializeCacheItem(item);
            var result = serializer.DeserializeCacheItem<SerializerPoccoSerializable>(data, pocco.GetType());

            result.Should().BeEquivalentTo(item);
            pocco.Should().BeEquivalentTo(item.Value);
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

            result.Should().BeEquivalentTo(items);
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
                actSet.Should().NotThrow();
                cache.Get<SerializerPoccoSerializable>(key).Should().BeEquivalentTo(pocco);
            }
        }

        #endregion protobuf serializer

        #region Bond binary serializer

        [Theory]
        [InlineData(true)]
        [InlineData(float.MaxValue)]
        [InlineData(int.MaxValue)]
        [InlineData(long.MaxValue)]
        [InlineData("some string")]
        [ReplaceCulture]
        public void BondBinarySerializer_CacheItem_Primitives<T>(T value)
        {
            // arrange
            var serializer = new BondCompactBinaryCacheSerializer();
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
        public void BondBinarySerializer_CacheItemOfObject_Primitives<T>(T value)
        {
            // arrange
            var serializer = new BondCompactBinaryCacheSerializer();
            var item = new CacheItem<object>("key", value);

            // act
            var data = serializer.SerializeCacheItem(item);

            // not using the type defined, expecting the serializer object to store the actualy value type correctly...
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
        public void BondBinarySerializer_Pocco()
        {
            // arrange
            var serializer = new BondCompactBinaryCacheSerializer();
            var item = SerializerPoccoSerializable.Create();

            // act
            var data = serializer.Serialize(item);
            var result = serializer.Deserialize(data, item.GetType());

            result.Should().BeEquivalentTo(item);
        }

        [Fact]
        public void BondBinarySerializer_DoesNotSupportPrimitives()
        {
            // arrange
            var serializer = new BondCompactBinaryCacheSerializer();

            // act
            var data = serializer.Serialize("test");
            Action act = () => serializer.Deserialize(data, typeof(string));

            data.Length.Should().Be(1);
            data[0].Should().Be(0);
            act.Should().Throw<Exception>("Bond does not support primitives.");
        }

        [Fact]
        public void BondBinarySerializer_CacheItemWithPocco()
        {
            // arrange
            var serializer = new BondCompactBinaryCacheSerializer();
            var pocco = SerializerPoccoSerializable.Create();
            var item = new CacheItem<SerializerPoccoSerializable>("key", "region", pocco, ExpirationMode.Absolute, TimeSpan.FromDays(1));

            var x = serializer.Serialize(pocco);

            // act
            var data = serializer.SerializeCacheItem(item);
            var result = serializer.DeserializeCacheItem<SerializerPoccoSerializable>(data, pocco.GetType());

            result.Should().BeEquivalentTo(item);
        }

        [Fact]
        public void BondBinarySerializer_ObjectCacheItemWithPocco()
        {
            // arrange
            var serializer = new BondCompactBinaryCacheSerializer();
            var pocco = SerializerPoccoSerializable.Create();
            var item = new CacheItem<object>("key", "region", pocco, ExpirationMode.Absolute, TimeSpan.FromDays(1));

            var x = serializer.Serialize(pocco);

            // act
            var data = serializer.SerializeCacheItem(item);
            var result = serializer.DeserializeCacheItem<object>(data, pocco.GetType());

            result.Should().BeEquivalentTo(item);
        }

        [Fact]
        public void BondBinarySerializer_CacheItemWithDerivedPocco()
        {
            // arrange
            var serializer = new BondCompactBinaryCacheSerializer();
            var pocco = DerivedPocco.CreateDerived();
            var item = new CacheItem<SerializerPoccoSerializable>("key", "region", pocco, ExpirationMode.Absolute, TimeSpan.FromDays(1));

            // act
            var data = serializer.SerializeCacheItem(item);
            var result = serializer.DeserializeCacheItem<SerializerPoccoSerializable>(data, pocco.GetType());

            result.Should().BeEquivalentTo(item);
            pocco.Should().BeEquivalentTo(item.Value);
        }

        [Fact]
        [Trait("category", "Redis")]
        public void BondBinarySerializer_FullAddGet()
        {
            using (var cache = TestManagers.CreateRedisCache(serializer: Serializer.BondBinary))
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
                actSet.Should().NotThrow();
                cache.Get<SerializerPoccoSerializable>(key).Should().BeEquivalentTo(pocco);
            }
        }

        #endregion Bond binary serializer

        [Theory]
        [ClassData(typeof(TestCacheManagers))]
        public void Serializer_FullAddGet<T>(T cache)
            where T : ICacheManager<object>
        {
            using (cache)
            {
                // arrange
                var key = Guid.NewGuid().ToString();
                var pocco = SerializerPoccoSerializable.Create();

                // act
                Func<bool> actSet = () => cache.Add(key, pocco);

                // assert
                actSet().Should().BeTrue("Should add the key");
                cache.Get<SerializerPoccoSerializable>(key).Should().BeEquivalentTo(pocco);
            }
        }

        private void FullAddGetWithSerializer(Serializer serializer)
        {
            using (var cache = TestManagers.CreateRedisCache(serializer: serializer))
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
                actSet.Should().NotThrow();
                cache.Get<SerializerPoccoSerializable>(key).Should().BeEquivalentTo(pocco);
            }
        }

        private static class DataGenerator
        {
            private static Random random = new Random();

            public static string GetString() => Guid.NewGuid().ToString();

            public static int GetInt() => random.Next();

            public static string[] GetStrings() => Enumerable.Repeat(0, 100).Select(p => GetString()).ToArray();
        }

        private class SerializerTestCacheHandle : BaseCacheHandle<string>
        {
            public SerializerTestCacheHandle(CacheManagerConfiguration managerConfiguration, CacheHandleConfiguration configuration, ICacheSerializer serializer)
                : base(managerConfiguration, configuration)
            {
                this.Serializer = serializer;
            }

            public ICacheSerializer Serializer { get; set; }

            public override int Count
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            protected override ILogger Logger
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override void Clear()
            {
                throw new NotImplementedException();
            }

            public override void ClearRegion(string region)
            {
                throw new NotImplementedException();
            }

            public override bool Exists(string key)
            {
                throw new NotImplementedException();
            }

            public override bool Exists(string key, string region)
            {
                throw new NotImplementedException();
            }

            protected override bool AddInternalPrepared(CacheItem<string> item)
            {
                throw new NotImplementedException();
            }

            protected override CacheItem<string> GetCacheItemInternal(string key)
            {
                throw new NotImplementedException();
            }

            protected override CacheItem<string> GetCacheItemInternal(string key, string region)
            {
                throw new NotImplementedException();
            }

            protected override void PutInternalPrepared(CacheItem<string> item)
            {
                throw new NotImplementedException();
            }

            protected override bool RemoveInternal(string key)
            {
                throw new NotImplementedException();
            }

            protected override bool RemoveInternal(string key, string region)
            {
                throw new NotImplementedException();
            }
        }

#if !NETCOREAPP1

        [Serializable]
#endif
        [ProtoContract]
        [ProtoInclude(20, typeof(DerivedPocco))]
        [Bond.Schema]
        [MessagePackObject]
        private class SerializerPoccoSerializable
        {
            [ProtoMember(1)]
            [Bond.Id(1)]
            [Key(0)]
            public string StringProperty { get; set; }

            [ProtoMember(2)]
            [Bond.Id(2)]
            [Key(1)]
            public int IntProperty { get; set; }

            [ProtoMember(3)]
            [Bond.Id(3)]
            [Key(2)]
            public string[] StringArrayProperty { get; set; }

            [ProtoMember(4)]
            [Bond.Id(4)]
            [Key(3)]
            public List<string> StringListProperty { get; set; }

            [ProtoMember(5)]
            [Bond.Id(5)]
            [Key(4)]
            public Dictionary<string, ChildPocco> ChildDictionaryProperty { get; set; }

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

#if !NETCOREAPP1

        [Serializable]
#endif
        [ProtoContract]
        [Bond.Schema]
        [MessagePackObject]
        private class ChildPocco
        {
            [ProtoMember(1)]
            [Bond.Id(1)]
            [Key(0)]
            public string StringProperty { get; set; }
        }

#if !NETCOREAPP1

        [Serializable]
#endif
        [ProtoContract]
        [Bond.Schema]
        [MessagePackObject]
        private class DerivedPocco : SerializerPoccoSerializable
        {
            [ProtoMember(6)]
            [Bond.Id(6)]
            [Key(5)]
            public string DerivedStringProperty { get; set; }

            public static DerivedPocco CreateDerived()
            {
                var rnd = new Random();
                return new DerivedPocco()
                {
                    StringProperty = DataGenerator.GetString(),
                    IntProperty = DataGenerator.GetInt(),
                    StringArrayProperty = DataGenerator.GetStrings(),
                    StringListProperty = new List<string>(DataGenerator.GetStrings()),
                    DerivedStringProperty = DataGenerator.GetString(),
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
    }
}
