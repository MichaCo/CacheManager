//using System;
//using System.Configuration;
//using CacheManager.Core.Configuration;
//using Couchbase;
//using Couchbase.Configuration;
//using Couchbase.Management;

//namespace CacheManager.Memcached
//{
//    public class CouchbaseCacheHandle<T> : MemcachedClientHandle<T>
//    {
//        private static readonly string DefaultSectionName = "default";

//        private static readonly string DefaultCouchbaseSectionName = "couchbase";
        
//        public Bucket Bucket
//        {
//            get
//            {
//                var section = this.GetSection();
//                var cluster = new CouchbaseCluster(section);
//                var bucket = cluster.GetBucket(section.Bucket);

//                return bucket;
//            }
//        }

//        public override int Count
//        {
//            get
//            {
//                return (int)this.Bucket.BasicStats.ItemCount;
//            }
//        }

//        public ICouchbaseClientConfiguration GetCouchbaseClientConfiguration
//        {
//            get
//            {
//                return this.GetSection();
//            }
//        }

//        public CouchbaseCacheHandle(CacheHandleConfiguration<T> configuration)
//            : base(configuration)
//        {
//            // initialize memcached client with section name which must be equal to handle name...
//            // Default is "enyim.com/memcached"

//            var sectionName = GetSectionName(configuration.HandleName);
//            this.Cache = new CouchbaseClient(sectionName);
//        }

//        private ICouchbaseClientConfiguration GetSection()
//        {
//            string sectionName = GetSectionName(this.Configuration.HandleName);
//            ICouchbaseClientConfiguration section = (ICouchbaseClientConfiguration)ConfigurationManager.GetSection(sectionName);

//            if (section == null)
//            {
//                throw new ConfigurationErrorsException("Memcached client section " + sectionName + " is not found.");
//            }

//            return section;
//        }

//        private static string GetSectionName(string handleName)
//        {
//            if (handleName.Equals(DefaultSectionName, StringComparison.OrdinalIgnoreCase))
//            {
//                return DefaultCouchbaseSectionName;
//            }
//            else
//            {
//                return handleName;
//            }
//        }
//    }
//}