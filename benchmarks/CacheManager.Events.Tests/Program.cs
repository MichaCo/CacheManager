﻿using System;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CacheManager.Events.Tests
{
    internal class Program
    {
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

            var app = new CommandLineApplication(false);
            app.Command("redis", (cmdApp) => new RedisCommand(cmdApp, loggerFactory), throwOnUnexpectedArg: true);
            app.Command("redisAndMemory", (cmdApp) => new RedisAndMemoryCommand(cmdApp, loggerFactory), throwOnUnexpectedArg: true);
            app.Command("redisAndMemoryNoMessages", (cmdApp) => new RedisAndMemoryNoMessagingCommand(cmdApp, loggerFactory), throwOnUnexpectedArg: true);
            app.Command("memoryOnly", (cmdApp) => new MemoryOnlyCommand(cmdApp, loggerFactory), throwOnUnexpectedArg: true);
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
}
