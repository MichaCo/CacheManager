using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;

namespace CacheManager.Core.Utility
{
    /// <summary>
    /// Utility class to do <c>null</c> and other checks.
    /// </summary>
    public static class Guard
    {
        /// <summary>
        /// Validates that <paramref name="value"/> is not <c>null</c> and otherwise throws an exception.
        /// <c>Structs</c> are allowed although <paramref name="value"/> cannot be <c>null</c> in this case.
        /// </summary>
        /// <typeparam name="T">The type of <paramref name="value"/>.</typeparam>
        /// <param name="value">The parameter value to validate.</param>
        /// <param name="parameterName">The parameter name.</param>
        /// <returns>The <paramref name="value"/>, if not <c>null</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <c>null</c>.</exception>
        [DebuggerStepThrough]
        public static T NotNull<T>([ValidatedNotNull]T value, string parameterName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return value;
        }

        /// <summary>
        /// Validates that <paramref name="value"/> is not null or empty and otherwise throws an exception.
        /// </summary>
        /// <param name="value">The parameter value to validate.</param>
        /// <param name="name">The parameter name.</param>
        /// <returns>The <paramref name="value"/>, if not null or empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is empty.</exception>
        [DebuggerStepThrough]
        public static string NotNullOrEmpty([ValidatedNotNull]string value, string name)
        {
            if (value == null)
            {
                throw new ArgumentNullException(name);
            }

            if (value.Length == 0)
            {
                throw new ArgumentException("Value must not be empty", name);
            }

            return value;
        }

        /// <summary>
        /// Validates that <paramref name="value"/> is not null or empty and otherwise throws an exception.
        /// </summary>
        /// <param name="value">The parameter value to validate.</param>
        /// <param name="name">The parameter name.</param>
        /// <returns>The <paramref name="value"/>, if not null or empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is empty.</exception>
        /// <typeparam name="T">Type of the collection.</typeparam>
        [DebuggerStepThrough]
        public static ICollection<T> NotNullOrEmpty<T>([ValidatedNotNull]ICollection<T> value, string name)
        {
            if (value == null)
            {
                throw new ArgumentNullException(name);
            }

            if (value.Count == 0)
            {
                throw new ArgumentException("Value must not be empty", name);
            }

            return value;
        }

        /// <summary>
        /// Validates that <paramref name="value"/> is not null, empty or contains whitespace only
        /// and otherwise throws an exception.
        /// </summary>
        /// <param name="value">The parameter value to validate.</param>
        /// <param name="name">The parameter name.</param>
        /// <returns>The <paramref name="value"/> if not null or empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is empty.</exception>
        [DebuggerStepThrough]
        public static string NotNullOrWhiteSpace([ValidatedNotNull]string value, string name)
        {
            NotNullOrEmpty(value, name);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value must not be empty", name);
            }

            return value;
        }

        /// <summary>
        /// Validates that <paramref name="condition"/> is true and otherwise throws an exception.
        /// </summary>
        /// <param name="condition">The condition to validate.</param>
        /// <param name="message">The message to throw if the configuration is <c>false</c>.</param>
        /// <param name="args"><c>string.Format</c> will be used to format <paramref name="message"/>
        /// and <c>args</c> to create the exception message.</param>
        /// <returns><c>true</c> if the <paramref name="condition"/> is valid.</returns>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="condition"/> is false.</exception>
        [DebuggerStepThrough]
        public static bool Ensure(bool condition, string message, params object[] args)
        {
            if (!condition)
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture, message, args));
            }

            return true;
        }

        /// <summary>
        /// Validates that <paramref name="value"/> is not <c>null</c> and otherwise throws an exception.
        /// <c>Structs</c> are allowed although <paramref name="value"/> cannot be <c>null</c> in this case.
        /// </summary>
        /// <typeparam name="T">The type of <paramref name="value"/>.</typeparam>
        /// <param name="value">The value to validate.</param>
        /// <param name="message">The message to throw if the <paramref name="value"/> is <c>null</c>.</param>
        /// <param name="args"><c>string.Format</c> will be used to format <paramref name="message"/>
        /// and <c>args</c> to create the exception message.</param>
        /// <returns>The <paramref name="value"/> if not <c>null</c>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="value"/> is <c>null</c>.</exception>
        [DebuggerStepThrough]
        public static T EnsureNotNull<T>([ValidatedNotNull]T value, string message, params object[] args)
            where T : class
        {
            if (value == null)
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture, message, args));
            }

            return value;
        }
    }

    /// <summary>
    /// Indicates to Code Analysis that a method validates a particular parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class ValidatedNotNullAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidatedNotNullAttribute"/> class.
        /// </summary>
        public ValidatedNotNullAttribute()
        {
        }
    }
}
