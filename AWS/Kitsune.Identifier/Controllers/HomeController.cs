using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Kitsune.Identifier.Models;
using Kitsune.Models;
using Kitsune.Models.KLM;
using KitsuneLayoutManager;

namespace Kitsune.Identifier.Controllers
{
    public class HomeController : Controller
    {
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public IActionResult Index()
        {
            return View("Error");
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        [HttpGet]
        public IActionResult Ping()
        {
            return Ok();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public IActionResult Execute([FromBody]KitsuneV2KLMRequestModel request)
        {
            var klmResponse = KitsuneLayoutManager.RequestHandler.GetHtmlFromKlmV2Async(request).GetAwaiter().GetResult();
            if (klmResponse == null)
            {
                return Ok("");
            }
            return Ok(klmResponse);
        }

        [HttpPost]
        public IActionResult KPreview([FromBody]KitsunePreviewModel previewModel)
        {
            if (previewModel != null)
            {
                try
                {
                    var byteStream = Convert.FromBase64String(previewModel.FileContent);
                    previewModel.FileContent = System.Text.Encoding.UTF8.GetString(byteStream);

                    return Ok(RequestHandler.GetKLMPreviewResponse(previewModel.ProjectId, previewModel.FileContent, previewModel.View, previewModel.ViewType, previewModel.WebsiteTag, previewModel.DeveloperId, previewModel.UrlParams, previewModel.NoCacheQueryParam, false));
                }
                catch (Exception ex) { throw; }
            }
            return null;
        }
    }
}
