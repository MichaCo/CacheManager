using System;
using System.Globalization;
using CacheManager.Core.Utility;
using Microsoft.Extensions.Logging;
using ILogger = CacheManager.Core.Logging.ILogger;
using LogLevel = CacheManager.Core.Logging.LogLevel;

namespace CacheManager.Logging
{
#pragma warning disable SA1600
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class MicrosoftLoggerFactoryAdapter : Core.Logging.ILoggerFactory, IDisposable
    {
        private readonly ILoggerFactory _parentFactory;

        public MicrosoftLoggerFactoryAdapter()
        {
            _parentFactory = new LoggerFactory();
        }

        public MicrosoftLoggerFactoryAdapter(ILoggerFactory parentFactory)
        {
            Guard.NotNull(parentFactory, nameof(parentFactory));
            _parentFactory = parentFactory;
        }

        ~MicrosoftLoggerFactoryAdapter()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new MicrosoftLoggerAdapter(_parentFactory.CreateLogger(categoryName));
        }

        public ILogger CreateLogger<T>(T instance)
        {
            return new MicrosoftLoggerAdapter(new Logger<T>(_parentFactory));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _parentFactory.Dispose();
            }
        }
    }

    internal class MicrosoftLoggerAdapter : ILogger
    {
        private static readonly Func<object, Exception, string> _formatter = MessageFormatter;
        private readonly Microsoft.Extensions.Logging.ILogger _logger;

        public MicrosoftLoggerAdapter(Microsoft.Extensions.Logging.ILogger logger)
        {
            Guard.NotNull(logger, nameof(logger));

            _logger = logger;
        }

        public IDisposable BeginScope(object state)
        {
            return _logger.BeginScope(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _logger.IsEnabled(GetExternalLogLevel(logLevel));
        }

        public void Log(LogLevel logLevel, int eventId, object message, Exception exception)
        {
            _logger.Log(GetExternalLogLevel(logLevel), eventId, message, exception, _formatter);
        }

        private static string MessageFormatter(object state, Exception error)
        {
            if (state == null && error == null)
            {
                throw new InvalidOperationException("No message or exception details were found to create a message for the log.");
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
                case LogLevel.Debug:
                    return Microsoft.Extensions.Logging.LogLevel.Debug;
                case LogLevel.Trace:
                    return Microsoft.Extensions.Logging.LogLevel.Trace;
                case LogLevel.Information:
                    return Microsoft.Extensions.Logging.LogLevel.Information;
                case LogLevel.Warning:
                    return Microsoft.Extensions.Logging.LogLevel.Warning;
                case LogLevel.Error:
                    return Microsoft.Extensions.Logging.LogLevel.Error;
                case LogLevel.Critical:
                    return Microsoft.Extensions.Logging.LogLevel.Critical;
            }

            return Microsoft.Extensions.Logging.LogLevel.None;
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore SA1600
}
