## Cache Manager Configuration
A big goal of Cache Manager is to make it easy to work with different cache systems, but at the same time it should be flexible to adopt to different needs.
Therefore the configuration is a pretty important part of Cache Manager. 

Developers have the choice to use different ways to configure Cache Manager. Via code using the `CacheFactory` or `ConfigurationBuilder`, or via *web/app.config* using the Cache Manager's configuration section.

That being said, most cache provider already come with configuration sections to configure the different clients. Cache Manager will not add another configuration for those, instead it will use the existing configuration options of the different clients!

> **Hint**
> To do so, Cache Manager uses the Cache Handle's name to find the 3rd party's configuration. 
> Make sure to read the cache handle configuration documentation for specific information of how to configure it correctly. It may vary from implementation to implementation.

### CacheFactory
To create a Cache Manager instance you can use the `CacheFactory` class which has two different methods: 

**`Build`** can be used to create a new cache configuration via an `Action` using a fluent configuration builder. 

	var cache = CacheFactory.Build("cacheName", settings => settings
        .WithUpdateMode(CacheUpdateMode.Up)
        .WithSystemRuntimeCacheHandle("handleName")
	        .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10)));

**`FromConfiguration`** takes either the `CacheManagerConfiguration` object or the `name` of the Cache Manager configured in the *app/web.config* file of the application. The type of the Cache Manager instance also must be specified now.

	var cache = CacheFactory.FromConfiguration<string>("cacheName")

Optionally, the name of the Cache Manager instance and the name of the configured cache can be defined separated, to create multiple instances from the same configuration:

	var cache = CacheFactory.FromConfiguration<int>("cacheInstanceName", "configuredCacheName")

If you want to separate the creation of the configuration object and the creation of the Cache Manager instance, use the `ConfigurationBuilder` to create the configuration.

	var cfg = ConfigurationBuilder.BuildConfiguration(settings =>
		{
			settings.WithUpdateMode(CacheUpdateMode.Up)
                .WithSystemRuntimeCacheHandle("handleName")
					.WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10));
		});

	var cache = CacheFactory.FromConfiguration<string>("cacheName", cfg);
	cache.Add("key", "value");

And now, you can reuse the configuration and create another Cache Manager instance for a different type for example:

	var cache = CacheFactory.FromConfiguration<int>("numbers", cfg);


### Configuration Section
The Cache Manager configuration section has two main parts, the `managers`  collection which is used to configure the cache manager instances. The `name` of the element within the collection can be passed to `CacheFactory.FromConfiguration`.

And the `cacheHandles` collection which lists the available (installed) cache handle types. Those will be used by referencing the `id` to form a cache in the `managers` collection.

    <cacheManager xmlns="http://tempuri.org/CacheManagerCfg.xsd">
      <managers>
        <cache name="cacheName" updateMode="Up" enableStatistics="false" enablePerformanceCounters="false">
          <handle name="handleName" ref="systemRuntimeHandle" expirationMode="Absolute" timeout="50s"/>
        </cache>
      </managers>
      <cacheHandles>
        <handleDef  id="systemRuntimeHandle" type="CacheManager.SystemRuntimeCaching.MemoryCacheHandle`1, CacheManager.SystemRuntimeCaching"
            defaultExpirationMode="Sliding" defaultTimeout="5m"/>
      </cacheHandles>
    </cacheManager>

The cacheHandles' elements can be defined with default values for expiration mode and timeout. It can be overridden by the `handle` element though. The `type` must a `Type` extending from `BaseCacheHandle`, it also has to be an open generic at this point.

> **Hint**
> To make configuration via .config file easier, enable intellisense by adding the `xmlns` attribute to he `cacheManagers` section and add the CacheManagerCfg.xsd file to your solution. The xsd file can be found in  [solution dir]/packages/CacheManager.Core.x.x.x.x/CacheManagerCfg.xsd
> See also [this answer on stackoverflow][stackoverflow-config-xsd]

[TOC]