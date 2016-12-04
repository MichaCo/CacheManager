using System;
using System.Linq;
using CacheManager.Core;
using Microsoft.AspNetCore.Mvc;

namespace AspnetCore.WebApp.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private readonly ICacheManager<string> cache;

        public ValuesController(ICacheManager<string> valuesCache)
        {
            this.cache = valuesCache;
        }

        // DELETE api/values/key
        [HttpDelete("{key}")]
        public IActionResult Delete(string key)
        {
            if (this.cache.Remove(key))
            {
                return Ok();
            }

            return NotFound();
        }

        // GET api/values/key
        [HttpGet("{key}")]
        public IActionResult Get(string key)
        {
            var value = this.cache.GetCacheItem(key);
            if (value == null)
            {
                return NotFound();
            }

            return Json(value.Value);
        }

        // POST api/values/key
        [HttpPost("{key}")]
        public IActionResult Post(string key, [FromBody]string value)
        {
            if (this.cache.Add(key, value))
            {
                return Ok();
            }

            return BadRequest("Item already exists.");
        }

        // PUT api/values/key
        [HttpPut("{key}")]
        public IActionResult Put(string key, [FromBody]string value)
        {
            if (this.cache.AddOrUpdate(key, value, (v) => value) != null)
            {
                return Ok();
            }

            return NotFound();
        }
    }
}