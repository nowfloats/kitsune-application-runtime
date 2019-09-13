using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using KitsuneLayoutManager.Helper;
using System.Web;
using System.Net;
using HtmlAgilityPack;
using Kitsune.Language.Models;
using Kitsune.Models;
using KitsuneLayoutManager.Models;
using Newtonsoft.Json;
using System.Configuration;
using KitsuneLayoutManager.Helper.Entity;
using KitsuneLayoutManager.Helper.MongoConnector;
using MongoDB.Bson;
using KitsuneLayoutManager.Constant;
using System.Threading.Tasks;
using Kitsune.Models.KLM;

namespace KitsuneLayoutManager
{
    public class RequestHandler
    {
        public const string GTLD = "", ServerName = "";
        public static bool isKLMApiCacheEnabled = (bool)KLMEnvironmentalConstants.KLMConfigurations?.APICache?.IsEnabled;
        private static KLMExecutor klmExecutor;

        //To-DO handle exception scenarios
        static RequestHandler()
        {
            try
            {
                if ((bool)KLMEnvironmentalConstants.KLMConfigurations?.APICache?.IsEnabled)
                    isKLMApiCacheEnabled = true;
            }
            catch { }
        }

        public const bool isProductionUse = true;
        
        public static async Task<KLMResponseModel> GetHtmlFromKlmAsync(RequestDetails request, string domainName, string url, string projectId = null,
			string schemeId = null, string s3UrlForResource = null, string noCacheQueryParam = null, string developerId = null,
			string urlPattern = null, string websiteId = null, Kitsune.Models.Project.KitsunePageType pageType = Kitsune.Models.Project.KitsunePageType.DEFAULT,
			KitsuneRequestUrlType kitsuneRequestUrlType = KitsuneRequestUrlType.PRODUCTION, string httpProtocol = "http://",
			List<Kitsune.Models.Project.ProjectComponent> components = null, string s3FolderUrl = null, string optimizedFilePath = null,
			string urlPatternRegex = null, int compilerVersion = 0, string fptag = null)
        {
            try
            {
                var tempVariableForCache = (!string.IsNullOrEmpty(noCacheQueryParam)) ? !(string.Compare(noCacheQueryParam, "true", true) == 0) : isKLMApiCacheEnabled;
                var functionLog = new Dictionary<string, long>(); var functionStopWatch = new Stopwatch();

                #region HTTP HEADER INFO

                var ipAddress = request.IPAddress;
                string perfLog = request.Perflog;

                #endregion

                functionStopWatch.Start();
                var websiteName = domainName.Split(',')[0];

                //if (isNFSite)
                //{
                //    var themeId = MongoHelper.GetThemeIdForUser(ipAddress, domainName);
                //    if (themeId != null)
                //    {
                //        projectId = themeId;
                //    }
                //}

                #region GET ENTITY INFO

                var entity = new KEntity();

                var EntityId = schemeId;
                //if (string.IsNullOrEmpty(EntityId))
                //{
                //    EntityId = "58d717e667962d6f40f5c198";
                //}
                if (tempVariableForCache && !string.IsNullOrEmpty(EntityId))
                {
                    entity = await CacheHelper.GetEntityInfoAsync(EntityId, tempVariableForCache);
                }

                if ((entity == null || entity.Classes == null) && !string.IsNullOrEmpty(EntityId))
                {
                    entity = await MongoHelper.GetLanguageEntityAsync(EntityId);
                    if (tempVariableForCache)
                    {
                        CacheHelper.SaveEntityInfo(EntityId, entity);
                    }
                }

                Helper.Helper.UpdateFunctionLog(functionLog, Helper.Constant.GETTING_ENTITY, functionStopWatch.ElapsedMilliseconds);

                var businessClass = Kitsune.Language.Helper.Helper.GetClassFromJson(entity);
                var auditLog = new Kitsune.Models.KLMAuditLogModel();

                #endregion

                Helper.Helper.UpdateFunctionLog(functionLog, Helper.Constant.GETTING_HTTP_HEADER_INFO, functionStopWatch.ElapsedMilliseconds);
                functionStopWatch.Reset();

                //if (!string.IsNullOrEmpty(websiteName))
                //{
                    #region WEBSITE DETAILS FROM CACHE

                    functionStopWatch.Start();

                    var requestUrl = new Uri(url);
                    var httpRequestObject = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(JsonConvert.SerializeObject(new { url = requestUrl.AbsoluteUri, urlpath = requestUrl.AbsolutePath, urlsegments = requestUrl.Segments }));

                    var view = string.Empty; var viewDetails = new Models.Pagination();
                    bool isDetailsView = false; var urlParamList = new Dictionary<string, string>(); bool isSearchView = false;
                    var queryString = string.Empty;

                    // GET URL FROM API
                    var rootaliasurl = String.Format("{0}{1}", httpProtocol, websiteName);
                    if (!string.IsNullOrEmpty(s3UrlForResource) && !string.IsNullOrEmpty(projectId))
                    {
                        isDetailsView = (pageType == Kitsune.Models.Project.KitsunePageType.DETAILS) ? true : false;
                        isSearchView = (pageType == Kitsune.Models.Project.KitsunePageType.SEARCH) ? true : false;
                        bool isDefaultView = (pageType == Kitsune.Models.Project.KitsunePageType.DEFAULT) ? true : false;

                        var tempUrl = new Uri(url);
                        viewDetails = PaginationHelper.GetViewDetails(url.Trim('/'), urlPattern, rootaliasurl, isDetailsView, isSearchView);
                        if (isDetailsView)
                        {
                            queryString = Helper.Helper.GetQueryStringForDL(urlPattern);
                        }
                    }
                    if (string.IsNullOrEmpty(s3UrlForResource))
                        return null;

                    dynamic websiteData = null;
				//TODO: Need to stop backward compatibility.
                    //if (compilerVersion != 1)
                    //{
                    //    var websiteDetails = CacheHelper.GetBusinessDetailsFromCache(websiteName, tempVariableForCache, domainName, isNFSite, businessClass, entity.EntityName, developerId, websiteId, EntityId == "5aa8ffd8942c3406a81d0d7c", null);
                    //    websiteData = websiteDetails;
                    //}

                   // if (websiteData == null)
                   // {
                        websiteData = (Newtonsoft.Json.Linq.JToken)JsonConvert.DeserializeObject("{ _system:{}, rootaliasurl:{} }");
                    //}

                    websiteData["_system"] = (Newtonsoft.Json.Linq.JToken)JsonConvert.DeserializeObject("{viewbag:{}}");
                    websiteData["_system"]["request"] = httpRequestObject;
                    if (!String.IsNullOrEmpty(websiteData?.rootaliasurl?.url?.Value))
                    {
                        rootaliasurl = websiteData.rootaliasurl.url.Value;
                    }
                    else
                    {
                        websiteData["rootaliasurl"] = (Newtonsoft.Json.Linq.JToken)JsonConvert.DeserializeObject($"{{ url :  '{rootaliasurl}'  }}");
                    }

                    websiteData["rootaliasurl"]["url"] = (rootaliasurl).ToLower();

                    //Get Component
					//To be reviewed
                    #region Component Data
                    if (components != null && components.Any())
                    {
                        try
                        {
                            websiteData["_system"]["components"] = ApiHelper.GetComponentsData(components, projectId, websiteId, url, s3FolderUrl, rootaliasurl, kitsuneRequestUrlType);
                        }
                        catch { }
                    }
                    #endregion

                    Helper.Helper.UpdateFunctionLog(functionLog, Helper.Constant.GETTING_FP_DETAILS, functionStopWatch.ElapsedMilliseconds);
                    functionStopWatch.Reset();

                    #endregion
				
                    #region GET HTML FROM URL
                    functionStopWatch.Start();

                    string htmlString;

				//TODO: Need to stop backward compatibility.
				//if (compilerVersion == 1)
				//{
				htmlString = Helper.Helper.GetHtmlStringFromUrl(s3UrlForResource + ".kc");
				//}
				//else
				//{
				//    htmlString = Helper.Helper.GetHtmlStringFromUrl(s3UrlForResource);
				//}
				KLMResponseModel klmResponse;
				if (string.IsNullOrEmpty(htmlString))
				{
					klmResponse = new KLMResponseModel();
					klmResponse.HtmlCode = string.Empty;
					return klmResponse;
				}

                    Helper.Helper.UpdateFunctionLog(functionLog, Helper.Constant.GET_HTML_FROM_URL, functionStopWatch.ElapsedMilliseconds);
                    functionStopWatch.Reset();
                    #endregion

                    

                    //if (compilerVersion == 1)
                    //{
                        byte[] bytes = Convert.FromBase64String(htmlString);
                        string stringValue = System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                        var jsonsettings = new JsonSerializerSettings();
                        jsonsettings.TypeNameHandling = TypeNameHandling.Auto;
                        KitsunePage page = JsonConvert.DeserializeObject<KitsunePage>(stringValue, jsonsettings);
                        BlockLevelKLMExecutor blklmexecutor = new BlockLevelKLMExecutor(entity, functionLog, page, viewDetails, rootaliasurl, websiteId, schemeId, url, urlPattern, urlPatternRegex, websiteData, fptag, kitsuneRequestUrlType == KitsuneRequestUrlType.PRODUCTION, developerId);
                        functionStopWatch.Start();

                        klmResponse = blklmexecutor.Execute();
                        klmResponse.PerfLog = functionLog;
                        Helper.Helper.UpdateFunctionLog(functionLog, "New KLM Flow", functionStopWatch.ElapsedMilliseconds);
                        functionStopWatch.Reset();

				//To be reviewed
                        #region  CUSTOM SUPPORT FOR KAPP MODULES
                        try
                        {
                            string componentString = "";
                            if (components != null && components.Count > 0 && websiteData?._system?.components != null)
                            {
                                foreach (var component in components)
                                {
                                    switch (component.ProjectId)
                                    {
                                        //RIA App ID
                                        case "5ab5190ba35c3b04e9817cb5":
                                            {
                                                if (kitsuneRequestUrlType == KitsuneRequestUrlType.PRODUCTION && websiteData["components"]?["_" + component.SchemaId] != null)
                                                {
                                                    componentString += "<img src='http://www.google-analytics.com/collect?v=1&tid=UA-35051129-38&t=event&ec=" + websiteData["components"]["_" + component.SchemaId]["notif_type"] ?? "" + "&ea=open&el=" + websiteData["components"]["_" + component.SchemaId]["website_domain"] ?? "" + "&cs=newsletter&cm=email&cn=" + websiteData["components"]["_" + component.SchemaId]["project_id"] ?? "" + "&cm1=1&cd1=" + websiteData["components"]["_" + component.SchemaId]["recipient_email"] ?? "" + "&cid=" + websiteData["components"]["_" + component.SchemaId]["website_user_id"] ?? "" + "' style='z-index:-1; display: none; visibility: hidden; width:0px; height:0px;' />";
                                                }
                                                break;
                                            }
                                    }
                                }
                            }
                            componentString += "</body>";
                            klmResponse.HtmlCode = klmResponse.HtmlCode.Replace("</body>", componentString);
                        }
                        catch { }
                        #endregion
                    //}
                    //else
                    //{
                    //    var document = new HtmlDocument();
                    //    htmlString = WebUtility.HtmlDecode(htmlString);
                    //    document.LoadHtml(htmlString);

                    //    #region  CUSTOM SUPPORT FOR KAPP MODULES
                    //    try
                    //    {
                    //        if (components != null && components.Count > 0 && websiteData?._system?.components != null)
                    //        {
                    //            foreach (var component in components)
                    //            {
                    //                switch (component.ProjectId)
                    //                {
                    //                    //RIA App ID
                    //                    case "5ab5190ba35c3b04e9817cb5":
                    //                        {
                    //                            try
                    //                            {
                    //                                if (kitsuneRequestUrlType == KitsuneRequestUrlType.PRODUCTION && websiteData["components"]["_" + component.SchemaId] != null)
                    //                                {
                    //                                    var tempKappNode = HtmlNode.CreateNode("<img src=\"http://www.google-analytics.com/collect?v=1&tid=UA-35051129-38&t=event&ec=[[_system.components._" + component.SchemaId + ".notif_type]]&ea=open&el=[[_system.components._" + component.SchemaId + ".website_domain]]&cs=newsletter&cm=email&cn=[[_system.components._" + component.SchemaId + ".project_id]]&cm1=1&cd1=[[_system.components._" + component.SchemaId + ".recipient_email]]&cid=[[_system.components._" + component.SchemaId + ".website_user_id]]\" style=\"z-index:-1; display: none; visibility: hidden; width:0px; height:0px;\" />");

                    //                                    var tempBodyDocumentReference = document.DocumentNode.SelectSingleNode("//body");
                    //                                    tempBodyDocumentReference.AppendChild(tempKappNode);
                    //                                }
                    //                            }
                    //                            catch { }
                    //                            break;
                    //                        }
                    //                }
                    //            }
                    //        }
                    //        htmlString = document.DocumentNode.OuterHtml;
                    //    }
                    //    catch { }
                    //    #endregion

                    //    klmExecutor = new KLMExecutor(kitsuneRequestUrlType);
                    //    htmlString = klmExecutor.Execute(websiteId, entity, websiteData, viewDetails, queryString, document, functionLog, isDetailsView, isNFSite);

                    //    #region MINIFY HTML
                    //    functionStopWatch.Start();
                    //    try
                    //    {
                    //        //var minify = Uglify.Html(htmlString, new NUglify.Html.HtmlSettings() { DecodeEntityCharacters = false, KeepOneSpaceWhenCollapsing = true });
                    //        NUglify.Html.HtmlSettings settings = new NUglify.Html.HtmlSettings() { DecodeEntityCharacters = false, RemoveOptionalTags = false, ShortBooleanAttribute = false };
                    //        //settings.TagsWithNonCollapsableWhitespaces.Add("p", false);
                    //        var minify = Uglify.Html(htmlString, settings);
                    //        htmlString = minify.Code;
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        //TODO: Log Error while minifing html
                    //    }
                    //    finally
                    //    {
                    //        functionStopWatch.Stop();
                    //        Helper.Helper.UpdateFunctionLog(functionLog, Helper.Constant.MINIFICATION, functionStopWatch.ElapsedMilliseconds);
                    //        functionStopWatch.Reset();
                    //    }
                    //    klmResponse = new KLMResponseModel();
                    //    klmResponse.HtmlCode = htmlString;
                    //    klmResponse.CacheableResult = true;
                    //    klmResponse.PerfLog = functionLog;
                    //    #endregion
                    //}
					
                    Helper.Helper.UpdateFunctionLog(functionLog, "update custom component modules", functionStopWatch.ElapsedMilliseconds);
                    functionStopWatch.Reset();

                    #region UPDATE LOG

                    auditLog = new KLMAuditLogModel() { _id = ObjectId.GenerateNewId().ToString(), city = null, country = null, createdOn = DateTime.UtcNow, functionalLog = functionLog, fpTag = websiteName, ipAddress = ipAddress, themeId = projectId, loadTime = functionStopWatch.Elapsed.Seconds };
                    KinesisHelper.LogKLMRequestDetailsIntoKinesis(auditLog, url);

                    #endregion

                    klmResponse.HtmlCode = klmResponse.HtmlCode.Replace("[LOG_ID]", auditLog._id);
                    klmResponse.HtmlCode = klmResponse.HtmlCode.Replace("[KITSUNE_WEBSITE_ID]", websiteId);

                    if (perfLog != null && perfLog?.ToLower() == "true")
                    {
                        klmResponse.HtmlCode += "\nPerf extract:\n";
                        foreach (string key in functionLog.Keys)
                        {
                            klmResponse.HtmlCode += key + " : " + functionLog[key] + "\n";
                        }
                    }
                    return klmResponse;
                //}
            }
            catch (Exception ex)
            {
                throw;
                //return ex.Message + ex.StackTrace;
            }

            return null;
        }

        public static string GetKLMPreviewResponse(string themeId, string htmlString, string view, string viewType, string fpTag, string developerId, string[] urlParams = null, string noCacheQueryParam = null, bool isNFSite = false, string customerId = null)
        {
            try
            {
                var tempVariableForCache = (!string.IsNullOrEmpty(noCacheQueryParam)) ? (string.Compare(noCacheQueryParam, "true", true) == 0) : isKLMApiCacheEnabled;

                //  htmlString = MongoHelper.GetHtmlForViewFromTheme("590b3f09ee786c1d88879129", "INDEX.HTML");
                var entity = EntityHelper.GetEntityFromAPI("58d717e667962d6f40f5c198", tempVariableForCache);
                var businessClass = Kitsune.Language.Helper.Helper.GetClassFromJson(entity);

                #region FP DETAILS FROM CACHE
                var websiteDetails = CacheHelper.GetBusinessDetailsFromCache(fpTag, tempVariableForCache, view, true, businessClass, null, developerId);
                dynamic fpDetails = websiteDetails;
                var rootaliasurl = fpDetails.SelectToken("rootaliasurl.url") != null ? fpDetails.rootaliasurl?.url?.Value : null;
                var viewDetails = new Pagination();
                var queryString = string.Empty;
                #endregion

                if (fpDetails != null)
                {
                    var document = new HtmlDocument();
                    document.LoadHtml(htmlString);

                    var urlParameters = new List<string>().ToArray();
                    viewDetails = new Pagination()
                    {
                        currentpagenumber = "1",
                        nextpage = new Kitsune.Server.Model.Kitsune.Link() { url = "#" },
                        prevpage = new Kitsune.Server.Model.Kitsune.Link() { url = "#" },
                        totalpagescount = "1",
                        searchtext = "test"
                    };
                    bool isDetailsView = false;
                    klmExecutor = new KLMExecutor(KitsuneRequestUrlType.PREVIEW);
                    return klmExecutor.Execute(customerId, entity, fpDetails, viewDetails, queryString, document, null, isDetailsView, isNFSite);

                    //#endregion
                }
            }
            catch (Exception ex)
            {
                return ex.Message + ex.StackTrace;
            }

            return null;
        }

        public static async Task<KLMResponseModel> GetHtmlFromKlmV2Async(KitsuneV2KLMRequestModel request)
        {
            try
            {
                var functionLog = new Dictionary<string, long>();
                var functionStopWatch = new Stopwatch();

                functionStopWatch.Start();

                #region GET ENTITY INFO

                var EntityId = request.SchemaId;
                if (string.IsNullOrEmpty(EntityId))
                {
                    EntityId = "58d717e667962d6f40f5c198";
                }
                KEntity entity = await MongoHelper.GetLanguageEntityAsync(EntityId);

                if (entity == null)
                    return null;

                Helper.Helper.UpdateFunctionLog(functionLog, KitsuneLayoutManager.Helper.Constant.GETTING_ENTITY, functionStopWatch.ElapsedMilliseconds);
                functionStopWatch.Reset();
                
                var auditLog = new KLMAuditLogModel();
                #endregion

                Helper.Helper.UpdateFunctionLog(functionLog, KitsuneLayoutManager.Helper.Constant.GETTING_HTTP_HEADER_INFO, functionStopWatch.ElapsedMilliseconds);
                functionStopWatch.Reset();

                #region GET HTML FROM URL
                functionStopWatch.Start();

                if (string.IsNullOrEmpty(request.HostedFilePath))
                    return null;
                string htmlString = Helper.Helper.GetHtmlStringFromUrl(request.HostedFilePath);
                if (string.IsNullOrEmpty(htmlString))
                    return null;

                Helper.Helper.UpdateFunctionLog(functionLog, KitsuneLayoutManager.Helper.Constant.GET_HTML_FROM_URL, functionStopWatch.ElapsedMilliseconds);
                functionStopWatch.Reset();
                #endregion

                #region get KitsunePage
                byte[] bytes = Convert.FromBase64String(htmlString);
                string stringValue = System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                var jsonsettings = new JsonSerializerSettings();
                jsonsettings.TypeNameHandling = TypeNameHandling.Auto;
                KitsunePage page = JsonConvert.DeserializeObject<KitsunePage>(stringValue, jsonsettings);
                #endregion

                #region FP DETAILS FROM CACHE
                var components = MongoHelper.GetProjectComponents(request.ProjectId, request.ProjectVersion);

                string sourcePath = page.SourcePath;
                var projectResourceDetails = MongoHelper.GetUrlPatternDetails(request.ProjectId, request.ProjectVersion, sourcePath);
                string urlPattern = projectResourceDetails?.UrlPattern;
                string urlPatternRegex = projectResourceDetails?.UrlPatternRegex;

                functionStopWatch.Start();

                var view = string.Empty; var viewDetails = new Models.Pagination();
                bool isDetailsView = false; var urlParamList = new Dictionary<string, string>(); bool isSearchView = false;
                var queryString = string.Empty;

                // GET URL FROM API
                if (!string.IsNullOrEmpty(request.HostedFilePath) && !string.IsNullOrEmpty(request.ProjectId))
                {
                    isDetailsView = (request.PageType == Kitsune.Models.Project.KitsunePageType.DETAILS) ? true : false;
                    isSearchView = (request.PageType == Kitsune.Models.Project.KitsunePageType.SEARCH) ? true : false;
                    bool isDefaultView = (request.PageType == Kitsune.Models.Project.KitsunePageType.DEFAULT) ? true : false;

                    viewDetails = PaginationHelper.GetViewDetails(request.IncomingUrl.Trim('/'), urlPattern, request.RootPath, isDetailsView, isSearchView);
                    if (isDetailsView)
                    {
                        queryString = Helper.Helper.GetQueryStringForDL(urlPattern);
                    }
                }

                dynamic websiteData = (Newtonsoft.Json.Linq.JToken)JsonConvert.DeserializeObject("{ _system:{}, rootaliasurl:{} }");
                websiteData["_system"] = (Newtonsoft.Json.Linq.JToken)JsonConvert.DeserializeObject("{viewbag:{}}");
                websiteData["rootaliasurl"] = (Newtonsoft.Json.Linq.JToken)JsonConvert.DeserializeObject($"{{ url :  '{request.RootPath}'  }}");

                //Get Component
                #region Component Data
                if (components != null && components.Any())
                {
                    try
                    {
                        websiteData["_system"]["components"] = ApiHelper.GetComponentsData(components, request.ProjectId, request.WebsiteId, request.IncomingUrl, request.HostedFilePath, request.RootPath);
                    }
                    catch { }
                }
                #endregion

                Helper.Helper.UpdateFunctionLog(functionLog, KitsuneLayoutManager.Helper.Constant.GETTING_FP_DETAILS, functionStopWatch.ElapsedMilliseconds);
                functionStopWatch.Reset();
                #endregion

                if (!request.RootPath.StartsWith("http"))
                {
                    Uri requesturl = new Uri(request.IncomingUrl);
                    request.RootPath = requesturl.Scheme + "://" + request.RootPath;
                }

                BlockLevelKLMExecutor blklmexecutor = new BlockLevelKLMExecutor(entity, functionLog, page, viewDetails, request.RootPath, request.WebsiteId, request.SchemaId, request.IncomingUrl, urlPattern, urlPatternRegex, websiteData?._system, request.WebsiteTag, false, request.DeveloperId);
                functionStopWatch.Start();
                KLMResponseModel klmResponse = blklmexecutor.Execute();
                functionStopWatch.Stop();
                Helper.Helper.UpdateFunctionLog(functionLog, "New KLM Flow", functionStopWatch.ElapsedMilliseconds);
                functionStopWatch.Reset();

                #region  CUSTOM SUPPORT FOR KAPP MODULES
                try
                {
                    string componentString = "";
                    if (components != null && components.Count > 0 && websiteData?._system?.components != null)
                    {
                        foreach (var component in components)
                        {
                            switch (component.ProjectId)
                            {
                                //RIA App ID
                                case "5ab5190ba35c3b04e9817cb5":
                                    {
                                        if (websiteData["components"]?["_" + component.SchemaId] != null)
                                        {
                                            componentString += "<img src='http://www.google-analytics.com/collect?v=1&tid=UA-35051129-38&t=event&ec=" + websiteData["components"]["_" + component.SchemaId]["notif_type"] ?? "" + "&ea=open&el=" + websiteData["components"]["_" + component.SchemaId]["website_domain"] ?? "" + "&cs=newsletter&cm=email&cn=" + websiteData["components"]["_" + component.SchemaId]["project_id"] ?? "" + "&cm1=1&cd1=" + websiteData["components"]["_" + component.SchemaId]["recipient_email"] ?? "" + "&cid=" + websiteData["components"]["_" + component.SchemaId]["website_user_id"] ?? "" + "' style='z-index:-1; display: none; visibility: hidden; width:0px; height:0px;' />";
                                        }
                                        break;
                                    }
                            }
                        }
                    }
                    componentString += "</body>";
                    klmResponse.HtmlCode = klmResponse.HtmlCode.Replace("</body>", componentString);
                }
                catch { }
                #endregion

                functionStopWatch.Stop();
                Helper.Helper.UpdateFunctionLog(functionLog, "update custom component modules", functionStopWatch.ElapsedMilliseconds);
                functionStopWatch.Reset();

                #region UPDATE LOG
                auditLog = new KLMAuditLogModel() { _id = ObjectId.GenerateNewId().ToString(), city = null, country = null, createdOn = DateTime.UtcNow, functionalLog = functionLog, fpTag = request.WebsiteTag, ipAddress = request.ipAddress, themeId = request.ProjectId, loadTime = functionStopWatch.Elapsed.Seconds };
                KinesisHelper.LogKLMRequestDetailsIntoKinesis(auditLog, request.IncomingUrl);
                #endregion

                klmResponse.HtmlCode = klmResponse.HtmlCode.Replace("[LOG_ID]", auditLog._id);
                klmResponse.HtmlCode = klmResponse.HtmlCode.Replace("[KITSUNE_WEBSITE_ID]", request.WebsiteId);
                klmResponse.PerfLog = functionLog;
                return klmResponse;
            }
            catch (Exception ex)
            {
                throw;
            }

            return null;
        }

    }
}