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
    /// <summary>
    /// Cache handle implementation based on the Enyim memcached client.
    /// </summary>
    public class MemcachedCacheHandle : MemcachedCacheHandle<object>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemcachedCacheHandle"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="configuration">The configuration.</param>
        public MemcachedCacheHandle(ICacheManager<object> manager, ICacheHandleConfiguration configuration)
            : base(manager, configuration)
        {
        }
    }

    /// <summary>
    /// Cache handle implementation based on the Enyim memcached client.
    /// </summary>
    /// <typeparam name="TCacheValue">The type of the cache value.</typeparam>
    public class MemcachedCacheHandle<TCacheValue> : MemcachedClientHandle<TCacheValue>
    {
        private static readonly string DefaultEnyimSectionName = "enyim.com/memcached";
        private static readonly string DefaultSectionName = "default";

        /// <summary>
        /// Initializes a new instance of the <see cref="MemcachedCacheHandle{TCacheValue}"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="configuration">The configuration.</param>
        /// <exception cref="System.Configuration.ConfigurationErrorsException">
        /// If the enyim configuration section could not be initialized.
        /// </exception>
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

        /// <summary>
        /// Gets the get memcached client configuration.
        /// </summary>
        /// <value>The get memcached client configuration.</value>
        public IMemcachedClientConfiguration GetMemcachedClientConfiguration
        {
            get
            {
                return this.GetSection();
            }
        }

        /// <summary>
        /// Gets the servers.
        /// </summary>
        /// <value>The servers.</value>
        public IList<IPEndPoint> Servers
        {
            get
            {
                return this.GetServers();
            }
        }

        /// <summary>
        /// Gets the server count.
        /// </summary>
        /// <returns>The count per server.</returns>
        public IEnumerable<long> GetServerCount()
        {
            foreach (var count in this.Cache.Stats().GetRaw("total_items"))
            {
                yield return long.Parse(count.Value, CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Gets the name of the enyim section.
        /// </summary>
        /// <param name="handleName">Name of the handle.</param>
        /// <returns>The section name.</returns>
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

        /// <summary>
        /// Gets the section.
        /// </summary>
        /// <returns>The client configuration.</returns>
        /// <exception cref="System.Configuration.ConfigurationErrorsException">
        /// If memcached client section was not found or there are no servers defined for memcached.
        /// </exception>
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

        private IList<IPEndPoint> GetServers()
        {
            var section = this.GetSection();
            return section.Servers;
        }
    }
}