
# Update Operations
Updating a cache item in a distributed cache is different from just changing the item within an in-process cache. 
With in-process caches, we can ensure thread safe writes, and with poco objects, the in-process cache will just keep the reference to that object and therefor always holds the same version for all threads. 

With distributed caches, we cannot assume the same. The cache item will be serialized and send to a server, in case multiple clients update the same item, one of the clients will win, the other one will loose.
Let's say you have a click counter stored in a distributed cache and both clients update the number. Before the update the number was 10 and every client increase the number by 1. The result would be 11 because one client would override the update of the other client...

To prevent such scenarios and ensure you don't loose any data, every distributed cache provider has some slightly different ways to handle that. 

Cache Manager provides a simple interface to make this whole process very easy to use, the `Update` method.

	cache.Update("key", counter => counter + 1);

The lambda expression provides the old value as input and takes the updated value as output.

Now, if a conflict occurs during the update operation, which can happen if another client updates the same item, Cache Manager will call the lambda again with the new version of the value as input.

Back to our example, Cache Manager would handle that version conflict if you use the `Update` method and increase the counter by one for the first client. The second client would handle the version conflict and increase the counter by one on the second try and the result would be correct.

Per default, Cache Manager will retry update operations as long as needed to successfully update the cache item. You can also limit the number of retries: 

	cache.Update(
		"key", 
		obj => "new value", 
		new UpdateItemConfig(100, VersionConflictHandling.EvictItemFromOtherCaches));

If Cache Manager reaches the limit, the Update will not be successful and you would have to handle that by reacting on the returned `boolean` value.

In addition you can configure what Cache Manager should do in case a version conflict occurred during the update operation. This will be executed even if the update was not successful (maybe because of the a limit)

Per default, Cache Manager will remove the cache item from all other handles, because it assumes the other handles don't have the same version of the cache item.

[TOC]