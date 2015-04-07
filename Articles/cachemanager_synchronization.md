#Cache Synchronization

## Multi-Layer Use Case
A common, more complex scenario would be, that you have a distributed cache, e.g. Redis, and you want to access that layer from multiple clients and share the cached data across those clients because the creation of the cached items is expensive, or you want to store data in one place and use it by multiple clients...  

Distributed caches are fast, but not as fast as in-process caches which keep your cache values in memory and do not have to use expensive serialization or network resources.   

In addition an application will usually read from cache a lot more than writing to it. 
Now if we put an in-process cache in front of the distributed cache, to read directly from memory, this would drastically increase the overall application's performance.   
To give you just a rough idea of the read performance difference, it can be up to 100 times faster or even more...
If a Redis cache for example can handle 10k Gets per second, a memory cache can perfrom 2 million.

## Challenges
There are some challenges with this scenario. We now store cache values in memory, what happens if the cache item was removed from cache by one client...
Of course, it will still be cached in memory by all other clients.

```sequence
ClientA->InProc A: Remove
ClientA->Distributed: Remove
ClientB->Distributed: Get(Miss)
ClientB->InProc B: Get(Hit)
```

## Update operations
Also updating a cache item in a distributed cache is different. With in process caches, we can ensure thread safe writes, with 
distributed caches, we cannot do it that easily. Every distributed cache provider has some slightly different ways to handle that...

> The good new is, that CacheManager handles all that for you behind the scenes!