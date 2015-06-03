# CacheManager
CacheManager is an open source abstraction layer for caching written in C#. It supports various cache providers and implements many advanced features.

The main goal of the CacheManager package is to make developer's life easier to handle even very complex caching scenarios.  
With CacheManager it is possible to implement multiple layers of caching, e.g. in-process caching in front of a distributed cache, in just a few lines of code.

CacheManager is not just an interface to unify the programming model for various cache providers, which will 
make it very easy to change the caching strategy later on in a project. It also offers additional features, like cache synchronization, concurrent updates, events, performance counters... 
The developer can opt-in to those features only if needed.

## Documentation
 
Documentation can be found within the [articles folder][articles] of the Cache Manager's repository and  hosted on my [website][mcweb]:

* [**Getting Started**][gettingstarted]
Is a very basic introduction of how to install and use Cache Manager
* [**Configuration**][configuration]
Explains how to configure Cache Manager via code or configuration file
* [**Features and Architecture**][featuresarticle]
A more advanced in depth introduction to all features of Cache Manager
* [**Update Operations**][updatearticle]
Explanation of why and when to use the update method instead of `Put` 
* [**Cache Synchronization**][cachesyncarticle]
Use case for and explanation of the Cache Back Plate feature.

There is also from source generated [html documentation][help] available online.

## Examples
* Examples included in the Cache Manager repository
	* [**Some usage examples**][program.cs]
	* [**ASP.NET MVC website**][outputcachesample] showcasing usage of the Cache Manager `OutputCache` provider
* [**Single Page Todo App with Cache Manager on Azure using Redis**][todosample]


## CacheManager Nuget Packages

| Package Name         | .Net 4.0         | .Net 4.5
|----------------------| :--------------: | :----------------:
| [CacheManager.Core] [Core.nuget] | x | x
| [CacheManager.StackExchange.Redis] [Redis.nuget] | x | x 
| [CacheManager.SystemRuntimeCaching] [SystemRuntimeCaching.nuget]  | x | x 
| [CacheManager.AppFabricCache] [AppFabricCache.nuget]  | - | x 
| [CacheManager.WindowsAzureCaching] [WindowsAzureCaching.nuget]  | - | x 
| [CacheManager.Memcached] [Memcached.nuget]  | x | x
| [CacheManager.Web] [Web.nuget]  | - | x
| [CacheManager.Couchbase] [Couchbase.nuget]  | - | x

## Features in Version: [0.5.x][releases] [![Build Status](https://travis-ci.org/MichaCo/CacheManager.svg?branch=master)](https://travis-ci.org/MichaCo/CacheManager) 

* One common interface for handling different caching technologies: `ICache<T>`
* Configurable via app/web.config or by code.
* Support for different cache providers
    * **MemoryCache** (System.Runtime.Caching)
    * **Redis** using [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis)
    * **Memcached** using [Enyim.Memcached](https://github.com/enyim/EnyimMemcached)
    * **Couchbase** using [Couchbase.Net.Client v2](https://github.com/couchbase/couchbase-net-client)
    * AppFabric cache (might get removed)
    * Azure cache (might get removed)
* **Update values with lock or transaction** for distributed caches. 
The interfaced provides a simple update method which internally ensures you work with the latest version.
And CacheManager handles version conflicts for you.
* **Strongly typed** cache interface.
* **Multiple layers**
By having multiple cache handles managed by CacheManager, you can easily implement layered caches. For example an in process cache infront of your distributed cache, to make read access faster.
CacheManager will synchronize those layers for you. 
    * `Put` and `Add` operations will always be excecuted on all cache handles registered on the manager.
    * On `Get`, there are different configuration options defined by `CacheUpdateMode`, if the item was available in one cache handle:
        * None: No update across the cache handles on Get
        * Up: Updates the handles "above"
        * All: Updates/Adds the item to all handles
* **Expiration**: It is possible to configure the expiration per cache handle within the manager or per cache item.
The following are the supported expiration modes:
    * Sliding expiration: On cache hit, the cache item expiration timeout will be extended by the configured amount.
    * Absolute expiration: The cache item will expire after the configured timeout.
* **Cache Regions**: Even if some cache systems do not support or implement cache regions, the CacheManager implements the mechanism.
This can be used to for example group elements and remove all of them at once.
* **Statistics**: Counters for all kind of cache actions.
* **Performance Counters**: To be able to inspect certain numbers with perfmon, CacheManager supports performance counters per instance of the manager and per cache handle.
* **Event System**: CacheManager triggers events for common cache actions:
OnGet, OnAdd, OnPut, OnRemove, OnClear, OnClearRegion
* **System.Web.OutputCache** implementation to use CacheManager as OutputCache provider which makes the OutputCache extremly flexible, for example by using a distributed cache like Redis across many web servers.
* **Cache clients synchronization** 
    * Implemented with the Redis pub/sub feature
    * (Other implementations without Redis might be an option for a later version)
* Supports .Net 4.0, .Net 4.5

[releases]: https://github.com/MichaCo/CacheManager/releases
[Core.nuget]: https://www.nuget.org/packages/CacheManager.Core
[Redis.nuget]: https://www.nuget.org/packages/CacheManager.StackExchange.Redis 
[SystemRuntimeCaching.nuget]: https://www.nuget.org/packages/CacheManager.SystemRuntimeCaching
[AppFabricCache.nuget]: https://www.nuget.org/packages/CacheManager.AppFabricCache
[WindowsAzureCaching.nuget]: https://www.nuget.org/packages/CacheManager.WindowsAzureCaching
[Memcached.nuget]: https://www.nuget.org/packages/CacheManager.Memcached
[Web.nuget]: https://www.nuget.org/packages/CacheManager.Web
[Couchbase.nuget]: https://www.nuget.org/packages/CacheManager.Couchbase
[mcweb]: http://michaconrad.com
[articles]: https://github.com/MichaCo/CacheManager/tree/master/Articles
[help]: http://michaco.github.io/Documentation/CacheManager/Help
[gettingstarted]: http://cachemanager.net/Documentation/Index/cachemanager_getting_started
[configuration]: http://cachemanager.net/Documentation/Index/cachemanager_configuration
[featuresarticle]: http://cachemanager.net/Documentation/Index/cachemanager_architecture
[updatearticle]: http://cachemanager.net/Documentation/Index/cachemanager_update
[cachesyncarticle]: http://cachemanager.net/Documentation/Index/cachemanager_synchronization
[program.cs]: https://github.com/MichaCo/CacheManager/blob/master/samples/CacheManager.Examples/Program.cs
[outputcachesample]: https://github.com/MichaCo/CacheManager/tree/master/samples/CacheManager.Samples.Mvc
[todosample]: http://cachemanager.net/Documentation/Index/cachemanager_backed_todo_web_app

[TOC]
