using System;
using System.Web;

namespace CacheManager.Web
{
    internal static class ContextFactory
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.String.Format(System.String,System.Object)", Justification = "It's fine")]
        public static HttpContextBase CreateContext()
        {
            if (HttpContext.Current == null)
            {
                throw new InvalidOperationException($"{nameof(HttpContext.Current)} is required for System.Web caching and must not be null.");
            }

            return new HttpContextWrapper(HttpContext.Current);
        }
    }
}