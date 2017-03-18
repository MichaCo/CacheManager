// disabling it for builds on Mono because setting the HttpContext.Current causes all kinds of strange exceptions
#if MOCK_HTTPCONTEXT_ENABLED
using System.IO;
using System.Web;
using CacheManager.Core;
using CacheManager.Core.Logging;
using CacheManager.Web;

namespace CacheManager.Tests
{
    internal class SystemWebCacheHandleWrapper<TCacheValue> : SystemWebCacheHandle<TCacheValue>
    {
        public SystemWebCacheHandleWrapper(CacheManagerConfiguration managerConfiguration, CacheHandleConfiguration configuration, ILoggerFactory loggerFactory)
            : base(managerConfiguration, configuration, loggerFactory)
        {
        }

        protected override HttpContextBase Context
        {
            get
            {
                if (HttpContext.Current == null)
                {
                    HttpContext.Current = new HttpContext(new HttpRequest("test", "http://test", string.Empty), new HttpResponse(new StringWriter()));
                }

                return new HttpContextWrapper(HttpContext.Current);
            }
        }
    }
}
#endif