# CacheManager

[![Build Status](https://travis-ci.org/MichaCo/CacheManager.svg?branch=master)](https://travis-ci.org/MichaCo/CacheManager)

The main goal of the CacheManager package is to make developer's life easier to handle even very complex caching scenarios.  
With CacheManager it is possible to implement multiple layers of caching, 
e.g. in-process caching in front of a distributed cache, in just a few lines of code.

CacheManager is not just an interface to unify the programming model for various cache providers, which will 
make it very easy to change the caching strategy later on in a project. It also offers additional features, like cache invalidation for above mentioned multi layer cache scenario for example. 
The developer can opt-in to those features only if needed.

## CacheManager Nuget Packages

| Package Name | .Net 4.0 | .Net 4.5
----------| :----------: | :------------:
| [CacheManager.Core] [Core.nuget] | x | x
| [CacheManager.StackExchange.Redis] [Redis.nuget] | x | x 
| [CacheManager.SystemRuntimeCaching] [SystemRuntimeCaching.nuget]  | x | x 
| [CacheManager.AppFabricCache] [AppFabricCache.nuget]  | - | x 
| [CacheManager.WindowsAzureCaching] [WindowsAzureCaching.nuget]  | - | x 
| [CacheManager.Memcached] [Memcached.nuget]  | x | x
| [CacheManager.Web] [Web.nuget]  | - | x
| [CacheManager.Couchbase] [Couchbase.nuget]  | - | x

## Features in Version: [0.4.3][releases]

* One common interface for handling different caching technologies: `ICache<T>`
* Configurable via app/web.config or by code.
* Support for different cache providers
    * **MemoryCache** (System.Runtime.Caching)
    * **Redis** using [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis)
    * **Memcached** using [Enyim.Memcached](https://github.com/enyim/EnyimMemcached)
    * **Couchbase** using [Couchbase.Net.Client v2](https://github.com/couchbase/couchbase-net-client)
    * AppFabric cache (might get descoped)
    * Azure cache (might get descoped)
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
* **Expiration**: It is possible to configure the expiration per cache manager, for each cache handle within the manager individually or even overrule the configuration on cache item level.
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
