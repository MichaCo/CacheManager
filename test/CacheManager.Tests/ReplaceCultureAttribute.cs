using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Threading;
using Xunit.Sdk;

namespace CacheManager.Tests
{
    /// <summary>
    /// Replaces the current culture and UI culture for the test.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ReplaceCultureAttribute : BeforeAfterTestAttribute
    {
        private const string defaultCultureName = "en-GB";
        private const string defaultUICultureName = "en-US";
        private CultureInfo originalCulture;
        private CultureInfo originalUICulture;

        /// <summary>
        /// Replaces the current culture and UI culture to en-GB and en-US respectively.
        /// </summary>
        public ReplaceCultureAttribute() :
            this(defaultCultureName, defaultUICultureName)
        {
        }

        /// <summary>
        /// Replaces the current culture and UI culture based on specified values.
        /// </summary>
        public ReplaceCultureAttribute(string currentCulture, string currentUICulture)
        {
            Culture = new CultureInfo(currentCulture);
            UICulture = new CultureInfo(currentUICulture);
        }

        /// <summary>
        /// The <see cref="Thread.CurrentCulture"/> for the test. Defaults to en-GB.
        /// </summary>
        /// <remarks>
        /// en-GB is used here as the default because en-US is equivalent to the InvariantCulture. We
        /// want to be able to find bugs where we're accidentally relying on the Invariant instead of the
        /// user's culture.
        /// </remarks>
        public CultureInfo Culture { get; }

        /// <summary>
        /// The <see cref="Thread.CurrentUICulture"/> for the test. Defaults to en-US.
        /// </summary>
        public CultureInfo UICulture { get; }

        public override void Before(MethodInfo methodUnderTest)
        {
            originalCulture = CultureInfo.CurrentCulture;
            originalUICulture = CultureInfo.CurrentUICulture;

#if DNX451 || NET40 || NET45
            Thread.CurrentThread.CurrentCulture = Culture;
            Thread.CurrentThread.CurrentUICulture = UICulture;
#else
            CultureInfo.CurrentCulture = Culture;
            CultureInfo.CurrentUICulture = UICulture;
#endif

        }

        public override void After(MethodInfo methodUnderTest)
        {
#if DNX451 || NET40 || NET45
            Thread.CurrentThread.CurrentCulture = originalCulture;
            Thread.CurrentThread.CurrentUICulture = originalUICulture;
#else
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
#endif
        }
    }
}