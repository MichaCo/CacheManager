using System;
using System.Threading;
using CacheManager.Core.Logging;
using StackExchange.Redis;

namespace CacheManager.Redis
{
    internal static class RetryHelper
    {
        private const string ErrorMessage = "Maximum number of tries exceeded to perform the action: {0}.";
        private const string WarningMessage = "Exception occurred performing an action. Retrying... {0}/{1}";

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

                // might occur on lua script execution on a readonly slave because the master just died.
                // Should recover via fail over
                catch (RedisServerException ex)
                {
                    if (ex.Message.Contains("unknown command"))
                    {
                        throw;
                    }
                    if (tries >= retries)
                    {
                        logger.LogError(ex, ErrorMessage, retries);
                        throw;
                    }

                    logger.LogWarn(ex, WarningMessage, tries, retries);
                    Thread.Sleep(timeOut);
                }
                catch (RedisConnectionException ex)
                {
                    if (tries >= retries)
                    {
                        logger.LogError(ex, ErrorMessage, retries);
                        throw;
                    }

                    logger.LogWarn(ex, WarningMessage, tries, retries);
                    Thread.Sleep(timeOut);
                }
                catch (TimeoutException ex)
                {
                    if (tries >= retries)
                    {
                        logger.LogError(ex, ErrorMessage, retries);
                        throw;
                    }

                    logger.LogWarn(ex, WarningMessage, tries, retries);
                    Thread.Sleep(timeOut);
                }
                catch (AggregateException aggregateException)
                {
                    if (tries >= retries)
                    {
                        logger.LogError(aggregateException, ErrorMessage, retries);
                        throw;
                    }

                    aggregateException.Handle(e =>
                    {
                        if(e is RedisServerException serverEx && serverEx.Message.Contains("unknown command"))
                        {
                            return false;
                        }

                        if (e is RedisConnectionException || e is System.TimeoutException || e is RedisServerException)
                        {
                            logger.LogWarn(e, WarningMessage, tries, retries);
                            Thread.Sleep(timeOut);

                            return true;
                        }

                        logger.LogCritical("Unhandled exception occurred.", aggregateException);
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
