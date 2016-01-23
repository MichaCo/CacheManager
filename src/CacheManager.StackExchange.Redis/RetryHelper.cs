using System;
using System.Linq;
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
                tries++;

                try
                {
                    return retryme();
                }

                // might occur on lua script excecution on a readonly slave because the master just died.
                // Should recover via fail over
                catch (StackRedis.RedisServerException)
                {
                    if (tries >= retries)
                    {
                        throw;
                    }
#if NET40
                    TaskEx.Delay(timeOut).Wait();
#else
                    Task.Delay(timeOut).Wait();
#endif
                }
                catch (StackRedis.RedisConnectionException)
                {
                    if (tries >= retries)
                    {
                        throw;
                    }
#if NET40
                    TaskEx.Delay(timeOut).Wait();
#else
                    Task.Delay(timeOut).Wait();
#endif
                }
                catch (TimeoutException)
                {
                    if (tries >= retries)
                    {
                        throw;
                    }
#if NET40
                    TaskEx.Delay(timeOut).Wait();
#else
                    Task.Delay(timeOut).Wait();
#endif
                }
                catch (AggregateException aggregateException)
                {
                    if (tries >= retries)
                    {
                        throw;
                    }

                    aggregateException.Handle(e =>
                    {
                        ////var connectionException = e as StackRedis.RedisConnectionException;
                        ////if (connectionException != null)
                        ////{
                        ////    if (connectionException.FailureType == StackRedis.ConnectionFailureType.UnableToConnect
                        ////        || connectionException.FailureType == StackRedis.ConnectionFailureType.AuthenticationFailure
                        ////        || connectionException.FailureType == StackRedis.ConnectionFailureType.UnableToResolvePhysicalConnection)
                        ////    {
                        ////        throw connectionException;
                        ////    }
                        ////}

                        if (e is StackRedis.RedisConnectionException || e is System.TimeoutException)
                        {
#if NET40
                            TaskEx.Delay(timeOut).Wait();
#else
                            Task.Delay(timeOut).Wait();
#endif

                            return true;
                        }

                        return false;
                    });
                }
            }
            while (tries < retries);

            return default(T);
        }

        public static void Retry(Action retryme, int timeOut, int retries)
        {
            Retry(
                () =>
                {
                    retryme();
                    return true;
                },
                timeOut,
                retries);
        }
    }
}