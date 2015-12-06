using System;
using System.Linq;
using System.Threading;
using System.Web.Mvc;
using System.Web.UI;
using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.Web;

namespace CacheManager.Samples.Mvc.Controllers
{
    public class CounterModel
    {
        public CounterModel(ICacheManager<int> cache, int adds)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            this.Adds = adds;
            this.AboutClicks = cache.Get("about");
            this.IndexClicks = cache.Get("index");
            this.ContactClicks = cache.Get("contact");
            this.Likes = cache.Get("like");
        }

        public int AboutClicks { get; }

        public int Adds { get; }

        public int ContactClicks { get; }

        public int IndexClicks { get; }

        public int Likes { get; }
    }

    [OutputCache(CacheProfile = "cacheManagerProfile")]
    public class HomeController : Controller
    {
        private static int adds = 0;
        private readonly ICacheManager<int> cache;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline", Justification = "needed")]
        static HomeController()
        {
            CacheManagerOutputCacheProvider.Cache.OnPut += Cache_OnPut;
        }

        public HomeController(ICacheManager<int> objCache)
        {
            this.cache = objCache;
        }

        public ActionResult About()
        {
            this.cache.AddOrUpdate("about", 1, (o) => o + 1);
            return this.View(new CounterModel(this.cache, adds));
        }

        [OutputCache(NoStore = true, Location = OutputCacheLocation.None)]
        public ActionResult Contact()
        {
            this.cache.AddOrUpdate("contact", 1, (o) => o + 1);
            return this.View(new CounterModel(this.cache, adds));
        }

        public ActionResult Index()
        {
            this.cache.AddOrUpdate("index", 1, (o) => o + 1);
            return this.View(new CounterModel(this.cache, adds));
        }

        [HttpPost]
        [OutputCache(NoStore = true, Location = OutputCacheLocation.None)]
        public int Like()
        {
            this.cache.AddOrUpdate("like", 1, (o) => o + 1);

            // CacheManagerOutputCacheProvider.Cache.Clear();
            return this.cache.Get("like");
        }

        private static void Cache_OnPut(object sender, CacheActionEventArgs e)
        {
            Interlocked.Increment(ref adds);
        }
    }
}