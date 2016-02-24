---
uid: CacheManager.Core.CacheFactory.Build(System.String,System.Action{CacheManager.Core.ConfigurationBuilderCachePart})
remarks: 
---

The following example shows how to use this overload to build a CacheManagerConfiguration 
and pass it to the CacheFactory to create a new CacheManager instance.

```csharp
var cache = cachefactory.build("mycachename", settings =>
{
   settings
       .withupdatemode(cacheupdatemode.up)
       .withdictionaryhandle()
           .enableperformancecounters()
           .withexpiration(expirationmode.sliding, timespan.fromseconds(10));
});

cache.add("key", "value");
```
