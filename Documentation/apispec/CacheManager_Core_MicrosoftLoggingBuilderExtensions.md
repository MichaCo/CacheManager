---
uid: CacheManager.Core.MicrosoftLoggingBuilderExtensions
remarks: 'To use the different log providers, install the corresponding `Microsoft.Extensions.Logging.*` package.'
---

##### **Usage** 

Either configure a CacheManager specific logger factory:

```csharp
var builder = new Core.ConfigurationBuilder("myCache");
builder.WithMicrosoftLogging(f =>
{
    f.AddConsole(LogLevel.Information);
    f.AddDebug(LogLevel.Verbose);
});
```

Or pass in an existing `ILoggerFactory`:

```csharp
var builder = new Core.ConfigurationBuilder("myCache");
builder.WithMicrosoftLogging(loggerFactory);
```

