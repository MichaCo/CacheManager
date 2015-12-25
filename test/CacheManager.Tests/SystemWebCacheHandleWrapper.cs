#if !NET40
using System.IO;
using System.Web;
using CacheManager.Core;
using CacheManager.Web;

namespace CacheManager.Tests
{
    internal class SystemWebCacheHandleWrapper<TCacheValue> : SystemWebCacheHandle<TCacheValue>
    {
        public SystemWebCacheHandleWrapper(ICacheManager<TCacheValue> manager, CacheHandleConfiguration configuration)
            : base(manager, configuration)
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