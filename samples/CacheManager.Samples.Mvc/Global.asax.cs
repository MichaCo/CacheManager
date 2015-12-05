using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using CacheManager.Core;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Mvc;

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

            DependencyResolver.SetResolver(new UnityDependencyResolver(container));

            container.RegisterType(typeof(ICacheManager<>), new ContainerControlledLifetimeManager(),
                new InjectionFactory((c, targetType, name) =>
                {
                    return CacheFactory.FromConfiguration(targetType.GenericTypeArguments[0], "myCache");
                }));
        }
    }
}
