using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using CacheManager.Core;
using Enyim.Caching;
using Enyim.Caching.Configuration;

namespace CacheManager.Benchmarks
{
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
            DictionaryCache.Clear();
            RuntimeCache.Clear();
            RedisCache.Clear();
            MsMemoryCache.Clear();
            MemcachedCache.Clear();
            SetupBench();
        }

        [Benchmark(Baseline = true)]
        public void Dictionary()
        {
            Excecute(DictionaryCache);
        }

        [Benchmark]
        public void Runtime()
        {
            Excecute(RuntimeCache);
        }

        [Benchmark]
        public void MsMemory()
        {
            Excecute(MsMemoryCache);
        }

        [Benchmark]
        public void Redis()
        {
            Excecute(RedisCache);
        }

        [Benchmark]
        public void Memcached()
        {
            Excecute(MemcachedCache);
        }

        protected abstract void Excecute(ICacheManager<string> cache);

        protected virtual void SetupBench()
        {
        }
    }

    #region add

    public class AddSingleBenchmark : BaseCacheBenchmark
    {
        private string _key = Guid.NewGuid().ToString();

        protected override void Excecute(ICacheManager<string> cache)
        {
            if (!cache.Add(_key, "value"))
            {
                cache.Remove(_key);
            }
        }
    }

    public class AddWithRegionSingleBenchmark : BaseCacheBenchmark
    {
        private string _key = Guid.NewGuid().ToString();

        protected override void Excecute(ICacheManager<string> cache)
        {
            if (!cache.Add(_key, "value", "region"))
            {
                cache.Remove(_key);
            }
        }
    }

    #endregion add

    #region put

    public class PutSingleBenchmark : BaseCacheBenchmark
    {
        private string _key = Guid.NewGuid().ToString();

        protected override void Excecute(ICacheManager<string> cache)
        {
            cache.Put(_key, "value");
        }
    }

    public class PutWithRegionSingleBenchmark : BaseCacheBenchmark
    {
        private string _key = Guid.NewGuid().ToString();

        protected override void Excecute(ICacheManager<string> cache)
        {
            cache.Put(_key, "value", "region");
        }
    }

    #endregion put

    #region get

    public class GetSingleBenchmark : BaseCacheBenchmark
    {
        protected string Key = Guid.NewGuid().ToString();

        protected override void Excecute(ICacheManager<string> cache)
        {
            var val = cache.Get(Key);
            if (val == null)
            {
                throw new InvalidOperationException();
            }
        }

        protected override void SetupBench()
        {
            base.SetupBench();

            DictionaryCache.Add(Key, Key);
            DictionaryCache.Add(Key, Key, "region");
            RuntimeCache.Add(Key, Key);
            RuntimeCache.Add(Key, Key, "region");
            MsMemoryCache.Add(Key, Key);
            MsMemoryCache.Add(Key, Key, "region");
            MemcachedCache.Add(Key, Key);
            MemcachedCache.Add(Key, Key, "region");
            RedisCache.Add(Key, Key);
            RedisCache.Add(Key, Key, "region");
        }
    }

    public class GetWithRegionSingleBenchmark : GetSingleBenchmark
    {
        protected override void Excecute(ICacheManager<string> cache)
        {
            var val = cache.Get(Key, "region");
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
            var val = cache.Update(Key, (v) => v.Equals("bla") ? "bla" : "blub");
            if (val == null)
            {
                throw new InvalidOperationException();
            }
        }
    }

    #endregion upate
}