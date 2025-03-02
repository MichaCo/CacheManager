using System;
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
        private static readonly Type _byteArrayType = typeof(byte[]);
        private static readonly Type _byteType = typeof(byte);
        private static readonly Type _stringType = typeof(string);
        private static readonly Type _intType = typeof(int);
        private static readonly Type _uIntType = typeof(uint);
        private static readonly Type _shortType = typeof(short);
        private static readonly Type _uShortType = typeof(ushort);
        private static readonly Type _singleType = typeof(float);
        private static readonly Type _doubleType = typeof(double);
        private static readonly Type _boolType = typeof(bool);
        private static readonly Type _longType = typeof(long);
        private static readonly Type _uLongType = typeof(ulong);
        private static readonly Type _charType = typeof(char);
        private readonly ICacheSerializer _serializer;

        public RedisValueConverter(ICacheSerializer serializer)
        {
            NotNull(serializer, nameof(serializer));

            _serializer = serializer;
        }

        RedisValue IRedisValueConverter<byte[]>.ToRedisValue(byte[] value) => value;

        byte[] IRedisValueConverter<byte[]>.FromRedisValue(RedisValue value, string valueType) => value;

        RedisValue IRedisValueConverter<byte>.ToRedisValue(byte value) => (int)value;

        byte IRedisValueConverter<byte>.FromRedisValue(RedisValue value, string valueType) => (byte)(int)value;

        RedisValue IRedisValueConverter<string>.ToRedisValue(string value) => value;

        string IRedisValueConverter<string>.FromRedisValue(RedisValue value, string valueType) => value;

        RedisValue IRedisValueConverter<int>.ToRedisValue(int value) => value;

        int IRedisValueConverter<int>.FromRedisValue(RedisValue value, string valueType) => (int)value;

        RedisValue IRedisValueConverter<uint>.ToRedisValue(uint value) => value;

        uint IRedisValueConverter<uint>.FromRedisValue(RedisValue value, string valueType) => (uint)value;

        RedisValue IRedisValueConverter<short>.ToRedisValue(short value) => value;

        short IRedisValueConverter<short>.FromRedisValue(RedisValue value, string valueType) => (short)value;

        RedisValue IRedisValueConverter<ushort>.ToRedisValue(ushort value) => (int)value;

        ushort IRedisValueConverter<ushort>.FromRedisValue(RedisValue value, string valueType) => (ushort)(int)value;

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

        RedisValue IRedisValueConverter<char>.ToRedisValue(char value) => (uint)value;

        char IRedisValueConverter<char>.FromRedisValue(RedisValue value, string valueType) => (char)(uint)value;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Scope = "member", Target = "CacheManager.Redis.RedisValueConverter.#CacheManager.Redis.IRedisValueConverter`1<System.Object>.ToRedisValue(System.Object)", Justification = "For performance reasons we don't do checks at this point. Also, its internally used only.")]
        RedisValue IRedisValueConverter<object>.ToRedisValue(object value)
        {
            var valueType = value.GetType();
            if (valueType == _byteArrayType)
            {
                var converter = (IRedisValueConverter<byte[]>)this;
                return converter.ToRedisValue((byte[])value);
            }
            else if (valueType == _byteType)
            {
                var converter = (IRedisValueConverter<byte>)this;
                return converter.ToRedisValue((byte)value);
            }
            else if (valueType == _stringType)
            {
                var converter = (IRedisValueConverter<string>)this;
                return converter.ToRedisValue((string)value);
            }
            else if (valueType == _intType)
            {
                var converter = (IRedisValueConverter<int>)this;
                return converter.ToRedisValue((int)value);
            }
            else if (valueType == _uIntType)
            {
                var converter = (IRedisValueConverter<uint>)this;
                return converter.ToRedisValue((uint)value);
            }
            else if (valueType == _shortType)
            {
                var converter = (IRedisValueConverter<short>)this;
                return converter.ToRedisValue((short)value);
            }
            else if (valueType == _uShortType)
            {
                var converter = (IRedisValueConverter<ushort>)this;
                return converter.ToRedisValue((ushort)value);
            }
            else if (valueType == _singleType)
            {
                var converter = (IRedisValueConverter<float>)this;
                return converter.ToRedisValue((float)value);
            }
            else if (valueType == _doubleType)
            {
                var converter = (IRedisValueConverter<double>)this;
                return converter.ToRedisValue((double)value);
            }
            else if (valueType == _boolType)
            {
                var converter = (IRedisValueConverter<bool>)this;
                return converter.ToRedisValue((bool)value);
            }
            else if (valueType == _longType)
            {
                var converter = (IRedisValueConverter<long>)this;
                return converter.ToRedisValue((long)value);
            }
            else if (valueType == _uLongType)
            {
                var converter = (IRedisValueConverter<ulong>)this;
                return converter.ToRedisValue((ulong)value);
            }
            else if (valueType == _charType)
            {
                var converter = (IRedisValueConverter<char>)this;
                return converter.ToRedisValue((char)value);
            }

            return _serializer.Serialize(value);
        }

        object IRedisValueConverter<object>.FromRedisValue(RedisValue value, string type)
        {
            var valueType = TypeCache.GetType(type);

            if (valueType == _byteArrayType)
            {
                var converter = (IRedisValueConverter<byte[]>)this;
                return converter.FromRedisValue(value, type);
            }
            else if (valueType == _byteType)
            {
                var converter = (IRedisValueConverter<byte>)this;
                return converter.FromRedisValue(value, type);
            }
            else if (valueType == _stringType)
            {
                var converter = (IRedisValueConverter<string>)this;
                return converter.FromRedisValue(value, type);
            }
            else if (valueType == _intType)
            {
                var converter = (IRedisValueConverter<int>)this;
                return converter.FromRedisValue(value, type);
            }
            else if (valueType == _uIntType)
            {
                var converter = (IRedisValueConverter<uint>)this;
                return converter.FromRedisValue(value, type);
            }
            else if (valueType == _shortType)
            {
                var converter = (IRedisValueConverter<short>)this;
                return converter.FromRedisValue(value, type);
            }
            else if (valueType == _uShortType)
            {
                var converter = (IRedisValueConverter<ushort>)this;
                return converter.FromRedisValue(value, type);
            }
            else if (valueType == _singleType)
            {
                var converter = (IRedisValueConverter<float>)this;
                return converter.FromRedisValue(value, type);
            }
            else if (valueType == _doubleType)
            {
                var converter = (IRedisValueConverter<double>)this;
                return converter.FromRedisValue(value, type);
            }
            else if (valueType == _boolType)
            {
                var converter = (IRedisValueConverter<bool>)this;
                return converter.FromRedisValue(value, type);
            }
            else if (valueType == _longType)
            {
                var converter = (IRedisValueConverter<long>)this;
                return converter.FromRedisValue(value, type);
            }
            else if (valueType == _uLongType)
            {
                var converter = (IRedisValueConverter<ulong>)this;
                return converter.FromRedisValue(value, type);
            }
            else if (valueType == _charType)
            {
                var converter = (IRedisValueConverter<char>)this;
                return converter.FromRedisValue(value, type);
            }

            return Deserialize(value, type);
        }

        public RedisValue ToRedisValue<T>(T value) => _serializer.Serialize(value);

        public T FromRedisValue<T>(RedisValue value, string valueType) => (T)Deserialize(value, valueType);

        private object Deserialize(RedisValue value, string valueType)
        {
            var type = TypeCache.GetType(valueType);
            EnsureNotNull(type, "Type could not be loaded, {0}.", valueType);

            return _serializer.Deserialize(value, type);
        }
    }
}
