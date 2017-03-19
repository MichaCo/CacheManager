using System;
using System.Globalization;

namespace CacheManager.Core.Logging
{
#pragma warning disable SA1600
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public static class LoggerExtensions
    {
        //// Critical

        public static void LogCritical(this ILogger logger, string message, params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Log(LogLevel.Critical, 0, new FormatMessage(message, args), null);
        }

        public static void LogCritical(this ILogger logger, int eventId, string message, params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Log(LogLevel.Critical, eventId, new FormatMessage(message, args), null);
        }

        public static void LogCritical(this ILogger logger, Exception exception, string message, params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Log(LogLevel.Critical, 0, new FormatMessage(message, args), exception);
        }

        public static void LogCritical(this ILogger logger, int eventId, Exception exception, string message, params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Log(LogLevel.Critical, eventId, new FormatMessage(message, args), exception);
        }

        //// DEBUG

        public static void LogDebug(this ILogger logger, string message, params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Log(LogLevel.Debug, 0, new FormatMessage(message, args), null);
        }

        public static void LogDebug(this ILogger logger, int eventId, string message, params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Log(LogLevel.Debug, eventId, new FormatMessage(message, args), null);
        }

        public static void LogDebug(this ILogger logger, Exception exception, string message, params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Log(LogLevel.Debug, 0, new FormatMessage(message, args), exception);
        }

        public static void LogDebug(this ILogger logger, int eventId, Exception exception, string message, params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Log(LogLevel.Debug, eventId, new FormatMessage(message, args), exception);
        }

        //// Error

        public static void LogError(this ILogger logger, string message, params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Log(LogLevel.Error, 0, new FormatMessage(message, args), null);
        }

        public static void LogError(this ILogger logger, int eventId, string message, params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Log(LogLevel.Error, eventId, new FormatMessage(message, args), null);
        }

        public static void LogError(this ILogger logger, Exception exception, string message, params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Log(LogLevel.Error, 0, new FormatMessage(message, args), exception);
        }

        public static void LogError(this ILogger logger, int eventId, Exception exception, string message, params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Log(LogLevel.Error, eventId, new FormatMessage(message, args), exception);
        }

        //// Information

        public static void LogInfo(this ILogger logger, string message, params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Log(LogLevel.Information, 0, new FormatMessage(message, args), null);
        }

        public static void LogInfo(this ILogger logger, int eventId, string message, params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Log(LogLevel.Information, eventId, new FormatMessage(message, args), null);
        }

        public static void LogInfo(this ILogger logger, Exception exception, string message, params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Log(LogLevel.Information, 0, new FormatMessage(message, args), exception);
        }

        public static void LogInfo(this ILogger logger, int eventId, Exception exception, string message, params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Log(LogLevel.Information, eventId, new FormatMessage(message, args), exception);
        }

        //// Trace

        public static void LogTrace(this ILogger logger, string message, params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Log(LogLevel.Trace, 0, new FormatMessage(message, args), null);
        }

        public static void LogTrace(this ILogger logger, int eventId, string message, params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Log(LogLevel.Trace, eventId, new FormatMessage(message, args), null);
        }

        public static void LogTrace(this ILogger logger, Exception exception, string message, params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Log(LogLevel.Trace, 0, new FormatMessage(message, args), exception);
        }

        public static void LogTrace(this ILogger logger, int eventId, Exception exception, string message, params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Log(LogLevel.Trace, eventId, new FormatMessage(message, args), exception);
        }

        //// Warning

        public static void LogWarn(this ILogger logger, string message, params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Log(LogLevel.Warning, 0, new FormatMessage(message, args), null);
        }

        public static void LogWarn(this ILogger logger, int eventId, string message, params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Log(LogLevel.Warning, eventId, new FormatMessage(message, args), null);
        }

        public static void LogWarn(this ILogger logger, Exception exception, string message, params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Log(LogLevel.Warning, 0, new FormatMessage(message, args), exception);
        }

        public static void LogWarn(this ILogger logger, int eventId, Exception exception, string message, params object[] args)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            logger.Log(LogLevel.Warning, eventId, new FormatMessage(message, args), exception);
        }

        private class FormatMessage
        {
            private readonly string _format;
            private readonly object[] _args;

            public FormatMessage(string format, params object[] args)
            {
                _format = format;
                _args = args;
            }

            public override string ToString()
            {
                if (_args == null || _args.Length == 0)
                {
                    return _format;
                }

                try
                {
                    return string.Format(CultureInfo.CurrentCulture, _format, _args);
                }
                catch
                {
                    return "Failed to format string";
                }
            }
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore SA1600
}