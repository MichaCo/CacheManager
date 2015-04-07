[TOC]
#Cache Synchronization

## Multi-Layer Use Case
A common, more complex scenario would be, that you have a distributed cache, e.g. Redis, and you want to access that layer from multiple clients and share the cached data across those clients because the creation of the cached items is expensive, or you want to store data in one place and use it by multiple clients...  

Distributed caches are fast, but not as fast as in-process caches which keep your cache values in memory and do not have to use expensive serialization or network resources.   

In addition an application will usually read from cache a lot more than writing to it. 
Now if we put an in-process cache in front of the distributed cache, to read directly from memory, this would drastically increase the overall application's performance.   
To give you just a rough idea of the read performance difference, it can be up to 100 times faster or even more...
If a Redis cache for example can handle 10k Gets per second, a memory cache can perfrom 2 million.

### Challenges
There are some challenges with this scenario. We now store cache values in memory, what happens if the cache item was removed from cache by one client...
Of course, it will still be cached in memory by all other clients.

Let's take the following scenario: 

* ClientA removes an item from the cache. Cache Manager will not call `Remove` on both cache handles.
* ClientB does a Get on the same item
	* Cache Manager doesn't find the item in the distributed cache, but in the in-process cache layer of ClientB

```sequence
ClientA->InProc A: Remove
ClientA->Distributed: Remove
ClientB->Distributed: Get(Miss)
ClientB->InProc B: Get(Hit)
```
This means that ClientB works with out of sync data. 

To prevent this, Cache Manager has a feature called **CacheBackPlate** which will try to synchronize multiple cache clients.

## Cache Back Plates

