using System;
using CacheManager.Core;
using CacheManager.Core.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CacheManager.Serialization
{
    public class CacheItemConverter<T> : JsonConverter
    {
        private const string KeyName = "Key";
        private const string RegionName = "Region";
        private const string ValueName = "Value";
        private const string CreatedUtcName = "CreatedUtc";
        private const string LastAccessedUtcName = "LastAccessedUtc";
        private const string ExpirationModeName = "ExpirationMode";
        private const string ExpirationTimeoutName = "ExpirationTimeout";
        private const string ValueTypeName = "ValueType";

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(CacheItem<T>).IsAssignableFrom(objectType);
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            Read(reader);

            string key = string.Empty;
            string region = string.Empty;
            T value = default(T);
            DateTime created = default(DateTime);
            DateTime lastAccess = default(DateTime);
            ExpirationMode expiration = default(ExpirationMode);
            TimeSpan timeout = default(TimeSpan);
            string typeName = string.Empty;

            while (reader.TokenType == JsonToken.PropertyName)
            {
                string propertyName = reader.Value.ToString();
                if (string.Equals(propertyName, KeyName, StringComparison.Ordinal))
                {
                    Read(reader);
                    key = serializer.Deserialize<string>(reader);
                }
                else if (string.Equals(propertyName, RegionName, StringComparison.Ordinal))
                {
                    Read(reader);
                    region = serializer.Deserialize<string>(reader);
                }
                else if (string.Equals(propertyName, ValueName, StringComparison.Ordinal))
                {
                    Read(reader);
                    value = serializer.Deserialize<T>(reader);
                }
                else if (string.Equals(propertyName, CreatedUtcName, StringComparison.Ordinal))
                {
                    Read(reader);
                    created = serializer.Deserialize<DateTime>(reader);
                }
                else if (string.Equals(propertyName, LastAccessedUtcName, StringComparison.Ordinal))
                {
                    Read(reader);
                    lastAccess = serializer.Deserialize<DateTime>(reader);
                }
                else if (string.Equals(propertyName, ExpirationModeName, StringComparison.Ordinal))
                {
                    Read(reader);
                    expiration = serializer.Deserialize<ExpirationMode>(reader);
                }
                else if (string.Equals(propertyName, ExpirationTimeoutName, StringComparison.Ordinal))
                {
                    Read(reader);
                    timeout = serializer.Deserialize<TimeSpan>(reader);
                }
                else if (string.Equals(propertyName, ValueTypeName, StringComparison.Ordinal))
                {
                    Read(reader);
                    typeName = serializer.Deserialize<string>(reader);
                }
                else
                {
                    reader.Skip();
                }

                Read(reader);
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new JsonSerializationException("CacheItem.Key did not deserialize but must be null.");
            }

            // type conversion based on the type stored in the cache item.
            // this will back cast even long to int, if the target was int and Newtonsoft json 
            // deserialized it as long... so this should help a lot for compatibility...
            // could of course cause serious issues somewhere...
            // anyways, if the target object is too complex or cannot be deserialized. One should use CacheItem<string> and serialize him/her self.
            var targetType = Type.GetType(typeName);

            if (value.GetType() != targetType)
            {
                var jValue = value as JObject;
                if (jValue != null)
                {
                    value = (T)jValue.ToObject(Type.GetType(typeName));
                }
                else if (value.GetType() == typeof(string) && targetType == typeof(byte[]))
                {
                    value = (T)(object)Convert.FromBase64String(value.ToString());
                }
                else
                {
                    value = (T)Convert.ChangeType(value, targetType);
                }
            }

            if (string.IsNullOrWhiteSpace(region))
            {
                return new CacheItem<T>(key, value, expiration, timeout)
                {
                    LastAccessedUtc = lastAccess
                };
            }

            return new CacheItem<T>(key, value, region, expiration, timeout)
            {
                LastAccessedUtc = lastAccess
            };
        }

        private static void Read(JsonReader reader)
        {
            if (!reader.Read())
                throw new JsonSerializationException("Unexpected end when reading CacheItem.");
        }
    }
}