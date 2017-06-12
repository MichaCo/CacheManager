using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.UI;
using CacheManager.Core.Internal;

namespace OutputCacheExample.Controllers
{
    [OutputCache(Location = OutputCacheLocation.ServerAndClient, Duration = 0)]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var cache = CacheManager.Web.CacheManagerOutputCacheProvider.Cache;

            var model = new CacheInfoModel()
            {
            };

            foreach (var handle in cache.CacheHandles)
            {
                model.Layers.Add(handle.Configuration.Name);
                model.CacheCount.Add(handle.Configuration.Name, handle.Count);

                var stats = new Dictionary<CacheStatsCounterType, long>();

                stats.Add(CacheStatsCounterType.AddCalls, handle.Stats.GetStatistic(CacheStatsCounterType.AddCalls));
                stats.Add(CacheStatsCounterType.PutCalls, handle.Stats.GetStatistic(CacheStatsCounterType.PutCalls));
                stats.Add(CacheStatsCounterType.GetCalls, handle.Stats.GetStatistic(CacheStatsCounterType.GetCalls));
                stats.Add(CacheStatsCounterType.Hits, handle.Stats.GetStatistic(CacheStatsCounterType.Hits));
                stats.Add(CacheStatsCounterType.Misses, handle.Stats.GetStatistic(CacheStatsCounterType.Misses));
                model.Stats.Add(handle.Configuration.Name, stats);
            }

            return View(model);
        }

        [OutputCache(Location = OutputCacheLocation.Server, Duration = 2)]
        public ActionResult PageA()
        {
            return View();
        }

        [OutputCache(Location = OutputCacheLocation.Server, Duration = 10)]
        public ActionResult PageB()
        {
            return View();
        }

        [OutputCache(Location = OutputCacheLocation.Server, Duration = 30)]
        public ActionResult PageC()
        {
            return View();
        }
    }

    public class CacheInfoModel
    {
        public CacheInfoModel()
        {
        }

        public List<string> Layers { get; } = new List<string>();

        public Dictionary<string, int> CacheCount { get; } = new Dictionary<string, int>();

        public Dictionary<string, Dictionary<CacheStatsCounterType, long>> Stats { get; } = new Dictionary<string, Dictionary<CacheStatsCounterType, long>>();
    }
}