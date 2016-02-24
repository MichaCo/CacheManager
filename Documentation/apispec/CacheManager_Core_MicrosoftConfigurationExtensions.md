---
uid: CacheManager.Core.MicrosoftConfigurationExtensions
remarks: 'To be able to use the different configuration providers like JSON or XML, you have to install the corresponding 
`Microsoft.Extensions.Configuration.*` package(s).'
---

##### **Usage**

The following is a basic example of how to use the extensions. See the [configuration documentation](http://cachemanager.net/Documentation/Index/cachemanager_configuration) for more details.

```csharp
// setting up the configuration providers
var builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
    .AddJsonFile("cache.json");

// build the configuration
this.Configuration = builder.Build();

// retrieve the CacheManager configuration
var jsonConfiguration = 
    this.Configuration.GetCacheConfiguration();
```

To create a JSON cache configuration, create a new `.json` file and use the [cachemanager.json](http://cachemanager.net/schemas/cachemanager.json) schema to
get all the benefits of IntelliSense and validation.

```JSON
{
  "$schema": "http://cachemanager.net/schemas/cachemanager.json",
  "cacheManagers": [
    {
        "name": "MyCache",
        "handles": [ { "knownType": "SystemRuntime" } ]
    }
}
```