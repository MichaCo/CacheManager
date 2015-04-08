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

## Multiple Cache Layers
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

## Cache Item handling
Now how does the BaseCacheManager handle values in caches?  
This depends on configuration in some cases, lets have a look at the basic cache operations:

`Set` and `Put` adds and/or overrides a cached value. The cache manager will add or put the cache item into **all configured cache handles**. This is necessary because in general we want to have all layers of our cache in sync.

`Remove`, `Clear` and `ClearRegion` also act on all configured cache handles.

### Get operations
Let's say we have two cache handles configured, and the `Get` operation finds the `Key` within the second cache handle. 
Now we can assume that the two configured layers of the cache have some purpose and that the CacheManager should maybe update the other cache handles.   

There are 3 different configuration options for Cache Manager to handle this, defined by `CacheUpdateMode`:

* **None** - setting `CacheUpdateMode`to `None` will instruct the Cache Manager to do nothing on cache hits.
* **Up** - instructs the Cache Manager to update cache handles "above" the one the cache item was found in. The order of the cache handles matter in this case. 
* **All** - instructs the Cache Manager to update all other cache handles

[TOC]