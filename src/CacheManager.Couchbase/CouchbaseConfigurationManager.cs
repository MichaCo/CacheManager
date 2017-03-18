using System.Collections.Generic;
using System.Configuration;
using Couchbase;
using Couchbase.Configuration.Client;
using Couchbase.Configuration.Client.Providers;
using Couchbase.Core;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Couchbase
{
    /// <summary>
    /// Manages configurations for the couchbase cache handle.
    /// <para>
    /// The configurations will be added by the configuration builder or configuration loader and
    /// then referenced via handle's name.
    /// </para>
    /// </summary>
    public static class CouchbaseConfigurationManager
    {
        private static Dictionary<string, ClientConfiguration> _configurations = new Dictionary<string, ClientConfiguration>();
        private static Dictionary<string, IBucket> _buckets = new Dictionary<string, IBucket>();

        /// <summary>
        /// Adds the configuration.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="configuration">The configuration.</param>
        /// <exception cref="System.ArgumentNullException">If name or configuration are null.</exception>
        public static void AddConfiguration(string name, ClientConfiguration configuration)
        {
            NotNullOrWhiteSpace(name, nameof(name));
            NotNull(configuration, nameof(configuration));

            if (!_configurations.ContainsKey(name))
            {
                _configurations.Add(name, configuration);
            }
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The configuration.</returns>
        /// <exception cref="System.ArgumentNullException">If name is null.</exception>
        /// <exception cref="System.InvalidOperationException">
        /// If no configuration or section can be found for configuration.
        /// </exception>
        public static ClientConfiguration GetConfiguration(string name)
        {
            NotNullOrWhiteSpace(name, nameof(name));

            if (_configurations.ContainsKey(name))
            {
                return _configurations[name];
            }

            var section = ConfigurationManager.GetSection(name) as CouchbaseClientSection;
            EnsureNotNull(section, "No configuration or section found for configuration: {0}.", name);

            var clientConfiguration = new ClientConfiguration(section);
            _configurations.Add(name, clientConfiguration);
            return clientConfiguration;
        }

        /// <summary>
        /// Gets the bucket configuration.
        /// </summary>
        /// <param name="clientConfiguration">The client configuration.</param>
        /// <param name="bucketName">Name of the bucket.</param>
        /// <returns>The configuration for the named bucket.</returns>
        /// <exception cref="System.InvalidOperationException">No bucket with the name found.</exception>
        public static BucketConfiguration GetBucketConfiguration(ClientConfiguration clientConfiguration, string bucketName)
        {
            NotNull(clientConfiguration, nameof(clientConfiguration));
            NotNullOrWhiteSpace(bucketName, nameof(bucketName));

            BucketConfiguration configuration;
            Ensure(
                clientConfiguration.BucketConfigs.TryGetValue(bucketName, out configuration),
                "No bucket with bucket name {0} found.",
                bucketName);

            return configuration;
        }

        /// <summary>
        /// Gets a couchbase cluster from configuration.
        /// </summary>
        /// <param name="clientConfiguration">The client configuration.</param>
        /// <returns>The couchbase cluster.</returns>
        public static Cluster GetCluster(ClientConfiguration clientConfiguration) => new Cluster(clientConfiguration);

        /// <summary>
        /// Gets a couchbase bucket from configuration.
        /// </summary>
        /// <param name="clientConfiguration">The client configuration.</param>
        /// <param name="configurationName">Name of the configuration.</param>
        /// <param name="bucketName">Name of the bucket.</param>
        /// <returns>The couchbase bucket.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// If bucketName or clientConfiguration are null.
        /// </exception>
        internal static IBucket GetBucket(ClientConfiguration clientConfiguration, string configurationName, string bucketName)
        {
            NotNullOrWhiteSpace(bucketName, nameof(bucketName));
            NotNull(clientConfiguration, nameof(clientConfiguration));

            IBucket bucket;

            var bucketKey = configurationName + "_" + bucketName;

            if (_buckets.ContainsKey(bucketKey))
            {
                bucket = _buckets[bucketKey];
            }
            else
            {
                // todo: is this correct/needed?
                var bucketConfig = GetBucketConfiguration(clientConfiguration, bucketName);
                if (!string.IsNullOrWhiteSpace(bucketConfig.Password))
                {
                    bucket = GetCluster(clientConfiguration).OpenBucket(bucketName, bucketConfig.Password);
                }
                else
                {
                    bucket = GetCluster(clientConfiguration).OpenBucket(bucketName);
                }

                _buckets.Add(bucketKey, bucket);
            }

            return bucket;
        }
    }
}