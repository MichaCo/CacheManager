using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using CacheManager.Core;
using Microsoft.Practices.Unity;

namespace CacheManager.Samples.Mvc
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            var container = new UnityContainer();

            System.Web.Mvc.DependencyResolver.SetResolver((t) => container.Resolve(t), (t) => container.ResolveAll(t));

            var cache = CacheFactory.FromConfiguration<int>("myCache");
            container.RegisterInstance(cache);
        }
    }
}
