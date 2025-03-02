using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace CacheManager.Benchmarks
{
    [ExcludeFromCodeCoverage]
    public class Program
    {
        public static void Main(string[] args)
        {
            do
            {
                var config = ManualConfig.CreateMinimumViable()
                    .AddJob(Job.Default
                        .WithIterationCount(10)
                        .WithWarmupCount(2)
                        .WithLaunchCount(1))
                    .AddDiagnoser(BenchmarkDotNet.Diagnosers.MemoryDiagnoser.Default);

                BenchmarkSwitcher
                    .FromAssembly(typeof(Program).GetTypeInfo().Assembly)
                    .Run(args, config);

                Console.WriteLine("done!");
                Console.WriteLine("Press escape to exit or any key to continue...");
            } while (Console.ReadKey().Key != ConsoleKey.Escape);
        }
    }
}
