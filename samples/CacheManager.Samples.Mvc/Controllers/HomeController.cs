using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using CacheManager.Core;
using CacheManager.Web;

namespace CacheManager.Samples.Mvc.Controllers
{
    [OutputCache(CacheProfile = "cacheManagerProfile")]
    public class HomeController : Controller
    {
        private static long adds = 0L;
        private readonly ICacheManager<int> cache;

        public HomeController(ICacheManager<int> objCache)
        {
            this.cache = objCache;
        }

        static HomeController()
        {
            CacheManagerOutputCacheProvider.Cache.OnPut += Cache_OnPut;
        }

        static void Cache_OnPut(object sender, Core.Cache.CacheActionEventArgs e)
        {
            adds++;
        }

        public ActionResult Index()
        {
            this.cache.Update("index", (o) => o + 1);
            return View(new CounterModel(this.cache, adds));
        }

        public ActionResult About()
        {
            this.cache.Update("about", (o) => o + 1);
            return View(new CounterModel(this.cache, adds));
        }

        [OutputCache(NoStore=true, Location = OutputCacheLocation.None)]
        public ActionResult Contact()
        {
            this.cache.Update("contact", (o) => o + 1);
            return View(new CounterModel(this.cache, adds));
        }

        [HttpPost]
        [OutputCache(NoStore = true, Location = OutputCacheLocation.None)]
        public int Like()
        {
            this.cache.Update("like", (o) => o + 1);

            CacheManagerOutputCacheProvider.Cache.Clear();
            return this.cache.Get("like");
        }
    }

    public class CounterModel
    {
        public CounterModel(ICacheManager<int> cache, long adds)
        {
            this.Adds = adds;
            this.AboutClicks = cache.Get("about");
            this.IndexClicks = cache.Get("index");
            this.ContactClicks = cache.Get("contact");
            this.Likes = cache.Get("like");
        }

        public long Adds { get; set; }
        public int AboutClicks { get; set; }
        public int IndexClicks { get; set; }
        public int ContactClicks { get; set; }
        public int Likes { get; set; }
    }
}