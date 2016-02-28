using System;
using CacheManager.Couchbase;
using Couchbase.Configuration.Client;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core
{
    /// <summary>
    /// Extensions for the configuration builder specific to the Couchbase cache handle.
    /// </summary>
    public static class CouchbaseConfigurationBuilderExtensions
    {
        /// <summary>
        /// Adds a <see cref="ClientConfiguration"/> for the given key.
        /// <para>The key will be matched with the Couchbase cache handle name.</para>
        /// </summary>
        /// <param name="part">The part.</param>
        /// <param name="configurationKey">The key which has to match with the cache handle name.</param>
        /// <param name="config">The Couchbase configuration object.</param>
        /// <returns>The configuration builder.</returns>
        /// <exception cref="System.ArgumentNullException">If key or config are null.</exception>
        public static ConfigurationBuilderCachePart WithCouchbaseConfiguration(this ConfigurationBuilderCachePart part, string configurationKey, ClientConfiguration config)
        {
            NotNullOrWhiteSpace(configurationKey, nameof(configurationKey));
            NotNull(config, nameof(config));

            CouchbaseConfigurationManager.AddConfiguration(configurationKey, config);
            return part;
        }

        /// <summary>
        /// Adds a <see cref="BucketCacheHandle{TCacheValue}"/> with the required name.
        /// <para>
        /// This handle requires a Couchbase <see cref="ClientConfiguration"/> to be defined with
        /// the <paramref name="couchbaseConfigurationKey"/> matching the configuration's key.
        /// </para>
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="couchbaseConfigurationKey">
        /// The configuration key will be used as name for the cache handle and to retrieve the
        /// connection configuration.
        /// </param>
        /// <returns>The part.</returns>
        /// <exception cref="ArgumentNullException">Thrown if handleName is null.</exception>
        public static ConfigurationBuilderCacheHandlePart WithCouchbaseCacheHandle(
            this ConfigurationBuilderCachePart part,
            string couchbaseConfigurationKey) =>
            WithCouchbaseCacheHandle(part, couchbaseConfigurationKey, false);

        /// <summary>
        /// Adds a <see cref="BucketCacheHandle{TCacheValue}"/> with the required name.
        /// <para>
        /// This handle requires a Couchbase <see cref="ClientConfiguration"/> to be defined with
        /// the <paramref name="couchbaseConfigurationKey"/> matching the configuration's key.
        /// </para>
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="couchbaseConfigurationKey">
        /// The Couchbase configuration identifier will be used as name for the cache handle and to
        /// retrieve the connection configuration.
        /// </param>
        /// <param name="isBackplaneSource">
        /// Set this to true if this cache handle should be the source of the backplane.
        /// <para>This setting will be ignored if no backplane is configured.</para>
        /// </param>
        /// <returns>The part.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if handleName or handleType are null.
        /// </exception>
        public static ConfigurationBuilderCacheHandlePart WithCouchbaseCacheHandle(
            this ConfigurationBuilderCachePart part,
            string couchbaseConfigurationKey,
            bool isBackplaneSource)
        {
            NotNull(part, nameof(part));
            NotNullOrWhiteSpace(couchbaseConfigurationKey, nameof(couchbaseConfigurationKey));

            return part.WithHandle(typeof(BucketCacheHandle<>), couchbaseConfigurationKey, isBackplaneSource);
        }

        /// <summary>
        /// Adds a <see cref="BucketCacheHandle{TCacheValue}"/> with the required name.
        /// <para>
        /// This handle requires a Couchbase <see cref="ClientConfiguration"/> to be defined with
        /// the <paramref name="couchbaseConfigurationKey"/> matching the configuration's key.
        /// </para>
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="couchbaseConfigurationKey">
        /// The configuration key will be used as name for the cache handle and to retrieve the
        /// connection configuration.
        /// </param>
        /// <param name="bucketName">
        /// The name of the Couchbase bucket which should be used by the cache handle.
        /// </param>
        /// <returns>The part.</returns>
        /// <exception cref="System.ArgumentNullException">If bucketName is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown if handleName is null.</exception>
        public static ConfigurationBuilderCacheHandlePart WithCouchbaseCacheHandle(
            this ConfigurationBuilderCachePart part,
            string couchbaseConfigurationKey,
            string bucketName) =>
            WithCouchbaseCacheHandle(part, couchbaseConfigurationKey, bucketName, false);

        /// <summary>
        /// Adds a <see cref="BucketCacheHandle{TCacheValue}"/> with the required name.
        /// <para>
        /// This handle requires a Couchbase <see cref="ClientConfiguration"/> to be defined with
        /// the <paramref name="couchbaseConfigurationKey"/> matching the configuration's key.
        /// </para>
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="couchbaseConfigurationKey">
        /// The Couchbase configuration identifier will be used as name for the cache handle and to
        /// retrieve the connection configuration.
        /// </param>
        /// <param name="bucketName">
        /// The name of the Couchbase bucket which should be used by the cache handle.
        /// </param>
        /// <param name="isBackplaneSource">
        /// Set this to true if this cache handle should be the source of the backplane.
        /// <para>This setting will be ignored if no backplane is configured.</para>
        /// </param>
        /// <returns>The part.</returns>
        /// <exception cref="System.ArgumentNullException">If bucketName is null.</exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if handleName or handleType are null.
        /// </exception>
        public static ConfigurationBuilderCacheHandlePart WithCouchbaseCacheHandle(this ConfigurationBuilderCachePart part, string couchbaseConfigurationKey, string bucketName, bool isBackplaneSource)
        {
            NotNull(part, nameof(part));
            NotNullOrWhiteSpace(bucketName, nameof(bucketName));

            return part.WithHandle(typeof(BucketCacheHandle<>), couchbaseConfigurationKey + ":" + bucketName, isBackplaneSource);
        }
    }
}