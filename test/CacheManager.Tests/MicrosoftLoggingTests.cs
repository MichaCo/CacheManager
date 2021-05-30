using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CacheManager.Core;
using CacheManager.Logging;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CacheManager.Tests
{
    [ExcludeFromCodeCoverage]
    public class MicrosoftLoggingTests
    {
        private class TestLoggingBuilder : ILoggingBuilder
        {
            public TestLoggingBuilder()
            {
                Services = new ServiceCollection();
            }

            public IServiceCollection Services { get; }
        }

        [Fact]
        public void AspNetCoreLogging_MinLogLevel_Trace()
        {
            var services = new ServiceCollection();
            var provider = services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Trace)).BuildServiceProvider();
            var external = provider.GetRequiredService<ILoggerFactory>();

            var loggerFactory = new MicrosoftLoggerFactoryAdapter(() => external);
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
            var services = new ServiceCollection();
            var provider = services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug)).BuildServiceProvider();
            var external = provider.GetRequiredService<ILoggerFactory>();

            var loggerFactory = new MicrosoftLoggerFactoryAdapter(() => external);
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
            var services = new ServiceCollection();
            var provider = services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information)).BuildServiceProvider();
            var external = provider.GetRequiredService<ILoggerFactory>();

            var loggerFactory = new MicrosoftLoggerFactoryAdapter(() => external);
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
            var services = new ServiceCollection();
            var provider = services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning)).BuildServiceProvider();
            var external = provider.GetRequiredService<ILoggerFactory>();

            var loggerFactory = new MicrosoftLoggerFactoryAdapter(() => external);
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
            var services = new ServiceCollection();
            var provider = services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Error)).BuildServiceProvider();
            var external = provider.GetRequiredService<ILoggerFactory>();

            var loggerFactory = new MicrosoftLoggerFactoryAdapter(() => external);
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
            var services = new ServiceCollection();
            var provider = services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Critical)).BuildServiceProvider();
            var external = provider.GetRequiredService<ILoggerFactory>();

            var loggerFactory = new MicrosoftLoggerFactoryAdapter(() => external);
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
        public void AspNetCoreLogging_Builder_InvalidLoggerFactory()
        {
            Action act = () => ConfigurationBuilder.BuildConfiguration(
                s => s.WithMicrosoftLogging(null));

            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Equals("loggerFactory");
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
