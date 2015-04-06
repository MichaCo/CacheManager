## Getting started with Cache Manager

### Basics
If you download one of the Cache Manager's nuget packages you'll find
that there is a CacheManager.Core package. This package contains 
the base implementation of the Cache Manager.

The [Core][1] Namespace contains the main interfaces and classes you will configure the cache application and work with.

All other packages, like the [CacheManager.SystemRuntimeCaching][2] primarily contain a cache implementation for 
the main cache manager based on a specific cache provider (in this case System.Runtime.Caching).




[1]:http://michaco.github.io/Documentation/CacheManager/Help/html/N_CacheManager_Core.htm
[2]:https://www.nuget.org/packages/CacheManager.SystemRuntimeCaching/