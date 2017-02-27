<!--
{title:"CacheManager - Configuration",
abstract: "Details and code examples of all the different options, CacheManager provides to configure caching in your application, at development- and at run-time.",
lastUpdate:"2016-02-16"
}
-->
# Configuration
A big goal of Cache Manager is to make it easy to work with different cache systems, but at the same time it should be flexible to adopt to different needs.
Therefore, the configuration is a pretty important part of Cache Manager. 

Developers have the choice to use different ways to configure Cache Manager. Via code using the `CacheFactory` or `ConfigurationBuilder`, by using `Microsoft.Extensions.Configuration` or via *web/app.config* using the Cache Manager's configuration section.

Some parts of CacheManager's configuration need different vendor specific configurations, like a Redis connection for example. To link those configurations together, each part has a `configurationKey` property (or `name` of the configuration which will be the fallback if `configurationKey` is not specified).

## CacheFactory
To create a Cache Manager instance, you can use the `CacheFactory` class which has two different methods: 

**`Build`** can be used to create a new cache configuration via an `Action` using a fluent configuration builder. 
```cs
var cache = CacheFactory.Build("cacheName", settings => settings
       .WithUpdateMode(CacheUpdateMode.Up)
       .WithSystemRuntimeCacheHandle("handleName")
	       .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10)));
```
**`FromConfiguration`** takes either the `CacheManagerConfiguration` object or the `name` of the Cache Manager configured in the *app/web.config* file of the application. The type of the Cache Manager instance also must be specified now.
```cs
var cache = CacheFactory.FromConfiguration<string>("cacheName")
```
Optionally, the name of the Cache Manager instance and the name of the configured cache can be defined separated, to create multiple instances from the same configuration:
```cs
var cache = CacheFactory.FromConfiguration<int>("cacheInstanceName", "configuredCacheName")
```
If you want to separate the creation of the configuration object and the creation of the Cache Manager instance, use the `ConfigurationBuilder` to create the configuration.
```cs
var cfg = ConfigurationBuilder.BuildConfiguration(settings =>
	{
		settings.WithUpdateMode(CacheUpdateMode.Up)
               .WithSystemRuntimeCacheHandle("handleName")
				.WithExpiration(ExpirationMode.Sliding, TimeSpan.FromSeconds(10));
	});

var cache = CacheFactory.FromConfiguration<string>("cacheName", cfg);
cache.Add("key", "value");
```
And now, you can reuse the configuration and create another Cache Manager instance for a different type for example:
```cs
var cache = CacheFactory.FromConfiguration<int>("numbers", cfg);
```

### Instantiate BaseCacheManager
Instead of the `CacheFactory`, you can also create a new CacheManager instance by instantiating the `BaseCacheManager` class directly and pass in a `CacheManagerConfiguration`.
```cs
var config = new ConfigurationBuilder()
    .WithSystemRuntimeCacheHandle()
    .Build();

var cache = new BaseCacheManager<string>(config);
```
## ConfigurationBuilder
The `ConfigurationBuilder` has several static methods to initialize a CacheManagerConfiguration, to build a new one or load one from `App.config`.
But it can also be instantiated to create new configurations or edit existing ones.
The same extension methods will be available as usual, the difference to the static method is, that you have to call `Build` at some point, to exit the configuration context and return the new configuration object.
```cs
 var config = new ConfigurationBuilder()
     .WithSystemRuntimeCacheHandle()
         .EnableStatistics()
     .Build();

 config = new ConfigurationBuilder(config)
     .WithMicrosoftLogging(f => f.AddConsole())
     .Build();
```
This path can be used to pass a `ConfigurationBuilder` around if you split the configuration up into multiple methods  for example. Or, if you load a configuration from JSON or XML and you want to add something to it.

The `CacheManagerConfiguration` also has a new property `Builder`. Calling `config.Builder` is the same as if you would call `new ConfigurationBuilder(config)`.

## Microsoft.Extensions.Configuration
With the new Microsoft.Extensions.Configuration framework, Microsoft introduced a great way to handle custom configurations from many different sources. The preferred one is a plain JSON text file.
For this regards, CacheManager now also has a JSON schema file, located at http://cachemanager.michaco.net/schemas/cachemanager.json.

### JSON Schema
With this schema, it is really very easy and convenient if you create new configuration files in Visual Studio,  because it gives full auto completion and validation on each element.

Just add a new JSON file to your project and add the "$schema" directive to it:
```json
{
  "$schema": "http://cachemanager.michaco.net/schemas/cachemanager.json#"
}
```
from there on, you should see validation messages in the error window of Visual Studio. First one will be *Missing required property cacheManagers*, which means that you have to have a `cacheManagers` property of type array... and so on.
Here is a complete example:

```json
{
  "$schema": "http://cachemanager.michaco.net/schemas/cachemanager.json#",
  "redis": [
    {
      "key": "redisConnection",
      "connectionString": "localhost:6379,allowAdmin=true"
    }
  ],
  "cacheManagers": [
    {
      "maxRetries": 1000,
      "name": "cachename",
      "retryTimeout": 100,
      "updateMode": "Full",
      "backplane": {
        "key": "redisConString",
        "knownType": "Redis",
        "channelName": "test"
      },
      "loggerFactory": {
        "knownType": "Microsoft"
      },
      "serializer": {
        "knownType": "Json"
      },
      "handles": [
        {
          "knownType": "SystemRuntime",
          "enablePerformanceCounters": true,
          "enableStatistics": true,
          "expirationMode": "Absolute",
          "expirationTimeout": "0:0:23",
          "isBackplaneSource": false,
          "name": "sys cache"
        },
        {
          "knownType": "Redis",
          "key": "redisConnection",
          "isBackplaneSource": true
        }
      ]
    }
  ]
}
```
### Known Types
As you might have noticed in the example above, there are a lot of `knownType` properties. Known types are there to make configuration in text files easier and less error prone. Usually, you would have to declare the fully qualified type of a cache handle, logger factory or serializer. With `knownType` you can select from a predefined set of types which are known to CacheManager.
So for example instead of defining the type of a redis cache handle and writing `CacheManager.Redis.RedisCacheHandle´1, CacheManager.StackExchange.Redis` you just say `knownType: Redis` and the implementation will evaluate that to the full type name.
> **Hint**: If you specify any type or knownType but didn't install the corresponding NuGet package which contains that type, you'll get an error at the point you instantiate the CacheManager.

### Loading Configuration from JSON file
Loading a configuration via Microsoft's API works slightly different than the regular ConfigurationBuilder/Factory approach. You will have to build an `IConfiguration` first, and then call the new extension method `GetCacheConfiguration` to retrieve a `CacheManagerConfiguration`.

So, first create the `Microsoft.Extensions.Configuration.IConfiguration`
```cs
var builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
    .AddJsonFile("cache.json");
this.Configuration = builder.Build();
```
and then call
```cs
var jsonConfiguration = 
    this.Configuration.GetCacheConfiguration();
```
The default overload without parameters assumes that there is only one `CacheManagerConfiguration` defined in the `cacheManagers` section of the JSON file.
If you have multiple managers defined, specify the name of the manager you want to load:
```cs
var jsonConfiguration = 
    this.Configuration.GetCacheConfiguration("cachename")
        .Builder
        .WithMicrosoftLogging(f =>
        {
            f.AddDebug(LogLevel.Information);
        })
        .Build();
```

You can also get all `CacheManagerConfiguration`s by calling `.GetCacheConfigurations()`.

## Configuration Section
The Cache Manager configuration section has two main parts, the `managers`  collection which is used to configure the cache manager instances. The `name` of the element within the collection can be passed to `CacheFactory.FromConfiguration`.

And the `cacheHandles` collection which lists the available (installed) cache handle types. Those will be used by referencing the `id` to form a cache in the `managers` collection.
```xml
<cacheManager xmlns="http://cachemanager.michaco.net/schemas/CacheManagerCfg.xsd">
  <managers>
    <cache name="cacheName" updateMode="Up" enableStatistics="false" enablePerformanceCounters="false">
      <handle name="handleName" ref="systemRuntimeHandle" expirationMode="Absolute" timeout="50s"/>
    </cache>
  </managers>
  <cacheHandles>
    <handleDef  id="systemRuntimeHandle" type="CacheManager.SystemRuntimeCaching.MemoryCacheHandle`1, CacheManager.SystemRuntimeCaching"
        defaultExpirationMode="Sliding" defaultTimeout="5m"/>
  </cacheHandles>
</cacheManager>
```
The cacheHandles' elements can be defined with default values for expiration mode and timeout. It can be overridden by the `handle` element though. The `type` must a `Type` extending from `BaseCacheHandle`, it also has to be an open generic at this point.

> **Hint**
> To make configuration via .config file easier, enable intellisense by adding the `xmlns` attribute to he `cacheManagers` section and add the CacheManagerCfg.xsd file to your solution. The xsd file can be found in  [solution dir]/packages/CacheManager.Core.x.x.x.x/CacheManagerCfg.xsd
> See also [this answer on stackoverflow][stackoverflow-config-xsd]

[stackoverflow-config-xsd]: http://stackoverflow.com/questions/742905/enabling-intellisense-for-custom-sections-in-config-files

[TOC]