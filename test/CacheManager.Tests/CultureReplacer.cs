using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using Xunit;

namespace CacheManager.Tests
{
    [ExcludeFromCodeCoverage]
    public class CultureReplacer : IDisposable
    {
        private readonly CultureInfo originalCulture;
        private readonly CultureInfo originalUICulture;
        private readonly long threadId;

        // Culture => Formatting of dates/times/money/etc, defaults to en-GB because en-US is the
        // same as InvariantCulture We want to be able to find issues where the InvariantCulture is
        // used, but a specific culture should be.
        // 
        // UICulture => Language
        public CultureReplacer(string culture = "en-GB", string uiCulture = "en-US")
        {
            this.originalCulture = Thread.CurrentThread.CurrentCulture;
            this.originalUICulture = Thread.CurrentThread.CurrentUICulture;
            this.threadId = Thread.CurrentThread.ManagedThreadId;

            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(uiCulture);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                Assert.True(Thread.CurrentThread.ManagedThreadId == this.threadId, "The current thread is not the same as the thread invoking the constructor. This should never happen.");
                Thread.CurrentThread.CurrentCulture = this.originalCulture;
                Thread.CurrentThread.CurrentUICulture = this.originalUICulture;
            }
        }
    }
}