using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using CacheManager.Core;
using CacheManager.Core.Cache;
using CacheManager.Core.Configuration;
using CacheManager.Tests.TestCommon;
using FluentAssertions;
using Xunit;
using Xunit.Extensions;

namespace CacheManager.Tests.Core
{
    /// <summary>
    ///
    /// </summary>
    [ExcludeFromCodeCoverage]
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