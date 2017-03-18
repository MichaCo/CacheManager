namespace CacheManager.Core.Configuration
{
#if !NETSTANDARD

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Configuration;

    /// <summary>
    /// Configuration section for the CacheManager.
    /// </summary>
    public sealed class CacheManagerSection : ConfigurationSection
    {
        /// <summary>
        /// The default section name.
        /// </summary>
        public const string DefaultSectionName = "cacheManager";

        private const string HandlesName = "cacheHandles";
        private const string ManagersName = "managers";
        private const string RedisName = "redis";

        /// <summary>
        /// Gets the cache handle definitions.
        /// </summary>
        /// <value>The cache handle definitions.</value>
        [ConfigurationProperty(HandlesName)]
        [ConfigurationCollection(typeof(CacheHandleDefinitionCollection), AddItemName = "handleDef")]
        public CacheHandleDefinitionCollection CacheHandleDefinitions => (CacheHandleDefinitionCollection)this[HandlesName];

        /// <summary>
        /// Gets the cache managers.
        /// </summary>
        /// <value>The cache managers.</value>
        [ConfigurationProperty(ManagersName)]
        [ConfigurationCollection(typeof(CacheManagerCollection), AddItemName = "cache")]
        public CacheManagerCollection CacheManagers => (CacheManagerCollection)this[ManagersName];

        /// <summary>
        /// Gets or sets the XMLNS.
        /// </summary>
        /// <value>The XMLNS.</value>
        [ConfigurationProperty("xmlns", IsRequired = false)]
        public string Xmlns { get; set; }
    }

    /// <summary>
    /// Part of the section defining the available cache handles.
    /// </summary>
    public sealed class CacheHandleDefinition : ConfigurationElement
    {
        /// <summary>
        /// Gets or sets the default expiration mode.
        /// </summary>
        /// <value>The default expiration mode.</value>
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
        /// <value>The default timeout.</value>
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

        /// <summary>
        /// Gets or sets the type of the handle.
        /// </summary>
        /// <value>The type of the handle.</value>
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
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
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

    /// <summary>
    /// The collection of cache handle definitions.
    /// </summary>
    public sealed class CacheHandleDefinitionCollection : ConfigurationElementCollection, IEnumerable<CacheHandleDefinition>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CacheHandleDefinitionCollection"/> class.
        /// </summary>
        public CacheHandleDefinitionCollection()
        {
            AddElementName = "handleDef";
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate
        /// through the collection.
        /// </returns>
        public new IEnumerator<CacheHandleDefinition> GetEnumerator()
        {
            var enu = base.GetEnumerator();
            enu.Reset();
            while (enu.MoveNext())
            {
                yield return (CacheHandleDefinition)enu.Current;
            }
        }

        /// <summary>
        /// When overridden in a derived class, creates a new <see cref="T:System.Configuration.ConfigurationElement"/>.
        /// </summary>
        /// <returns>A new <see cref="T:System.Configuration.ConfigurationElement"/>.</returns>
        protected override ConfigurationElement CreateNewElement() => new CacheHandleDefinition();

        /// <summary>
        /// Gets the element key for a specified configuration element when overridden in a derived class.
        /// </summary>
        /// <param name="element">
        /// The <see cref="T:System.Configuration.ConfigurationElement"/> to return the key for.
        /// </param>
        /// <returns>
        /// An <see cref="T:System.Object"/> that acts as the key for the specified <see cref="T:System.Configuration.ConfigurationElement"/>.
        /// </returns>
        protected override object GetElementKey(ConfigurationElement element) => ((CacheHandleDefinition)element).Id;
    }

    /// <summary>
    /// Collection of cache configurations.
    /// </summary>
    public sealed class CacheManagerCollection : ConfigurationElementCollection, IEnumerable<CacheManagerHandleCollection>
    {
        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate
        /// through the collection.
        /// </returns>
        public new IEnumerator<CacheManagerHandleCollection> GetEnumerator()
        {
            var enu = base.GetEnumerator();
            enu.Reset();
            while (enu.MoveNext())
            {
                yield return (CacheManagerHandleCollection)enu.Current;
            }
        }

        /// <summary>
        /// When overridden in a derived class, creates a new <see cref="T:System.Configuration.ConfigurationElement"/>.
        /// </summary>
        /// <returns>A new <see cref="T:System.Configuration.ConfigurationElement"/>.</returns>
        protected override ConfigurationElement CreateNewElement() => new CacheManagerHandleCollection();

        /// <summary>
        /// Gets the element key for a specified configuration element when overridden in a derived class.
        /// </summary>
        /// <param name="element">
        /// The <see cref="T:System.Configuration.ConfigurationElement"/> to return the key for.
        /// </param>
        /// <returns>
        /// An <see cref="T:System.Object"/> that acts as the key for the specified <see cref="T:System.Configuration.ConfigurationElement"/>.
        /// </returns>
        protected override object GetElementKey(ConfigurationElement element) => ((CacheManagerHandleCollection)element).Name;
    }

    /// <summary>
    /// Configuration element which defines a cache handle configuration within a cache manager configuration.
    /// </summary>
    /// <see cref="CacheHandleConfiguration"/>
    /// <see cref="CacheManagerConfiguration"/>
    public sealed class CacheManagerHandle : ConfigurationElement
    {
        /// <summary>
        /// Gets or sets the expiration mode.
        /// </summary>
        /// <value>The expiration mode.</value>
        [ConfigurationProperty("expirationMode", IsRequired = false)]
        public string ExpirationMode
        {
            get
            {
                return (string)this["expirationMode"];
            }
            set
            {
                this["expirationMode"] = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is backplane source.
        /// </summary>
        /// <value><c>true</c> if this instance is backplane source; otherwise, <c>false</c>.</value>
        [ConfigurationProperty("isBackplaneSource", IsRequired = false)]
        public bool IsBackplaneSource
        {
            get
            {
                return (bool)this["isBackplaneSource"];
            }
            set
            {
                this["isBackplaneSource"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
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

        /// <summary>
        /// Gets or sets the reference handle identifier.
        /// </summary>
        /// <value>The reference handle identifier.</value>
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
        /// <value>The timeout.</value>
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

    /// <summary>
    /// The collection of cache handles defined for a cache manager.
    /// </summary>
    public sealed class CacheManagerHandleCollection : ConfigurationElementCollection, IEnumerable<CacheManagerHandle>
    {
        private const string BackplaneNameKey = "backplaneName";
        private const string BackplaneTypeKey = "backplaneType";
        private const string SerializerTypeKey = "serializerType";
        private const string EnablePerformanceCountersKey = "enablePerformanceCounters";
        private const string EnableStatisticsKey = "enableStatistics";
        private const string MaxRetriesKey = "maxRetries";
        private const string NameKey = "name";
        private const string RetryTimeoutKey = "retryTimeout";
        private const string UpdateModeKey = "updateMode";

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheManagerHandleCollection"/> class.
        /// </summary>
        public CacheManagerHandleCollection()
        {
            AddElementName = "handle";
        }

        /// <summary>
        /// Gets or sets the name of the backplane.
        /// </summary>
        /// <value>The name of the backplane.</value>
        [ConfigurationProperty(BackplaneNameKey, IsRequired = false)]
        public string BackplaneName
        {
            get
            {
                return (string)this[BackplaneNameKey];
            }
            set
            {
                this[BackplaneNameKey] = value;
            }
        }

        /// <summary>
        /// Gets or sets the type of the backplane.
        /// </summary>
        /// <value>The type of the backplane.</value>
        [ConfigurationProperty(BackplaneTypeKey, IsRequired = false)]
        public string BackplaneType
        {
            get
            {
                return (string)this[BackplaneTypeKey];
            }
            set
            {
                this[BackplaneTypeKey] = value;
            }
        }

        /// <summary>
        /// Gets or sets the type of the serializer.
        /// </summary>
        /// <value>The type of the serializer.</value>
        [ConfigurationProperty(SerializerTypeKey, IsRequired = false)]
        public string SerializerType
        {
            get
            {
                return (string)this[SerializerTypeKey];
            }
            set
            {
                this[SerializerTypeKey] = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether performance counters should be enabled.
        /// </summary>
        /// <value><c>true</c> if performance counters should be enabled; otherwise, <c>false</c>.</value>
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

        /// <summary>
        /// Gets or sets a value indicating whether statistics should be enabled.
        /// </summary>
        /// <value><c>true</c> if statistics should be enabled; otherwise, <c>false</c>.</value>
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

        /// <summary>
        /// Gets or sets the number of maximum retries.
        /// </summary>
        /// <value>The number of maximum retries.</value>
        [ConfigurationProperty(MaxRetriesKey, IsRequired = false)]
        public int? MaximumRetries
        {
            get
            {
                return (int?)this[MaxRetriesKey];
            }
            set
            {
                this[MaxRetriesKey] = value;
            }
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
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

        /// <summary>
        /// Gets or sets the retry timeout.
        /// </summary>
        /// <value>The retry timeout.</value>
        [ConfigurationProperty(RetryTimeoutKey, IsRequired = false)]
        public int? RetryTimeout
        {
            get
            {
                return (int?)this[RetryTimeoutKey];
            }
            set
            {
                this[RetryTimeoutKey] = value;
            }
        }

        /// <summary>
        /// Gets or sets the update mode.
        /// </summary>
        /// <value>The update mode.</value>
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

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate
        /// through the collection.
        /// </returns>
        public new IEnumerator<CacheManagerHandle> GetEnumerator()
        {
            var enu = base.GetEnumerator();
            enu.Reset();
            while (enu.MoveNext())
            {
                yield return (CacheManagerHandle)enu.Current;
            }
        }

        /// <summary>
        /// When overridden in a derived class, creates a new <see cref="T:System.Configuration.ConfigurationElement"/>.
        /// </summary>
        /// <returns>A new <see cref="T:System.Configuration.ConfigurationElement"/>.</returns>
        protected override ConfigurationElement CreateNewElement() => new CacheManagerHandle();

        /// <summary>
        /// Gets the element key for a specified configuration element when overridden in a derived class.
        /// </summary>
        /// <param name="element">
        /// The <see cref="T:System.Configuration.ConfigurationElement"/> to return the key for.
        /// </param>
        /// <returns>
        /// An <see cref="T:System.Object"/> that acts as the key for the specified <see cref="T:System.Configuration.ConfigurationElement"/>.
        /// </returns>
        protected override object GetElementKey(ConfigurationElement element) => ((CacheManagerHandle)element).Name;
    }

#endif
}