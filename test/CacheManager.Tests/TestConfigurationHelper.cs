using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Tests
{
    [ExcludeFromCodeCoverage]
    public static class TestConfigurationHelper
    {
#if !NETCOREAPP1

        public static string GetCfgFileName(string fileName)
        {
            NotNullOrWhiteSpace(fileName, nameof(fileName));
            var basePath = Environment.CurrentDirectory;
            return basePath + (fileName.StartsWith("/") ? fileName : "/" + fileName);
        }

#endif
    }
}