﻿using System;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using CacheManager.Core;

namespace CacheManager.Benchmarks
{
    [ExcludeFromCodeCoverage]
    public abstract class BaseCacheBenchmark
    {
        private static ICacheManagerConfiguration BaseConfig
            => new CacheConfigurationBuilder()
            .WithMaxRetries(10)
            .WithRetryTimeout(500)
            .WithJsonSerializer()
            .WithUpdateMode(CacheUpdateMode.Up)
            .Build();


        protected ICacheManager<string> DictionaryCache = new BaseCacheManager<string>(BaseConfig.Builder.WithDictionaryHandle().Build());

        protected ICacheManager<string> RuntimeCache = new BaseCacheManager<string>(BaseConfig.Builder.WithSystemRuntimeCacheHandle().Build());

        protected ICacheManager<string> RedisCache = new BaseCacheManager<string>(
                BaseConfig
                .Builder
                .WithRedisConfiguration("redisKey", "localhost:6379,allowAdmin=true")
                .WithRedisCacheHandle("redisKey")
                .Build());

        protected ICacheManager<string> MsMemoryCache = new BaseCacheManager<string>(BaseConfig.Builder.WithMicrosoftMemoryCacheHandle().Build());

        [GlobalSetup]
        public void Setup()
        {
            DictionaryCache.Clear();
            RuntimeCache.Clear();
            RedisCache.Clear();
            MsMemoryCache.Clear();
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

        protected abstract void Excecute(ICacheManager<string> cache);

        protected virtual void SetupBench()
        {
        }
    }

    #region add

    [ExcludeFromCodeCoverage]
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

    [ExcludeFromCodeCoverage]
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

    [ExcludeFromCodeCoverage]
    public class PutSingleBenchmark : BaseCacheBenchmark
    {
        private string _key = Guid.NewGuid().ToString();

        protected override void Excecute(ICacheManager<string> cache)
        {
            cache.Put(_key, "value");
        }
    }

    [ExcludeFromCodeCoverage]
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

    [ExcludeFromCodeCoverage]
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
            RedisCache.Add(Key, Key);
            RedisCache.Add(Key, Key, "region");
        }
    }

    [ExcludeFromCodeCoverage]
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

    [ExcludeFromCodeCoverage]
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
