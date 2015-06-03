using System;
using System.Linq;
using System.Diagnostics;
using CacheManager.Core;
using CacheManager.Core.Configuration;
using Microsoft.Framework.Runtime;
using System.Reflection;

namespace CacheManager.Config.Tests
{
    internal class Program
    {
        public Program(IApplicationEnvironment app,
                       IRuntimeEnvironment runtime,
                       IRuntimeOptions options,
                       ILibraryManager libraryManager,
                       IAssemblyLoaderContainer container,
                       IAssemblyLoadContextAccessor accessor)
        {
            Console.WriteLine("ApplicationName: {0} {1}", app.ApplicationName, app.Version);
            Console.WriteLine("ApplicationBasePath: {0}", app.ApplicationBasePath);
            Console.WriteLine("Framework: {0}", app.RuntimeFramework.FullName);
            Console.WriteLine("Runtime: {0} {1} {2}", runtime.RuntimeType, runtime.RuntimeArchitecture, runtime.RuntimeVersion);
            Console.WriteLine("System: {0} {1}", runtime.OperatingSystem, runtime.OperatingSystemVersion);

            var names = libraryManager
                .GetLibraries()
                .SelectMany(p => p.LoadableAssemblies)
                .Select(p => p.FullName).ToArray();

            var first = names.First();
        }

        public void Main(string[] args)
        {
            var swatch = Stopwatch.StartNew();
            int iterations = int.MaxValue;
            swatch.Restart();
            var cacheConfiguration = ConfigurationBuilder.BuildConfiguration(cfg =>
            {
                cfg.WithUpdateMode(CacheUpdateMode.Up);

#if DNXCORE50
                cfg.WithDictionaryHandle("default")
                    .EnablePerformanceCounters();
                Console.WriteLine("Using Dictionary cache handle");
#else
                cfg.WithSystemRuntimeCacheHandle("default")
                    .EnablePerformanceCounters();
                Console.WriteLine("Using System Runtime cache handle");

                cfg.WithRedisCacheHandle("redis", true)
                    .EnablePerformanceCounters();

                //cfg.WithRedisBackPlate("redis");

                cfg.WithRedisConfiguration("redis", config =>
                {
                    config.WithAllowAdmin()
                        .WithDatabase(0)
                        .WithEndpoint("localhost", 6379)
                        .WithConnectionTimeout(1000);
                });
#endif
            });
            
            for (int i = 0; i < iterations; i++)
            {
                Tests.CacheThreadTest(
                    CacheFactory.FromConfiguration<string>("cache", cacheConfiguration),
                    i + 10);

                Tests.SimpleAddGetTest(
                    // CacheFactory.FromConfiguration(cacheConfiguration),
                    CacheFactory.FromConfiguration<object>("cache", cacheConfiguration));
                // CacheUpdateTest(cache);

                // Console.WriteLine(string.Format("Iterations ended after {0}ms.", swatch.ElapsedMilliseconds));
                Console.WriteLine("---------------------------------------------------------");
                swatch.Restart();
            }

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("We are done...");
            Console.ReadLine();
        }
    }
}