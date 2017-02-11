using System;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace CacheManager.Benchmarks
{
    public class CacheManagerBenchConfig : ManualConfig
    {
        public CacheManagerBenchConfig()
        {
            Add(Job.MediumRun
                .With(Platform.X64));
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            do
            {
                BenchmarkSwitcher
                    .FromAssembly(typeof(Program).GetTypeInfo().Assembly)
                    .Run(args);

                Console.WriteLine("done!");
                Console.WriteLine("Press escape to exit or any key to continue...");
            } while (Console.ReadKey().Key != ConsoleKey.Escape);
        }
    }
}