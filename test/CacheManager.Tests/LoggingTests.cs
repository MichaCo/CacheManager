using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CacheManager.Core;
using CacheManager.Core.Logging;
using FluentAssertions;
using Xunit;

namespace CacheManager.Tests
{
    [ExcludeFromCodeCoverage]
    public class LoggingTests
    {
        //// Critical

        [Fact]
        public void Logging_ExtensionTest_CriticalA()
        {
            var logger = new TestLogger();

            logger.LogCritical("some message.");

            logger.Last.EventId.Should().Be(0);
            logger.Last.Exception.Should().BeNull();
            logger.Last.LogLevel.Should().Be(LogLevel.Critical);
            logger.Last.Message.ToString().Should().Be("some message.");
        }

        [Fact]
        public void Logging_ExtensionTest_CriticalB()
        {
            var logger = new TestLogger();

            logger.LogCritical("message {0} {1}.", "a", "b");

            logger.Last.EventId.Should().Be(0);
            logger.Last.Exception.Should().BeNull();
            logger.Last.LogLevel.Should().Be(LogLevel.Critical);
            logger.Last.Message.ToString().Should().Be("message a b.");
        }

        [Fact]
        public void Logging_ExtensionTest_CriticalC()
        {
            var logger = new TestLogger();

            logger.LogCritical(11, "message {0} {1}.", "a", "b");

            logger.Last.EventId.Should().Be(11);
            logger.Last.Exception.Should().BeNull();
            logger.Last.LogLevel.Should().Be(LogLevel.Critical);
            logger.Last.Message.ToString().Should().Be("message a b.");
        }

        [Fact]
        public void Logging_ExtensionTest_CriticalD()
        {
            var logger = new TestLogger();

            logger.LogCritical(new InvalidCastException(), "message {0} {1}.", "a", "b");

            logger.Last.EventId.Should().Be(0);
            logger.Last.Exception.Should().BeOfType<InvalidCastException>();
            logger.Last.LogLevel.Should().Be(LogLevel.Critical);
            logger.Last.Message.ToString().Should().Be("message a b.");
        }

        [Fact]
        public void Logging_ExtensionTest_CriticalE()
        {
            var logger = new TestLogger();

            logger.LogCritical(33, new InvalidCastException(), "message {0} {1}.", "a", "b");

            logger.Last.EventId.Should().Be(33);
            logger.Last.Exception.Should().BeOfType<InvalidCastException>();
            logger.Last.LogLevel.Should().Be(LogLevel.Critical);
            logger.Last.Message.ToString().Should().Be("message a b.");
        }

        //// Debug

        [Fact]
        public void Logging_ExtensionTest_DebugA()
        {
            var logger = new TestLogger();

            logger.LogDebug("some message.");

            logger.Last.EventId.Should().Be(0);
            logger.Last.Exception.Should().BeNull();
            logger.Last.LogLevel.Should().Be(LogLevel.Debug);
            logger.Last.Message.ToString().Should().Be("some message.");
        }

        [Fact]
        public void Logging_ExtensionTest_DebugB()
        {
            var logger = new TestLogger();

            logger.LogDebug("message {0} {1}.", "a", "b");

            logger.Last.EventId.Should().Be(0);
            logger.Last.Exception.Should().BeNull();
            logger.Last.LogLevel.Should().Be(LogLevel.Debug);
            logger.Last.Message.ToString().Should().Be("message a b.");
        }

        [Fact]
        public void Logging_ExtensionTest_DebugC()
        {
            var logger = new TestLogger();

            logger.LogDebug(11, "message {0} {1}.", "a", "b");

            logger.Last.EventId.Should().Be(11);
            logger.Last.Exception.Should().BeNull();
            logger.Last.LogLevel.Should().Be(LogLevel.Debug);
            logger.Last.Message.ToString().Should().Be("message a b.");
        }

        [Fact]
        public void Logging_ExtensionTest_DebugD()
        {
            var logger = new TestLogger();

            logger.LogDebug(new InvalidCastException(), "message {0} {1}.", "a", "b");

            logger.Last.EventId.Should().Be(0);
            logger.Last.Exception.Should().BeOfType<InvalidCastException>();
            logger.Last.LogLevel.Should().Be(LogLevel.Debug);
            logger.Last.Message.ToString().Should().Be("message a b.");
        }

        [Fact]
        public void Logging_ExtensionTest_DebugE()
        {
            var logger = new TestLogger();

            logger.LogDebug(33, new InvalidCastException(), "message {0} {1}.", "a", "b");

            logger.Last.EventId.Should().Be(33);
            logger.Last.Exception.Should().BeOfType<InvalidCastException>();
            logger.Last.LogLevel.Should().Be(LogLevel.Debug);
            logger.Last.Message.ToString().Should().Be("message a b.");
        }

        //// Error

        [Fact]
        public void Logging_ExtensionTest_ErrorA()
        {
            var logger = new TestLogger();

            logger.LogError("some message.");

            logger.Last.EventId.Should().Be(0);
            logger.Last.Exception.Should().BeNull();
            logger.Last.LogLevel.Should().Be(LogLevel.Error);
            logger.Last.Message.ToString().Should().Be("some message.");
        }

        [Fact]
        public void Logging_ExtensionTest_ErrorB()
        {
            var logger = new TestLogger();

            logger.LogError("message {0} {1}.", "a", "b");

            logger.Last.EventId.Should().Be(0);
            logger.Last.Exception.Should().BeNull();
            logger.Last.LogLevel.Should().Be(LogLevel.Error);
            logger.Last.Message.ToString().Should().Be("message a b.");
        }

        [Fact]
        public void Logging_ExtensionTest_ErrorC()
        {
            var logger = new TestLogger();

            logger.LogError(11, "message {0} {1}.", "a", "b");

            logger.Last.EventId.Should().Be(11);
            logger.Last.Exception.Should().BeNull();
            logger.Last.LogLevel.Should().Be(LogLevel.Error);
            logger.Last.Message.ToString().Should().Be("message a b.");
        }

        [Fact]
        public void Logging_ExtensionTest_ErrorD()
        {
            var logger = new TestLogger();

            logger.LogError(new InvalidCastException(), "message {0} {1}.", "a", "b");

            logger.Last.EventId.Should().Be(0);
            logger.Last.Exception.Should().BeOfType<InvalidCastException>();
            logger.Last.LogLevel.Should().Be(LogLevel.Error);
            logger.Last.Message.ToString().Should().Be("message a b.");
        }

        [Fact]
        public void Logging_ExtensionTest_ErrorE()
        {
            var logger = new TestLogger();

            logger.LogError(33, new InvalidCastException(), "message {0} {1}.", "a", "b");

            logger.Last.EventId.Should().Be(33);
            logger.Last.Exception.Should().BeOfType<InvalidCastException>();
            logger.Last.LogLevel.Should().Be(LogLevel.Error);
            logger.Last.Message.ToString().Should().Be("message a b.");
        }

        //// Info

        [Fact]
        public void Logging_ExtensionTest_InfoA()
        {
            var logger = new TestLogger();

            logger.LogInfo("some message.");

            logger.Last.EventId.Should().Be(0);
            logger.Last.Exception.Should().BeNull();
            logger.Last.LogLevel.Should().Be(LogLevel.Information);
            logger.Last.Message.ToString().Should().Be("some message.");
        }

        [Fact]
        public void Logging_ExtensionTest_InfoB()
        {
            var logger = new TestLogger();

            logger.LogInfo("message {0} {1}.", "a", "b");

            logger.Last.EventId.Should().Be(0);
            logger.Last.Exception.Should().BeNull();
            logger.Last.LogLevel.Should().Be(LogLevel.Information);
            logger.Last.Message.ToString().Should().Be("message a b.");
        }

        [Fact]
        public void Logging_ExtensionTest_InfoC()
        {
            var logger = new TestLogger();

            logger.LogInfo(11, "message {0} {1}.", "a", "b");

            logger.Last.EventId.Should().Be(11);
            logger.Last.Exception.Should().BeNull();
            logger.Last.LogLevel.Should().Be(LogLevel.Information);
            logger.Last.Message.ToString().Should().Be("message a b.");
        }

        [Fact]
        public void Logging_ExtensionTest_InfoD()
        {
            var logger = new TestLogger();

            logger.LogInfo(new InvalidCastException(), "message {0} {1}.", "a", "b");

            logger.Last.EventId.Should().Be(0);
            logger.Last.Exception.Should().BeOfType<InvalidCastException>();
            logger.Last.LogLevel.Should().Be(LogLevel.Information);
            logger.Last.Message.ToString().Should().Be("message a b.");
        }

        [Fact]
        public void Logging_ExtensionTest_InfoE()
        {
            var logger = new TestLogger();

            logger.LogInfo(33, new InvalidCastException(), "message {0} {1}.", "a", "b");

            logger.Last.EventId.Should().Be(33);
            logger.Last.Exception.Should().BeOfType<InvalidCastException>();
            logger.Last.LogLevel.Should().Be(LogLevel.Information);
            logger.Last.Message.ToString().Should().Be("message a b.");
        }

        //// Trace

        [Fact]
        public void Logging_ExtensionTest_TraceA()
        {
            var logger = new TestLogger();

            logger.LogTrace("some message.");

            logger.Last.EventId.Should().Be(0);
            logger.Last.Exception.Should().BeNull();
            logger.Last.LogLevel.Should().Be(LogLevel.Trace);
            logger.Last.Message.ToString().Should().Be("some message.");
        }

        [Fact]
        public void Logging_ExtensionTest_TraceB()
        {
            var logger = new TestLogger();

            logger.LogTrace("message {0} {1}.", "a", "b");

            logger.Last.EventId.Should().Be(0);
            logger.Last.Exception.Should().BeNull();
            logger.Last.LogLevel.Should().Be(LogLevel.Trace);
            logger.Last.Message.ToString().Should().Be("message a b.");
        }

        [Fact]
        public void Logging_ExtensionTest_TraceC()
        {
            var logger = new TestLogger();

            logger.LogTrace(11, "message {0} {1}.", "a", "b");

            logger.Last.EventId.Should().Be(11);
            logger.Last.Exception.Should().BeNull();
            logger.Last.LogLevel.Should().Be(LogLevel.Trace);
            logger.Last.Message.ToString().Should().Be("message a b.");
        }

        [Fact]
        public void Logging_ExtensionTest_TraceD()
        {
            var logger = new TestLogger();

            logger.LogTrace(new InvalidCastException(), "message {0} {1}.", "a", "b");

            logger.Last.EventId.Should().Be(0);
            logger.Last.Exception.Should().BeOfType<InvalidCastException>();
            logger.Last.LogLevel.Should().Be(LogLevel.Trace);
            logger.Last.Message.ToString().Should().Be("message a b.");
        }

        [Fact]
        public void Logging_ExtensionTest_TraceE()
        {
            var logger = new TestLogger();

            logger.LogTrace(33, new InvalidCastException(), "message {0} {1}.", "a", "b");

            logger.Last.EventId.Should().Be(33);
            logger.Last.Exception.Should().BeOfType<InvalidCastException>();
            logger.Last.LogLevel.Should().Be(LogLevel.Trace);
            logger.Last.Message.ToString().Should().Be("message a b.");
        }

        //// Warning

        [Fact]
        public void Logging_ExtensionTest_WarnA()
        {
            var logger = new TestLogger();

            logger.LogWarn("some message.");

            logger.Last.EventId.Should().Be(0);
            logger.Last.Exception.Should().BeNull();
            logger.Last.LogLevel.Should().Be(LogLevel.Warning);
            logger.Last.Message.ToString().Should().Be("some message.");
        }

        [Fact]
        public void Logging_ExtensionTest_WarnB()
        {
            var logger = new TestLogger();

            logger.LogWarn("message {0} {1}.", "a", "b");

            logger.Last.EventId.Should().Be(0);
            logger.Last.Exception.Should().BeNull();
            logger.Last.LogLevel.Should().Be(LogLevel.Warning);
            logger.Last.Message.ToString().Should().Be("message a b.");
        }

        [Fact]
        public void Logging_ExtensionTest_WarnC()
        {
            var logger = new TestLogger();

            logger.LogWarn(11, "message {0} {1}.", "a", "b");

            logger.Last.EventId.Should().Be(11);
            logger.Last.Exception.Should().BeNull();
            logger.Last.LogLevel.Should().Be(LogLevel.Warning);
            logger.Last.Message.ToString().Should().Be("message a b.");
        }

        [Fact]
        public void Logging_ExtensionTest_WarnD()
        {
            var logger = new TestLogger();

            logger.LogWarn(new InvalidCastException(), "message {0} {1}.", "a", "b");

            logger.Last.EventId.Should().Be(0);
            logger.Last.Exception.Should().BeOfType<InvalidCastException>();
            logger.Last.LogLevel.Should().Be(LogLevel.Warning);
            logger.Last.Message.ToString().Should().Be("message a b.");
        }

        [Fact]
        public void Logging_ExtensionTest_WarnE()
        {
            var logger = new TestLogger();

            logger.LogWarn(33, new InvalidCastException(), "message {0} {1}.", "a", "b");

            logger.Last.EventId.Should().Be(33);
            logger.Last.Exception.Should().BeOfType<InvalidCastException>();
            logger.Last.LogLevel.Should().Be(LogLevel.Warning);
            logger.Last.Message.ToString().Should().Be("message a b.");
        }

        [Fact]
        public void Logging_Builder_ValidFactory()
        {
            var cfg = ConfigurationBuilder.BuildConfiguration(
                s => s.WithLogging(typeof(NullLoggerFactory)));

            cfg.LoggerFactoryType.Should().NotBeNull();
            cfg.LoggerFactoryType.Should().Be(typeof(NullLoggerFactory));
        }

        [Fact]
        public void Logging_TypedLogger()
        {
            var cfg = ConfigurationBuilder.BuildConfiguration(
                s => s.WithLogging(typeof(NullLoggerFactory)));

            cfg.LoggerFactoryType.Should().NotBeNull();
        }
    }

    public class TestLoggerFactory : ILoggerFactory
    {
        private readonly TestLogger useLogger;

        public TestLoggerFactory(TestLogger useLogger)
        {
            this.useLogger = useLogger;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return useLogger;
        }

        public ILogger CreateLogger<T>(T instance)
        {
            return useLogger;
        }
    }

    public class TestLogger : ILogger
    {
        public TestLogger()
        {
            this.LogMessages = new List<LogMessage>();
        }

        public IList<LogMessage> LogMessages { get; }

        public LogMessage Last => this.LogMessages.Last();

        public IDisposable BeginScope(object state)
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log(LogLevel logLevel, int eventId, object message, Exception exception)
        {
            this.LogMessages.Add(new LogMessage(logLevel, eventId, message, exception));
        }
    }

    public class LogMessage
    {
        public LogMessage(LogLevel level, int eventId, object message, Exception exception)
        {
            this.LogLevel = level;
            this.EventId = eventId;
            this.Message = message;
            this.Exception = exception;
        }

        public LogLevel LogLevel { get; }

        public int EventId { get; }

        public object Message { get; }

        public Exception Exception { get; }
    }
}