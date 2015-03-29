using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Net;
using CacheManager.Core;
using CacheManager.Core.Configuration;
using Enyim.Caching;
using Enyim.Caching.Configuration;

namespace CacheManager.Memcached
{
    public class MemcachedCacheHandle : MemcachedCacheHandle<object>
    {
        public MemcachedCacheHandle(ICacheManager<object> manager, ICacheHandleConfiguration configuration)
            : base(manager, configuration)
        {
        }
    }

    public class MemcachedCacheHandle<TCacheValue> : MemcachedClientHandle<TCacheValue>
    {
        private static readonly string DefaultSectionName = "default";

        private static readonly string DefaultEnyimSectionName = "enyim.com/memcached";
        
        public IList<IPEndPoint> Servers
        {
            get
            {
                return this.GetServers();
            }
        }

        public IMemcachedClientConfiguration GetMemcachedClientConfiguration
        {
            get
            {
                return this.GetSection();
            }
        }

        public MemcachedCacheHandle(ICacheManager<TCacheValue> manager, ICacheHandleConfiguration configuration)
            : base(manager, configuration)
        {
            // initialize memcached client with section name which must be equal to handle name...
            // Default is "enyim.com/memcached"

            try
            {
                var sectionName = GetEnyimSectionName(configuration.HandleName);
                this.Cache = new MemcachedClient(sectionName);
            }
            catch (ConfigurationErrorsException ex)
            {
                throw new ConfigurationErrorsException("Failed to initialize " + this.GetType().Name + ". " + ex.BareMessage, ex);
            }
        }
        
        public IEnumerable<long> GetServerCount()
        {
            foreach (var count in this.Cache.Stats().GetRaw("total_items"))
            {
                yield return long.Parse(count.Value, CultureInfo.InvariantCulture);
            }
        }

        private IList<IPEndPoint> GetServers()
        {
            var section = GetSection();
            return section.Servers;
        }

        private IMemcachedClientConfiguration GetSection()
        {
            string sectionName = GetEnyimSectionName(this.Configuration.HandleName);
            MemcachedClientSection section = (MemcachedClientSection)ConfigurationManager.GetSection(sectionName);

            if (section == null)
            {
                throw new ConfigurationErrorsException("Memcached client section " + sectionName + " is not found.");
            }

            // validate
            if (section.Servers.Count <= 0)
            {
                throw new ConfigurationErrorsException("There are no servers defined for memcached.");
            }

            return section;
        }

        private static string GetEnyimSectionName(string handleName)
        {
            if (handleName.Equals(DefaultSectionName, StringComparison.OrdinalIgnoreCase))
            {
                return DefaultEnyimSectionName;
            }
            else
            {
                return handleName;
            }
        }
    }
}