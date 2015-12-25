using System.Web;

namespace CacheManager.Web
{
    internal static class ContextFactory
    {
        public static HttpContextBase CreateContext() => new HttpContextWrapper(HttpContext.Current);
    }
}