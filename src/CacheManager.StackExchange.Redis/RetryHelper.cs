using System;
using System.Threading.Tasks;
using CacheManager.Core.Logging;
using StackRedis = StackExchange.Redis;

namespace CacheManager.Redis
{
    internal static class RetryHelper
    {
        public static T Retry<T>(Func<T> retryme, int timeOut, int retries, ILogger logger)
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
                catch (StackRedis.RedisServerException ex)
                {
                    if (tries >= retries)
                    {
                        logger.LogError(ex, "Retries exceeded max retries {0}", retries);
                        throw;
                    }

                    logger.LogWarn(ex, "Exception occurred. retrying... {0}/{1}", tries, retries);
#if NET40
                    TaskEx.Delay(timeOut).Wait();
#else
                    Task.Delay(timeOut).Wait();
#endif
                }
                catch (StackRedis.RedisConnectionException ex)
                {
                    if (tries >= retries)
                    {
                        logger.LogError(ex, "Retries exceeded max retries {0}", retries);
                        throw;
                    }

                    logger.LogWarn(ex, "Exception occurred. retrying... {0}/{1}", tries, retries);
#if NET40
                    TaskEx.Delay(timeOut).Wait();
#else
                    Task.Delay(timeOut).Wait();
#endif
                }
                catch (TimeoutException ex)
                {
                    if (tries >= retries)
                    {
                        logger.LogError(ex, "Retries exceeded max retries {0}", retries);
                        throw;
                    }

                    logger.LogWarn(ex, "Exception occurred. retrying... {0}/{1}", tries, retries);
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
                        logger.LogError(aggregateException, "Retries exceeded max retries {0}", retries);
                        throw;
                    }

                    aggregateException.Handle(e =>
                    {
                        if (e is StackRedis.RedisConnectionException || e is System.TimeoutException || e is StackRedis.RedisServerException)
                        {
                            logger.LogWarn(e, "Exception occurred. retrying... {0}/{1}", tries, retries);
#if NET40
                            TaskEx.Delay(timeOut).Wait();
#else
                            Task.Delay(timeOut).Wait();
#endif

                            return true;
                        }

                        logger.LogCritical("Exception occurred.", aggregateException);
                        return false;
                    });
                }
            }
            while (tries < retries);

            return default(T);
        }

        public static void Retry(Action retryme, int timeOut, int retries, ILogger logger)
        {
            Retry(
                () =>
                {
                    retryme();
                    return true;
                },
                timeOut,
                retries,
                logger);
        }
    }
}