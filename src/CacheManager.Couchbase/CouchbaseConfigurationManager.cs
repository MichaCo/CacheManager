using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using Couchbase;
using Couchbase.Configuration.Client;
using Couchbase.Configuration.Client.Providers;
using Couchbase.Core;

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
        private static Dictionary<string, ClientConfiguration> configurations = new Dictionary<string, ClientConfiguration>();
        private static Dictionary<string, IBucket> buckets = new Dictionary<string, IBucket>();

        /// <summary>
        /// Adds the configuration.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="configuration">The configuration.</param>
        /// <exception cref="System.ArgumentNullException">If name or configuration are null.</exception>
        public static void AddConfiguration(string name, ClientConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (!configurations.ContainsKey(name))
            {
                configurations.Add(name, configuration);
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
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (configurations.ContainsKey(name))
            {
                return configurations[name];
            }

            var section = ConfigurationManager.GetSection(name) as CouchbaseClientSection;
            if (section == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "No configuration or section found for configuration: {0}.", name));
            }

            var clientConfiguration = new ClientConfiguration(section);
            configurations.Add(name, clientConfiguration);
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
            if (clientConfiguration == null)
            {
                throw new ArgumentNullException(nameof(clientConfiguration));
            }

            if (string.IsNullOrWhiteSpace(bucketName))
            {
                throw new ArgumentException("Bucket's name cannot be empty", nameof(bucketName));
            }

            BucketConfiguration configuration;
            if (!clientConfiguration.BucketConfigs.TryGetValue(bucketName, out configuration))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "No bucket with bucket name {0} found.", bucketName));
            }

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
            if (string.IsNullOrWhiteSpace(bucketName))
            {
                throw new ArgumentNullException(nameof(bucketName));
            }

            if (clientConfiguration == null)
            {
                throw new ArgumentNullException(nameof(clientConfiguration));
            }

            IBucket bucket;

            var bucketKey = configurationName + "_" + bucketName;

            if (buckets.ContainsKey(bucketKey))
            {
                bucket = buckets[bucketKey];
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

                buckets.Add(bucketKey, bucket);
            }

            return bucket;
        }
    }
}