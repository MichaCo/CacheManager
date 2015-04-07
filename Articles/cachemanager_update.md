
## Update operations
Also updating a cache item in a distributed cache is different. With in process caches, we can ensure thread safe writes, with distributed caches, we cannot do it that easily. Every distributed cache provider has some slightly different ways to handle that...

Cache Manager provides a simple interface to make this whole process very easy to use, the `Update` method.

	cache.Update("key", obj => "new value");

The lambda expression provides the old value as input and takes the updated value as output.

Now, if a conflict occurs during the update operation, which can happen if another client updates the same item, cache manager will call the lambda again with the new version of the value.

Per default, Cache Manager will retry this operation as long as needed to successfully update the cache item. You can also limit the number of retries: 

	cache.Update(
		"key", 
		obj => "new value", 
		new UpdateItemConfig(100, VersionConflictHandling.EvictItemFromOtherCaches));

In addition you can configure what Cache Manager should do in case a version conflict occurred during the update operation. This will be executed even if the update was not successful (maybe because of the a limit)

Per default, Cache Manager will remove the cache item from all other handles, because it assumes the other handles don't have the same version of the cache item.