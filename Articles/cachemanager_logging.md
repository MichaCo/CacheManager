<!--
{title:"CacheManager - Logging Explained",
abstract: "In-depth walk through of how logging in CacheManager works and how you can use it, with many code and log output examples.",
lastUpdate:"2016-02-16"
}
-->
# Logging
Since version 0.7.2, CacheManager.Core comes with its own logging interface `CacheManager.Core.Logging` which is used internally to log standard cache operations, and to trace what decisions CacheManager is making.

## Example Log Output
The examples below are using the [`Microsoft.Extensions.Logging`](#microsoftextensionslogging) for printing log output to the console.

### Tracing
The first few lines are informational on CacheManager initialization, creating the different handles...

```nohighlight
CacheManager.Core.BaseCacheManager<string>: Information: Cache manager: adding cache handles...
CacheReflectionHelper: Information: Creating handle sys cache.
CacheReflectionHelper: Information: Creating handle d410accc-53ed-44ac-929f-a213f1503128.
```

If the `Trace` `LogLevel` is enabled, you will get trace information on each cache operation.

for the following two calls

``` cs
cache.Add("key", "value");
cache.AddOrUpdate("key", "value", _ => "update value", 22);
```
we should see that the `key` should be added, and then, during `AddOrUpdate` Add shouldn't work anymore, because the item exists already and the cache should try to update the item instead:
```nohighlight
CacheManager.Core.BaseCacheManager<object>: Trace: Add: key 
CacheManager.Core.BaseCacheManager<object>: Trace: Add: successfully added key  to handle redis
CacheManager.Core.BaseCacheManager<object>: Trace: Add: successfully added key  to handle default
```
`Add` was successful, now `AddOrUpdate`:
```nohighlight
CacheManager.Core.BaseCacheManager<object>: Trace: Add or update: key .
CacheManager.Core.BaseCacheManager<object>: Trace: Add: key 
CacheManager.Core.BaseCacheManager<object>: Trace: Add: key  to handle redis FAILED. Evicting items from other handles.
CacheManager.Core.BaseCacheManager<object>: Trace: Evict from other handles: key : excluding handle 1.
CacheManager.Core.BaseCacheManager<object>: Trace: Evict from handle: key : on handle default.
CacheManager.Core.BaseCacheManager<object>: Trace: Add or update: key : add failed, trying to update...
CacheManager.Core.BaseCacheManager<object>: Trace: Update: key .
CacheManager.Core.BaseCacheManager<object>: Trace: Update: key : tried on handle redis: result: Success.
CacheManager.Core.BaseCacheManager<object>: Trace: Evict from handles above: key : above handle 1.
CacheManager.Core.BaseCacheManager<object>: Trace: Evict from handle: key : on handle default.
CacheManager.Core.BaseCacheManager<object>: Trace: Add to handles below: key : below handle 1.
CacheManager.Core.BaseCacheManager<object>: Trace: Add or update: key : successfully updated.
```
In the example above, you can see how CacheManager works with multiple handles. CacheManager starts with the *lowest* cache handle (in this case Redis), and if the item exists, it will remove it from the other cache handles above to prevent potential conflicts and force an update of the handles on the next `Get`.
Then, the Update is successful. 
On the next Get, CacheManager should find the item in the Redis handle and add it to the first layer...
```cs
var val = cache.Get("key");
```
Trace Log for the `Get` operation:
```nohighlight
CacheManager.Core.BaseCacheManager<object>: Trace: Get: key .
CacheManager.Core.BaseCacheManager<object>: Trace: Get: key : item NOT found in handle default.
CacheManager.Core.BaseCacheManager<object>: Trace: Get: key : item found in handle redis.
CacheManager.Core.BaseCacheManager<object>: Trace: Add to handles: key : with update mode Up.
CacheManager.Core.BaseCacheManager<object>: Trace: Add to handles: key : adding to handle 0.
```
> **Hint**: Keep in mind that logging trace level information will cost a lot of performance. If you have 100s or 1000s or cache operations per second, it would log way too much information to a file, console or debug window. Use `LogLevel.Trace` only during development and to track down issues. For Production, `LogLevel.Information` and up should be more than enough.

### Warnings and Errors
The next example shows what happens if you use the Redis cache handle and the connection to the Redis server is lost (because I killed the instance).
```nohighlight
CacheManager.Core.BaseCacheManager<object>: Trace: Add: key 
CacheManager.Redis.RedisCacheHandle<object>: Warning: Exception occurred performing an action. Retrying... 1/50
StackExchange.Redis.RedisConnectionException: No connection is available to service this operation: EVAL
   bei StackExchange.Redis.ConnectionMultiplexer.ExecuteSyncImpl[T](Message message, ResultProcessor`1 processor, ServerEndPoint server)...
CacheManager.Redis.RedisCacheHandle<object>: Warning: Exception occurred performing an action. Retrying... 2/50
StackExchange.Redis.RedisConnectionException: No connection is available to service this operation: EVAL
   bei StackExchange.Redis.ConnectionMultiplexer.ExecuteSyncImpl[T](Message message, ResultProcessor`1 processor, ServerEndPoint server)...
```
As you can see, CacheManager logs warnings while retrying the operation. I configured the maximum number of retries to be 50. If CacheManager reaches 50 tries, it will throw the exception so that it bubbles up (and it also logs it of course)
```nohighlight
CacheManager.Redis.RedisCacheHandle<object>: Error: Maximum number of tries exceeded to perform the action: 50.
StackExchange.Redis.RedisConnectionException: No connection is available to service this operation: EVAL
   bei StackExchange.Redis.ConnectionMultiplexer.ExecuteSyncImpl[T](Message message, ResultProcessor`1 processor, ServerEndPoint server) in...
```

## The Logging Internals
There are two interfaces defined in `CacheManager.Core.Logging`  which are commonly known in logging frameworks, the `ILoggerFactory` and the `ILogger`. The factory is responsible for creating logger instances.
The `ILogger` has been defined in the most simplified way with only one `Log` method.
All other methods to log for certain log levels, string formatting, etc., will be extension methods.

## Logging Abstraction
Important to note is, that CacheManager doesn't implement those interfaces with its own logger framework to target different logging outputs like console, trace and what not. 
There are already many of those frameworks available. The intent is just abstraction, in a way that the CacheManager.Core package doesn't have hard dependencies to any of those 3rd party logging frameworks.

To actually use a 3rd party logging framework, CacheManager will implement an adapter for the external logger factory and logger, redirecting the internal `Log` calls to the external library.

## Configuration
The configuration works seamlessly and in the same fashion as everything else in CacheManager. The only thing which has to be defined is the `ILoggerFactory` which should be used by CacheManager to instantiate loggers.
Therefore, there is a new `WithLogging` configuration method which will take the type of the logger factory.
```cs
builder.WithLogging(typeof(MyLoggerFactory))
```
If you write your own logger factory, your implementation can use constructor injection to pass through things you might need. The only instance which gets injected by CacheManager during initialization of the logger factory out of the box is the current instance of `CacheManagerConfiguration`. If you other types, use the `args` parameter during `WithLogging` configuration.

## Microsoft.Extensions.Logging
The first implementation of the logging abstraction, which can be installed via NuGet, uses the new [Microsoft.Extensions.Logging][aspnetLogging] framework. 
The corresponding package is [`CacheManager.Microsoft.Extensions.Logging`][cmLoggingNuget].

Microsoft.Extensions.Logging has a great, simplified logging interface with support for the most common output targets like Console, TraceSource, Debug, EventLog.

Also, other popular logging frameworks can be configured to work with `Microsoft.Extensions.Logging` already: 

* Nlog: https://github.com/NLog/NLog.Framework.logging
* Elmah: https://github.com/elmahio/Elmah.Io.Framework.Logging

And more are listed in this [readme][aspnetLogging.Readme].

Of course, Microsoft.Extensions.Logging will also work cross platform and will be used in the new ASP.NET Core 1.0.

> **Note**: Those are the reasons why I picked this framework as the first logging implementation. If there is need to support other frameworks, like CommonLogging or e.g. NLog directly, let me know and post a feature request on GitHub.

### Configuration
For the Microsoft.Extensions.Logging CacheManager extension, there are new extension methods to configure the `ILoggerFactory` adapter in CacheManager.

To configure it, use `WithMicrosoftLogging`:
```cs
var builder = new Core.ConfigurationBuilder("myCache");
builder.WithMicrosoftLogging(f =>
{
    f.AddConsole(LogLevel.Information);
    f.AddDebug(LogLevel.Verbose);
});
```
Note that the extension allows you to configure the *external* logger factory. The Microsoft.Extensions.Logging comes with extension methods depending on the installed NuGet packages itself.
This means, to use e.g. `Console` logging, you have to install `Microsoft.Extensions.Logging.Console`. To also use the `Debug` target, you have to install `Microsoft.Extensions.Logging.Debug`, and so on...

Add the `Microsoft.Extensions.Logging` namespace to your usings, to get the extension methods.

If you use Microsoft.Extensions.Logging.ILoggerFactory already for your own logging and want CacheManager to use the same instance, use the 2nd overload and pass in your own instance.
```cs
.WithMicrosoftLogging(loggerFactory)
```
> **Hint**: CacheManager currently uses RC1 of the `Microsoft.Extensions.*` packages. The logging in this version uses LogLevel.Debug for actually tracing and LogLevel.Verbose for debug messages... This will change in RC2. For now, configure LogLevel.Debug on the Microsoft ILoggerFactory and also set LoggerFactory.MinLevel to Debug, otherwise you'll not see any trace messages.

[aspnetLogging]: https://github.com/aspnet/Logging
[aspnetLogging.Readme]: https://github.com/aspnet/Logging/blob/dev/README.md
[cmLoggingNuget]: https://www.nuget.org/packages/CacheManager.Microsoft.Extensions.Logging/

[TOC]