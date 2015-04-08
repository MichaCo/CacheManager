# Features and Architecture

## Standard operations
First and foremost Cache Manager will provide well known cache methods like Get, Put, Remove and Clear.
All cache items will have a `string Key` and `T` Value where `T` can be anything, e.g. `int`, `string` or even `object`.

    cache.Add("key", "value");
    var value = cache.Get("key");
    cache.Remove("key");
    cache.Clear();
        
### Regions
Cache Manager has overloads for all cache methods to support a `string Region` in addition to the `Key` to identify an item within the cache. 

    cache.Add("key", "value", "region");
    var value = cache.Get("key", "region");
    cache.Remove("key", "region");
    
> **Note** 
> Each cache handle might implement cache regions differently. Often the implementation will simply concatenate `region + key` to form the cache key. 
> Also the cache `Key` will only be accessible together with the region specified.

To clear a cache region, you can call `cache.ClearRegion("region")`

## Cache Handles
One of the main feature of Cache Manager is handling multiple cache layers. To define the cache layers the cache manager can have one or many so called cache handles.  
Each Cache Manager package for a cache provider implement such a cache handle.

This concept makes it very flexible in terms of caching strategy. And it makes it easy to start with and maybe create a more complex cache later.

To configure and add cache handles by code call the `WithHandle` method of the `ConfigurationBuilder`. 
Every cache provider specific Cache Manager package will provide an extension method to add the specific cache handle, e.g. `WithSystemRuntimeCacheHandle`, `WithRedisCacheHandle`...

Example:

    var cache = CacheFactory.Build("myCacheName", settings =>
    {
	    settings
		    .WithSystemRuntimeCacheHandle("handle1");
    });

Adding multiple cache handles looks pretty much the same:

    var cache = CacheFactory.Build("myCacheName", settings =>
    {
	    settings
		    .WithSystemRuntimeCacheHandle("handle1")
            .And
            .WithRedisCacheHandle("redis");
    });

> Read the Cache Synchronization article for more information about how to keep multiple cache layers in sync.

### Cache Item handling
The configured cache handles will be stored as a simple list. But it is important to know that the order of how the cache handles are added to the Cache Manager matters.  
When retrieving an item from Cache Manager, it will iterate over all cache handles and returns the item from the first cache handle it finds the item.   

All other cache operations, `Set`, `Put`, `Update`, `Remove`, `Clear` and `ClearRegion`will be executed on **all configured cache handles**.  This is necessary because in general we want to have all layers of our cache in sync.

### CacheUpdateMode
Let's say we have two cache handles configured, and the `Get` operation finds the `Key` within the second cache handle. 
Now we can assume that the two configured layers of the cache have some purpose and that the CacheManager should maybe update the other cache handles.   

There are 3 different configuration options for Cache Manager to handle this, defined by `CacheUpdateMode`:

* **None** - setting `CacheUpdateMode`to `None` will instruct the Cache Manager to do nothing on cache hits.
* **Up** - instructs the Cache Manager to update cache handles "above" the one the cache item was found in. The order of the cache handles matter in this case. 
* **All** - instructs the Cache Manager to update all other cache handles

## Configuration
The purpose of Cache Manager is to make it easy to work with different cache systems but at the same time it should be flexible to adopt to different needs.
Therefore the configuration is a pretty important part of Cache Manager. 

Developers have the choice to use different ways to configure Cache Manager. Via code using the `CacheFactory` or `ConfigurationBuilder`, or via web/app.config using the Cache Manager's configuration section.

That being said, most cache provider already come with configuration sections to configure the different clients. Cache Manager will not add another configuration for those, instead it will use the existing configuration options of the different clients!

> **Hint**
> To do so, Cache Manager uses the Cache Handle's name to find the 3rd party's configuration. 
> Make sure to read the cache handle configuration documentation for specific information of how to configure it correctly. It may vary from implementation to implementation.

### CacheFactory
To create a Cache Manager instance one should use the `CacheFactory` class which has two different methods: 

**`Build`** can be used to create a new cache configuration via an `Action` using a fluent configuration builder. 

	var cache = CacheFactory.Build("cacheName", settings => settings
        .WithUpdateMode(CacheUpdateMode.Up)
        .WithSystemRuntimeCacheHandle("handleName")
            .EnableStatistics()
            .EnablePerformanceCounters()
        .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10)));

**`FromConfiguration`** takes either the `CacheManagerConfiguration` object or the `name` of the Cache Manager configured in the .config file of the application.

	var cache = CacheFactory.FromConfiguration("cacheName")

If you want to separate the creation of the configuration object and the creation of the Cache Manager instance, use the `ConfigurationBuilder` to create the configuration.

	var cfg = ConfigurationBuilder.BuildConfiguration<object>("cacheName", settings =>
		{
			settings.WithUpdateMode(CacheUpdateMode.Up)
                .WithSystemRuntimeCacheHandle("handleName")
					.EnablePerformanceCounters()
					.WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10));
		});

	var cache = CacheFactory.FromConfiguration(cfg);
	cache.Add("key", "value");

### Configuration Section
The Cache Manager configuration section has two main parts, the `managers`  collection which is used to configure the cache manager instances. The `name` of the element within the collection can be passed to `CacheFactory.FromConfiguration`.

And the `cacheHandles` collection which lists the available (installed) cache handle types. Those will be used by referencing the `id` to form a cache in the `managers` collection.

    <cacheManager xmlns="http://tempuri.org/CacheManagerCfg.xsd">
      <managers>
        <cache name="cacheName" updateMode="Up" enableStatistics="true" enablePerformanceCounters="true">
          <handle name="handleName" ref="systemRuntimeHandle" expirationMode="Absolute" timeout="50s"/>
        </cache>
      </managers>
      <cacheHandles>
        <handleDef  id="systemRuntimeHandle" type="CacheManager.SystemRuntimeCaching.MemoryCacheHandle`1, CacheManager.SystemRuntimeCaching"
            defaultExpirationMode="Sliding" defaultTimeout="5m"/>
      </cacheHandles>
    </cacheManager>

The cacheHandles' elements can be defined with default values for expiration mode and timeout. It can be overridden by the `handle` element though.

> **Hint**
> To make configuration via .config file easier, enable intellisense by adding the `xmlns` attribute to he `cacheManagers` section and add the CacheManagerCfg.xsd file to your solution. The xsd file can be found in  [solution dir]\packages\CacheManager.Core.x.x.x.x\CacheManagerCfg.xsd
> See also [this answer on stackoverflow](http://stackoverflow.com/questions/742905/enabling-intellisense-for-custom-sections-in-config-files)

## Cache Expiration
Setting an expiration timeout for cache items is a common thing when working with caching because we might not want the cache item to be stored in memory for ever, to free up resources.

With Cache Manager, it is possible to control the cache expiration on multiple levels or per cache item.

## Statistics and Counters

## Events

## System.Web.OutputCache

[TOC]