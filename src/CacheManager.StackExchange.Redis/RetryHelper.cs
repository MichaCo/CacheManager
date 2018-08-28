﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
#if NET40
                    TaskEx.Delay(timeOut).Wait();
#else
                    Task.Delay(timeOut).Wait();
#endif
                }
                catch (RedisConnectionException ex)
                {
                    if (tries >= retries)
                    {
                        logger.LogError(ex, ErrorMessage, retries);
                        throw;
                    }

                    logger.LogWarn(ex, WarningMessage, tries, retries);
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
                        logger.LogError(ex, ErrorMessage, retries);
                        throw;
                    }

                    logger.LogWarn(ex, WarningMessage, tries, retries);
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
#if NET40
                            TaskEx.Delay(timeOut).Wait();
#else
                            Task.Delay(timeOut).Wait();
#endif

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
        
        public static async ValueTask<T> RetryAsync<T>(Func<ValueTask<T>> retryme, int timeOut, int retries, ILogger logger)
        {
            var tries = 0;
            do
            {
                tries++;

                try
                {
                    return await retryme();
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
                    await Task.Delay(timeOut);
                }
                catch (RedisConnectionException ex)
                {
                    if (tries >= retries)
                    {
                        logger.LogError(ex, ErrorMessage, retries);
                        throw;
                    }

                    logger.LogWarn(ex, WarningMessage, tries, retries);
                    await Task.Delay(timeOut);
                }
                catch (TimeoutException ex)
                {
                    if (tries >= retries)
                    {
                        logger.LogError(ex, ErrorMessage, retries);
                        throw;
                    }

                    logger.LogWarn(ex, WarningMessage, tries, retries);
                    await Task.Delay(timeOut);
                }
                catch (AggregateException aggregateException)
                {
                    if (tries >= retries)
                    {
                        logger.LogError(aggregateException, ErrorMessage, retries);
                        throw;
                    }

                    var exceptionList = new List<Exception>();
                    foreach (var e in aggregateException.InnerExceptions)
                    {
                        if(e is RedisServerException serverEx && serverEx.Message.Contains("unknown command"))
                        {
                            exceptionList.Add(e);
                            continue;
                        }

                        if (e is RedisConnectionException || e is System.TimeoutException || e is RedisServerException)
                        {
                            logger.LogWarn(e, WarningMessage, tries, retries);
                            await Task.Delay(timeOut);
                            continue;
                        }

                        logger.LogCritical("Unhandled exception occurred.", aggregateException);
                        exceptionList.Add(e);
                    }

                    if (exceptionList.Count > 0)
                    {
                        throw new AggregateException(aggregateException.Message, exceptionList);
                    }
                }
            }
            while (tries < retries);

            return default(T);
        }

        public static async ValueTask RetryAsync(Func<ValueTask> retryme, int timeOut, int retries, ILogger logger)
        {
            await RetryAsync(
                async () =>
                {
                    await retryme();
                    return true;
                },
                timeOut,
                retries,
                logger);
        }
    }
}
