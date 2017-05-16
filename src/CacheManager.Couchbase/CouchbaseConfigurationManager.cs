using System;
using System.Linq;
using Couchbase;
using Couchbase.Configuration.Client;
using Couchbase.Core;
using static CacheManager.Core.Utility.Guard;
using Couchbase.Management;
using System.Collections.Concurrent;
using CacheManager.Core;

#if NET45
using System.Configuration;
using Couchbase.Configuration.Client.Providers;
#endif

namespace CacheManager.Couchbase
{
    /// <summary>
    /// Manages configurations for the couchbase cache handle.
    /// <para>
    /// As of version 1.0.2, changed the management of <see cref="IBucket"/>s as those instances are already
    /// managed by the <see cref="IClusterController"/> of the couchbase client libraray. No need to have additional collections of stuff in here.
    /// </para>
    /// <para>
    /// We keep track of added configurations via <see cref="CouchbaseConfigurationBuilderExtensions.WithCouchbaseConfiguration(ConfigurationBuilderCachePart, string, ClientConfiguration)"/>
    /// and eventually added predefined <see cref="ICluster"/>.
    /// Referencing still works via configuration key, although, if nothing in particular is defined, the fallback should always at least go to the couchbase default cluster settings.
    /// </para>
    /// <para>
    /// Also new, fallback to <see cref="ClusterHelper"/> which can be used to initialize settings of a cluster statically.
    /// </para>
    /// </summary>
    public class CouchbaseConfigurationManager
    {
        /// <summary>
        /// The default bucket name
        /// </summary>
        public const string DefaultBucketName = "default";

#if NET45

        /// <summary>
        /// The section name usually used for couchbase in app/web.config.
        /// </summary>
        public const string DefaultCouchbaseConfigurationSection = "couchbaseClients/couchbase";

#endif

        private static object _configLock = new object();
        private static ConcurrentDictionary<string, ClientConfiguration> _configurations = new ConcurrentDictionary<string, ClientConfiguration>();
        private static ConcurrentDictionary<string, ICluster> _clusters = new ConcurrentDictionary<string, ICluster>();
        private readonly string _configurationName;
        private readonly string _bucketName;
        private readonly string _bucketPassword;

        /// <summary>
        /// Initializes a new instance of the <see cref="CouchbaseConfigurationManager" /> class.
        /// </summary>
        /// <param name="configurationKey">The configuration name.</param>
        /// <param name="bucketName">The bucket name.</param>
        /// <param name="bucketPassword">The bucket password.</param>
        public CouchbaseConfigurationManager(string configurationKey, string bucketName = DefaultBucketName, string bucketPassword = null)
        {
            NotNullOrWhiteSpace(configurationKey, nameof(configurationKey));
            NotNullOrWhiteSpace(bucketName, nameof(bucketName));
            _configurationName = configurationKey;
            _bucketName = bucketName;
            _bucketPassword = bucketPassword;
        }

        /// <summary>
        /// Gets a bucket for configuration name and bucket name.
        /// </summary>
        /// <value>
        /// The bucket.
        /// </value>
        public IBucket Bucket => GetBucket(_configurationName, _bucketName, _bucketPassword);

        /// <summary>
        /// Gets a <see cref="IBucketManager" /> instance.
        /// If username and password have been defined in the bucket's configuration, those will be used to create the manager.
        /// </summary>
        /// <returns>The manager instance or null.</returns>
        public IBucketManager GetManager()
        {
            var bucket = Bucket;
            return string.IsNullOrWhiteSpace(bucket.Configuration.Username) ?
                bucket.CreateManager() :
                bucket.CreateManager(bucket.Configuration.Username, bucket.Configuration.Password);
        }

        /// <summary>
        /// Adds a already configured <see cref="IBucket"/> to the named collection of buckets.
        /// This can be referenced by the <see cref="BucketCacheHandle{TCacheValue}"/> via configuration key and <see cref="IBucket.Name"/>.
        /// </summary>
        /// <param name="configurationKey">The configuration key.</param>
        /// <param name="cluster">The bucket.</param>
        public static void AddCluster(string configurationKey, ICluster cluster)
        {
            NotNullOrWhiteSpace(configurationKey, nameof(configurationKey));
            NotNull(cluster, nameof(cluster));

            // not sure if we even need this, but eventually we have to create a new instance of that bucket
            _configurations.TryAdd(configurationKey, cluster.Configuration);
            _clusters.TryAdd(configurationKey, cluster);
        }

        /// <summary>
        /// Adds a <see cref="ClientConfiguration"/> for a <paramref name="configurationKey"/>.
        /// </summary>
        /// <param name="configurationKey">The name.</param>
        /// <param name="configuration">The configuration.</param>
        /// <exception cref="System.ArgumentNullException">If name or configuration are null.</exception>
        public static void AddConfiguration(string configurationKey, ClientConfiguration configuration)
        {
            NotNullOrWhiteSpace(configurationKey, nameof(configurationKey));
            NotNull(configuration, nameof(configuration));
            _configurations.TryAdd(configurationKey, configuration);
        }

        /// <summary>
        /// Gets a <see cref="ClientConfiguration"/> for the given <paramref name="configurationKeyOrSectionName"/>.
        /// <para>
        /// If the configuration is not already present and the target framework supports <c>ConfigurationManager</c>, the method tries to resolve the configuration from the
        /// section with the given name.
        /// </para>
        /// </summary>
        /// <param name="configurationKeyOrSectionName">The name.</param>
        /// <returns>The configuration.</returns>
        /// <exception cref="System.ArgumentNullException">If name is null.</exception>
        /// <exception cref="System.InvalidOperationException">
        /// If no configuration or section can be found for configuration.
        /// </exception>
        public static ClientConfiguration GetConfiguration(string configurationKeyOrSectionName)
        {
            NotNullOrWhiteSpace(configurationKeyOrSectionName, nameof(configurationKeyOrSectionName));

            if (_configurations.TryGetValue(configurationKeyOrSectionName, out ClientConfiguration configuration))
            {
                return configuration;
            }

#if NET45
            var section = ConfigurationManager.GetSection(configurationKeyOrSectionName) as CouchbaseClientSection;

            if (section == null)
            {
                return null;
            }

            var config = new ClientConfiguration(section);
            AddConfiguration(configurationKeyOrSectionName, config);
            return config;
#else

            return null;
#endif
        }

        private static ICluster GetCluster(string configurationKey)
        {
            NotNullOrWhiteSpace(configurationKey, nameof(configurationKey));

            if (!_clusters.TryGetValue(configurationKey, out ICluster cluster))
            {
                var config = GetConfiguration(configurationKey);
                if (config != null)
                {
                    cluster = new Cluster(config);
                }
                else
                {
                    // fallback to ClusterHelper as that's also a way ppl can configure this stuff...
                    try
                    {
                        cluster = ClusterHelper.Get();
                    }
                    catch (InitializationException)
                    {
                        // last fallback has also not been initialized yet
                        // this will use the development settings on localhost without any auth (might not work and blow up later).
                        cluster = new Cluster();
                    }

                    // update our configuration cache just in case
                    AddConfiguration(configurationKey, cluster.Configuration);
                }

                _clusters.TryAdd(configurationKey, cluster);
            }

            return cluster;
        }

        private static IBucket GetBucket(string configurationKey, string bucketName, string bucketPassword)
        {
            NotNullOrWhiteSpace(bucketName, nameof(bucketName));
            var cluster = GetCluster(configurationKey);
            if (cluster == null)
            {
                // should probably never occur.
                throw new InvalidOperationException("Cluster is not configured although we should fall back to ClusterHelper at least.");
            }

            return string.IsNullOrEmpty(bucketPassword) ? cluster.OpenBucket(bucketName) : cluster.OpenBucket(bucketName, bucketPassword);
        }
    }
}