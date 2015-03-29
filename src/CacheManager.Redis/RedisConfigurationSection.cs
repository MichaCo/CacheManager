using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;

namespace CacheManager.Redis
{
    /// <summary>
    /// Configuration section for the CacheManager.
    /// </summary>
    /// <example><![CDATA[
    /// <cacheManager.redis>
    ///   <connections>
    ///    <connection id="redis1"
    ///                    database="0"
    ///                    { connnectionString="redis0:6379,redis1:6380,keepAlive=180,allowAdmin=true" }
    ///                    OR
    ///                    { 
    ///                    allowAdmin="true|false"
    ///                    password=""
    ///                    ssl="true|false"
    ///                    sslHost="string"
    ///                    connectionTimeout="ms" 
    ///                    } 
    ///            >
    ///        <endpoints>
    ///            <endpoint host="127.0.0.1" port="6379" />
    ///            <endpoint host="127.0.0.1" port="6380"/>
    ///        </endpoints>
    ///    </connection>
    ///    <connection id="redisN">...</connection>
    ///  <connections/>
    ///</cacheManager.redis>
    /// ]]>
    /// </example>
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

    public sealed class RedisOptionCollection : ConfigurationElementCollection, IEnumerable<RedisOptions>
    {
        public RedisOptionCollection()
        {
            AddElementName = "connection";
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

    public sealed class RedisConfigurationSection : ConfigurationSection
    {
        public const string DefaultSectionName = "cacheManager.Redis";

        private const string ConfigurationsName = "connections";

        [ConfigurationProperty(ConfigurationsName)]
        [ConfigurationCollection(typeof(RedisOptionCollection), AddItemName = "connection")]
        public RedisOptionCollection Connections
        {
            get
            {
                return (RedisOptionCollection)base[ConfigurationsName];
            }
        }

        [ConfigurationProperty("xmlns", IsRequired = false)]
        public string Xmlns { get; set; }
    }
}