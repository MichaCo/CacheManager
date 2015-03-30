using System;
using System.Collections.Generic;
using System.Configuration;
using Couchbase;
using Couchbase.Configuration.Client;
using Couchbase.Configuration.Client.Providers;
using Couchbase.Core;
using Couchbase.Core.Buckets;

namespace CacheManager.Couchbase
{
    public static class CouchbaseConfigurationManager
    {
        private static Dictionary<string, ClientConfiguration> configurations = new Dictionary<string, ClientConfiguration>();
        private static Dictionary<string, IBucket> buckets = new Dictionary<string, IBucket>();

        public static void AddConfiguration(string name, ClientConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            if (!configurations.ContainsKey(name))
            {
                configurations.Add(name, configuration);
            }
        }

        public static ClientConfiguration GetConfiguration(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }

            if (configurations.ContainsKey(name))
            {
                return configurations[name];
            }

            var section = ConfigurationManager.GetSection(name) as CouchbaseClientSection;
            if (section == null)
            {
                throw new InvalidOperationException("No configuration or section found for configuration " + name + ".");
            }
            
            var clientConfiguration = new ClientConfiguration(section);
            configurations.Add(name, clientConfiguration);
            return clientConfiguration;
        }

        public static BucketConfiguration GetBucketConfiguration(ClientConfiguration clientConfiguration, string bucketName)
        {
            BucketConfiguration configuration;
            if (!clientConfiguration.BucketConfigs.TryGetValue(bucketName, out configuration))
            {
                throw new InvalidOperationException("Not bucket with bucket name " + bucketName + " found.");
            }

            return configuration;
        }

        public static Cluster GetCluster(ClientConfiguration clientConfiguration)
        {
            return new Cluster(clientConfiguration);
        }

        internal static IBucket GetBucket(ClientConfiguration clientConfiguration, string configurationName, string bucketName)
        {
            if (string.IsNullOrWhiteSpace(bucketName))
            {
                throw new ArgumentNullException("bucketName");
            }

            if (clientConfiguration == null)
            {
                throw new ArgumentNullException("clientConfiguration");
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