using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Threading;
using Xunit.Sdk;

namespace CacheManager.Tests
{
    [ExcludeFromCodeCoverage]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments", Justification = "nope")]
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ReplaceCultureAttribute : BeforeAfterTestAttribute
    {
        private const string DefaultCultureName = "en-GB";
        private const string DefaultUICultureName = "en-US";
        private CultureInfo originalCulture;
        private CultureInfo originalUICulture;

        public ReplaceCultureAttribute()
            : this(DefaultCultureName, DefaultUICultureName)
        {
        }

        public ReplaceCultureAttribute(string currentCulture, string currentUICulture)
        {
            this.CurrentCulture = new CultureInfo(currentCulture);
            this.CurrentUICulture = new CultureInfo(currentUICulture);
        }

        public CultureInfo CurrentCulture { get; }

        public CultureInfo CurrentUICulture { get; }

        public override void Before(MethodInfo methodUnderTest)
        {
            this.originalCulture = CultureInfo.CurrentCulture;
            this.originalUICulture = CultureInfo.CurrentUICulture;

            Thread.CurrentThread.CurrentCulture = this.CurrentCulture;
            Thread.CurrentThread.CurrentUICulture = this.CurrentUICulture;
        }

        public override void After(MethodInfo methodUnderTest)
        {
            Thread.CurrentThread.CurrentCulture = this.originalCulture;
            Thread.CurrentThread.CurrentUICulture = this.originalUICulture;
        }
    }
}
