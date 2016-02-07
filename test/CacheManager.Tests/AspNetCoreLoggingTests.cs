using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FluentAssertions;
using Xunit;
using Microsoft.Extensions.Logging;
using CacheManager.Core;

namespace CacheManager.Tests
{
    [ExcludeFromCodeCoverage]
    public class AspNetCoreLoggingTests
    {
        [Fact]
        public void AspNetCoreLogging_MinLogLevel_Trace()
        {
            var loggerFactory = new AspNetCore.Logging.AspNetLoggerFactory();

            // TODO: remove in RC2
            loggerFactory.MinimumLevel = LogLevel.Debug;

            // TODO: change Debug to Trace in RC2 (yes, in RC1 Verbose is higher than debug, and Verbose got renamed to Trace, later, too!)
            loggerFactory.AddConsole(LogLevel.Debug);

            var logger = (loggerFactory as Core.Logging.ILoggerFactory).CreateLogger("cat") as Core.Logging.ILogger;

            logger.Should().NotBeNull();
            logger.IsEnabled(Core.Logging.LogLevel.Trace).Should().BeTrue();
            logger.IsEnabled(Core.Logging.LogLevel.Debug).Should().BeTrue();
            logger.IsEnabled(Core.Logging.LogLevel.Information).Should().BeTrue();
            logger.IsEnabled(Core.Logging.LogLevel.Warning).Should().BeTrue();
            logger.IsEnabled(Core.Logging.LogLevel.Error).Should().BeTrue();
            logger.IsEnabled(Core.Logging.LogLevel.Critical).Should().BeTrue();
        }

        [Fact]
        public void AspNetCoreLogging_MinLogLevel_Debug()
        {
            var loggerFactory = new AspNetCore.Logging.AspNetLoggerFactory();

            // TODO: change Verbose to Debug in RC2 (yes, in RC1 Verbose is higher than debug, and Verbose got renamed to Trace, later, too!)
            loggerFactory.AddConsole(LogLevel.Verbose);

            var logger = (loggerFactory as Core.Logging.ILoggerFactory).CreateLogger("cat") as Core.Logging.ILogger;

            logger.Should().NotBeNull();
            logger.IsEnabled(Core.Logging.LogLevel.Trace).Should().BeFalse();
            logger.IsEnabled(Core.Logging.LogLevel.Debug).Should().BeTrue();
            logger.IsEnabled(Core.Logging.LogLevel.Information).Should().BeTrue();
            logger.IsEnabled(Core.Logging.LogLevel.Warning).Should().BeTrue();
            logger.IsEnabled(Core.Logging.LogLevel.Error).Should().BeTrue();
            logger.IsEnabled(Core.Logging.LogLevel.Critical).Should().BeTrue();
        }

        [Fact]
        public void AspNetCoreLogging_MinLogLevel_Info()
        {
            var loggerFactory = new AspNetCore.Logging.AspNetLoggerFactory();

            loggerFactory.AddConsole(LogLevel.Information);

            var logger = (loggerFactory as Core.Logging.ILoggerFactory).CreateLogger("cat") as Core.Logging.ILogger;

            logger.Should().NotBeNull();
            logger.IsEnabled(Core.Logging.LogLevel.Trace).Should().BeFalse();
            logger.IsEnabled(Core.Logging.LogLevel.Debug).Should().BeFalse();
            logger.IsEnabled(Core.Logging.LogLevel.Information).Should().BeTrue();
            logger.IsEnabled(Core.Logging.LogLevel.Warning).Should().BeTrue();
            logger.IsEnabled(Core.Logging.LogLevel.Error).Should().BeTrue();
            logger.IsEnabled(Core.Logging.LogLevel.Critical).Should().BeTrue();
        }

        [Fact]
        public void AspNetCoreLogging_MinLogLevel_Warn()
        {
            var loggerFactory = new AspNetCore.Logging.AspNetLoggerFactory();

            loggerFactory.AddConsole(LogLevel.Warning);

            var logger = (loggerFactory as Core.Logging.ILoggerFactory).CreateLogger("cat") as Core.Logging.ILogger;

            logger.Should().NotBeNull();
            logger.IsEnabled(Core.Logging.LogLevel.Trace).Should().BeFalse();
            logger.IsEnabled(Core.Logging.LogLevel.Debug).Should().BeFalse();
            logger.IsEnabled(Core.Logging.LogLevel.Information).Should().BeFalse();
            logger.IsEnabled(Core.Logging.LogLevel.Warning).Should().BeTrue();
            logger.IsEnabled(Core.Logging.LogLevel.Error).Should().BeTrue();
            logger.IsEnabled(Core.Logging.LogLevel.Critical).Should().BeTrue();
        }

        [Fact]
        public void AspNetCoreLogging_MinLogLevel_Error()
        {
            var loggerFactory = new AspNetCore.Logging.AspNetLoggerFactory();

            loggerFactory.AddConsole(LogLevel.Error);

            var logger = (loggerFactory as Core.Logging.ILoggerFactory).CreateLogger("cat") as Core.Logging.ILogger;

            logger.Should().NotBeNull();
            logger.IsEnabled(Core.Logging.LogLevel.Trace).Should().BeFalse();
            logger.IsEnabled(Core.Logging.LogLevel.Debug).Should().BeFalse();
            logger.IsEnabled(Core.Logging.LogLevel.Information).Should().BeFalse();
            logger.IsEnabled(Core.Logging.LogLevel.Warning).Should().BeFalse();
            logger.IsEnabled(Core.Logging.LogLevel.Error).Should().BeTrue();
            logger.IsEnabled(Core.Logging.LogLevel.Critical).Should().BeTrue();
        }

        [Fact]
        public void AspNetCoreLogging_MinLogLevel_Critical()
        {
            var loggerFactory = new AspNetCore.Logging.AspNetLoggerFactory();

            loggerFactory.AddConsole(LogLevel.Critical);

            var logger = (loggerFactory as Core.Logging.ILoggerFactory).CreateLogger("cat") as Core.Logging.ILogger;

            logger.Should().NotBeNull();
            logger.IsEnabled(Core.Logging.LogLevel.Trace).Should().BeFalse();
            logger.IsEnabled(Core.Logging.LogLevel.Debug).Should().BeFalse();
            logger.IsEnabled(Core.Logging.LogLevel.Information).Should().BeFalse();
            logger.IsEnabled(Core.Logging.LogLevel.Warning).Should().BeFalse();
            logger.IsEnabled(Core.Logging.LogLevel.Error).Should().BeFalse();
            logger.IsEnabled(Core.Logging.LogLevel.Critical).Should().BeTrue();
        }

        [Fact]
        public void AspNetCoreLogging_Builder_InvalidFactory()
        {
            Action act = () => ConfigurationBuilder.BuildConfiguration(
                s => s.WithAspNetLogging(null));

            act.ShouldThrow<ArgumentNullException>().WithMessage("*factory*");
        }

        [Fact]
        public void AspNetCoreLogging_Builder_ValidFactory()
        {
            var cfg = ConfigurationBuilder.BuildConfiguration(
                s => s.WithAspNetLogging(f => f.AddConsole()));

            cfg.LoggerFactory.Should().NotBeNull();
            cfg.LoggerFactory.CreateLogger("something").Should().NotBeNull();
        }

        [Fact]
        public void AspNetCoreLogging_TypedLogger()
        {
            var loggerFactory = new AspNetCore.Logging.AspNetLoggerFactory();
            var logger = (loggerFactory as Core.Logging.ILoggerFactory).CreateLogger(this) as Core.Logging.ILogger;
            logger.Should().NotBeNull();
        }
    }
}