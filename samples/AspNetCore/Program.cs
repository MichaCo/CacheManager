using System;
using System.Linq;
using System.Net;
using Garnet;
using Garnet.server;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AspnetCore.WebApp;

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
        using var server = StartServer();
        CreateHostBuilder(args).Build().Run();
    }

    public static IWebHostBuilder CreateHostBuilder(string[] args) =>
        WebHost.CreateDefaultBuilder(args)
            .UseStartup<Startup>()
            .ConfigureAppConfiguration((ctx, builder) =>
            {
                builder.AddJsonFile("cache.json", optional: false);
            });
}
