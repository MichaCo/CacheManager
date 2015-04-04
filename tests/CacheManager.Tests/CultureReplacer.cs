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
        private readonly CultureInfo _originalCulture;
        private readonly CultureInfo _originalUICulture;
        private readonly long _threadId;

        // Culture => Formatting of dates/times/money/etc, defaults to en-GB because en-US is the same as InvariantCulture
        // We want to be able to find issues where the InvariantCulture is used, but a specific culture should be.
        //
        // UICulture => Language
        public CultureReplacer(string culture = "en-GB", string uiCulture = "en-US")
        {
            _originalCulture = Thread.CurrentThread.CurrentCulture;
            _originalUICulture = Thread.CurrentThread.CurrentUICulture;
            _threadId = Thread.CurrentThread.ManagedThreadId;

            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(uiCulture);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                Assert.True(Thread.CurrentThread.ManagedThreadId == _threadId, "The current thread is not the same as the thread invoking the constructor. This should never happen.");
                Thread.CurrentThread.CurrentCulture = _originalCulture;
                Thread.CurrentThread.CurrentUICulture = _originalUICulture;
            }
        }
    }
}