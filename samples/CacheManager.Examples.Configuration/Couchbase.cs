using System;
using System.Collections.Generic;
using CacheManager.Core;
using Couchbase;
using Couchbase.Authentication;
using Couchbase.Configuration.Client;

namespace Configuration
{
    public class Couchbase
    {
        public void UsingClusterHelper()
        {
            var cfg = new ClientConfiguration()
            {
                Servers = new List<Uri>()
                {
                    new Uri("http://127.0.0.1:8091")
                }
            };

            ClusterHelper.Initialize(cfg);
            ClusterHelper.Get().Authenticate(new PasswordAuthenticator("admin", "password"));

            // using cluster helper is enough for CacheManager since 1.0.2 as it falls back to ClusterHelper internally
            var cacheConfig = new ConfigurationBuilder()
                .WithCouchbaseCacheHandle("cb")
                .Build();

            var cache = new BaseCacheManager<int>(cacheConfig);
            cache.AddOrUpdate("test", 1, (v) => v + 1);
        }

        public void UsingAppConfig()
        {
            // see couchbaseClients/couchbase section in app.config of this project
            var cacheConfig = new ConfigurationBuilder()
                .WithCouchbaseCacheHandle("couchbaseClients/couchbase")
                .Build();

            var cache = new BaseCacheManager<int>(cacheConfig);
            cache.AddOrUpdate("test", 1, (v) => v + 1);
        }

        public void UsingAppConfigWithAuthentication()
        {
            // see couchbaseClients/couchbase section in app.config of this project
            // Note: even though we pass in "cb", CacheManager will fall back to the
            // default couchbase section at couchbaseClients/couchbase!
            // We could also pass in the section name explicitly instead of "cb".

            ClusterHelper.Initialize("couchbaseClients/couchbase");
            ClusterHelper.Get().Authenticate(new PasswordAuthenticator("admin", "password"));

            var cacheConfig = new ConfigurationBuilder()
                .WithCouchbaseCacheHandle("keydoesnotmatter")
                .Build();

            var cache = new BaseCacheManager<int>(cacheConfig);
            cache.AddOrUpdate("test", 1, (v) => v + 1);
        }

        public void UsingClientConfiguration()
        {
            var cacheConfig = new ConfigurationBuilder()
                .WithCouchbaseConfiguration("myConfig", new ClientConfiguration()) // add the configuration you need here...
                .WithCouchbaseCacheHandle("myConfig")
                .Build();
        }

        public void UsingAlreadyDefinedCluster()
        {
            var cluster = new Cluster(new ClientConfiguration());
            cluster.Authenticate(new PasswordAuthenticator("admin", "password"));

            var cacheConfig = new ConfigurationBuilder()
                .WithCouchbaseCluster("myCluster", cluster)
                .WithCouchbaseCacheHandle("myCluster")
                .Build();
        }

        public void UsingExplicitBucketName()
        {
            ClusterHelper.Initialize();
            ClusterHelper.Get().Authenticate(new PasswordAuthenticator("admin", "password"));

            var cacheConfig = new ConfigurationBuilder()
                .WithCouchbaseCacheHandle("keydoesnotmatter", "beer-sample")    // passing in a bucket name which should be used
                .Build();

            var cache = new BaseCacheManager<int>(cacheConfig);
            cache.AddOrUpdate("test", 1, (v) => v + 1);
        }

        public void UsingExplicitBucketNameWithPassword()
        {
            var cacheConfig = new ConfigurationBuilder()
                .WithCouchbaseConfiguration("cb", new ClientConfiguration())
                .WithCouchbaseCacheHandle("cb", "secret-bucket", "secret")    // passing in a bucket-name and bucket-password
                .Build();

            var cache = new BaseCacheManager<int>(cacheConfig);
            cache.AddOrUpdate("test", 1, (v) => v + 1);
        }
    }
}