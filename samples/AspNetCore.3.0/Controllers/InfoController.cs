using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers
{
    [Route("api/[controller]")]
    public class InfoController : Controller
    {
        [HttpGet("")]
        public IActionResult Index()
        {
            var result = new Dictionary<string, string>();
            result.Add(nameof(Microsoft.Extensions.Configuration), typeof(Microsoft.Extensions.Configuration.ConfigurationProvider).Assembly.GetName().Version.ToString());
            result.Add(nameof(Microsoft.Extensions.Logging), typeof(Microsoft.Extensions.Logging.LoggerFactory).Assembly.GetName().Version.ToString());
            result.Add(nameof(Microsoft.Extensions.Caching), typeof(Microsoft.Extensions.Caching.Memory.MemoryCache).Assembly.GetName().Version.ToString());
            result.Add(nameof(Microsoft.Extensions.DependencyInjection), typeof(Microsoft.Extensions.DependencyInjection.ServiceProvider).Assembly.GetName().Version.ToString());
            result.Add(nameof(Microsoft.Extensions.Options), typeof(Microsoft.Extensions.Options.Options).Assembly.GetName().Version.ToString());
            result.Add(nameof(Microsoft.AspNetCore.Hosting), typeof(Microsoft.AspNetCore.Hosting.WebHostBuilder).Assembly.GetName().Version.ToString());

            return Json(result);
        }
    }
}
