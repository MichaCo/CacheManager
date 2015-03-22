using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;

namespace CacheManager.Core.Configuration
{
    /// <summary>
    /// Configuration section for the CacheManager.
    /// </summary>
    /// <example><![CDATA[
    /// <cacheManager>
    ///     <redis>
    ///         <redisOption id="redis1"
    ///                         database="0"
    ///                         { connnectionString="redis0:6379,redis1:6380,keepAlive=180,allowAdmin=true" }
    ///                         OR
    ///                         { 
    ///                          allowAdmin="true|false"
    ///                          password=""
    ///                          ssl="true|false"
    ///                          sslHost="string"
    ///                          connectionTimeout="ms" 
    ///                         } 
    ///                 >
    ///             <endpoints>
    ///                 <endpoint host="127.0.0.1" port="6379" />
    ///                 <endpoint host="127.0.0.1" port="6380"/>
    ///             </endpoints>
    ///         </redisOption>
    ///     </redis>
    ///     <managers>
    ///         <cache name="cache1" updateMode="Up">
    ///             <handle name="Handle1" ref="MemoryCacheHandle" timeout="1" expirationMode="Sliding" />
    ///             <handle name="Handle2" ref="AzureDataCacheHandle" timeout="50" expirationMode="Sliding" />
    ///         </cache>
    ///         <cache name="cache2">
    ///             <handle name="NamedMemCache" useNamedCache="true" ref="MemoryCacheHandle" timeout="10" expirationMode="Absolute" />
    ///         </cache>
    ///     </managers>
    ///     <cacheHandles>
    ///         <handleDef id="MemoryCacheHandle" type="CacheManager.SystemRuntimeCaching.MemoryCacheHandle, CacheManager.SystemRuntimeCaching"
    ///             defaultTimeout="20" defaultExpirationMode="Sliding"/>
    ///         <handleDef id="AzureDataCacheHandle" type="CacheManager.WindowsAzureCaching.AzureDataCacheHandle, CacheManager.WindowsAzureCaching"/>
    ///     </cacheHandles>
    /// </cacheManager>
    /// ]]>
    /// </example>
    public sealed class CacheManagerSection : ConfigurationSection
    {
        public const string SectionName = "cacheManager";

        private const string ManagersName = "managers";

        private const string HandlesName = "cacheHandles";

        private const string RedisName = "redis";

        [ConfigurationProperty(ManagersName)]
        [ConfigurationCollection(typeof(CacheManagerCollection), AddItemName = "cache")]
        public CacheManagerCollection CacheManagers
        {
            get
            {
                return (CacheManagerCollection)base[ManagersName];
            }
        }

        [ConfigurationProperty(HandlesName)]
        [ConfigurationCollection(typeof(CacheHandleDefinitionCollection), AddItemName = "handleDef")]
        public CacheHandleDefinitionCollection CacheHandleDefinitions
        {
            get
            {
                return (CacheHandleDefinitionCollection)base[HandlesName];
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Redis"), ConfigurationProperty(RedisName)]
        [ConfigurationCollection(typeof(RedisOptionCollection), AddItemName = "redisOption")]
        public RedisOptionCollection RedisOptions
        {
            get
            {
                return (RedisOptionCollection)base[RedisName];
            }
        }

        [ConfigurationProperty("xmlns", IsRequired = false)]
        public string Xmlns { get; set; }
    }

    public sealed class CacheManagerCollection : ConfigurationElementCollection, IEnumerable<CacheManagerHandleCollection>
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new CacheManagerHandleCollection();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((CacheManagerHandleCollection)element).Name;
        }

        public new IEnumerator<CacheManagerHandleCollection> GetEnumerator()
        {
            var enu = base.GetEnumerator();
            enu.Reset();
            while (enu.MoveNext())
            {
                yield return (CacheManagerHandleCollection)enu.Current;
            }
        }
    }

    public sealed class CacheManagerHandleCollection : ConfigurationElementCollection, IEnumerable<CacheManagerHandle>
    {
        private const string NameKey = "name";
        private const string UpdateModeKey = "updateMode";
        private const string EnablePerformanceCountersKey = "enablePerformanceCounters";
        private const string EnableStatisticsKey = "enableStatistics";
        private const string MaxRetriesKey = "maxRetries";
        private const string RetryTimeoutKey = "retryTimeout";

        public CacheManagerHandleCollection()
        {
            AddElementName = "handle";
        }

        [ConfigurationProperty(NameKey, IsKey = true, IsRequired = true)]
        public string Name
        {
            get
            {
                return (string)this[NameKey];
            }
            set
            {
                this[NameKey] = value;
            }
        }

        [ConfigurationProperty(UpdateModeKey, IsRequired = false, DefaultValue = CacheUpdateMode.Up)]
        public CacheUpdateMode UpdateMode
        {
            get
            {
                return (CacheUpdateMode)this[UpdateModeKey];
            }
            set
            {
                this[UpdateModeKey] = value;
            }
        }

        [ConfigurationProperty(EnablePerformanceCountersKey, IsRequired = false, DefaultValue = false)]
        public bool EnablePerformanceCounters
        {
            get
            {
                return (bool)this[EnablePerformanceCountersKey];
            }
            set
            {
                this[EnablePerformanceCountersKey] = value;
            }
        }

        [ConfigurationProperty(EnableStatisticsKey, IsRequired = false, DefaultValue = true)]
        public bool EnableStatistics
        {
            get
            {
                return (bool)this[EnableStatisticsKey];
            }
            set
            {
                this[EnableStatisticsKey] = value;
            }
        }
        
        [ConfigurationProperty(MaxRetriesKey, IsRequired = false)]
        public int? MaximumRetries
        {
            get
            {
                return (int?)base[MaxRetriesKey];
            }
            set
            {
                this[MaxRetriesKey] = value;
            }
        }

        [ConfigurationProperty(RetryTimeoutKey, IsRequired = false)]
        public int? RetryTimeout
        {
            get
            {
                return (int?)base[RetryTimeoutKey];
            }
            set
            {
                this[RetryTimeoutKey] = value;
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new CacheManagerHandle();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((CacheManagerHandle)element).Name;
        }

        public new IEnumerator<CacheManagerHandle> GetEnumerator()
        {
            var enu = base.GetEnumerator();
            enu.Reset();
            while (enu.MoveNext())
            {
                yield return (CacheManagerHandle)enu.Current;
            }
        }
    }

    public sealed class CacheManagerHandle : ConfigurationElement
    {
        [ConfigurationProperty("name", IsKey = true, IsRequired = true)]
        public string Name
        {
            get
            {
                return (string)this["name"];
            }
            set
            {
                this["name"] = value;
            }
        }

        [ConfigurationProperty("ref", IsRequired = true)]
        public string RefHandleId
        {
            get
            {
                return (string)this["ref"];
            }
            set
            {
                this["ref"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the default timeout for the cache handle. If not overruled by the cache manager configuration, this
        /// value will be used instead. If nothing is defined, no expiration will be used.
        /// <para>
        /// It is possible to define timeout in hours minutes or seconds by having a number + suffix,
        /// e.g. 10h means 10 hours, 5m means 5 minutes, 23s means 23 seconds. 
        /// </para>
        /// If no suffix is defined, minutes will be used.
        /// </summary>
        [ConfigurationProperty("timeout", IsRequired = false)]
        public string Timeout
        {
            get
            {
                return (string)this["timeout"];
            }
            set
            {
                this["timeout"] = value;
            }
        }

        [ConfigurationProperty("expirationMode", IsRequired = false, DefaultValue = ExpirationMode.None)]
        public ExpirationMode ExpirationMode
        {
            get
            {
                return (ExpirationMode)this["expirationMode"];
            }
            set
            {
                this["expirationMode"] = value;
            }
        }
    }

    public sealed class CacheHandleDefinitionCollection : ConfigurationElementCollection, IEnumerable<CacheHandleDefinition>
    {
        public CacheHandleDefinitionCollection()
        {
            AddElementName = "handleDef";
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new CacheHandleDefinition();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((CacheHandleDefinition)element).Id;
        }

        public new IEnumerator<CacheHandleDefinition> GetEnumerator()
        {
            var enu = base.GetEnumerator();
            enu.Reset();
            while (enu.MoveNext())
            {
                yield return (CacheHandleDefinition)enu.Current;
            }
        }
    }

    public sealed class CacheHandleDefinition : ConfigurationElement
    {
        [ConfigurationProperty("id", IsKey = true, IsRequired = true)]
        public string Id
        {
            get
            {
                return (string)this["id"];
            }
            set
            {
                this["id"] = value;
            }
        }

        [ConfigurationProperty("type", IsRequired = true)]
        [TypeConverter(typeof(TypeNameConverter))]
        public Type HandleType
        {
            get
            {
                return (Type)this["type"];
            }
            set
            {
                this["type"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the default timeout for the cache handle. If not overruled by the cache manager configuration, this
        /// value will be used instead. If nothing is defined, no expiration will be used.
        /// <para>
        /// It is possible to define timeout in hours minutes or seconds by having a number + suffix,
        /// e.g. 10h means 10 hours, 5m means 5 minutes, 23s means 23 seconds.
        /// </para>
        /// If no suffix is defined, minutes will be used.
        /// </summary>
        [ConfigurationProperty("defaultTimeout", IsRequired = false)]
        public string DefaultTimeout
        {
            get
            {
                return (string)this["defaultTimeout"];
            }
            set
            {
                this["defaultTimeout"] = value;
            }
        }

        [ConfigurationProperty("defaultExpirationMode", IsRequired = false, DefaultValue = ExpirationMode.None)]
        public ExpirationMode DefaultExpirationMode
        {
            get
            {
                return (ExpirationMode)this["defaultExpirationMode"];
            }
            set
            {
                this["defaultExpirationMode"] = value;
            }
        }
    }

    public sealed class Endpoint : ConfigurationElement
    {
        [ConfigurationProperty("host", IsRequired = true)]
        public string Host
        {
            get
            {
                return (string)this["host"];
            }
            set
            {
                this["host"] = value;
            }
        }

        [ConfigurationProperty("port", IsRequired = true)]
        public int Port
        {
            get
            {
                return (int)this["port"];
            }
            set
            {
                this["port"] = value;
            }
        }
    }

    public sealed class EndpointCollection : ConfigurationElementCollection, IEnumerable<Endpoint>
    {
        public EndpointCollection()
        {
            AddElementName = "endpoint";
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new Endpoint();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            var elem = ((Endpoint)element);
            return elem.Host + ":" + elem.Port;
        }

        public new IEnumerator<Endpoint> GetEnumerator()
        {
            var enu = base.GetEnumerator();
            enu.Reset();
            while (enu.MoveNext())
            {
                yield return (Endpoint)enu.Current;
            }
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Redis")]
    public sealed class RedisOptions : ConfigurationElement
    {
        private const string EndpointsName = "endpoints";

        [ConfigurationProperty(EndpointsName)]
        [ConfigurationCollection(typeof(EndpointCollection), AddItemName = "endpoint")]
        public EndpointCollection Endpoints
        {
            get
            {
                return (EndpointCollection)base[EndpointsName];
            }
        }

        [ConfigurationProperty("id", IsKey = true, IsRequired = true)]
        public string Id
        {
            get
            {
                return (string)this["id"];
            }
            set
            {
                this["id"] = value;
            }
        }

        [ConfigurationProperty("connectionString", IsRequired = false)]
        public string ConnectionString
        {
            get
            {
                return (string)this["connectionString"];
            }
            set
            {
                this["connectionString"] = value;
            }
        }

        [ConfigurationProperty("allowAdmin", IsRequired = false, DefaultValue = false)]
        public bool AllowAdmin
        {
            get
            {
                return (bool)this["allowAdmin"];
            }
            set
            {
                this["allowAdmin"] = value;
            }
        }

        [ConfigurationProperty("password", IsRequired = false)]
        public string Password
        {
            get
            {
                return (string)this["password"];
            }
            set
            {
                this["password"] = value;
            }
        }

        [ConfigurationProperty("ssl", IsRequired = false, DefaultValue = false)]
        public bool Ssl
        {
            get
            {
                return (bool)this["ssl"];
            }
            set
            {
                this["ssl"] = value;
            }
        }

        [ConfigurationProperty("sslHost", IsRequired = false)]
        public string SslHost
        {
            get
            {
                return (string)this["sslHost"];
            }
            set
            {
                this["sslHost"] = value;
            }
        }

        [ConfigurationProperty("connectionTimeout", IsRequired = false, DefaultValue = 5000)]
        public int ConnectionTimeout
        {
            get
            {
                return (int)this["connectionTimeout"];
            }
            set
            {
                this["connectionTimeout"] = value;
            }
        }

        [ConfigurationProperty("database", IsRequired = false, DefaultValue = 0)]
        public int Database
        {
            get
            {
                return (int)this["database"];
            }
            set
            {
                this["database"] = value;
            }
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Redis")]
    public sealed class RedisOptionCollection : ConfigurationElementCollection, IEnumerable<RedisOptions>
    {
        public RedisOptionCollection()
        {
            AddElementName = "redisOption";
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new RedisOptions();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((RedisOptions)element).Id;
        }

        public new IEnumerator<RedisOptions> GetEnumerator()
        {
            var enu = base.GetEnumerator();
            enu.Reset();
            while (enu.MoveNext())
            {
                yield return (RedisOptions)enu.Current;
            }
        }
    }
}