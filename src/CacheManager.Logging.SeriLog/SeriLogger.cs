using System;

using cachelog = CacheManager.Core.Logging;
using Serilog;
using Serilog.Events;

namespace CacheManager.Logging.SeriLog
{
    public class SeriLogger : cachelog.ILogger
    {
        private ILogger _logger;

        public SeriLogger()
        {
            _logger = Serilog.Log.Logger;
        }

        public SeriLogger(string categoryName) : this()
        {
            SetCategoryName(categoryName);
        }

        public ILogger SetCategoryName(string name)
        {
            return _logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, name);
        }

        public IDisposable BeginScope(object state)
        {
            return null;
        }

        public bool IsEnabled(cachelog.LogLevel logLevel)
        {
            return _logger.IsEnabled(Convert(logLevel));
        }

        public void Log(cachelog.LogLevel logLevel, int eventId, object message, Exception exception)
        {
            _logger.Write(Convert(logLevel), exception, "{0}:{1}", eventId, message);
        }

        public static LogEventLevel Convert(cachelog.LogLevel level)
        {
            switch (level)
            {
                case cachelog.LogLevel.Warning:
                    return LogEventLevel.Warning;

                case cachelog.LogLevel.Error:
                    return LogEventLevel.Error;

                case cachelog.LogLevel.Information:
                    return LogEventLevel.Information;

                case cachelog.LogLevel.Debug:
                    return LogEventLevel.Debug;

                case cachelog.LogLevel.Trace:
                    return LogEventLevel.Verbose;

                case cachelog.LogLevel.Critical:
                default:
                    return LogEventLevel.Fatal;
            }
        }
    }
}
