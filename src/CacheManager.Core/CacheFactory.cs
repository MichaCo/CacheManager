using System;
using CacheManager.Core.Cache;
using CacheManager.Core.Configuration;

namespace CacheManager.Core
{
    /// <summary>
    /// Helper class to instantiate new <see cref="ICacheManager{TCacheValue}"/> instances from configuration.
    /// </summary>
    public static class CacheFactory
    {
        /// <summary>
        /// <para>Instantiates a cache manager from app.config or web.config.</para>
        /// <para>The <paramref name="name" /> must match with one cache element defined in your config file.</para>
        /// </summary>
        /// <example>
        /// The following example show how to use the CacheFactory to create a new cache manager
        /// instance from app/web.config.
        /// <code><![CDATA[
        ///     var cache = CacheFactory.FromConfiguration<object>("myCache");
        ///     cache.Add("key", "value");
        /// ]]></code>
        /// </example>
        /// <param name="name">
        /// The name of the configured cache element within the config file.
        /// </param>
        /// <typeparam name="TCacheValue">The type of the cache item value.</typeparam>
        /// <returns>The cache manager instance.</returns>
        /// <seealso cref="ICacheManager{TCacheValue}"/>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if the <paramref name="name" /> is null or an empty string.
        /// </exception>
        /// <exception cref="System.Configuration.ConfigurationErrorsException">
        /// Thrown if there are configuration errors within the cacheManager section.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if no cacheManager section is defined or on certain configuration errors
        /// related to the cache handles.
        /// </exception>
        public static ICacheManager<TCacheValue> FromConfiguration<TCacheValue>(string name)
        {
            return CacheReflectionHelper.FromConfiguration<TCacheValue>(name);
        }

        /// <summary>
        /// <para>
        /// Instantiates a cache manager using the given <paramref name="configuration"/>.
        /// </para>
        /// </summary>
        /// <example>
        /// The following example show how to build a <c>CacheManagerConfiguration</c>
        /// and then using the <c>CacheFactory</c> to create a new cache manager instance.
        /// <code><![CDATA[
        ///CacheManagerConfiguration<object> managerConfiguration = ConfigurationBuilder.BuildConfiguration<object>("myCacheName", settings =>
        /// {
        ///     settings.WithUpdateMode(CacheUpdateMode.Up)
        ///         .WithHandle<DictionaryCacheHandle<object>>("handle1")
        ///             .EnablePerformanceCounters()
        ///             .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10));
        /// });
        /// 
        /// var cache = CacheFactory.FromConfiguration<object>(managerConfiguration);
        /// cache.Add("key", "value");
        /// ]]></code>
        /// </example>
        /// <param name="configuration">
        /// The configured which will be used to configure the cache manager instance.
        /// </param>
        /// <typeparam name="TCacheValue">The type of the cache item value.</typeparam>
        /// <returns>The cache manager instance.</returns>
        /// <see cref="ConfigurationBuilder"/>
        /// <seealso cref="ICacheManager{TCacheValue}"/>
        /// <seealso cref="CacheManagerConfiguration{TCacheValue}"/>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if the <paramref name="configuration"/> is null.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown on certain configuration errors related to the cache handles.
        /// </exception>
        public static ICacheManager<TCacheValue> FromConfiguration<TCacheValue>(CacheManagerConfiguration<TCacheValue> configuration)
        {
            return CacheReflectionHelper.FromConfiguration<TCacheValue>(configuration);
        }

        /// <summary>
        /// <para>
        /// Instantiates a cache manager using the inline configuration defined by <paramref name="settings"/>.
        /// </para>
        /// <para>
        /// This Build method returns a <c>ICacheManager</c> with cache item type being <c>System.Object</c>.
        /// </para>
        /// </summary>
        /// <example>
        /// The following example show how to build a <c>CacheManagerConfiguration</c>
        /// and then using the <c>CacheFactory</c> to create a new cache manager instance.
        /// <code><![CDATA[
        ///var cache = CacheFactory.Build("myCacheName", settings =>
        ///{
        ///    settings
        ///        .WithUpdateMode(CacheUpdateMode.Up)
        ///        .WithHandle<DictionaryCacheHandle>("handle1")
        ///            .EnablePerformanceCounters()
        ///            .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10));
        ///});
        ///
        ///cache.Add("key", "value");
        /// ]]></code>
        /// </example>
        /// <param name="cacheName">
        /// The name of the cache manager instance.
        /// </param>
        /// <param name="settings">
        /// The configuration. Use the settings element to configure the cache manager instance,
        /// add cache handles and also to configure the cache handles in a fluent way.
        /// </param>
        /// <returns>The cache manager instance with cache item type being <c>System.Object</c>.</returns>
        /// <seealso cref="ICacheManager{TCacheValue}"/>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if the <paramref name="cacheName"/> or <paramref name="settings"/> is null.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown on certain configuration errors related to the cache handles.
        /// </exception>
        public static ICacheManager<object> Build(string cacheName, Action<ConfigurationBuilderCachePart<object>> settings)
        {
            return Build<object>(cacheName, settings);
        }

        /// <summary>
        /// <para>
        /// Instantiates a cache manager using the inline configuration defined by <paramref name="settings"/>.
        /// </para>
        /// </summary>
        /// <example>
        /// The following example show how to build a <c>CacheManagerConfiguration</c>
        /// and then using the <c>CacheFactory</c> to create a new cache manager instance.
        /// <code><![CDATA[
        ///var cache = CacheFactory.Build("myCacheName", settings =>
        ///{
        ///    settings
        ///        .WithUpdateMode(CacheUpdateMode.Up)
        ///        .WithHandle<DictionaryCacheHandle>("handle1")
        ///            .EnablePerformanceCounters()
        ///            .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10));
        ///});
        ///
        ///cache.Add("key", "value");
        /// ]]></code>
        /// </example>
        /// <param name="cacheName">
        /// The name of the cache manager instance.
        /// </param>
        /// <param name="settings">
        /// The configuration. Use the settings element to configure the cache manager instance,
        /// add cache handles and also to configure the cache handles in a fluent way.
        /// </param>
        /// <typeparam name="TCacheValue">The type of the cache item value.</typeparam>
        /// <returns>The cache manager instance with cache item type being <c>TCacheValue</c>.</returns>
        /// <seealso cref="ICacheManager{TCacheValue}"/>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if the <paramref name="cacheName"/> or <paramref name="settings"/> is null.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown on certain configuration errors related to the cache handles.
        /// </exception>
        public static ICacheManager<TCacheValue> Build<TCacheValue>(string cacheName, Action<ConfigurationBuilderCachePart<TCacheValue>> settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            var part = new ConfigurationBuilderCachePart<TCacheValue>(cacheName);
            settings(part);
            return CacheReflectionHelper.FromConfiguration<TCacheValue>(part.Configuration);
        }
    }
}