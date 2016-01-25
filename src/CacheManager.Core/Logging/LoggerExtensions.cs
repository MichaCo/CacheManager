using System;
using System.Globalization;

namespace CacheManager.Core.Logging
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public static class LoggerExtensions
    {
        //// Critical
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Performance")]
        public static void LogCritical(this ILogger logger, string message)
        {
            logger.Log(LogLevel.Critical, 0, message, null);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Performance")]
        public static void LogCritical(this ILogger logger, string message, Exception exception)
        {
            logger.Log(LogLevel.Critical, 0, message, exception);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Performance")]
        public static void LogCritical(this ILogger logger, int eventId, string message)
        {
            logger.Log(LogLevel.Critical, eventId, message, null);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Performance")]
        public static void LogCritical(this ILogger logger, int eventId, string message, Exception exception)
        {
            logger.Log(LogLevel.Critical, eventId, message, exception);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Performance")]
        public static void LogCritical(this ILogger logger, Exception exception, string format, params object[] args)
        {
            logger.Log(LogLevel.Critical, 0, new FormatMessage(format, args), exception);
        }

        //// DEBUG
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Performance")]
        public static void LogDebug(this ILogger logger, string message)
        {
            logger.Log(LogLevel.Debug, 0, message, null);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Performance")]
        public static void LogDebug(this ILogger logger, int eventId, string message)
        {
            logger.Log(LogLevel.Debug, eventId, message, null);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Performance")]
        public static void LogDebug(this ILogger logger, string format, params object[] args)
        {
            logger.Log(LogLevel.Debug, 0, new FormatMessage(format, args), null);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Performance")]
        public static void LogDebug(this ILogger logger, int eventId, string format, params object[] args)
        {
            logger.Log(LogLevel.Debug, eventId, new FormatMessage(format, args), null);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "2", Justification = "Performance")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Performance")]
        public static void LogDebug<T>(this ILogger logger, string message, CacheItem<T> cacheItem)
        {
            logger.Log(LogLevel.Debug, 0, new FormatMessage("{0} [CacheItem: {1} {2}]", message, cacheItem.Key, cacheItem.Region), null);
        }

        //// Error

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Performance")]
        public static void LogError(this ILogger logger, string message)
        {
            logger.Log(LogLevel.Error, 0, message, null);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Performance")]
        public static void LogError(this ILogger logger, int eventId, string message)
        {
            logger.Log(LogLevel.Error, eventId, message, null);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Performance")]
        public static void LogError(this ILogger logger, string format, params object[] args)
        {
            logger.Log(LogLevel.Error, 0, new FormatMessage(format, args), null);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Performance")]
        public static void LogError(this ILogger logger, int eventId, string format, params object[] args)
        {
            logger.Log(LogLevel.Error, eventId, new FormatMessage(format, args), null);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "2", Justification = "Performance")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Performance")]
        public static void LogError<T>(this ILogger logger, string message, CacheItem<T> cacheItem)
        {
            logger.Log(LogLevel.Error, 0, new FormatMessage("{0} [CacheItem: {1} {2}]", message, cacheItem.Key, cacheItem.Region), null);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Performance")]
        public static void LogError(this ILogger logger, string message, Exception exception)
        {
            logger.Log(LogLevel.Error, 0, message, exception);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Performance")]
        public static void LogError(this ILogger logger, int eventId, string message, Exception exception)
        {
            logger.Log(LogLevel.Error, eventId, message, exception);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Performance")]
        public static void LogError(this ILogger logger, Exception exception, string format, params object[] args)
        {
            logger.Log(LogLevel.Error, 0, new FormatMessage(format, args), exception);
        }

        //// Information
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Performance")]
        public static void LogInfo(this ILogger logger, string format, params object[] args)
        {
            logger.Log(LogLevel.Information, 0, new FormatMessage(format, args), null);
        }

        //// Trace
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Performance")]
        public static void LogTrace(this ILogger logger, string format, params object[] args)
        {
            logger.Log(LogLevel.Trace, 0, new FormatMessage(format, args), null);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "2", Justification = "Performance")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Performance")]
        public static void LogTrace<T>(this ILogger logger, string message, CacheItem<T> cacheItem)
        {
            logger.Log(LogLevel.Trace, 0, new FormatMessage("{0} [CacheItem: {1} {2}]", message, cacheItem.Key, cacheItem.Region), null);
        }

        //// Warning
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Performance")]
        public static void LogWarn(this ILogger logger, string format, params object[] args)
        {
            logger.Log(LogLevel.Warning, 0, new FormatMessage(format, args), null);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Performance")]
        public static void LogWarn(this ILogger logger, string message, Exception exception)
        {
            logger.Log(LogLevel.Warning, 0, message, exception);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Performance")]
        public static void LogWarn(this ILogger logger, int eventId, string message)
        {
            logger.Log(LogLevel.Warning, eventId, message, null);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Performance")]
        public static void LogWarn(this ILogger logger, int eventId, string message, Exception exception)
        {
            logger.Log(LogLevel.Warning, eventId, message, exception);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "Performance")]
        public static void LogWarn(this ILogger logger, Exception exception, string format, params object[] args)
        {
            logger.Log(LogLevel.Warning, 0, new FormatMessage(format, args), exception);
        }

        private class FormatMessage
        {
            private readonly string format;
            private readonly object[] args;

            public FormatMessage(string format, params object[] args)
            {
                this.format = format;
                this.args = args;
            }

            public override string ToString()
            {
                return string.Format(CultureInfo.CurrentCulture, this.format, this.args);
            }
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}