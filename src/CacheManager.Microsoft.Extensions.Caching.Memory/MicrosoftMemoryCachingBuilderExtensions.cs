using System;
using System.Linq;
using CacheManager.MicrosoftCachingMemory;

namespace CacheManager.Core
{
    /// <summary>
    /// Extensions for the configuration builder specific to Microsoft.Extensions.Caching.Memory cache handle.
    /// </summary>
    public static class MicrosoftMemoryCachingBuilderExtensions
    {
        private const string DefaultName = "default";

        public static ConfigurationBuilderCacheHandlePart WithMicrosoftMemoryCacheHandle(
            this ConfigurationBuilderCachePart part, string instanceName)
            => WithMicrosoftMemoryCacheHandle(part, instanceName, false);

        public static ConfigurationBuilderCacheHandlePart WithMicrosoftMemoryCacheHandle(
            this ConfigurationBuilderCachePart part)
            => part?.WithHandle(typeof(MemoryCacheHandle<>), DefaultName, false);

        public static ConfigurationBuilderCacheHandlePart WithMicrosoftMemoryCacheHandle(
            this ConfigurationBuilderCachePart part, string instanceName, bool isBackplaneSource)
            => part?.WithHandle(typeof(MemoryCacheHandle<>), instanceName, isBackplaneSource);
    }
}