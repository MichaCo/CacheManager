using System;

namespace CacheManager.Core.Logging
{
    /// <summary>
    /// Represents a type used to perform logging.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs a message for the given <paramref name="logLevel"/>.
        /// </summary>
        /// <param name="logLevel">The log level.</param>
        /// <param name="eventId">The optional even id.</param>
        /// <param name="message">The log message.</param>
        /// <param name="exception">The optional exception.</param>
        void Log(LogLevel logLevel, int eventId, object message, Exception exception);

        /// <summary>
        /// Checks if the given LogLevel is enabled.
        /// </summary>
        /// <param name="logLevel">The log level.</param>
        /// <returns><c>True</c> if the <paramref name="logLevel"/> is enabled, <c>False</c> otherwise.</returns>
        bool IsEnabled(LogLevel logLevel);

        /// <summary>
        /// Begins a logical operation scope.
        /// </summary>
        /// <param name="state">The identifier for the scope.</param>
        /// <returns>An <c>IDisposable</c> that ends the logical operation scope on dispose.</returns>
        IDisposable BeginScope(object state);
    }

    internal class NullLogger : ILogger
    {
        public IDisposable BeginScope(object state) => null;

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log(LogLevel logLevel, int eventId, object message, Exception exception)
        {
        }
    }
}