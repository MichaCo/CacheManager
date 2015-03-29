using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackRedis = StackExchange.Redis;

namespace CacheManager.Redis
{
    internal static class RetryHelper
    {
        public static T Retry<T>(Func<T> retryme, int timeOut, int retries)
        {
            var tries = 0;
            do
            {
                try
                {
                    return retryme();
                }
                catch (StackRedis.RedisConnectionException)
                {
                    Task.Delay(timeOut).Wait();
                }
                catch (System.TimeoutException)
                {
                    Task.Delay(timeOut).Wait();
                }
                catch (AggregateException ag)
                {
                    ag.Handle(e =>
                    {
                        if (e is StackRedis.RedisConnectionException || e is System.TimeoutException)
                        {
                            Task.Delay(timeOut).Wait();
                            return true;
                        }

                        return false;
                    });
                }
            } while (tries < retries);

            return default(T);
        }

        public static void Retry(Action retryme, int timeOut, int retries)
        {
            var result = Retry<bool>(() => { retryme(); return true; }, timeOut, retries);
        }
    }
}
