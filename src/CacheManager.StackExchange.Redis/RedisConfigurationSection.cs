using System.Collections.Generic;
using System.Configuration;

namespace CacheManager.Redis
{
    /// <summary>
    /// Configuration section for the CacheManager.
    /// </summary>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// <cacheManager.redis>
    ///   <connections>
    ///    <connection id="redis1"
    ///                    database="0"
    ///                    database="113"
    ///                    strictCompatibilityModeVersion="the redis version, e.g. 2.6, or leave null"
    ///
    ///                    { connectionString="redis0:6379,redis1:6380,keepAlive=180,allowAdmin=true" }
    ///                    OR
    ///                    {
    ///                       allowAdmin="true|false"
    ///                       password=""
    ///                       ssl="true|false"
    ///                       sslHost="string"
    ///                       connectionTimeout="ms"
    ///                       twemproxyEnabled="true|false"
    ///                    }
    ///            >
    ///        <endpoints>
    ///            <endpoint host="127.0.0.1" port="6379" />
    ///            <endpoint host="127.0.0.1" port="6380"/>
    ///        </endpoints>
    ///    </connection>
    ///    <connection id="redisN">...</connection>
    ///  <connections/>
    /// </cacheManager.redis>
    /// ]]>
    /// </code>
    /// </example>
    public sealed class Endpoint : ConfigurationElement
    {
        /// <summary>
        /// Gets or sets the host.
        /// </summary>
        /// <value>The host.</value>
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

        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        /// <value>The port.</value>
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

    /// <summary>
    /// Collection of end point configurations.
    /// </summary>
    public sealed class EndpointCollection : ConfigurationElementCollection, IEnumerable<Endpoint>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EndpointCollection"/> class.
        /// </summary>
        public EndpointCollection()
        {
            AddElementName = "endpoint";
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate
        /// through the collection.
        /// </returns>
        public new IEnumerator<Endpoint> GetEnumerator()
        {
            var enu = base.GetEnumerator();
            enu.Reset();
            while (enu.MoveNext())
            {
                yield return (Endpoint)enu.Current;
            }
        }

        /// <summary>
        /// When overridden in a derived class, creates a new <see cref="T:System.Configuration.ConfigurationElement"/>.
        /// </summary>
        /// <returns>A new <see cref="T:System.Configuration.ConfigurationElement"/>.</returns>
        protected override ConfigurationElement CreateNewElement() => new Endpoint();

        /// <summary>
        /// Gets the element key for a specified configuration element when overridden in a derived class.
        /// </summary>
        /// <param name="element">
        /// The <see cref="T:System.Configuration.ConfigurationElement"/> to return the key for.
        /// </param>
        /// <returns>
        /// An <see cref="T:System.Object"/> that acts as the key for the specified <see cref="T:System.Configuration.ConfigurationElement"/>.
        /// </returns>
        protected override object GetElementKey(ConfigurationElement element)
        {
            var elem = (Endpoint)element;
            return elem.Host + ":" + elem.Port;
        }
    }

    /// <summary>
    /// The main section for redis configurations.
    /// </summary>
    public sealed class RedisConfigurationSection : ConfigurationSection
    {
        /// <summary>
        /// The default section name.
        /// </summary>
        public const string DefaultSectionName = "cacheManager.Redis";

        private const string ConfigurationsName = "connections";

        /// <summary>
        /// Gets the connections.
        /// </summary>
        /// <value>The connections.</value>
        [ConfigurationProperty(ConfigurationsName)]
        [ConfigurationCollection(typeof(RedisOptionCollection), AddItemName = "connection")]
        public RedisOptionCollection Connections => (RedisOptionCollection)this[ConfigurationsName];

        /// <summary>
        /// Gets or sets the XMLNS.
        /// </summary>
        /// <value>The XMLNS.</value>
        [ConfigurationProperty("xmlns", IsRequired = false)]
        public string Xmlns { get; set; }
    }

    /// <summary>
    /// Collection of redis configurations.
    /// </summary>
    public sealed class RedisOptionCollection : ConfigurationElementCollection, IEnumerable<RedisOptions>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisOptionCollection"/> class.
        /// </summary>
        public RedisOptionCollection()
        {
            AddElementName = "connection";
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate
        /// through the collection.
        /// </returns>
        public new IEnumerator<RedisOptions> GetEnumerator()
        {
            var enu = base.GetEnumerator();
            enu.Reset();
            while (enu.MoveNext())
            {
                yield return (RedisOptions)enu.Current;
            }
        }

        /// <summary>
        /// When overridden in a derived class, creates a new <see cref="T:System.Configuration.ConfigurationElement"/>.
        /// </summary>
        /// <returns>A new <see cref="T:System.Configuration.ConfigurationElement"/>.</returns>
        protected override ConfigurationElement CreateNewElement() => new RedisOptions();

        /// <summary>
        /// Gets the element key for a specified configuration element when overridden in a derived class.
        /// </summary>
        /// <param name="element">
        /// The <see cref="T:System.Configuration.ConfigurationElement"/> to return the key for.
        /// </param>
        /// <returns>
        /// An <see cref="T:System.Object"/> that acts as the key for the specified <see cref="T:System.Configuration.ConfigurationElement"/>.
        /// </returns>
        protected override object GetElementKey(ConfigurationElement element) => ((RedisOptions)element).Id;
    }

    /// <summary>
    /// The redis configuration element.
    /// </summary>
    public sealed class RedisOptions : ConfigurationElement
    {
        private const string EndpointsName = "endpoints";

        /// <summary>
        /// Gets or sets a value indicating whether advanced commands are allowed.
        /// </summary>
        /// <value><c>true</c> if admin commands should be allowed; otherwise, <c>false</c>.</value>
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

        /// <summary>
        /// Gets or sets a value indicating whether keyspace events should be enabled and the redis cache handle should listen for them.
        /// </summary>
        [ConfigurationProperty("enableKeyspaceNotifications", IsRequired = false, DefaultValue = false)]
        public bool EnableKeyspaceNotifications
        {
            get
            {
                return (bool)this["enableKeyspaceNotifications"];
            }
            set
            {
                this["enableKeyspaceNotifications"] = value;
            }
        }

        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        /// <value>The connection string.</value>
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

        /// <summary>
        /// Gets or sets the connection timeout.
        /// </summary>
        /// <value>The connection timeout.</value>
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

        /// <summary>
        /// Gets or sets the database.
        /// </summary>
        /// <value>The database.</value>
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

        /// <summary>
        /// Gets the endpoints.
        /// </summary>
        /// <value>The endpoints.</value>
        [ConfigurationProperty(EndpointsName)]
        [ConfigurationCollection(typeof(EndpointCollection), AddItemName = "endpoint")]
        public EndpointCollection Endpoints => (EndpointCollection)this[EndpointsName];

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

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        /// <value>The password.</value>
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

        /// <summary>
        /// Gets or sets a value indicating whether SSL should be enabled or not.
        /// </summary>
        /// <value><c>true</c> if SSL should be enabled; otherwise, <c>false</c>.</value>
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

        /// <summary>
        /// Gets or sets the SSL host.
        /// </summary>
        /// <value>The SSL host.</value>
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

        /// <summary>
        /// Gets or sets a value indicating whether Twemproxy is used or not.
        /// </summary>
        /// <value><c>true</c> if Twemproxy is used; otherwise, <c>false</c>.</value>
        [ConfigurationProperty("twemproxyEnabled", IsRequired = false, DefaultValue = false)]
        public bool TwemproxyEnabled
        {
            get
            {
                return (bool)this["twemproxyEnabled"];
            }
            set
            {
                this["twemproxyEnabled"] = value;
            }
        }

        /// <summary>
        /// Gets or sets a value setting an explicit version compatibility mode.
        /// </summary>
        /// <value>The Redis version to use.</value>
        [ConfigurationProperty("strictCompatibilityModeVersion", IsRequired = false)]
        public string StrictCompatibilityModeVersion
        {
            get
            {
                return (string)this["strictCompatibilityModeVersion"];
            }
            set
            {
                this["strictCompatibilityModeVersion"] = value;
            }
        }
    }
}
