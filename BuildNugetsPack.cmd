rem .nuget\NuGet Update -self
.nuget\nuget.exe pack src\CacheManager.Core\CacheManager.Core.csproj -o D:\_Nuget\CacheManager -Prop Configuration=Release -verbosity detailed 
.nuget\nuget.exe pack src\CacheManager.SystemRuntimeCaching\CacheManager.SystemRuntimeCaching.csproj -o D:\_Nuget\CacheManager -Prop Configuration=Release -verbosity detailed 
.nuget\nuget.exe pack src\CacheManager.WindowsAzureCaching\CacheManager.WindowsAzureCaching.csproj -o D:\_Nuget\CacheManager -Prop Configuration=Release -verbosity detailed 
.nuget\nuget.exe pack src\CacheManager.Web\CacheManager.Web.csproj -o D:\_Nuget\CacheManager -Prop Configuration=Release -verbosity detailed 
.nuget\nuget.exe pack src\CacheManager.AppFabricCache\CacheManager.AppFabricCache.csproj -o D:\_Nuget\CacheManager -Prop Configuration=Release -verbosity detailed 
.nuget\nuget.exe pack src\CacheManager.Memcached\CacheManager.Memcached.csproj -o D:\_Nuget\CacheManager -Prop Configuration=Release -verbosity detailed 
.nuget\nuget.exe pack src\CacheManager.StackExchange.Redis\CacheManager.StackExchange.Redis.csproj -o D:\_Nuget\CacheManager -Prop Configuration=Release -verbosity detailed 
.nuget\nuget.exe pack src\CacheManager.Couchbase\CacheManager.Couchbase.csproj -o D:\_Nuget\CacheManager -Prop Configuration=Release -verbosity detailed 

@PAUSE