using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CacheManager.Core;
using CacheManager.Logging;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CacheManager.Tests
{
    [ExcludeFromCodeCoverage]
    public class MicrosoftLoggingTests
    {
        [Fact]
        public void AspNetCoreLogging_MinLogLevel_Trace()
        {
            var external = new LoggerFactory();

            external.AddConsole(LogLevel.Trace);

            var loggerFactory = new MicrosoftLoggerFactoryAdapter(external);
            var logger = loggerFactory.CreateLogger("cat");

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
            var external = new LoggerFactory();

            external.AddConsole(LogLevel.Debug);

            var loggerFactory = new MicrosoftLoggerFactoryAdapter(external);
            var logger = loggerFactory.CreateLogger("cat");

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
            var external = new LoggerFactory();
            external.AddConsole(LogLevel.Information);
            var loggerFactory = new MicrosoftLoggerFactoryAdapter(external);
            var logger = loggerFactory.CreateLogger("cat");

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
            var external = new LoggerFactory();
            external.AddConsole(LogLevel.Warning);
            var loggerFactory = new MicrosoftLoggerFactoryAdapter(external);
            var logger = loggerFactory.CreateLogger("cat");

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
            var external = new LoggerFactory();
            external.AddConsole(LogLevel.Error);
            var loggerFactory = new MicrosoftLoggerFactoryAdapter(external);
            var logger = loggerFactory.CreateLogger("cat");

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
            var external = new LoggerFactory();
            external.AddConsole(LogLevel.Critical);
            var loggerFactory = new MicrosoftLoggerFactoryAdapter(external);
            var logger = loggerFactory.CreateLogger("cat");

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
                s => s.WithMicrosoftLogging((Action<ILoggerFactory>)null));

            act.ShouldThrow<ArgumentNullException>().WithMessage("*factory*");
        }

        [Fact]
        public void AspNetCoreLogging_Builder_InvalidLoggerFactory()
        {
            Action act = () => ConfigurationBuilder.BuildConfiguration(
                s => s.WithMicrosoftLogging((ILoggerFactory)null));

            act.ShouldThrow<ArgumentNullException>().WithMessage("*loggerFactory*");
        }

        [Fact]
        public void AspNetCoreLogging_Builder_ValidFactory()
        {
            var cfg = ConfigurationBuilder.BuildConfiguration(
                s => s.WithMicrosoftLogging(f => f.AddConsole()));

            cfg.LoggerFactoryType.Should().NotBeNull();
            cfg.LoggerFactoryType.Should().Be(typeof(MicrosoftLoggerFactoryAdapter));
        }

        [Fact]
        public void AspNetCoreLogging_TypedLogger()
        {
            var loggerFactory = new MicrosoftLoggerFactoryAdapter();
            var logger = (loggerFactory as Core.Logging.ILoggerFactory).CreateLogger(this) as Core.Logging.ILogger;
            logger.Should().NotBeNull();
        }
    }
}