using System;
using System.Globalization;
using CacheManager.Core.Utility;
using Microsoft.Extensions.Logging;
using ILogger = CacheManager.Core.Logging.ILogger;
using LogLevel = CacheManager.Core.Logging.LogLevel;

namespace CacheManager.AspNetCore.Logging
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class AspNetLoggerFactory : LoggerFactory, Core.Logging.ILoggerFactory
    {
        public AspNetLoggerFactory()
        {
        }

        ILogger Core.Logging.ILoggerFactory.CreateLogger(string categoryName)
        {
            return new AspNetLoggerWrapper(this.CreateLogger(categoryName));
        }

        ILogger Core.Logging.ILoggerFactory.CreateLogger<T>(T instance)
        {
            return new AspNetLoggerWrapper(new Logger<T>(this));
        }
    }

    internal class AspNetLoggerWrapper : ILogger
    {
        private static readonly Func<object, Exception, string> Formatter = MessageFormatter;
        private readonly Microsoft.Extensions.Logging.ILogger logger;

        public AspNetLoggerWrapper(Microsoft.Extensions.Logging.ILogger logger)
        {
            Guard.NotNull(logger, nameof(logger));

            this.logger = logger;
        }

        public IDisposable BeginScope(object state)
        {
            return this.logger.BeginScopeImpl(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return this.logger.IsEnabled(GetExternalLogLevel(logLevel));
        }

        public void Log(LogLevel logLevel, int eventId, object message, Exception exception)
        {
            this.logger.Log(GetExternalLogLevel(logLevel), eventId, message, exception, Formatter);
        }

        private static string MessageFormatter(object state, Exception error)
        {
            if (state == null && error == null)
            {
                throw new InvalidOperationException("No message or exception details were found " +
                    "to create a message for the log.");
            }

            if (state == null)
            {
                return error.ToString();
            }

            if (error == null)
            {
                return state.ToString();
            }

            return string.Format(CultureInfo.CurrentCulture, "{0}{1}{2}", state, Environment.NewLine, error);
        }

        private static Microsoft.Extensions.Logging.LogLevel GetExternalLogLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Critical:
                    return Microsoft.Extensions.Logging.LogLevel.Critical;
                case LogLevel.Debug:
                    return Microsoft.Extensions.Logging.LogLevel.Debug;
                case LogLevel.Error:
                    return Microsoft.Extensions.Logging.LogLevel.Error;
                case LogLevel.Information:
                    return Microsoft.Extensions.Logging.LogLevel.Information;
                case LogLevel.Trace:
                    return Microsoft.Extensions.Logging.LogLevel.Trace;
                case LogLevel.Warning:
                    return Microsoft.Extensions.Logging.LogLevel.Warning;
            }

            return Microsoft.Extensions.Logging.LogLevel.None;
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
