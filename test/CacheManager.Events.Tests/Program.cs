using System;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace CacheManager.Events.Tests
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var loggerFactory = new LoggerFactory()
                  .AddConsole(LogLevel.Warning);

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