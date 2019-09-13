using Kitsune.Models;
using Kitsune.Models.KLM;
using KitsuneLayoutManager.Constant;
using KitsuneLayoutManager.Helper;
using KitsuneLayoutManager.Helper.CacheHandler;
using KitsuneLayoutManager.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Text;

namespace KLM.Web.Controllers
{
    [Route("Web")]
    [ApiController]
    public class WebController : ControllerBase
    {
        [HttpGet]
        public ActionResult<string> Get()
        {
            return "OK";
        }

        [HttpPost("execute")]
        public IActionResult Execute([FromBody]KitsuneV2KLMRequestModel request)
        {
            KLMResponseModel klmResponse = KitsuneLayoutManager.RequestHandler.GetHtmlFromKlmV2Async(request).GetAwaiter().GetResult();
            if (klmResponse == null)
            {
                return Ok("");
            }
            return Ok(klmResponse);
        }

        [HttpGet("clearCache")]
        public IActionResult ClearCache([FromQuery] string host)
        {
            //string host = Request.Host.Value;
            try
            {
                CacheHelper.DeleteCacheEntity(host);
            }
            catch { throw; }
            return Ok("");
        }
    }
}
