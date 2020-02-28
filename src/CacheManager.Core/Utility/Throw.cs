using System;
using System.Runtime.ExceptionServices;

namespace CacheManager.Core.Utility
{
    /// <summary>
    /// <see cref="Exception"/> related helper.
    /// </summary>
    internal static class Throw
    {
        /// <summary>
        /// Rethrows the supplied exception without losing its stack trace.
        /// Prefer <code>throw;</code> where possible, this method is useful for rethrowing
        /// <see cref="Exception.InnerException" />
        /// which cannot be done with the <code>throw;</code> semantics.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public static void Rethrow(
            this Exception exception)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }
}
