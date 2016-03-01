<!--
{title:"CacheManager - Features and Architecture",
abstract: "An overview of the primary features and functionality of CacheManager.",
lastUpdate:"2016-02-16"
}
-->
# Features and Architecture

## Standard operations
First and foremost Cache Manager will provide well known cache methods like Get, Put, Remove and Clear.
All cache items will have a `string Key` and `T` Value where `T` can be anything, e.g. `int`, `string` or even `object`. The Cache Manager is implemented as a strongly typed cache interface.
```cs
cache.Add("key", "value");
var value = cache.Get("key");
cache.Remove("key");
cache.Clear();
```   
### Regions
Cache Manager has overloads for all cache methods to support a `string Region` in addition to the `Key` to identify an item within the cache. 
```cs
cache.Add("key", "value", "region");
var value = cache.Get("key", "region");
cache.Remove("key", "region");
```
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
```cs
var cache = CacheFactory.Build<string>("myCacheName", settings =>
{
	settings
		.WithSystemRuntimeCacheHandle("handle1");
});
```
Adding multiple cache handles looks pretty much the same:
```cs
var cache = CacheFactory.Build<string>("myCacheName", settings =>
{
	settings
	.WithSystemRuntimeCacheHandle("handle1")
	       .And
	       .WithRedisCacheHandle("redis");
});
```
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
```cs
var item = new CacheItem<string>(
	"key", "value", ExpirationMode.Absolute, TimeSpan.FromMinutes(10));
cache.Add(item);
```
To retrieve a cache item and change the expiration, use the `GetCacheItem` method.
```cs
var item = cache.GetCacheItem("key");
item.WithExpiration(ExpirationMode.Sliding, TimeSpan.FromMinutes(15));
cache.Put(item);
```
> **Note**
> Currently the Memcached and Couchbase cache handles do not support sliding expiration.

## Events
The Cache Manager `ICacheManager` interface defines several events which get triggered on cache operations. 
The events will be fired only once per cache operation, not per cache handle!

To subscribe to an event, simply add an event listener like that:
```cs
cache.OnAdd += (sender, args) => ...;
```
Events are available for `Add`, `Clear`, `ClearRegion`, `Get`, `Put`, `Remove` and `Update` operations.

The event arguments passed into the listener depend on the event, for `Add`,`Get`, `Put` and `Remove` the `CacheActionEventArgs` will provide the `Key` and `Region` of the cache operation. `Region` might be empty though.
`OnClearRegion` will provide the `Region` and `OnUpdate` gives you the `UpdateItemResult` and `UdateItemConfig` in addition to the `Key` and `Region`.

## Statistics and Counters
Ever wondered how many cache misses and hits occurred while running your application? 
There are two models implemented in Cache Manager to get those numbers.

The statistics and Windows performance counters. Both are stored per cache handle.

Both can be enabled or disabled per cache handle. The configuration can be done via .config file or `ConfigurationBuilder`
```cs
var cache = CacheFactory.Build("cacheName", settings => settings
       .WithSystemRuntimeCacheHandle("handleName")
	       .EnableStatistics()
	       .EnablePerformanceCounters());
```
> **Note**
> Disabling statistics will also disable performance counters and enabling performance counters will enable statistics.
> Collecting the numbers and updating performance counters can cause a slight performance decrease, only use it for analysis in production if really needed. 

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
```cs
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
```
### Performance Counters

If performance counters are enabled, Cache Manager will try to create a new `PerformanceCounterCategory` named ".Net CacheManager" with several counters below. 

Server Explorer:
![Performance Counters in Server Explorer][server-explorer]

> **Note** 
> The creation of performance counter categories might fail because your application might run in a security context which doesn't allow the creation.
> In this case Cache Manager will silently disable performance counters.

To see performance counters in action, run "perfmon.exe", select "Performance Monitor" and click the green plus sign on the toolbar. Now find ".Net Cache Manager" in the list (should be at the top)  and select the instances and counters you want to track.

The result should look similar to this:
![Performance Counters in Server Explorer][perfmon]

The instance name displayed in Performance Monitor is the host name of your application combined with the cache and cache handle's name. 

## System.Web.OutputCache
The [CacheManager.Web][cm.web] Nuget package contains an implementation for `System.Web.OutputCache` which uses the cache manager to store the page results, if the `OutputCache` is configured to store it on the server.

Configuration of the `OutputCache` can be done via web.config:
```cs
<system.web>	    
  <caching>
    <outputCache defaultProvider="CacheManagerOutputCacheProvider">
      <providers>
        <add cacheName="websiteCache" name="CacheManagerOutputCacheProvider" type="CacheManager.Web.CacheManagerOutputCacheProvider, CacheManager.Web" />
      </providers>
    </outputCache>
  </caching>
</system.web>
```
The `cacheName` attribute within the `add` tag is important. This will let CacheManager know which `cache` configuration to use. The configuration must also be provided via web.config, configuration by code is not supported!

[stackoverflow-config-xsd]: http://stackoverflow.com/questions/742905/enabling-intellisense-for-custom-sections-in-config-files
[server-explorer]: https://raw.githubusercontent.com/MichaCo/CacheManager/dev/Articles/media/cachemanager-architecture/performance-counters.jpg "Performance Counters"
[perfmon]: https://raw.githubusercontent.com/MichaCo/CacheManager/dev/Articles/media/cachemanager-architecture/performance-counters2.jpg "Perfmon.exe"
[cm.web]:  https://www.nuget.org/packages/CacheManager.Web/



[TOC]

