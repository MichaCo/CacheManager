# CacheManager
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

| Package Name | .Net 4.0  | .Net 4.5  | Minimum .NET Platform Standard version *
|--------------| :-------: | :-------: | :-------: 
| [CacheManager.Core][Core.nuget] | x | x | 1.2
| [CacheManager.StackExchange.Redis][Redis.nuget] | x | x | 1.5
| [CacheManager.SystemRuntimeCaching][SystemRuntimeCaching.nuget]  | x | x | -
| [CacheManager.Microsoft.Extensions.Caching.Memory][MSCache.nuget] | - | \>=4.5.1 | 1.3
| [CacheManager.Microsoft.Extensions.Configuration][Configuration.nuget] | - | x | 1.2
| [CacheManager.Microsoft.Extensions.Logging][Logging.nuget] | - | x | 1.2
| [CacheManager.Serialization.Bond][Bond.nuget] | x | x | 1.2
| [CacheManager.Serialization.Json][Json.nuget] | x | x | 1.2
| [CacheManager.Serialization.ProtoBuf][ProtoBuf.nuget] | x | x | 1.3
| [CacheManager.Web][Web.nuget]  | - | x | -
| [CacheManager.Memcached][Memcached.nuget]  | x | x | -
| [CacheManager.Couchbase][Couchbase.nuget]  | - | x | -

\* *"Minimum .NET Platform Standard" version means, that at least the listed version per package must be targeted if you want to use it. See the [documentation](https://github.com/dotnet/standard/blob/master/docs/versions.md) for more details of what .NET platform standard means.*

### Beta Packages
Beta versions of the CacheManager packages are getting pushed to https://www.myget.org/gallery/cachemanager on each build. 
Add the following feed, if you want to play with the not yet released bits: 

    https://www.myget.org/F/cachemanager/api/v3/index.json

To find which check-in created which build, use this [build history](https://ci.appveyor.com/project/MichaCo/cachemanager-ak9g3/history).

## Documentation
 
Documentation can be found within the [articles folder][articles] of the Cache Manager's repository and  hosted on my [website][cmweb]:

* [**Getting Started**][gettingstarted]
Is a very basic introduction of how to install and use Cache Manager
* [**Configuration**][configuration]
Explains how to configure Cache Manager via code or configuration file
* [**Features and Architecture**][featuresarticle]
A more advanced in depth introduction to all features of Cache Manager
* [**Update Operations**][updatearticle]
Explanation of why and when to use the update method instead of `Put` 
* [**Serialization**][serialization]
Cache value serialization and configuration options explained in detail.
* [**Cache Synchronization**][cachesyncarticle]
Use case for and explanation of the Cache Backplane feature.
* [**Logging**][logging]
The logging abstraction and implementations explained

There is also from source generated [html documentation][help] available online.

## Examples
* Examples included in the Cache Manager repository
	* [**Some usage examples**][program.cs]
	* [**Sample ASP.NET Core website**][corewebsample]
* [**Single Page Todo App with Cache Manager on Azure using Redis**][todosample]

## Benchmarks
See [benchmarks page](https://github.com/MichaCo/CacheManager/blob/dev/Benchmarks.md)

## Features in Version [1.0.x][releases] 

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
* **System.Web.OutputCache** implementation to use CacheManager as OutputCache provider which makes the OutputCache extremely flexible, for example by using a distributed cache like Redis across many web servers.
* **Cache clients synchronization** 
    * Implemented with the Redis pub/sub feature
    * (Other implementations without Redis might be an option for a later version)
* Supports .Net 4.0, .Net 4.5, and can be used in cross platform projects with the new **.NET Core** runtime

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
[cmweb]:  http://cachemanager.michaco.net
[articles]: https://github.com/MichaCo/CacheManager/tree/master/Articles
[help]: http://cachemanager.michaco.net/Documentation/api
[gettingstarted]: http://cachemanager.michaco.net/Documentation/Index/cachemanager_getting_started
[configuration]: http://cachemanager.michaco.net/Documentation/Index/cachemanager_configuration
[featuresarticle]: http://cachemanager.michaco.net/Documentation/Index/cachemanager_architecture
[updatearticle]: http://cachemanager.michaco.net/Documentation/Index/cachemanager_update
[cachesyncarticle]: http://cachemanager.michaco.net/Documentation/Index/cachemanager_synchronization
[logging]: http://cachemanager.michaco.net/Documentation/Index/cachemanager_logging
[serialization]: http://cachemanager.michaco.net/Documentation/Index/cachemanager_serialization
[program.cs]: https://github.com/MichaCo/CacheManager/blob/master/samples/CacheManager.Examples/Program.cs
[corewebsample]: https://github.com/MichaCo/CacheManager/tree/dev/samples/AspnetCore.WebApp
[todosample]: http://cachemanager.michaco.net/Documentation/Index/cachemanager_backed_todo_web_app
[Json.nuget]: https://www.nuget.org/packages/CacheManager.Serialization.Json
[Logging.nuget]: https://www.nuget.org/packages/CacheManager.Microsoft.Extensions.Logging
[Configuration.nuget]: https://www.nuget.org/packages/CacheManager.Microsoft.Extensions.Configuration
[MSCache.nuget]: https://www.nuget.org/packages/CacheManager.Microsoft.Extensions.Caching.Memory
[ProtoBuf.nuget]: https://www.nuget.org/packages/CacheManager.Serialization.ProtoBuf
[Bond.nuget]: https://www.nuget.org/packages/CacheManager.Serialization.Bond

[TOC]
