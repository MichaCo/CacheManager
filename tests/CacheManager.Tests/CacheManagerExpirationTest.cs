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
            var cfg = ConfigurationBuilder.LoadConfigurationFile<string>(fileName, cacheName);

            using (var cache = CacheFactory.FromConfiguration(cfg))
            {
                cache.Put("key", "value");
                
                Thread.Sleep(500);

                cache.Get("key").Should().Be("value");

                Thread.Sleep(501);

                cache.Get("key").Should().BeNull("Should be expired.");
            }
        }

        private static string GetCfgFileName(string fileName)
        {
            return AppDomain.CurrentDomain.BaseDirectory + (fileName.StartsWith("\\") ? fileName : "\\" + fileName);
        }
    }
}