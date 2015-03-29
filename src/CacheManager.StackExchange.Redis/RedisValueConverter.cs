using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using StackRedis = StackExchange.Redis;

namespace CacheManager.Redis
{
    // this looks strange but yes it has a reason.
    // I could use a serializer to get and set values from redis
    // but using the "native" supported types from Stackexchange.Redis, instead of using a 
    // serializer is up to 10x faster... so its more than worth the effort...

    // basically I have to cast the values to and from RedisValue which has implicit conversions to/from
    // those types defined internally...
    // I cannot simply cast to my TCacheValue, because its generic, and not defined as class or struct or anything...
    // so there is basically no other way
    internal interface IRedisValueConverter<T>
    {
        StackRedis.RedisValue ToRedisValue(T value);
        T FromRedisValue(StackRedis.RedisValue value, string valueType);
    }

    internal class RedisValueConverter :
        IRedisValueConverter<byte[]>,
        IRedisValueConverter<string>,
        IRedisValueConverter<int>,
        IRedisValueConverter<double>,
        IRedisValueConverter<bool>,
        IRedisValueConverter<long>,
        IRedisValueConverter<object>
    {

        private static readonly Type byteArrayType = typeof(byte[]);
        private static readonly Type stringType = typeof(string);
        private static readonly Type intType = typeof(int);
        private static readonly Type doubleType = typeof(double);
        private static readonly Type boolType = typeof(bool);
        private static readonly Type longType = typeof(long);

        StackRedis.RedisValue IRedisValueConverter<byte[]>.ToRedisValue(byte[] value)
        {
            return value;
        }

        byte[] IRedisValueConverter<byte[]>.FromRedisValue(StackRedis.RedisValue value, string valueType)
        {
            return value;
        }

        StackRedis.RedisValue IRedisValueConverter<string>.ToRedisValue(string value)
        {
            return value;
        }

        string IRedisValueConverter<string>.FromRedisValue(StackRedis.RedisValue value, string valueType)
        {
            return value;
        }

        StackRedis.RedisValue IRedisValueConverter<int>.ToRedisValue(int value)
        {
            return value;
        }

        int IRedisValueConverter<int>.FromRedisValue(StackRedis.RedisValue value, string valueType)
        {
            return (int)value;
        }

        StackRedis.RedisValue IRedisValueConverter<double>.ToRedisValue(double value)
        {
            return value;
        }

        double IRedisValueConverter<double>.FromRedisValue(StackRedis.RedisValue value, string valueType)
        {
            return (double)value;
        }

        StackRedis.RedisValue IRedisValueConverter<bool>.ToRedisValue(bool value)
        {
            return value;
        }

        bool IRedisValueConverter<bool>.FromRedisValue(StackRedis.RedisValue value, string valueType)
        {
            return (bool)value;
        }

        StackRedis.RedisValue IRedisValueConverter<long>.ToRedisValue(long value)
        {
            return value;
        }

        long IRedisValueConverter<long>.FromRedisValue(StackRedis.RedisValue value, string valueType)
        {
            return (long)value;
        }

        // for object this could be epcial because the value could be any of the supported values
        // or any king of object...
        // to also have the performance benefit from the known types, lets try to use it for 
        // object based cache, too
        StackRedis.RedisValue IRedisValueConverter<object>.ToRedisValue(object value)
        {
            var valueType = value.GetType();
            if (valueType == byteArrayType)
            {
                var converter = (IRedisValueConverter<byte[]>)this;
                return converter.ToRedisValue((byte[])value);
            }
            else if (valueType == stringType)
            {
                var converter = (IRedisValueConverter<string>)this;
                return converter.ToRedisValue((string)value);
            }
            else if (valueType == intType)
            {
                var converter = (IRedisValueConverter<int>)this;
                return converter.ToRedisValue((int)value);
            }
            else if (valueType == doubleType)
            {
                var converter = (IRedisValueConverter<double>)this;
                return converter.ToRedisValue((double)value);
            }
            else if (valueType == boolType)
            {
                var converter = (IRedisValueConverter<bool>)this;
                return converter.ToRedisValue((bool)value);
            }
            else if (valueType == longType)
            {
                var converter = (IRedisValueConverter<long>)this;
                return converter.ToRedisValue((long)value);
            }

            return ToBytes(value);
        }

        object IRedisValueConverter<object>.FromRedisValue(StackRedis.RedisValue value, string valueType)
        {
            if (valueType == byteArrayType.FullName)
            {
                var converter = (IRedisValueConverter<byte[]>)this;
                return converter.FromRedisValue(value, valueType);
            }
            else if (valueType == stringType.FullName)
            {
                var converter = (IRedisValueConverter<string>)this;
                return converter.FromRedisValue(value, valueType);
            }
            else if (valueType == intType.FullName)
            {
                var converter = (IRedisValueConverter<int>)this;
                return converter.FromRedisValue(value, valueType);
            }
            else if (valueType == doubleType.FullName)
            {
                var converter = (IRedisValueConverter<double>)this;
                return converter.FromRedisValue(value, valueType);
            }
            else if (valueType == boolType.FullName)
            {
                var converter = (IRedisValueConverter<bool>)this;
                return converter.FromRedisValue(value, valueType);
            }
            else if (valueType == longType.FullName)
            {
                var converter = (IRedisValueConverter<long>)this;
                return converter.FromRedisValue(value, valueType);
            }

            return FromBytes<object>(value);
        }

        public static byte[] ToBytes(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                binaryFormatter.Serialize(memoryStream, obj);
                byte[] objectDataAsStream = memoryStream.ToArray();
                return objectDataAsStream;
            }
        }

        public static T FromBytes<T>(byte[] stream)
        {
            if (stream == null)
            {
                return default(T);
            }

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream(stream))
            {
                return (T)binaryFormatter.Deserialize(memoryStream);
            }
        }
    }
}