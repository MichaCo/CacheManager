using System;
using System.Web;

namespace CacheManager.Web
{
    internal static class ContextFactory
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "HttpContext", Justification = "External naming")]
        public static HttpContextBase CreateContext()
        {
            if (HttpContext.Current == null)
            {
                throw new InvalidOperationException("HttpContext.Current is required for System.Web caching and must not be null.");
            }

            return new HttpContextWrapper(HttpContext.Current);
        }
    }
}