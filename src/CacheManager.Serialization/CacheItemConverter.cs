using System;
using CacheManager.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CacheManager.Serialization
{
    /// <summary>
    /// A <see cref="JsonConverter"/> for cache items.
    /// <para>
    /// This was experimental and not in use at the moment.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The cache item's value type.</typeparam>
    public class CacheItemConverter<T> : JsonConverter
    {
        private const string CreatedUtcName = "CreatedUtc";
        private const string ExpirationModeName = "ExpirationMode";
        private const string ExpirationTimeoutName = "ExpirationTimeout";
        private const string KeyName = "Key";
        private const string LastAccessedUtcName = "LastAccessedUtc";
        private const string RegionName = "Region";
        private const string ValueName = "Value";
        private const string ValueTypeName = "ValueType";

        /// <summary>
        /// Gets a value indicating whether this <see cref="T:Newtonsoft.Json.JsonConverter" /> can read JSON.
        /// </summary>
        /// <value>
        /// <c>true</c> if this <see cref="T:Newtonsoft.Json.JsonConverter" /> can read JSON; otherwise, <c>false</c>.
        /// </value>
        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="T:Newtonsoft.Json.JsonConverter" /> can write JSON.
        /// </summary>
        /// <value>
        /// <c>true</c> if this <see cref="T:Newtonsoft.Json.JsonConverter" /> can write JSON; otherwise, <c>false</c>.
        /// </value>
        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// <c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            return typeof(CacheItem<T>).IsAssignableFrom(objectType);
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="T:Newtonsoft.Json.JsonReader" /> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing value of object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>
        /// The object value.
        /// </returns>
        /// <exception cref="Newtonsoft.Json.JsonSerializationException">CacheItem.Key did not deserialize but must be null.</exception>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

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

            // type conversion based on the type stored in the cache item. this will back cast even
            // long to int, if the target was int and Newtonsoft json deserialized it as long... so
            // this should help a lot for compatibility... could of course cause serious issues
            // somewhere... anyways, if the target object is too complex or cannot be deserialized.
            // One should use CacheItem<string> and serialize him/her self.
            var targetType = Type.GetType(typeName);

            if (value.GetType() != targetType)
            {
                var jasonValue = value as JObject;
                if (jasonValue != null)
                {
                    value = (T)jasonValue.ToObject(Type.GetType(typeName));
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

        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="T:Newtonsoft.Json.JsonWriter" /> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <exception cref="System.NotSupportedException">This method is not supported.</exception>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Reads the specified reader.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <exception cref="Newtonsoft.Json.JsonSerializationException">Unexpected end when reading CacheItem.</exception>
        private static void Read(JsonReader reader)
        {
            if (!reader.Read())
            {
                throw new JsonSerializationException("Unexpected end when reading CacheItem.");
            }
        }
    }
}