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

                    logger.LogWarn("Exception occurred. Retrying...", ex);
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

                    logger.LogWarn("Exception occurred. Retrying...", ex);
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

                    logger.LogWarn("Exception occurred. Retrying...", ex);
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
                            logger.LogWarn("Exception occurred. Retrying...", aggregateException);
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