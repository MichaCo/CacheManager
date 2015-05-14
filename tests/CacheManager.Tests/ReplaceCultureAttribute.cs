using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Threading;

namespace CacheManager.Tests
{
    /// <summary>
    /// Replaces the current culture and UI culture for the test.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ReplaceCultureAttribute : Xunit.Sdk.BeforeAfterTestAttribute
    {
        private CultureInfo originalCulture;
        private CultureInfo originalUICulture;

        public ReplaceCultureAttribute()
        {
            this.Culture = "en-GB";
            this.UICulture = "en-US";
        }

        /// <summary>
        /// Gets or sets <see cref="Thread.CurrentCulture" /> for the test. Defaults to en-GB.
        /// </summary>
        /// <value>
        /// The culture.
        /// </value>
        /// <remarks>
        /// <c>en-GB</c> is used here as the default because en-US is equivalent to the InvariantCulture.
        /// We want to be able to find bugs where we're accidentally relying on the Invariant
        /// instead of the user's culture.
        /// </remarks>
        public string Culture { get; set; }

        /// <summary>
        /// Gets or sets <see cref="Thread.CurrentUICulture" /> for the test. Defaults to en-US.
        /// </summary>
        /// <value>
        /// The UI culture.
        /// </value>
        public string UICulture { get; set; }

        public override void Before(MethodInfo methodUnderTest)
        {
            this.originalCulture = Thread.CurrentThread.CurrentCulture;
            this.originalUICulture = Thread.CurrentThread.CurrentUICulture;

            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(this.Culture);
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(this.UICulture);
        }

        public override void After(MethodInfo methodUnderTest)
        {
            Thread.CurrentThread.CurrentCulture = this.originalCulture;
            Thread.CurrentThread.CurrentUICulture = this.originalUICulture;
        }
    }
}