﻿using System;
using System.Net;
using Garnet;
using Garnet.server;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CacheManager.Events.Tests;


internal class Program
{
    public static GarnetServer StartServer(ILoggerFactory loggerFactory)
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

    private static void Main(string[] args)
    {
        
        var services = new ServiceCollection();
        services.AddLogging(c =>
        {
            c.AddConsole();
            c.SetMinimumLevel(LogLevel.Warning);
        });

        var provider = services.BuildServiceProvider();
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        
        using var server = StartServer(loggerFactory);

        var app = new CommandLineApplication();
        app.Command("redis", (cmdApp) => new RedisCommand(cmdApp, loggerFactory));
        app.Command("redisAndMemory", (cmdApp) => new RedisAndMemoryCommand(cmdApp, loggerFactory));
        app.Command("redisAndMemoryNoMessages", (cmdApp) => new RedisAndMemoryNoMessagingCommand(cmdApp, loggerFactory));
        app.Command("memoryOnly", (cmdApp) => new MemoryOnlyCommand(cmdApp, loggerFactory));
        app.HelpOption("-h|--help");
        if (args.Length == 0)
        {
            app.ShowHelp();
        }

        try
        {
            app.Execute(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}
