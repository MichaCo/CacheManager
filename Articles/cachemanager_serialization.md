<!--
{title:"CacheManager - Serialization",
abstract: "In-depth walk through of how to use and customize cache value serialization in CacheManager.",
lastUpdate:"2016-02-28",
published:"2016-02-28"
}
-->
# Serialization
Since CacheManager version [0.7](https://github.com/MichaCo/CacheManager/releases/tag/0.7.0), serialization of cache values can be configured and extended. 

In general, serialization is only used by CacheManager, if the cache handle cannot store the object reference in memory. This is the case for any distributed cache, because in those scenarios, the cache value has to be stored remotely/out of process.
For cache handles like the `DictionaryCacheHandle` or `SystemRuntimeCaching`, serialization is not needed and doesn't have to be configured at all.

## Implementation Details
The `ICacheSerializer` interface defines the contract for cache item and cache value serialization. 
Those methods are different, because some cache handles might only have to serialize the cache value itself and not the `CacheItem<T>`. The redis cache handle, for example, does serialize the value only, and all the meta data is stored within the redis hash.

The `object Deserialize(byte[] data, Type target);` method takes the `target` type, which will be the actually `Type` of the value stored. This is needed in cases where a) the `TCacheValue` of the cache is e.g. `object` or b) is an interface or c) maybe the actual `Type` is a derived class of `TCacheValue`.
In all those scenarios, the actual `Type` might defer from `TCacheValue` and would otherwise lead to issues.
The actual `Type` is stored within the `CacheItem<TCacheValue>` and CacheManager will pass that value to the deserialization method.

## Default and Fall-Back
The `CacheManager.Core` package contains only one implementation based on the .NET binary serialization. If nothing else is specified, this one will be used.

Because binary serialization is not available in the .NET core clr, it cannot be used by projects targeting cross platform frameworks. In this case, you have to install e.g. the JSON serializer. 

## JSON Serialization
One alternative to the default serialization uses [Json.NET](http://www.newtonsoft.com/json).
To use the JSON serializer instead of the binary serializer, you have to install the `CacheManager.Serialization.Json` Nuget package and configure CacheManager to use it.

## Configuration
The configuration stores only the `Type` of the serializer which should be used. The moment a CacheManager instance gets created, this type will be resolved.

### By Code
As usual, there are extension methods to configure the serializer type. A general one, which allows you to implement and configure your own serializer, and specific ones for the implementations.
```csharp
// add a custom serializer
var config = new ConfigurationBuilder()
	.WithSerializer(typeof(MySerializer))

// use JSON serializer
var config = new ConfigurationBuilder()
	.WithJsonSerializer();
```

### Via JSON configuration
If you use the `CacheManager.Microsoft.Extensions.Configuration`, you can configure it, too.
The following example shows the `serializer` property only, with all the options:

```json
{
	"$schema" : "http://cachemanager.michaco.net/schemas/cachemanager.json",
	"cacheManagers" : [{
			"serializer" : {
				"knownType" : "Json | Binary",
				"type" : "MyType"
			}
		}
	]
}
```

### Via App-/Web.config
The XML configuration schema can also be used to configure the serializer. Therefore, the `cache` node under `managers` has an optional property `serializerType`, which must be defined as assembly qualified type.

[TOC]