using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CacheManager.Core.Utility
{
    /// <summary>
    /// Time related helper.
    /// </summary>
    public static class Clock
    {
        /// <summary>
        /// Number of ticks per millisecond.
        /// </summary>
        public const long TicksPerMillisecond = 10000;

        /// <summary>
        /// Ticks since 1970.
        /// </summary>
        public const long UnixEpochTicks = TimeSpan.TicksPerDay * DaysTo1970;

        /// <summary>
        /// Seconds since 1970.
        /// </summary>
        public const long UnixEpochSeconds = UnixEpochTicks / TimeSpan.TicksPerSecond;

        // Number of days in a non-leap year
        private const int DaysPerYear = 365;

        // Number of days in 4 years
        private const int DaysPer4Years = DaysPerYear * 4 + 1;       // 1461

        // Number of days in 100 years
        private const int DaysPer100Years = DaysPer4Years * 25 - 1;  // 36524

        // Number of days in 400 years
        private const int DaysPer400Years = DaysPer100Years * 4 + 1; // 146097

        // Number of days from 1/1/0001 to 12/31/1969
        private const int DaysTo1970 = DaysPer400Years * 4 + DaysPer100Years * 3 + DaysPer4Years * 17 + DaysPerYear; // 719,162

        /// <summary>
        /// Computes a timestamp representing milliseconds since 1970.
        /// </summary>
        /// <returns>The milliseconds.</returns>
#if !NET40
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
#endif
        public static long GetUnixTimestampMillis()
        {
            return (DateTime.UtcNow.Ticks - UnixEpochTicks) / TicksPerMillisecond;
        }

        /// <summary>
        /// Computes a timestamp representing ticks since 1970.
        /// </summary>
        /// <returns>The ticks.</returns>
#if !NET40
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
#endif
        public static long GetUnixTimestampTicks()
        {
            return DateTime.UtcNow.Ticks - UnixEpochTicks;
        }

        /// <summary>
        /// Computes the milliseconds since 1970 up to the given <paramref name="date"/>.
        /// </summary>
        /// <param name="date">The <see cref="DateTime"/> base.</param>
        /// <returns>The milliseconds since 1970.</returns>
#if !NET40
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
#endif
        public static long ToUnixTimestampMillis(DateTime date)
        {
            return (date.Ticks - UnixEpochTicks) / TicksPerMillisecond;
        }
    }
}