using System;
using System.Globalization;

namespace CacheManager.Core.Utility
{
    public static class Guard
    {
        public static T NotNull<T>(T value, string name)
        {
            if (value == null)
            {
                throw new ArgumentNullException(name);
            }

            return value;
        }
        
        public static string NotNullOrEmpty(string value, string name)
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

        public static string NotNullOrWhiteSpace(string value, string name)
        {
            NotNullOrEmpty(value, name);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value must not be empty", name);
            }

            return value;
        }

        public static bool Ensure(bool condition, string message, params object[] args)
        {
            if (!condition)
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture, message, args));
            }

            return true;
        }

        public static T EnsureNotNull<T>(T value, string message, params object[] args)
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
}
