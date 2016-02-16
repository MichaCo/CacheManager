<!--
{title:"CacheManager - Update Operations",
abstract: "This article explains the details of the update methods of CacheManager, which handle safe cache operations in distributed systems where you have to deal with concurrency.",
lastUpdate:"2016-02-16"
}
-->
# Update Operations
This article explains the details of the `Update` methods of CacheManager, which handle safe cache operations in distributed systems where you have to deal with concurrency.

## Why / When to use `Update`
Updating a cache item in a distributed cache is different from just changing the item within an in-process cache. 
With in-process caches, we can ensure thread safe writes, and with poco objects, the in-process cache will just keep the reference to that object and therefor always holds the same version for all threads. 

With distributed caches, we cannot assume the same. The cache item will be serialized and send to a server, in case multiple clients update the same item, one of the clients will win, the other one will loose.
Let's say you have a click counter stored in a distributed cache and both clients update the number. Before the update the number was 10 and every client increase the number by 1. The result would be 11 because one client would override the update of the other client...

To prevent such scenarios and ensure you don't loose any data, every distributed cache provider has some slightly different ways to handle that. 

## How to use `Update`
Cache Manager provides a simple interface to make this whole process very easy to use, the `Update` method.
```cs
cache.Update("key", counter => counter + 1);
```
The lambda expression provides the old value as input and takes the updated value as output.

Now, if a conflict occurs during the update operation, which can happen if another client updates the same item, Cache Manager will call the lambda again with the new version of the value as input.

Back to our example, Cache Manager would handle that version conflict if you use the `Update` method and increase the counter by one for the first client. The second client would handle the version conflict and increase the counter by one on the second try and the result would be correct.

Per default, Cache Manager will retry update operations 50 times. You can adjust limit of retries via `CacheManagerConfiguration` or by passing in the number of retries to the update method: 
```cs
cache.Update("key", obj => "new value", 100);
```
If Cache Manager reaches the limit, the Update will not be successful and you would have to handle that by reacting on the returned value. Also, CacheManager will remove the cache item from all other handles, because it could be that the other handles don't have the same version of the cache item.

## Update method variants
There are currently three different method which provide the update functionality:

* `Update`
As shown above, the `Update` method will try to safely update the element with your instruction.
The method returns the updated value if the update was successful and `Null` if not.
* `TryUpdate`
Does the same as `Update` but returns `True` if the update was successful, `False` if not, and it has an `out` parameter with the updated value which will also be `Null` if the update failed. 
* `AddOrUpdate`
This method can be used to ensure the cached item is present before updating it. If the cache item is not already stored in cache, it will be added, otherwise the update function will get executed.

## Example 1
Let's look at a simple example using all the `Update` methods:

First create a cache
```cs
var cache = CacheFactory.Build<string>(
	"myCache", 
	s => s.WithSystemRuntimeCacheHandle("handle"));
	
Console.WriteLine("Testing update...");
```
Inspect what happens if we try to update an item which has not yet been added to the cache:
```cs
string newValue;
if (!cache.TryUpdate("test", v => "item has not yet been added", out newValue))
{
    Console.WriteLine("Value not added?: {0}", newValue == null);
}
```
Now we add it to the cache
```cs
cache.Add("test", "start");
Console.WriteLine("Inital value: {0}", cache["test"]);
```
Let's see what `AddOrUpdate` does, it should run the update in this case:
```cs
cache.AddOrUpdate("test", "adding again?", v => "updating and not adding");
Console.WriteLine("After AddOrUpdate: {0}", cache["test"]);
```
Removing the item, will cause the following `Update` call to return `Null` again
```cs
cache.Remove("test");
var removeValue = cache.Update("test", v => "updated?");
Console.WriteLine("Value after remove is null?: {0}", removeValue == null);
```
## Example 2
The second example will increase a counter in a loop:

```cs
cache.AddOrUpdate("counter", 0, v => v + 1);

Console.WriteLine("Initial value: {0}", cache.Get("counter"));

for (int i = 0; i < 12345; i++)
{
    cache.Update("counter", v => v + 1);
}

Console.WriteLine("Final value: {0}", cache.Get("counter"));
```

[TOC]