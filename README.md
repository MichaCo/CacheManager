CacheManager is an open source caching abstraction layer for .NET written in C#. It supports various cache providers and implements many advanced features.

The main goal of the CacheManager package is to make developer's life easier to handle even very complex caching scenarios.  
With CacheManager it is possible to implement multiple layers of caching, e.g. in-process caching in front of a distributed cache, in just a few lines of code.

CacheManager is not just an interface to unify the programming model for various cache providers, which will make it very easy to change the caching strategy later on in a project. It also offers additional features, like cache synchronization, concurrent updates, serialization, events, performance counters... 
The developer can opt-in to those features only if needed.

## Build Status

Build Server | Status
--- | :---:
Windows, MSBuild | [![Build status](https://ci.appveyor.com/api/projects/status/mv4u7eg5vq6ty5s4?svg=true)](https://ci.appveyor.com/project/MichaCo/cachemanager-ak9g3)
Linux, Mono | -

## CacheManager Nuget Packages

| Package Name | FullFramework | .NET Standard |
| -------------- | :-------: | :-------: | 
| [CacheManager.Core][Core.nuget] | 4.5 | 2.0 |
| [CacheManager.StackExchange.Redis][Redis.nuget] | 4.6.1 | 2.0 |
| [CacheManager.SystemRuntimeCaching][SystemRuntimeCaching.nuget]  | 4.5 | 2.0 |
| [CacheManager.Microsoft.Extensions.Caching.Memory][MSCache.nuget]  | (4.6.1) | 2.0 |
| [CacheManager.Microsoft.Extensions.Configuration][Configuration.nuget]  | 4.6.1 | 2.0 |
| [CacheManager.Microsoft.Extensions.Logging][Logging.nuget]  | (4.6.1) | 2.0 |
| [CacheManager.Serialization.DataContract][DataContract.nuget] | 4.5 | 2.0 |
| [CacheManager.Serialization.Bond][Bond.nuget] | 4.5 | 2.0 |
| [CacheManager.Serialization.Json][Json.nuget] | 4.5 | 2.0 |
| [CacheManager.Serialization.ProtoBuf][ProtoBuf.nuget] | 4.5 | 2.0 |
| [CacheManager.Web][Web.nuget] | 4.5 | - |
| [CacheManager.Memcached][Memcached.nuget] | 4.5 | - |
| [CacheManager.Couchbase][Couchbase.nuget] | 4.5 | 2.0 |


Supported framework targets changed since CacheManager 2.0. In case you have to target .NET 40 for example, you can still use CacheManager 1.x!


### Beta Packages
Beta versions of the CacheManager packages are getting pushed to https://www.myget.org/gallery/cachemanager on each build. 
Add the following feed, if you want to play with the not yet released bits: 

    https://www.myget.org/F/cachemanager/api/v3/index.json

To find which check-in created which build, use this [build history](https://ci.appveyor.com/project/MichaCo/cachemanager-ak9g3/history).

## Documentation
 
Documentation can be found on [cachemanager.michaco.net][cmweb]:

* [**Getting Started**][gettingstarted]
* [**Configuration**][configuration]
* [**Features and Architecture**][featuresarticle]
* [**Update Operations**][updatearticle]
* [**Serialization**][serialization]
* [**Cache Synchronization**][cachesyncarticle]
* [**Logging**][logging]

Generated [**API documentation**][help] is also available.

## Blog Posts

CacheManager related blog posts can be found on [my website](http://michaco.net/blog?tag=CacheManager)

## Examples
* Examples included in the Cache Manager repository
	* [**Some usage examples**][program.cs]
	* [**Sample ASP.NET Core website**][corewebsample]
* [**Single Page Todo App with Cache Manager on Azure using Redis**][todosample]

## Benchmarks
See [benchmarks results](https://github.com/MichaCo/CacheManager/blob/dev/Benchmarks.md) on GitHub.

## List of Features

* One common interface for handling different caching technologies: `ICache<T>`
* Configurable by
	* Code with many different paths and a fluent configuration builder
	* Microsoft.Extensions.Configuration
	* App/Web.config
	* See [configuration docs][configuration]
* Support for different cache providers
    * **MemoryCache** (System.Runtime.Caching)
	* **MemoryCache** based on Microsoft.Extensions.Caching.Memory
    * **Redis** using [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis)
    * **Memcached** using [Enyim.Memcached](https://github.com/enyim/EnyimMemcached)
    * **Couchbase** using [Couchbase.Net.Client v2](https://github.com/couchbase/couchbase-net-client)
    * **System.Web.Caching** based (included in the Web package)
* **Serialization** can now be configured.
Serialization is only needed in distributed caches. If no additional serialization package is installed and configured, Binary serialization will be used (if available)
The following are the currently available serialization options:
	* Binary (build in if the full CLR is being used)
	* **Json** based on the popular Newtonsoft.Json library
	* **Json** with Gzip compression
    * **Bond** based on Microsoft.Bond supporting all three available variants
    * **DataContract** based on System.Runtime.Serialization library supporting binary, Json & Json with Gzip compression
	* **Protocol Buffer** Google's protobuf. The package uses Mark's [protobuf-net](https://github.com/mgravell/protobuf-net) implementation.
* **Update values with lock or transaction** for distributed caches. 
The interfaced provides a simple update method which internally ensures you work with the latest version.
And CacheManager handles version conflicts for you.
* **Logging** CacheManager comes with an extensible logging API.
    * All standard cache operations are logged
    * Based on log levels more or less information will be logged (try Trace and Debug)
    * Current concrete implementation is based on the ASP.NET Core logging. Other implementation of CacheManager's ILoggerFactory might follow.
* **Strongly typed** cache interface.
* **Multiple layers**
By having multiple cache handles managed by CacheManager, you can easily implement layered caches. For example, an in process cache in front of your distributed cache, to make read access faster.
CacheManager will synchronize those layers for you. 
    * `Put` and `Add` operations will always be executed on all cache handles registered on the manager.
    * On `Get`, there are different configuration options defined by `CacheUpdateMode`, if the item was available in one cache handle:
        * None: No update across the cache handles on Get
        * Up: Updates the handles "above"
        * All: Updates/Adds the item to all handles
* **Expiration**: It is possible to configure the expiration per cache handle within the manager or per cache item.
The following are the supported expiration modes:
    * Sliding expiration: On cache hit, the cache item expiration timeout will be extended by the configured amount.
    * Absolute expiration: The cache item will expire after the configured timeout.
    * Since 1.0.0, evictions triggered by the cache vendor can trigger events and updates
* **Cache Regions**: Even if some cache systems do not support or implement cache regions, the CacheManager implements the mechanism.
This can be used to for example group elements and remove all of them at once.
* **Statistics**: Counters for all kind of cache actions.
* **Performance Counters**: To be able to inspect certain numbers with `perfmon`, CacheManager supports performance counters per instance of the manager and per cache handle.
* **Event System**: CacheManager triggers events for common cache actions:
OnGet, OnAdd, OnPut, OnRemove, OnClear, OnClearRegion
   * Events also get triggered by the backplane (if enabled) when multiple instances are sharing the same cache.
   * New `OnRemoveByHandle` events triggered by actual expiration or memory pressure eviction by the cache vendor
   * Events also get triggered through the backplane and via Redis keyspace events (if configured)
* **System.Web.OutputCache** implementation to use CacheManager as OutputCache provider which makes the OutputCache extremely flexible, for example by using a distributed cache like Redis across many web servers.
* **Cache clients synchronization** 
    * Implemented with the Redis pub/sub feature
* Supports .Net 4.5, and can be used in cross platform projects with the new **.NET Core** runtime

[releases]: https://github.com/MichaCo/CacheManager/releases
[Core.nuget]: https://www.nuget.org/packages/CacheManager.Core
[Redis.nuget]: https://www.nuget.org/packages/CacheManager.StackExchange.Redis 
[SystemRuntimeCaching.nuget]: https://www.nuget.org/packages/CacheManager.SystemRuntimeCaching
[AppFabricCache.nuget]: https://www.nuget.org/packages/CacheManager.AppFabricCache
[WindowsAzureCaching.nuget]: https://www.nuget.org/packages/CacheManager.WindowsAzureCaching
[Memcached.nuget]: https://www.nuget.org/packages/CacheManager.Memcached
[Web.nuget]: https://www.nuget.org/packages/CacheManager.Web
[Couchbase.nuget]: https://www.nuget.org/packages/CacheManager.Couchbase
[mcweb]: http://michaco.net
[cmweb]:  http://cachemanager.michaco.net
[articles]: https://github.com/MichaCo/CacheManager/tree/master/Articles
[help]: http://cachemanager.michaco.net/Documentation/api
[gettingstarted]: http://cachemanager.michaco.net/Documentation/CacheManagerGettingStarted
[configuration]: http://cachemanager.michaco.net/Documentation/CacheManagerConfiguration
[featuresarticle]: http://cachemanager.michaco.net/Documentation/CacheManagerArchitecture
[updatearticle]: http://cachemanager.michaco.net/Documentation/CacheManagerUpdateOperations
[cachesyncarticle]: http://cachemanager.michaco.net/Documentation/CacheManagerCacheSynchronization
[logging]: http://cachemanager.michaco.net/Documentation/CacheManagerLogging
[serialization]: http://cachemanager.michaco.net/Documentation/CacheManagerSerialization
[program.cs]: https://github.com/MichaCo/CacheManager/blob/master/samples/CacheManager.Examples/Program.cs
[corewebsample]: https://github.com/MichaCo/CacheManager/tree/dev/samples/AspnetCore.WebApp
[todosample]: http://michaco.net/blog/SinglePageTodoAppwithCacheManager
[Json.nuget]: https://www.nuget.org/packages/CacheManager.Serialization.Json
[Logging.nuget]: https://www.nuget.org/packages/CacheManager.Microsoft.Extensions.Logging
[Configuration.nuget]: https://www.nuget.org/packages/CacheManager.Microsoft.Extensions.Configuration
[MSCache.nuget]: https://www.nuget.org/packages/CacheManager.Microsoft.Extensions.Caching.Memory
[ProtoBuf.nuget]: https://www.nuget.org/packages/CacheManager.Serialization.ProtoBuf
[Bond.nuget]: https://www.nuget.org/packages/CacheManager.Serialization.Bond
[DataContract.nuget]: https://www.nuget.org
