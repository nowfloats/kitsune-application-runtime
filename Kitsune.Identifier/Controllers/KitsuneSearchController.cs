using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kitsune.Identifier.Helpers;
using Kitsune.Identifier.Models;
using Microsoft.AspNetCore.Mvc;

namespace Kitsune.Identifier.Controllers
{
    public class KitsuneSearchController : Controller
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="domain">WebsiteId</param>
        /// <param name="queryString"></param>
        /// <param name="id">DeveloperId</param>
        /// <returns></returns>
        public IActionResult Index(string domain, string queryString, string id, string websiteId,int skipBy = 0)
        {
            var httpProtocol = (Request.IsHttps) ? "https://" : "http://";

            var ksearchObject = APIHelper.GetKSearchObject(domain, queryString); //TODO: ksearch isPreview=true for demo Identifier
            var kDynamicSearchResults = APIHelper.GetKDynamicSearchObject(websiteId, queryString, id, skipBy);

            if (kDynamicSearchResults != null && kDynamicSearchResults.Data != null && kDynamicSearchResults.Data.Count() > 0)
            {
                if (ksearchObject == null || (ksearchObject != null && ksearchObject.SearchObjects.Count() == 0))
                    ksearchObject = new KSearchModel();

                if (ksearchObject != null && (ksearchObject.SearchObjects == null || ksearchObject.SearchObjects.Count() == 0))
                    ksearchObject.SearchObjects = new List<SearchObject>();


                foreach (var dynamicSearch in kDynamicSearchResults.Data)
                {
                    ksearchObject.SearchObjects.Add(new SearchObject()
                    {
                        Count = 1,
                        Description = dynamicSearch.Text,
                        Keywords = dynamicSearch.Keywords,
                        S3Url = String.Format("{0}{1}", httpProtocol, dynamicSearch.Url?.ToLower()),
                        Title = dynamicSearch.Text
                    });
                }
            }

            //if (ksearchObject == null)
            //{
            //    //error page
            //    return HttpNotFound();
            //}

            ViewBag.QueryString = queryString;
            ViewBag.urlList = ksearchObject?.SearchObjects;
            ViewBag.faviconIcon = ksearchObject?.FaviconUrl;
            //ViewBag.UrlList = urlList;

            return View();
        }
    }
}