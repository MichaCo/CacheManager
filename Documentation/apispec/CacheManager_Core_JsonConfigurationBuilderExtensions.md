---
uid: CacheManager.Core.JsonConfigurationBuilderExtensions
---

Configuring CacheManager to use the JSON serializer will replace the default Binary serializer.

##### **Usage**

Basic usage example:

```csharp
var builder = new Core.ConfigurationBuilder();
builder.WithJsonSerializer();
```

optionally, `Newtonsoft.Json.JsonSerializerSettings` can be specified for (de)serialization.
See the [official documentation](http://www.newtonsoft.com/json/help/html/SerializationSettings.htm) for more details.

```csharp
builder.WithJsonSerializer(new JsonSerializerSettings(), new JsonSerializerSettings());
```

