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
	        .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10)));

**`FromConfiguration`** takes either the `CacheManagerConfiguration` object or the `name` of the Cache Manager configured in the .config file of the application.

	var cache = CacheFactory.FromConfiguration("cacheName")

If you want to separate the creation of the configuration object and the creation of the Cache Manager instance, use the `ConfigurationBuilder` to create the configuration.

	var cfg = ConfigurationBuilder.BuildConfiguration<object>("cacheName", settings =>
		{
			settings.WithUpdateMode(CacheUpdateMode.Up)
                .WithSystemRuntimeCacheHandle("handleName")
					.WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10));
		});

	var cache = CacheFactory.FromConfiguration(cfg);
	cache.Add("key", "value");

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

The cacheHandles' elements can be defined with default values for expiration mode and timeout. It can be overridden by the `handle` element though.

> **Hint**
> To make configuration via .config file easier, enable intellisense by adding the `xmlns` attribute to he `cacheManagers` section and add the CacheManagerCfg.xsd file to your solution. The xsd file can be found in  [solution dir]/packages/CacheManager.Core.x.x.x.x/CacheManagerCfg.xsd
> See also [this answer on stackoverflow][stackoverflow-config-xsd]

## Cache Expiration
Setting an expiration timeout for cache items is a common thing when working with caching because we might not want the cache item to be stored in memory for ever and to free up resources.

With Cache Manager, it is possible to control the cache expiration per cache handle or override it per cache item.

As seen in the examples above, setting the expiration always has two parts, the `ExpirationMode` and the timeout (`TimeSpan`), e.g. `.WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10))`.

The `ExpirationMode` has three possible values:

* **None** will instruct the Cache Manager to not set any expiration.
* **Absolute** will set an absolute date on which the cache item should expire.
* **Sliding** will also set a date on which the cache item should expire, but this date will be extended by `expirationTimeout` every time the cache item gets hit.

To control the expiration per cache item, a `CacheItem` object has to be created and passed to the Cache Manager's methods instead of key value. 

> **Hint** 
> This is really only needed to control the expiration per cache item. To simply get or put an item, use `Get(key)`, `Put(key, value)`...

	var item = new CacheItem<string>("key", "value", ExpirationMode.Absolute, TimeSpan.FromMinutes(10));
    cache.Add(item);

To retrieve a cache item and change the expiration, use the `GetCacheItem` method.

    var item = cache.GetCacheItem("key");
    item.WithExpiration(ExpirationMode.Sliding, TimeSpan.FromMinutes(15));
    cache.Put(item);

> **Note**
> Currently the Memcached and Couchbase cache handles do not support sliding expiration.

## Statistics and Counters
Ever wondered how many cache misses and hits occurred while running your application? 
There are two models implemented in Cache Manager to get those numbers.

The statistics and Windows performance counters. Both are stored per cache handle.

Both can be enabled or disabled per cache manager instance. Especially performance counters can cause a performance overhead and should only be used for analysis if needed. The configuration can be done via .config file or `ConfigurationBuilder`

	var cache = CacheFactory.Build("cacheName", settings => settings
        .WithSystemRuntimeCacheHandle("handleName")
	        .EnableStatistics()
	        .EnablePerformanceCounters());

> **Note**
> Disabling statistics though will also disable performance counters and enabling performance counters will enable statistics.

### Statistics
Statistics are a collection of numbers identified via `CacheStatsCounterType` enum which stores the following numbers:

* cache hits 
* cache misses
* number of items in the cache
* number of `Remove` calls
* number of `Add` calls
* number of `Put` calls
* number of `Get` calls
* number of `Clear` calls
* number of `ClearRegion` calls

Statistics can be retrieved for each handle by calling `handle.GetStatistic(CacheStatsCounterType)`.

*Example:*

    foreach (var handle in cache.CacheHandles)
    {
        var stats = handle.Stats;
        Console.WriteLine(string.Format(
                "Items: {0}, Hits: {1}, Miss: {2}, Remove: {3}, ClearRegion: {4}, Clear: {5}, Adds: {6}, Puts: {7}, Gets: {8}",
                    stats.GetStatistic(CacheStatsCounterType.Items),
                    stats.GetStatistic(CacheStatsCounterType.Hits),
                    stats.GetStatistic(CacheStatsCounterType.Misses),
                    stats.GetStatistic(CacheStatsCounterType.RemoveCalls),
                    stats.GetStatistic(CacheStatsCounterType.ClearRegionCalls),
                    stats.GetStatistic(CacheStatsCounterType.ClearCalls),
                    stats.GetStatistic(CacheStatsCounterType.AddCalls),
                    stats.GetStatistic(CacheStatsCounterType.PutCalls),
                    stats.GetStatistic(CacheStatsCounterType.GetCalls)
                ));
    }

### Performance Counters

If performance counters are enabled, Cache Manager will try to create new `PerformanceCounterCategory` named ".Net CacheManager". With several counters below. This is how it will look like in Server Explorer:
![Performance Counters in Server Explorer][server-explorer]

> **Note** 
> The creation of performance counter categories might fail because your application might run in a security context which doesn't allow the creation.
> In this case Cache Manager will silently disable performance counters.

To watch performance counters in action, run "perfmon.exe", select "Performance Monitor" and click the green plus sign on the toolbar. Now find ".Net Cache Manager" in the list, (should be at the top). And select the instances and counters you want to track.

The result should look similar to this:
![Performance Counters in Server Explorer][perfmon]

The instance name displayed in Performance Monitor is the host name of your application combined with the cache and cache handle's name. 

## Events

## System.Web.OutputCache

[stackoverflow-config-xsd]: http://stackoverflow.com/questions/742905/enabling-intellisense-for-custom-sections-in-config-files
[server-explorer]: https://github.com/MichaCo/CacheManager/raw/master/Articles/media/cachemanager-architecture/performance-counters.jpg "Performance Counters"
[perfmon]: https://github.com/MichaCo/CacheManager/raw/master/Articles/media/cachemanager-architecture/performance-counters2.jpg "Perfmon.exe"


[TOC]