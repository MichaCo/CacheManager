using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

using Microsoft.Extensions.Logging;
namespace AspnetCore.WebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .ConfigureLogging(b => b.SetMinimumLevel(LogLevel.Trace))
                .ConfigureAppConfiguration((ctx, builder) =>
                {
                    builder.AddJsonFile("cache.json", optional: false);
                })
                .Build();

            host.Run();
        }
    }
}
