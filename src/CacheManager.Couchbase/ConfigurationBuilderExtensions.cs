using System;
using CacheManager.Core.Configuration;
using Couchbase.Configuration.Client;

namespace CacheManager.Couchbase
{
    public static class ConfigurationBuilderExtensions
    {
        /// <summary>
        /// Adds a <see cref="ClientConfiguration"/> for the given key.
        /// <para>
        /// The key will be matched with the couchbase cache handle name.
        /// </para>
        /// </summary>
        /// <param name="key">The key which has to match with the cache handle name.</param>
        /// <param name="config">The couchbase configuration object.</param>
        /// <returns>The configuration builder.</returns>
        public static ConfigurationBuilderCachePart<TCacheValue> WithCouchbaseConfiguration<TCacheValue>(this ConfigurationBuilderCachePart<TCacheValue> part, string key, ClientConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException("key");
            }
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            CouchbaseConfigurationManager.AddConfiguration(key, config);
            return part;
        }        
        
        /// <summary>
        /// Add a <see cref="BucketCacheHandle"/> with the required name.
        /// <para>
        /// This handle requires a couchbase <see cref="ClientConfiguration"/> to be defined with the <paramref name="couchbaseConfigurationKey"/> matching the configuration's key.
        /// </para>
        /// </summary>
        /// <param name="couchbaseConfigurationKey">The configuration key will be used as name for the cache handle and to retrieve the connection configuration.</param>
        /// <param name="part">The builder part</param>
        /// <exception cref="ArgumentNullException">Thrown if handleName is null.</exception>
        public static ConfigurationBuilderCacheHandlePart<TCacheValue> WithCouchbaseCacheHandle<TCacheValue>(this ConfigurationBuilderCachePart<TCacheValue> part, string couchbaseConfigurationKey)
        {
            return part.WithHandle<BucketCacheHandle<TCacheValue>>(couchbaseConfigurationKey);
        }

        /// <summary>
        /// Add a <see cref="BucketCacheHandle"/> with the required name.
        /// <para>
        /// This handle requires a couchbase <see cref="ClientConfiguration"/> to be defined with the <paramref name="couchbaseConfigurationKey"/> matching the configuration's key.
        /// </para>
        /// </summary>
        /// <param name="part">The builder part</param>
        /// <param name="couchbaseConfigurationKey">The redis configuration identifier will be used as name for the cache handle and to retrieve the connection configuration.</param>
        /// <param name="isBackPlateSource">Set this to true if this cache handle should be the source of the back plate.
        /// <para>This setting will be ignored if no back plate is configured.</para>
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown if handleName or handleType are null.</exception>
        public static ConfigurationBuilderCacheHandlePart<TCacheValue> WithCouchbaseCacheHandle<TCacheValue>(this ConfigurationBuilderCachePart<TCacheValue> part, string couchbaseConfigurationKey, bool isBackPlateSource)
        {
            return part.WithHandle<BucketCacheHandle<TCacheValue>>(couchbaseConfigurationKey, isBackPlateSource);
        }

        /// <summary>
        /// Add a <see cref="BucketCacheHandle"/> with the required name.
        /// <para>
        /// This handle requires a couchbase <see cref="ClientConfiguration"/> to be defined with the <paramref name="couchbaseConfigurationKey"/> matching the configuration's key.
        /// </para>
        /// </summary>
        /// <param name="couchbaseConfigurationKey">The configuration key will be used as name for the cache handle and to retrieve the connection configuration.</param>
        /// <param name="bucketName">The name of the couchbase bucket which should be used by the cache handle.</param>
        /// <param name="part">The builder part</param>
        /// <exception cref="ArgumentNullException">Thrown if handleName is null.</exception>
        public static ConfigurationBuilderCacheHandlePart<TCacheValue> WithCouchbaseCacheHandle<TCacheValue>(this ConfigurationBuilderCachePart<TCacheValue> part, string couchbaseConfigurationKey, string bucketName)
        {
            if (string.IsNullOrWhiteSpace(bucketName))
            {
                throw new ArgumentNullException("bucketName");
            }

            return part.WithHandle<BucketCacheHandle<TCacheValue>>(couchbaseConfigurationKey + ":" + bucketName);
        }

        /// <summary>
        /// Add a <see cref="BucketCacheHandle"/> with the required name.
        /// <para>
        /// This handle requires a couchbase <see cref="ClientConfiguration"/> to be defined with the <paramref name="couchbaseConfigurationKey"/> matching the configuration's key.
        /// </para>
        /// </summary>
        /// <param name="part">The builder part</param>
        /// <param name="couchbaseConfigurationKey">The redis configuration identifier will be used as name for the cache handle and to retrieve the connection configuration.</param>
        /// <param name="bucketName">The name of the couchbase bucket which should be used by the cache handle.</param>
        /// <param name="isBackPlateSource">Set this to true if this cache handle should be the source of the back plate.
        /// <para>This setting will be ignored if no back plate is configured.</para>
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown if handleName or handleType are null.</exception>
        public static ConfigurationBuilderCacheHandlePart<TCacheValue> WithCouchbaseCacheHandle<TCacheValue>(this ConfigurationBuilderCachePart<TCacheValue> part, string couchbaseConfigurationKey, string bucketName, bool isBackPlateSource)
        {
            if (string.IsNullOrWhiteSpace(bucketName))
            {
                throw new ArgumentNullException("bucketName");
            }

            return part.WithHandle<BucketCacheHandle<TCacheValue>>(couchbaseConfigurationKey + ":" + bucketName, isBackPlateSource);
        }
    }
}