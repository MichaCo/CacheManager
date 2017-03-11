using System;
using System.Collections;
using System.Linq;
using CacheManager.Core.Internal;
using StackExchange.Redis;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Redis
{
    internal interface IRedisValueConverter
    {
        RedisValue ToRedisValue<T>(T value);

        T FromRedisValue<T>(RedisValue value, string valueType);
    }

    internal interface IRedisValueConverter<T>
    {
        RedisValue ToRedisValue(T value);

        T FromRedisValue(RedisValue value, string valueType);
    }

    internal class RedisValueConverter :
        IRedisValueConverter,
        IRedisValueConverter<byte[]>,
        IRedisValueConverter<byte>,
        IRedisValueConverter<string>,
        IRedisValueConverter<int>,
        IRedisValueConverter<uint>,
        IRedisValueConverter<short>,
        IRedisValueConverter<ushort>,
        IRedisValueConverter<float>,
        IRedisValueConverter<double>,
        IRedisValueConverter<bool>,
        IRedisValueConverter<long>,
        IRedisValueConverter<ulong>,
        IRedisValueConverter<char>,
        IRedisValueConverter<object>
    {
        private static readonly Type ByteArrayType = typeof(byte[]);
        private static readonly Type ByteType = typeof(byte);
        private static readonly Type StringType = typeof(string);
        private static readonly Type IntType = typeof(int);
        private static readonly Type UIntType = typeof(uint);
        private static readonly Type ShortType = typeof(short);
        private static readonly Type UShortType = typeof(ushort);
        private static readonly Type SingleType = typeof(float);
        private static readonly Type DoubleType = typeof(double);
        private static readonly Type BoolType = typeof(bool);
        private static readonly Type LongType = typeof(long);
        private static readonly Type ULongType = typeof(ulong);
        private static readonly Type CharType = typeof(char);
        private readonly ICacheSerializer serializer;

        public RedisValueConverter(ICacheSerializer serializer)
        {
            NotNull(serializer, nameof(serializer));

            this.serializer = serializer;
        }

        RedisValue IRedisValueConverter<byte[]>.ToRedisValue(byte[] value) => value;

        byte[] IRedisValueConverter<byte[]>.FromRedisValue(RedisValue value, string valueType) => value;

        RedisValue IRedisValueConverter<byte>.ToRedisValue(byte value) => value;

        byte IRedisValueConverter<byte>.FromRedisValue(RedisValue value, string valueType) => (byte)value;

        RedisValue IRedisValueConverter<string>.ToRedisValue(string value) => value;

        string IRedisValueConverter<string>.FromRedisValue(RedisValue value, string valueType) => value;

        RedisValue IRedisValueConverter<int>.ToRedisValue(int value) => value;

        int IRedisValueConverter<int>.FromRedisValue(RedisValue value, string valueType) => (int)value;

        RedisValue IRedisValueConverter<uint>.ToRedisValue(uint value) => value;

        uint IRedisValueConverter<uint>.FromRedisValue(RedisValue value, string valueType) => (uint)value;

        RedisValue IRedisValueConverter<short>.ToRedisValue(short value) => value;

        short IRedisValueConverter<short>.FromRedisValue(RedisValue value, string valueType) => (short)value;

        RedisValue IRedisValueConverter<ushort>.ToRedisValue(ushort value) => value;

        ushort IRedisValueConverter<ushort>.FromRedisValue(RedisValue value, string valueType) => (ushort)value;

        RedisValue IRedisValueConverter<float>.ToRedisValue(float value) => (double)value;

        float IRedisValueConverter<float>.FromRedisValue(RedisValue value, string valueType) => (float)(double)value;

        RedisValue IRedisValueConverter<double>.ToRedisValue(double value) => value;

        double IRedisValueConverter<double>.FromRedisValue(RedisValue value, string valueType) => (double)value;

        RedisValue IRedisValueConverter<bool>.ToRedisValue(bool value) => value;

        bool IRedisValueConverter<bool>.FromRedisValue(RedisValue value, string valueType) => (bool)value;

        RedisValue IRedisValueConverter<long>.ToRedisValue(long value) => value;

        long IRedisValueConverter<long>.FromRedisValue(RedisValue value, string valueType) => (long)value;

        // ulong can exceed the supported lenght of storing integers (which is signed 64bit integer)
        // also, even if we do not exceed long.MaxValue, the SA client stores it as double for no aparent reason => cast to long fixes it.
        RedisValue IRedisValueConverter<ulong>.ToRedisValue(ulong value) => value > long.MaxValue ? (RedisValue)value.ToString() : checked((long)value);

        ulong IRedisValueConverter<ulong>.FromRedisValue(RedisValue value, string valueType) => ulong.Parse(value);

        RedisValue IRedisValueConverter<char>.ToRedisValue(char value) => value;

        char IRedisValueConverter<char>.FromRedisValue(RedisValue value, string valueType) => (char)value;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Scope = "member", Target = "CacheManager.Redis.RedisValueConverter.#CacheManager.Redis.IRedisValueConverter`1<System.Object>.ToRedisValue(System.Object)", Justification = "For performance reasons we don't do checks at this point. Also, its internally used only.")]
        RedisValue IRedisValueConverter<object>.ToRedisValue(object value)
        {
            var valueType = value.GetType();
            if (valueType == ByteArrayType)
            {
                var converter = (IRedisValueConverter<byte[]>)this;
                return converter.ToRedisValue((byte[])value);
            }
            else if (valueType == ByteType)
            {
                var converter = (IRedisValueConverter<byte>)this;
                return converter.ToRedisValue((byte)value);
            }
            else if (valueType == StringType)
            {
                var converter = (IRedisValueConverter<string>)this;
                return converter.ToRedisValue((string)value);
            }
            else if (valueType == IntType)
            {
                var converter = (IRedisValueConverter<int>)this;
                return converter.ToRedisValue((int)value);
            }
            else if (valueType == UIntType)
            {
                var converter = (IRedisValueConverter<uint>)this;
                return converter.ToRedisValue((uint)value);
            }
            else if (valueType == ShortType)
            {
                var converter = (IRedisValueConverter<short>)this;
                return converter.ToRedisValue((short)value);
            }
            else if (valueType == UShortType)
            {
                var converter = (IRedisValueConverter<ushort>)this;
                return converter.ToRedisValue((ushort)value);
            }
            else if (valueType == SingleType)
            {
                var converter = (IRedisValueConverter<float>)this;
                return converter.ToRedisValue((float)value);
            }
            else if (valueType == DoubleType)
            {
                var converter = (IRedisValueConverter<double>)this;
                return converter.ToRedisValue((double)value);
            }
            else if (valueType == BoolType)
            {
                var converter = (IRedisValueConverter<bool>)this;
                return converter.ToRedisValue((bool)value);
            }
            else if (valueType == LongType)
            {
                var converter = (IRedisValueConverter<long>)this;
                return converter.ToRedisValue((long)value);
            }
            else if (valueType == ULongType)
            {
                var converter = (IRedisValueConverter<ulong>)this;
                return converter.ToRedisValue((ulong)value);
            }
            else if (valueType == CharType)
            {
                var converter = (IRedisValueConverter<char>)this;
                return converter.ToRedisValue((char)value);
            }

            return this.serializer.Serialize(value);
        }

        object IRedisValueConverter<object>.FromRedisValue(RedisValue value, string type)
        {
            var valueType = TypeCache.GetType(type);

            if (valueType == ByteArrayType)
            {
                var converter = (IRedisValueConverter<byte[]>)this;
                return converter.FromRedisValue(value, type);
            }
            else if (valueType == ByteType)
            {
                var converter = (IRedisValueConverter<byte>)this;
                return converter.FromRedisValue(value, type);
            }
            else if (valueType == StringType)
            {
                var converter = (IRedisValueConverter<string>)this;
                return converter.FromRedisValue(value, type);
            }
            else if (valueType == IntType)
            {
                var converter = (IRedisValueConverter<int>)this;
                return converter.FromRedisValue(value, type);
            }
            else if (valueType == UIntType)
            {
                var converter = (IRedisValueConverter<uint>)this;
                return converter.FromRedisValue(value, type);
            }
            else if (valueType == ShortType)
            {
                var converter = (IRedisValueConverter<short>)this;
                return converter.FromRedisValue(value, type);
            }
            else if (valueType == UShortType)
            {
                var converter = (IRedisValueConverter<ushort>)this;
                return converter.FromRedisValue(value, type);
            }
            else if (valueType == SingleType)
            {
                var converter = (IRedisValueConverter<float>)this;
                return converter.FromRedisValue(value, type);
            }
            else if (valueType == DoubleType)
            {
                var converter = (IRedisValueConverter<double>)this;
                return converter.FromRedisValue(value, type);
            }
            else if (valueType == BoolType)
            {
                var converter = (IRedisValueConverter<bool>)this;
                return converter.FromRedisValue(value, type);
            }
            else if (valueType == LongType)
            {
                var converter = (IRedisValueConverter<long>)this;
                return converter.FromRedisValue(value, type);
            }
            else if (valueType == ULongType)
            {
                var converter = (IRedisValueConverter<ulong>)this;
                return converter.FromRedisValue(value, type);
            }
            else if (valueType == CharType)
            {
                var converter = (IRedisValueConverter<char>)this;
                return converter.FromRedisValue(value, type);
            }

            return this.Deserialize(value, type);
        }

        public RedisValue ToRedisValue<T>(T value) => this.serializer.Serialize(value);

        public T FromRedisValue<T>(RedisValue value, string valueType) => (T)this.Deserialize(value, valueType);

        private object Deserialize(RedisValue value, string valueType)
        {
            var type = TypeCache.GetType(valueType);
            EnsureNotNull(type, "Type could not be loaded, {0}.", valueType);

            return this.serializer.Deserialize(value, type);
        }
    }
}