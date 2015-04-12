using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using CacheManager.Core;
using CacheManager.Core.Cache;
using CacheManager.Core.Configuration;
using FluentAssertions;
using Xunit;

namespace CacheManager.Tests
{
    /// <summary>
    ///
    /// </summary>
    [ExcludeFromCodeCoverage]
#if NET40
    [Trait("Framework", "NET40")]
#else
    [Trait("Framework", "NET45")]
#endif
    public class CacheManagerExpirationTest
    {
        [Fact]
        [ReplaceCulture]
        public void CacheManager_Configuration_AbsoluteExpires() 
        {
            // arrange
            string fileName = GetCfgFileName(@"\Configuration\configuration.ExpireTest.config");
            string cacheName = "MemoryCacheAbsoluteExpire";

            // act
            var cfg = ConfigurationBuilder.LoadConfigurationFile(fileName, cacheName);

            using (var cache = CacheFactory.FromConfiguration<string>(cacheName, cfg))
            {
                cache.Put("key", "value");
                
                Thread.Sleep(500);

                cache.Get("key").Should().Be("value");

                Thread.Sleep(501);

                cache.Get("key").Should().BeNull("Should be expired.");
            }
        }

        [Fact]
        public void BaseCacheHandle_ExpirationInherits_Issue_1()
        {
            using(var cache = CacheFactory.Build("testCache", settings => {
                settings.WithSystemRuntimeCacheHandle("handleA")
                        .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(10))
                    .And
                    .WithSystemRuntimeCacheHandle("handleB");
            }))
            {
                cache.Add("something", "stuip");

                var handles = cache.CacheHandles.ToArray();
                handles[0].GetCacheItem("something").ExpirationMode.Should().Be(ExpirationMode.Absolute);

                // second cache should not inherit the expiration
                handles[1].GetCacheItem("something").ExpirationMode.Should().Be(ExpirationMode.None);
                handles[1].GetCacheItem("something").ExpirationTimeout.Should().Be(default(TimeSpan));
            }
        }

        private static string GetCfgFileName(string fileName)
        {
            return AppDomain.CurrentDomain.BaseDirectory + (fileName.StartsWith("\\") ? fileName : "\\" + fileName);
        }
    }
}