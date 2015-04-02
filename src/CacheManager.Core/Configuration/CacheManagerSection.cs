using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;

namespace CacheManager.Core.Configuration
{
    public sealed class CacheHandleDefinition : ConfigurationElement
    {
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

        /// <summary>
        /// Gets or sets the default timeout for the cache handle. If not overruled by the cache
        /// manager configuration, this value will be used instead. If nothing is defined, no
        /// expiration will be used.
        /// <para>
        /// It is possible to define timeout in hours minutes or seconds by having a number +
        /// suffix, e.g. 10h means 10 hours, 5m means 5 minutes, 23s means 23 seconds.
        /// </para>If no suffix is defined, minutes will be used.
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
    }

    public sealed class CacheHandleDefinitionCollection : ConfigurationElementCollection, IEnumerable<CacheHandleDefinition>
    {
        public CacheHandleDefinitionCollection()
        {
            AddElementName = "handleDef";
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

        protected override ConfigurationElement CreateNewElement()
        {
            return new CacheHandleDefinition();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((CacheHandleDefinition)element).Id;
        }
    }

    public sealed class CacheManagerCollection : ConfigurationElementCollection, IEnumerable<CacheManagerHandleCollection>
    {
        public new IEnumerator<CacheManagerHandleCollection> GetEnumerator()
        {
            var enu = base.GetEnumerator();
            enu.Reset();
            while (enu.MoveNext())
            {
                yield return (CacheManagerHandleCollection)enu.Current;
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new CacheManagerHandleCollection();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((CacheManagerHandleCollection)element).Name;
        }
    }

    public sealed class CacheManagerHandle : ConfigurationElement
    {
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

        [ConfigurationProperty("isBackPlateSource", IsRequired = false, DefaultValue = false)]
        public bool IsBackPlateSource
        {
            get
            {
                return (bool)this["isBackPlateSource"];
            }
            set
            {
                this["isBackPlateSource"] = value;
            }
        }

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
        /// Gets or sets the default timeout for the cache handle. If not overruled by the cache
        /// manager configuration, this value will be used instead. If nothing is defined, no
        /// expiration will be used.
        /// <para>
        /// It is possible to define timeout in hours minutes or seconds by having a number +
        /// suffix, e.g. 10h means 10 hours, 5m means 5 minutes, 23s means 23 seconds.
        /// </para>If no suffix is defined, minutes will be used.
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
    }

    public sealed class CacheManagerHandleCollection : ConfigurationElementCollection, IEnumerable<CacheManagerHandle>
    {
        private const string BackPlateNameKey = "backPlateName";
        private const string BackPlateTypeKey = "backPlateType";
        private const string EnablePerformanceCountersKey = "enablePerformanceCounters";
        private const string EnableStatisticsKey = "enableStatistics";
        private const string MaxRetriesKey = "maxRetries";
        private const string NameKey = "name";
        private const string RetryTimeoutKey = "retryTimeout";
        private const string UpdateModeKey = "updateMode";

        public CacheManagerHandleCollection()
        {
            AddElementName = "handle";
        }

        [ConfigurationProperty(BackPlateNameKey, IsRequired = false)]
        public string BackPlateName
        {
            get
            {
                return (string)base[BackPlateNameKey];
            }
            set
            {
                this[BackPlateNameKey] = value;
            }
        }

        [ConfigurationProperty(BackPlateTypeKey, IsRequired = false)]
        public string BackPlateType
        {
            get
            {
                return (string)base[BackPlateTypeKey];
            }
            set
            {
                this[BackPlateTypeKey] = value;
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

        public new IEnumerator<CacheManagerHandle> GetEnumerator()
        {
            var enu = base.GetEnumerator();
            enu.Reset();
            while (enu.MoveNext())
            {
                yield return (CacheManagerHandle)enu.Current;
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
    }

    /// <summary>
    /// Configuration section for the CacheManager.
    /// </summary>
    /// <example>
    /// <![CDATA[ <cacheManager> <managers> <cache name="cache1" updateMode="Up"> <handle
    /// name="Handle1" ref="MemoryCacheHandle" timeout="1" expirationMode="Sliding" /> <handle
    /// name="Handle2" ref="AzureDataCacheHandle" timeout="50" expirationMode="Sliding" /> </cache>
    /// <cache name="cache2"> <handle name="NamedMemCache" useNamedCache="true"
    /// ref="MemoryCacheHandle" timeout="10" expirationMode="Absolute" /> </cache> </managers>
    /// <cacheHandles> <handleDef id="MemoryCacheHandle"
    /// type="CacheManager.SystemRuntimeCaching.MemoryCacheHandle,
    /// CacheManager.SystemRuntimeCaching" defaultTimeout="20" defaultExpirationMode="Sliding"/>
    /// <handleDef id="AzureDataCacheHandle"
    /// type="CacheManager.WindowsAzureCaching.AzureDataCacheHandle,
    /// CacheManager.WindowsAzureCaching"/> </cacheHandles> </cacheManager> ]]>
    /// </example>
    public sealed class CacheManagerSection : ConfigurationSection
    {
        public const string DefaultSectionName = "cacheManager";

        private const string HandlesName = "cacheHandles";
        private const string ManagersName = "managers";
        private const string RedisName = "redis";

        [ConfigurationProperty(HandlesName)]
        [ConfigurationCollection(typeof(CacheHandleDefinitionCollection), AddItemName = "handleDef")]
        public CacheHandleDefinitionCollection CacheHandleDefinitions
        {
            get
            {
                return (CacheHandleDefinitionCollection)base[HandlesName];
            }
        }

        [ConfigurationProperty(ManagersName)]
        [ConfigurationCollection(typeof(CacheManagerCollection), AddItemName = "cache")]
        public CacheManagerCollection CacheManagers
        {
            get
            {
                return (CacheManagerCollection)base[ManagersName];
            }
        }

        [ConfigurationProperty("xmlns", IsRequired = false)]
        public string Xmlns { get; set; }
    }
}