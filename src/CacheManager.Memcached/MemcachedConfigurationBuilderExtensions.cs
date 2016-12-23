using System;
using CacheManager.Memcached;
using Enyim.Caching;
using Enyim.Caching.Configuration;
using static CacheManager.Core.Utility.Guard;

namespace CacheManager.Core
{
    /// <summary>
    /// Extensions for the configuration builder specific to the Memcached cache handle.
    /// </summary>
    public static class MemcachedConfigurationBuilderExtensions
    {
        /// <summary>
        /// Adds a <see cref="MemcachedCacheHandle{TCacheValue}"/>. The <paramref name="configurationName"/> must match with cache configured via enyim configuration section.
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="configurationName">The configuration name.</param>
        /// <param name="isBackplaneSource">
        /// Set this to true if this cache handle should be the source of the backplane.
        /// <para>This setting will be ignored if no backplane is configured.</para>
        /// </param>
        /// <returns>The part.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if handleName or handleType are null.
        /// </exception>
        public static ConfigurationBuilderCacheHandlePart WithMemcachedCacheHandle(this ConfigurationBuilderCachePart part, string configurationName, bool isBackplaneSource = true)
        {
            NotNull(part, nameof(part));
            
            return part.WithHandle(typeof(MemcachedCacheHandle<>), configurationName, isBackplaneSource);
        }

        /// <summary>
        /// Adds a <see cref="MemcachedCacheHandle{TCacheValue}"/> with a preconfigured <see cref="MemcachedClient"/> instance.
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="client">The <see cref="MemcachedClient"/> to use for this cache handle.</param>
        /// <param name="isBackplaneSource">
        /// Set this to true if this cache handle should be the source of the backplane.
        /// <para>This setting will be ignored if no backplane is configured.</para>
        /// </param>
        /// <returns>The part.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="client"/> is null.
        /// </exception>
        public static ConfigurationBuilderCacheHandlePart WithMemcachedCacheHandle(this ConfigurationBuilderCachePart part, MemcachedClient client, bool isBackplaneSource = true)
        {
            NotNull(part, nameof(part));
            NotNull(client, nameof(client));

            return part.WithHandle(typeof(MemcachedCacheHandle<>), Guid.NewGuid().ToString(), isBackplaneSource, client);
        }

        /// <summary>
        /// Adds a <see cref="MemcachedCacheHandle{TCacheValue}"/>. The <paramref name="configurationName"/> must match with cache configured via enyim configuration section.
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="configurationName">The configuration name.</param>
        /// <param name="client">The <see cref="MemcachedClient"/> to use for this cache handle.</param>
        /// <param name="isBackplaneSource">
        /// Set this to true if this cache handle should be the source of the backplane.
        /// <para>This setting will be ignored if no backplane is configured.</para>
        /// </param>
        /// <returns>The part.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="client"/> is null.
        /// </exception>
        public static ConfigurationBuilderCacheHandlePart WithMemcachedCacheHandle(this ConfigurationBuilderCachePart part, string configurationName, MemcachedClient client, bool isBackplaneSource = true)
        {
            NotNull(part, nameof(part));

            return part.WithHandle(typeof(MemcachedCacheHandle<>), configurationName, isBackplaneSource, client);
        }

        /// <summary>
        /// Adds a <see cref="MemcachedCacheHandle{TCacheValue}"/> using the <paramref name="clientConfiguration"/> to setup a <see cref="MemcachedClient"/> instance.
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="clientConfiguration">The <see cref="MemcachedClientConfiguration"/> to use to create the <see cref="MemcachedClient"/> for this cache handle.</param>
        /// <param name="isBackplaneSource">
        /// Set this to true if this cache handle should be the source of the backplane.
        /// <para>This setting will be ignored if no backplane is configured.</para>
        /// </param>
        /// <returns>The part.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="clientConfiguration"/> is null.
        /// </exception>
        public static ConfigurationBuilderCacheHandlePart WithMemcachedCacheHandle(this ConfigurationBuilderCachePart part, MemcachedClientConfiguration clientConfiguration, bool isBackplaneSource = true)
        {
            NotNull(part, nameof(part));
            NotNull(clientConfiguration, nameof(clientConfiguration));

            return part.WithHandle(typeof(MemcachedCacheHandle<>), Guid.NewGuid().ToString(), isBackplaneSource, clientConfiguration);
        }

        /// <summary>
        /// Adds a <see cref="MemcachedCacheHandle{TCacheValue}"/> using the <paramref name="clientConfiguration"/> to setup a <see cref="MemcachedClient"/> instance.
        /// </summary>
        /// <param name="part">The builder part.</param>
        /// <param name="configurationName">The configuration name.</param>
        /// <param name="clientConfiguration">The <see cref="MemcachedClientConfiguration"/> to use to create the <see cref="MemcachedClient"/> for this cache handle.</param>
        /// <param name="isBackplaneSource">
        /// Set this to true if this cache handle should be the source of the backplane.
        /// <para>This setting will be ignored if no backplane is configured.</para>
        /// </param>
        /// <returns>The part.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="clientConfiguration"/> is null.
        /// </exception>
        public static ConfigurationBuilderCacheHandlePart WithMemcachedCacheHandle(this ConfigurationBuilderCachePart part, string configurationName, MemcachedClientConfiguration clientConfiguration, bool isBackplaneSource = true)
        {
            NotNull(part, nameof(part));

            return part.WithHandle(typeof(MemcachedCacheHandle<>), configurationName, isBackplaneSource, clientConfiguration);
        }
    }
}