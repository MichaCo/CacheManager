using System;
using CacheManager.Couchbase;
using Couchbase;
using Couchbase.Configuration.Client;
using Couchbase.Core;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core
{
    /// <summary>
    /// Extensions for the configuration builder specific to the Couchbase cache handle.
    /// </summary>
    public static class CouchbaseConfigurationBuilderExtensions
    {
        /// <summary>
        /// Adds a <see cref="ClientConfiguration" /> for the given key.
        /// <para>The key will be matched with the Couchbase cache handle name.</para>
        /// </summary>
        /// <param name="part">The part.</param>
        /// <param name="configurationKey">The key which has to match with the cache handle name.</param>
        /// <param name="config">The Couchbase configuration object.</param>
        /// <returns>
        /// The configuration builder.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">If <paramref name="configurationKey" /> or <paramref name="config" /> is null.</exception>
        public static ConfigurationBuilderCachePart WithCouchbaseConfiguration(this ConfigurationBuilderCachePart part, string configurationKey, ClientConfiguration config)
        {
            NotNullOrWhiteSpace(configurationKey, nameof(configurationKey));
            NotNull(config, nameof(config));

            CouchbaseConfigurationManager.AddConfiguration(configurationKey, config);
            return part;
        }

        /// <summary>
        /// Adds a <see cref="ClientConfiguration" /> for the given key.
        /// <para>The key will be matched with the Couchbase cache handle name.</para>
        /// </summary>
        /// <param name="part">The part.</param>
        /// <param name="configurationKey">The key which has to match with the cache handle name.</param>
        /// <param name="definition">The Couchbase configuration object.</param>
        /// <returns>
        /// The configuration builder.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">If <paramref name="configurationKey" /> or <paramref name="definition" /> is null.</exception>
        public static ConfigurationBuilderCachePart WithCouchbaseConfiguration(this ConfigurationBuilderCachePart part, string configurationKey, ICouchbaseClientDefinition definition)
        {
            return WithCouchbaseConfiguration(part, configurationKey, new ClientConfiguration(definition));
        }

        /// <summary>
        /// Adds an already configured <see cref="ICluster" /> for the given key. Use this in case you want to use the <paramref name="cluster" /> outside of CacheManager, too
        /// and you want to share this instance.
        /// <para>
        /// Use <paramref name="configurationKey" /> in <see cref="WithCouchbaseCacheHandle(ConfigurationBuilderCachePart, string, string, bool)" /> (or similar overloads)
        /// to have the cache handle use this configuration.
        /// </para><para>
        /// If your cluster requires authentication, you might have to configure <c>cluster.Authenticate(...)</c>.
        /// </para>
        /// </summary>
        /// <param name="part">The part.</param>
        /// <param name="configurationKey">The configuration key.</param>
        /// <param name="cluster">The <see cref="ICluster" />.</param>
        /// <returns>
        /// The configuration builder.
        /// <exception cref="System.ArgumentNullException">If <paramref name="configurationKey" /> or <paramref name="cluster" /> is null.</exception>
        /// </returns>
        public static ConfigurationBuilderCachePart WithCouchbaseCluster(this ConfigurationBuilderCachePart part, string configurationKey, ICluster cluster)
        {
            NotNullOrWhiteSpace(configurationKey, nameof(configurationKey));
            NotNull(cluster, nameof(cluster));

            CouchbaseConfigurationManager.AddCluster(configurationKey, cluster);
            return part;
        }
        
        /// <summary>
        /// Adds a <see cref="BucketCacheHandle{TCacheValue}" /> using the configuration referenced via <paramref name="couchbaseConfigurationKey" />.
        /// <para>
        /// The cache handle needs configuration specific to Couchbase, see remarks for details.
        /// </para>
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="couchbaseConfigurationKey">The configuration identifier.</param>
        /// <param name="bucketName">The name of the Couchbase bucket which should be used by the cache handle.</param>
        /// <param name="isBackplaneSource">Set this to <c>true</c> if this cache handle should be the source of the backplane. This setting will be ignored if no backplane is configured.</param>
        /// <returns>
        /// The part.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="bucketName" /> or <paramref name="couchbaseConfigurationKey" /> is null.</exception>
        /// <remarks>
        /// The Couchbase cache handle requires configuration which can be defined via:
        /// <list type="bullet"><item>
        /// <term>
        /// A configuration with a matching <paramref name="couchbaseConfigurationKey" /> being added via <see cref="WithCouchbaseConfiguration(ConfigurationBuilderCachePart, string, ClientConfiguration)" />.
        /// </term></item>
        /// <item><term>
        /// A cluster with a matching <paramref name="couchbaseConfigurationKey" /> being added via <see cref="WithCouchbaseCluster(ConfigurationBuilderCachePart, string, ICluster)" />.
        /// </term></item>
        /// <item><term>
        /// A <c>CouchbaseClientSection</c> configured in <c>App/Web.config</c> (only available on full .NET Framework).
        /// </term></item>
        /// <item><term>
        /// Or, the cluster has been configured via <see cref="ClusterHelper" /> and CacheManager will use the cluster returned by <see cref="ClusterHelper.Get" />.
        /// Anyways, this will be the last fallback which, if nothing has been configured at all, will fall back to the default server endpoint on <c>127.0.0.1:8091</c>.
        /// </term></item>
        /// </list>
        /// <para>
        /// If your cluster requires authentication, use either the <see cref="ClusterHelper" /> or add a <see cref="ICluster" /> with valid authentication via <c>cluster.Authenticate(...)</c>.
        /// </para>
        /// </remarks>
        public static ConfigurationBuilderCacheHandlePart WithCouchbaseCacheHandle(
            this ConfigurationBuilderCachePart part,
            string couchbaseConfigurationKey,
            string bucketName = CouchbaseConfigurationManager.DefaultBucketName,
            bool isBackplaneSource = true)
        {
            NotNull(part, nameof(part));
            NotNullOrWhiteSpace(bucketName, nameof(bucketName));

            return part.WithHandle(typeof(BucketCacheHandle<>), couchbaseConfigurationKey, isBackplaneSource, new BucketCacheHandleAdditionalConfiguration()
            {
                BucketName = bucketName
            });
        }

        /// <summary>
        /// Adds a <see cref="BucketCacheHandle{TCacheValue}" /> using the configuration referenced via <paramref name="couchbaseConfigurationKey" />.
        /// <para>
        /// The cache handle needs configuration specific to Couchbase, see remarks for details.
        /// </para>
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="couchbaseConfigurationKey">The configuration identifier.</param>
        /// <param name="bucketName">The name of the Couchbase bucket which should be used by the cache handle.</param>
        /// <param name="bucketPassword">The bucket password.</param>
        /// <param name="isBackplaneSource">Set this to <c>true</c> if this cache handle should be the source of the backplane. This setting will be ignored if no backplane is configured.</param>
        /// <returns>
        /// The part.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="bucketName" /> or <paramref name="couchbaseConfigurationKey" /> is null.</exception>
        /// <remarks>
        /// The Couchbase cache handle requires configuration which can be defined via:
        /// <list type="bullet"><item>
        /// <term>
        /// A configuration with a matching <paramref name="couchbaseConfigurationKey" /> being added via <see cref="WithCouchbaseConfiguration(ConfigurationBuilderCachePart, string, ClientConfiguration)" />.
        /// </term></item>
        /// <item><term>
        /// A cluster with a matching <paramref name="couchbaseConfigurationKey" /> being added via <see cref="WithCouchbaseCluster(ConfigurationBuilderCachePart, string, ICluster)" />.
        /// </term></item>
        /// <item><term>
        /// A <c>CouchbaseClientSection</c> configured in <c>App/Web.config</c> (only available on full .NET Framework).
        /// </term></item>
        /// <item><term>
        /// Or, the cluster has been configured via <see cref="ClusterHelper" /> and CacheManager will use the cluster returned by <see cref="ClusterHelper.Get" />.
        /// Anyways, this will be the last fallback which, if nothing has been configured at all, will fall back to the default server endpoint on <c>127.0.0.1:8091</c>.
        /// </term></item>
        /// </list>
        /// <para>
        /// If your cluster requires authentication, use either the <see cref="ClusterHelper" /> or add a <see cref="ICluster" /> with valid authentication via <c>cluster.Authenticate(...)</c>.
        /// </para>
        /// </remarks>
        public static ConfigurationBuilderCacheHandlePart WithCouchbaseCacheHandle(
            this ConfigurationBuilderCachePart part,
            string couchbaseConfigurationKey,
            string bucketName,
            string bucketPassword,
            bool isBackplaneSource = true)
        {
            NotNull(part, nameof(part));
            NotNullOrWhiteSpace(bucketName, nameof(bucketName));

            return part.WithHandle(typeof(BucketCacheHandle<>), couchbaseConfigurationKey, isBackplaneSource, new BucketCacheHandleAdditionalConfiguration()
            {
                BucketName = bucketName,
                BucketPassword = bucketPassword
            });
        }
    }
}