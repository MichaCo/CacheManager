CacheManager is an open source caching abstraction layer for .NET written in C#. It supports various cache providers and implements many advanced features.

The main goal of the CacheManager package is to make developer's life easier to handle even very complex caching scenarios.  
With CacheManager it is possible to implement multiple layers of caching, e.g. in-process caching in front of a distributed cache, in just a few lines of code.

CacheManager is not just an interface to unify the programming model for various cache providers, which will make it very easy to change the caching strategy later on in a project. It also offers additional features, like cache synchronization, concurrent updates, serialization, events, performance counters... 
The developer can opt-in to those features only if needed.

## Build Status

Branch | Status
--- | :---:
Dev | [![Build Status](https://dev.azure.com/michaco/CacheManager/_apis/build/status%2FMichaCo.CacheManager?branchName=dev)](https://dev.azure.com/michaco/CacheManager/_build/latest?definitionId=3&branchName=dev)
Master | [![Build Status](https://dev.azure.com/michaco/CacheManager/_apis/build/status%2FMichaCo.CacheManager?branchName=master)](https://dev.azure.com/michaco/CacheManager/_build/latest?definitionId=3&branchName=master)

## CacheManager Nuget Packages

| Package Name | FullFramework | .NET Standard | net8.0 |
| -------------- | :-------: | :-------: | :-------: | 
| [CacheManager.Core][Core.nuget] | 4.7.2 | 2.0 | net8.0 |
| [CacheManager.Microsoft.Extensions.Caching.Memory][MSCache.nuget]  | - | 2.0 | net8.0 |
| [CacheManager.Microsoft.Extensions.Configuration][Configuration.nuget]  | - | 2.0 | net8.0 |
| [CacheManager.Serialization.Bond][Bond.nuget]  | - | 2.0 | net8.0 |
| [CacheManager.Serialization.DataContract][DataContract.nuget]  | - | 2.0 | net8.0 |
| [CacheManager.Serialization.Json][Json.nuget]  | - | 2.0 | net8.0 |
| [CacheManager.Serialization.ProtoBuf][ProtoBuf.nuget]  | - | 2.0 | net8.0 |
| [CacheManager.StackExchange.Redis][Redis.nuget] | 4.7.2 | 2.0 | net8.0 |
| [CacheManager.SystemRuntimeCaching][SystemRuntimeCaching.nuget]  | 4.7.2 | 2.0 | net8.0 |

## Version 2.0 Breaking Changes

* CacheManager.Microsoft.Extensions.Logging is not a separated package anymore. Logging is now part of the Core package.
* CacheManager.Memcached is not supported anymore
* CacheManager.Couchbase is not supported anymore
* CacheManager.Web is not supported anymore
* PerformanceCounters are not available for now

### Testing with Microsoft.Garnet

For testing and benchmarking, this project is now using Microsoft.Garnet, which allows to create a Redis server which runs in process and is easy to setup.
This has some limitations though and before you use Microsoft.Garnet in production, be aware that for example key space notifications are not supported yet.

See https://github.com/microsoft/garnet for details.

## Beta Packages
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
* **Logging** using Microsoft.Extensions.Logging.
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
* **Event System**: CacheManager triggers events for common cache actions:
OnGet, OnAdd, OnPut, OnRemove, OnClear, OnClearRegion
   * Events also get triggered by the backplane (if enabled) when multiple instances are sharing the same cache.
   * New `OnRemoveByHandle` events triggered by actual expiration or memory pressure eviction by the cache vendor
   * Events also get triggered through the backplane and via Redis keyspace events (if configured)
* **Cache clients synchronization** 
    * Implemented with the Redis pub/sub feature

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
