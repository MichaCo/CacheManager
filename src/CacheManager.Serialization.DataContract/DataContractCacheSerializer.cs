using System;
using System.Runtime.Serialization;

namespace CacheManager.Serialization.DataContract
{
    /// <summary>
    /// This class uses <c>DataContractSerializer</c> for (de)serialization.
    /// </summary>
    public class DataContractCacheSerializer : DataContractCacheSerializerBase<DataContractSerializerSettings>
    {
        /// <summary>
        /// Creates instance of <c>DataContractCacheSerializer</c>.
        /// </summary>
        public DataContractCacheSerializer() : this(new DataContractSerializerSettings())
        {
        }

        /// <summary>
        /// Creates instance of <c>DataContractCacheSerializer</c>.
        /// </summary>
        /// <param name="serializerSettings">The settings for <c>DataContractSerializer</c>.</param>
        public DataContractCacheSerializer(DataContractSerializerSettings serializerSettings = null) : base(serializerSettings)
        {
        }

        /// <inheritdoc/>
        protected override XmlObjectSerializer GetSerializer(Type target)
        {
            if (SerializerSettings == null)
            {
                return new DataContractSerializer(target);
            }
            else
            {
                return new DataContractSerializer(target, SerializerSettings);
            }
        }
    }
}
