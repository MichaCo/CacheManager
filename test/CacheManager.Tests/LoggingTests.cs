using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CacheManager.Tests
{
    [ExcludeFromCodeCoverage]
    public class TestLoggerFactory : ILoggerFactory
    {
        private readonly TestLogger useLogger;

        public TestLoggerFactory(TestLogger useLogger)
        {
            this.useLogger = useLogger;
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return this.useLogger;
        }

        public ILogger CreateLogger<T>(T instance)
        {
            return this.useLogger;
        }

        public void Dispose()
        {
        }
    }

    [ExcludeFromCodeCoverage]
    public class TestLogger : ILogger<TestLogger>
    {
        public TestLogger()
        {
            this.LogMessages = new List<LogMessage>();
        }

        public IList<LogMessage> LogMessages { get; }

        public LogMessage Last => this.LogMessages.Last();

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            this.LogMessages.Add(new LogMessage(logLevel, eventId, formatter(state, exception), exception));
        }
    }

    [ExcludeFromCodeCoverage]
    public class LogMessage
    {
        public LogMessage(LogLevel level, EventId eventId, object message, Exception exception)
        {
            this.LogLevel = level;
            this.EventId = eventId;
            this.Message = message;
            this.Exception = exception;
        }

        public LogLevel LogLevel { get; }

        public EventId EventId { get; }

        public object Message { get; }

        public Exception Exception { get; }
    }
}
