﻿#if NET8_0_OR_GREATER

using System;
using System.Threading.Tasks;
using System.Threading;
using Garnet;
using Garnet.server;
using System.Net;
using Microsoft.Extensions.Logging;

#endif

namespace CacheManager.Tests
{
    public class RedisTestFixture : IDisposable
    {
#if NET8_0_OR_GREATER

        private static Lazy<GarnetServer> server = new Lazy<GarnetServer>(() => StartServer());

        private static int InstanceCount = 0;

        public static GarnetServer StartServer(ILoggerFactory loggerFactory = null)
        {
            var server = new GarnetServer(new GarnetServerOptions()
            {
                EnableLua = true,
                LuaOptions = new LuaOptions(LuaMemoryManagementMode.Native, string.Empty, TimeSpan.FromSeconds(20)),
                EndPoint = new IPEndPoint(IPAddress.Loopback, 6379),
            },
            loggerFactory: loggerFactory);
            try
            {
                server.Start();
                Task.Delay(1000).GetAwaiter().GetResult();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }

            return server;
        }

        public RedisTestFixture()
        {
            Interlocked.Increment(ref InstanceCount);

            var info = server.Value.Metrics.GetInfoMetrics();
        }

        public void Dispose()
        {
            var current = Interlocked.Decrement(ref InstanceCount);

            // Console.WriteLine($"Disposing... {current}");
            if (current == 0)
            {
                server.Value.Dispose();
                server = new Lazy<GarnetServer>(() => StartServer());
            }
        }

#else

        public void Dispose()
        {
        }

#endif
    }
}
