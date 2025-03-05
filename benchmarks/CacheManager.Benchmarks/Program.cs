using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Garnet;
using Garnet.server;
using Microsoft.Extensions.Logging;

namespace CacheManager.Benchmarks;

[ExcludeFromCodeCoverage]
public class Program
{
    public static GarnetServer StartServer(ILoggerFactory loggerFactory = null)
    {
        var server = new GarnetServer(new GarnetServerOptions()
        {
            EnableLua = true,
            LuaOptions = new LuaOptions(LuaMemoryManagementMode.Native, string.Empty, TimeSpan.FromSeconds(5)),
            EndPoint = new IPEndPoint(IPAddress.Loopback, 6379)
        },
        loggerFactory: loggerFactory);

        server.Start();

        return server;
    }

    public static void Main(string[] args)
    {
        StartServer();

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
