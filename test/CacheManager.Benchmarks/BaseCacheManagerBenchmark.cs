using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using CacheManager.Core;
using Enyim.Caching;
using Enyim.Caching.Configuration;

namespace CacheManager.Benchmarks
{
    [Config(typeof(CacheManagerBenchConfig))]
    public abstract class BaseCacheBenchmark
    {
        private static ICacheManagerConfiguration BaseConfig
            => new ConfigurationBuilder()
            .WithMaxRetries(10)
            .WithRetryTimeout(500)
            .WithJsonSerializer()
            .WithUpdateMode(CacheUpdateMode.Up)
            .Build();

        private static IMemcachedClientConfiguration MemcachedConfig
        {
            get
            {
                var cfg = new MemcachedClientConfiguration();
                cfg.AddServer("localhost", 11211);
                return cfg;
            }
        }

        protected ICacheManager<string> DictionaryCache = new BaseCacheManager<string>(BaseConfig.Builder.WithDictionaryHandle().Build());

        protected ICacheManager<string> RuntimeCache = new BaseCacheManager<string>(BaseConfig.Builder.WithSystemRuntimeCacheHandle().Build());

        protected ICacheManager<string> RedisCache = new BaseCacheManager<string>(
                BaseConfig
                .Builder
                .WithRedisConfiguration("redisKey", "localhost:6379,allowAdmin=true")
                .WithRedisCacheHandle("redisKey")
                .Build());

        protected ICacheManager<string> MsMemoryCache = new BaseCacheManager<string>(BaseConfig.Builder.WithMicrosoftMemoryCacheHandle().Build());

        protected ICacheManager<string> MemcachedCache =
            new BaseCacheManager<string>(BaseConfig.Builder
                .WithMemcachedCacheHandle(new MemcachedClient(MemcachedConfig)).Build());

        [Setup]
        public void Setup()
        {
            this.DictionaryCache.Clear();
            this.RuntimeCache.Clear();
            this.RedisCache.Clear();
            this.MsMemoryCache.Clear();
            this.MemcachedCache.Clear();
            this.SetupBench();
        }

        [Benchmark(Baseline = true)]
        public void Dictionary()
        {
            Excecute(this.DictionaryCache);
        }

        [Benchmark]
        public void Runtime()
        {
            Excecute(this.RuntimeCache);
        }

        [Benchmark]
        public void MsMemory()
        {
            Excecute(this.MsMemoryCache);
        }

        [Benchmark]
        public void Redis()
        {
            Excecute(this.RedisCache);
        }

        [Benchmark]
        public void Memcached()
        {
            Excecute(this.MemcachedCache);
        }

        protected abstract void Excecute(ICacheManager<string> cache);

        protected virtual void SetupBench()
        {
        }
    }

    #region add

    public class AddSingleBenchmark : BaseCacheBenchmark
    {
        private string key = Guid.NewGuid().ToString();

        protected override void Excecute(ICacheManager<string> cache)
        {
            if (!cache.Add(key, "value"))
            {
                cache.Remove(key);
            }
        }
    }

    public class AddWithRegionSingleBenchmark : BaseCacheBenchmark
    {
        private string key = Guid.NewGuid().ToString();

        protected override void Excecute(ICacheManager<string> cache)
        {
            if (!cache.Add(key, "value", "region"))
            {
                cache.Remove(key);
            }
        }
    }

    #endregion add

    #region put

    public class PutSingleBenchmark : BaseCacheBenchmark
    {
        private string key = Guid.NewGuid().ToString();

        protected override void Excecute(ICacheManager<string> cache)
        {
            cache.Put(key, "value");
        }
    }

    public class PutWithRegionSingleBenchmark : BaseCacheBenchmark
    {
        private string key = Guid.NewGuid().ToString();

        protected override void Excecute(ICacheManager<string> cache)
        {
            cache.Put(key, "value", "region");
        }
    }

    #endregion put

    #region get

    public class GetSingleBenchmark : BaseCacheBenchmark
    {
        protected string key = Guid.NewGuid().ToString();

        protected override void Excecute(ICacheManager<string> cache)
        {
            var val = cache.Get(key);
            if (val == null)
            {
                throw new InvalidOperationException();
            }
        }

        protected override void SetupBench()
        {
            base.SetupBench();

            this.DictionaryCache.Add(key, key);
            this.DictionaryCache.Add(key, key, "region");
            this.RuntimeCache.Add(key, key);
            this.RuntimeCache.Add(key, key, "region");
            this.MsMemoryCache.Add(key, key);
            this.MsMemoryCache.Add(key, key, "region");
            this.MemcachedCache.Add(key, key);
            this.MemcachedCache.Add(key, key, "region");
            this.RedisCache.Add(key, key);
            this.RedisCache.Add(key, key, "region");
        }
    }

    public class GetWithRegionSingleBenchmark : GetSingleBenchmark
    {
        protected override void Excecute(ICacheManager<string> cache)
        {
            var val = cache.Get(key, "region");
            if (val == null)
            {
                throw new InvalidOperationException();
            }
        }
    }

    #endregion get

    #region update

    public class UpdateSingleBenchmark : GetSingleBenchmark
    {
        protected override void Excecute(ICacheManager<string> cache)
        {
            var val = cache.Update(key, (v) => v.Equals("bla") ? "bla" : "blub");
            if (val == null)
            {
                throw new InvalidOperationException();
            }
        }
    }

    #endregion upate
}