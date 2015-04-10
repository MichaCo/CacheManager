
# Single Page To-Do App backed by Cache Manager

We will create a single page To-Do web site backed via Cache Manager.

To implement this, we will use an existing AngularJS based [examples][1] from [todomvc.com][2]. All credits for the JavaScript part of this sample site goes to them of course. 
I will just show the service implementation backed by Cache Manager.

If you don't know what the sample ToDo app does, go to [todomvc.com][2], there are many different implementations of the same application which looks similar to this:

![todomvc example][3]

## Basic Functionality
With the app the user can add new todos, edit existing ones, delete them and set them to completed state. Also there is one button which allows deleting of all completed.

## Rest Service
Our service therefore has to provide the following methods:

* Get - retrieves all existing todos
* Get(id) - retrieves on todo by id
* Post(todo) - creates a new todo and assigns a new id to id then returns it.
* Put(todo) - updates the todo
* Delete(id) - removes one todo by id
* Delete - removes all completed todos

## Project setup
We will implement the service with asp.net WebAPI, so lets create an empty WebAPI project and add the sample code to it.  So that our solution looks like that:
![enter image description here][5]

> **Hint**
> Don't worry, you can simply get the full source code of this sample from the [website repository on Github][4]

I also installed some additional nuget packages, CacheManager packages, Unity and Json.Net.

## Model
Let's add the Todo Model to the solution, the model has the three properties id, title and completed.

	using System;
	using System.Linq;
	using Newtonsoft.Json;

	namespace Website.Models
	{
	    [Serializable]
	    [JsonObject]
	    public class Todo
	    {
	        [JsonProperty(PropertyName = "id")]
	        public int Id { get; set; }
	
	        [JsonProperty(PropertyName = "title")]
	        public string Title { get; set; }
	
	        [JsonProperty(PropertyName = "completed")]
	        public bool Completed { get; set; }
	    }
	}

To get the correct json notation we define the `JsonProperty`'s name. Also we have to mark the object to be `Serializable` otherwise cache handles like the Redis handle cannot store the Todo entity.

## Setting up Cache Manager
Our service should use Cache Manager to store and retrieve the todo items. To get the Cache Manager isntance into the controller, I will use Unity as our IoC container. This could be done in many different ways, but it is actually pretty simple and need...

In Global.asax during app initialization (`Application_Start`) we just have to create the `IUnityContainer` and register the Cache Manager instance. And we have to tell the Web API framework to use unity as the dependency resolver...

    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);

            var container = new UnityContainer();
            GlobalConfiguration.Configuration.DependencyResolver = new UnityDependencyResolver(container);

            var cache = CacheFactory.Build("todos", settings =>
            {
                settings
                    .WithSystemRuntimeCacheHandle("inprocess");
            });

            container.RegisterInstance(cache);
        }
    }

Now we can let Unity inject the Cache Manager instance into our controller, so we don't have to worry about where this is coming from in our Api controller.

## Implementing the Rest Service
Now lets create the service. Todo so, we let MVC scaffold a full crud Web API controller. The generated code will be using `string` as return types, we'll change that to our `Todo` model.

Let's also add one property which gets injected via Unity. We could also use constructor injection, but this would just be more code to write...


	[Dependency]
	protected ICacheManager<object> todoCache { get; set; }

### How to store the todo items
There are now two possible ways to store the items. We know we have to retrieve all of them and also remove only a subset sometimes... 
We could store the todos as a list on one cache key. But this is usually not very efficient if we think about scaling and performance etc... 
The better solution would be to store each item with its ID as cache key.

With the second solution we have to think about how to retrieve all items.
One solution is to store another key in the cache which only stores all available ids. 
This way we also already have a way to generate new ids, because if we think about distributed caches and maybe many threads creating new todos at the same time, we don't want to have collisions (multiple threads creating new items with the same id in this case).

### Handling all keys separated 
To prevent typing errors we will add a const for the cache key which stores all the todos' keys.

Then I'll add a simple private property to retrieve the list of ids. If the key is not present, we'll simply add an empty array into the cache.

I'm using `Add` here because `Add` only adds the value only if it is not already present.

	   // key to store all available todos' keys.
	   private const string KeysKey = "todo-sample-keys";
	   
	   // retrieves all todos' keys or adds an empty int array if the key is not set
	   private List<int> AllKeys
	   {
	       get
	       {
	           var keys = todoCache.Get<int[]>(KeysKey);
	
	           if (keys == null)
	           {
	               keys = new int[] { };
	               todoCache.Add(KeysKey, keys);
	           }
	
	           return keys.ToList();
	       }
	   }

### Get and Get by Id
We can use the `AllKeys` property and iterate over the keys to return the list of `Todo`´s.
For `Get` by id, we simply call `cache.Get(id)`.

	   // GET: api/ToDo
	   public IEnumerable<Todo> Get()
	   {
	       var keys = this.AllKeys;
	
	       foreach (var key in keys)
	       {
	           yield return this.Get(key);
	       }
	   }
	
	   // GET: api/ToDo/5
	   public Todo Get(int id)
	   {
	       return todoCache.Get<Todo>(id);
	   } 

### Put
Updating an existing item is also very easy, we just have to use `cache.Put`:


	 // PUT: api/ToDo/5
	  public void Put(int id, [FromBody]Todo value)
	  {
	      todoCache.Put(id, value);
	  }

### Post
Creating a new item is a little bit more complicated. Because we store all the available ids on a separated cache key, we have to keep this list updated. Also, for creating a new item, we need to generated a new id.

To do this safely, even with distributed caches, we have to use the Cache Manager `Update` method, otherwise we could run into issues with multiple clients using the same id for new items.


	   // POST: api/ToDo
	   public Todo Post([FromBody]Todo value)
	   {
	       int newId = -1;
	       todoCache.Update(KeysKey, obj =>
	       {
	           var keys = (obj as int[]).ToList();
	           newId = !keys.Any() ? 1 : keys.Max() + 1;
	           keys.Add(newId);
	           return keys.ToArray();
	       });
	
	       value.Id = newId;
	       todoCache.Add(newId, value);
	       return value;
	   }

As discussed in the [`Update` article][6], the update Action might be called multiple times depending on version conflicts, but we will always receive the "fresh" value in this case `obj` and we just have increase the counter and add the id to the list.
If a version conflict occurs during the update process, our changes will be discarded and the Action runs again, so we will not add the new id multiple times, only the `Max` value will be different every iteration because someone else just added a todo...

Afterwards we can update the `Id` property of our `Todo` and finally `Add` it to our cache and return it. 

### Delete
Delete all completed `Todo`´s will just iterate over all existing `Todo`´s and check the `Completed` state and then call `Delete` by id.

	   // DELETE ALL completed: api/ToDo
	   public void Delete()
	   {
	       var keys = this.AllKeys;
	
	       foreach (var key in keys)
	       {
	           var item = this.Get(key);
	           if (item != null && item.Completed)
	           {
	               this.Delete(item.Id);
	           }
	       }
	   }
        
### Delete by Id
Delete by Id has a similar problem as our `Post` method, we also have to update the key holding all the todos´ ids. Same thing here, we'll use the `Update` method to ensure we work on the correct version of the ids array. All we have to do is removing the id from the array though.

	   // DELETE: api/ToDo/5
	   public void Delete(int id)
	   {
	       todoCache.Remove(id);
	       todoCache.Update(KeysKey, obj =>
	       {
	           var keys = (obj as int[]).ToList();
	           keys.Remove(id);
	           return keys.ToArray();
	       });
	   }

And that's basically all we have to do, not really a lot of code is needed to use Cache Manager as storage layer. And now you have a extremely scale-able...  (Todo) system

Ok yes, todos are not that amazing, but imagine more complex objects and scenarios ;)

## Using Cache Manager to scale
Now as you might have recognized int the "Setting up Cache Manager" section of this article, I only specified one in process cache handle. So this means our todos are just stored in memory and will be gone whenever the web app gets restarted.

But what we can now do is changing this to use e.g. Redis

    var cache = CacheFactory.Build("todos", settings =>
    {
        settings
            .WithSystemRuntimeCacheHandle("inprocess")
                .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(10))
            .And
            .WithRedisConfiguration("redisLocal", "localhost:6379,ssl=false")
            .WithRedisCacheHandle("redisLocal", true)
                .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(20));
    });

## Hosting
To host this Redis backed web app on e.g. Azure. We can tweak the configuration slightly and use connection strings to connect to the redis instance. This way we don't have to store connection information in code.
And in addition we can use the back plate feature, so that if we have multiple instances of our web app running in Azure, the `inprocess` cache handle will still be synchronized!

    var cache = CacheFactory.Build("todos", settings =>
    {
        settings
            .WithSystemRuntimeCacheHandle("inprocess")
                .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(10))
            .And
            .WithRedisBackPlate("redis.azure.us")
            .WithRedisCacheHandle("redis.azure.us", true)
                .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMinutes(20));
    });

You can either add the connection string via web.config `ConnectionStrings` section, or add it via Azure Management Portal (which is the preferred way for security reasons...).
On the Azure Management Portal, click on your web app, "All Settings", "Application Settings" and scroll down to "Connection Strings" and add the connection string to the list.
It should look similar to this:

![Azure portal][7]

The connection string itself must contain at least the host, SSL being set to true and the password being set to one of the Redis Access Keys provided by the portal.

	hostName:6380,ssl=true,password=ThEaCcessKey


----------
And that's it, you can see the sample site in action on [cachemanager-todo.azurewebsites.net][demo] and [browse the code on Github][3]

[demo]: http://cachemanager-todo.azurewebsites.net/
[1]: https://github.com/tastejs/todomvc/tree/gh-pages/examples/angularjs
[2]: http://todomvc.com/
[3]: https://github.com/MichaCo/CacheManager/raw/master/Articles/media/cachemanager-single-page-todo-app/todo-app.jpg
[4]: https://github.com/MichaCo/MichaCo.Websites/tree/master/cachemanager-todo.azurewebsites.net/Website
[5]: https://github.com/MichaCo/CacheManager/raw/master/Articles/media/cachemanager-single-page-todo-app/adding-todosample-into-webapi-project.jpg
[6]: http://mconrad.azurewebsites.net/Documentation/Index/cachemanager_update
[7]: https://github.com/MichaCo/CacheManager/raw/master/Articles/media/cachemanager-single-page-todo-app/cachemanager-todo-appsettings.jpg