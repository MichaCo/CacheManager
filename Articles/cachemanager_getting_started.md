# Getting started with Cache Manager

## Basics
Cache Manager is structured into many different Nuget packages.
The one which is common but must not be installed stand alone is CacheManager.Core. All other packages will add functionality, cache handles and other features, which are totally optional.

To get more details about the Cache Manager packages and features, read the [readme file][readme] and the [features article][features].

But now let's get started...

## New Console Application
To get started let us create a new .Net C# console application in visual studio:

![New project][newProject]

Now right click the project in solution explorer and click "Manage Nuget Packages", put "cachemanger" into the search box on the top right, you should get a list of packages like this:
![Add nuget][addnuget]

This will actually install already all you need to use cache manager with the `System.Runtime.Caching` based in-process cache.

Now let's add some code to the newly created program.cs `Main` method:

	using System;
	using CacheManager.Core;
	
	namespace ConsoleApplication
	{
	    class Program
	    {
	        static void Main(string[] args)
	        {
	            var cache = CacheFactory.Build("getStartedCache", settings =>
	            {
	                settings.WithSystemRuntimeCacheHandle("handleName");
	            });
	        }
	    }
	}

This will create a new Cache Manager instance.
To use the instance, we can add some test code. We will add two items, updated the second and then read both keys.

    cache.Add("keyA", "valueA");
    cache.Put("keyB", 23);
    cache.Update("keyB", v => 42);

    Console.WriteLine("KeyA is " + cache.Get("keyA"));      // should be valueA
    Console.WriteLine("KeyB is " + cache.Get("keyB"));      // should be 42

Let's remove one key and see if it worked:

    cache.Remove("keyA");

    Console.WriteLine("KeyA removed? " + (cache.Get("keyA") == null).ToString());

    Console.WriteLine("We are done...");
    Console.ReadKey();

Hopefully this worked out just fine. Now you should be set to play around with the cache!

[readme]: http://mconrad.azurewebsites.net/Home/CacheManager
[features]: http://mconrad.azurewebsites.net/Documentation/Index/cachemanager_architecture
[sysCache]: https://www.nuget.org/packages/CacheManager.SystemRuntimeCaching/
[newProject]: https://github.com/MichaCo/CacheManager/raw/master/Articles/media/cachemanager-getting-started/create-console-app.jpg
[addnuget]: https://github.com/MichaCo/CacheManager/raw/master/Articles/media/cachemanager-getting-started/add-nuget.jpg


[TOC]