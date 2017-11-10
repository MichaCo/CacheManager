using System;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Xml;

namespace CacheManager.Serialization.DataContract
{
    /// <summary>
    /// This class uses <c>DataContractJsonSerialzer</c> for (de)serialization.
    /// </summary>
    public class DataContractJsonCacheSerializer : DataContractCacheSerializerBase<DataContractJsonSerializerSettings>
    {
        /// <summary>
        /// Creates instance of <c>DataContractJsonCacheSerializer</c>.
        /// </summary>
        /// <param name="serializerSettings">Serializer's settings</param>
        public DataContractJsonCacheSerializer(DataContractJsonSerializerSettings serializerSettings = null) : base(serializerSettings)
        {

        }
        /// <inheritdoc/>
        protected override XmlObjectSerializer GetSerializer(Type target)
        {
            if (this.SerializerSettings == null)
            {
                return new DataContractJsonSerializer(target);
            }
            else
            {
                return new DataContractJsonSerializer(target, this.SerializerSettings);
            }
        }
    }
}
