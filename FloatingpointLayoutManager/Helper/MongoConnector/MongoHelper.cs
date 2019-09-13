using Kitsune.Language.Models;
using Kitsune.Models;
using Kitsune.Models.Project;
using Kitsune.Models.Theme;
using Kitsune.Models.WebsiteModels;
using KitsuneLayoutManager.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static KitsuneLayoutManager.Models.ExternalModels;
using Constants = Kitsune.Helper.Constants;

namespace KitsuneLayoutManager.Helper.MongoConnector
{
    public static partial class MongoHelper
    {
        static IMongoClient _server;
        static IMongoClient _kitsuneSchemaServer;
        static IMongoClient floatDBclient;
        static IMongoDatabase _floatDatabase;
        static IMongoDatabase _kitsuneDB;
        static IMongoDatabase _kitsuneSchemaDB;

        private static string serverUrl = KLM_Constants.KitsuneMongoServerUrl;
        private static string dbName = KLM_Constants.KitsuneDatabaseName;

        private static string kitsuneSchemaDBConnectionUrl = KLM_Constants.KitsuneSchemaMongoServerUrl;
        private static string schemaDBName = KLM_Constants.KitsuneSchemaDatabaseName;


        internal static void InitiateConnection()
        {
            try
            {
                if (_server == null)
                {
                    _server = new MongoClient(serverUrl);
                    _kitsuneDB = _server.GetDatabase(dbName);
                }

                if (_kitsuneSchemaServer == null)
                {
                    _kitsuneSchemaServer = new MongoClient(kitsuneSchemaDBConnectionUrl);
                    _kitsuneSchemaDB = _kitsuneSchemaServer.GetDatabase(schemaDBName);
                }
            }
            catch (Exception ex)
            {
                var tmp = ex.ToString();
            }
        }

        #region Mongo helper functions (Common functions)

        internal static ProjectionDefinition<T> GenerateMongoProjection<T>(string[] includeFields, string[] excludeFields)
        {
            try
            {
                if (includeFields != null)
                {
                    var projectionBuilder = Builders<T>.Projection.Include(includeFields.First());
                    foreach (var field in includeFields.Skip(1))
                        projectionBuilder = projectionBuilder.Include(field);

                    if (excludeFields != null)
                    {
                        foreach (var field in excludeFields)
                            projectionBuilder = projectionBuilder.Exclude(field);
                    }

                    return projectionBuilder;
                }
            }
            catch { }
            return null;
        }

        internal static List<T> CommonKitsuneDBGetQueryDetailsForAnyCollection<T>(string collectionName, FilterDefinition<T> filter, string[] includeFields = null, string[] excludeFields = null, int limit = 10, int skipCount = 0, SortDefinition<T> sortDefinition = null)
        {
            try
            {
                if (_server == null)
                    InitiateConnection();

                var collection = _kitsuneDB.GetCollection<T>(collectionName);
                var project = GenerateMongoProjection<T>(includeFields, excludeFields);

                var documents = collection.Find(filter).Project<T>(project).Skip(skipCount).Limit(limit).Sort(sortDefinition).ToList();

                if (documents.Any())
                    return documents;
            }
            catch (Exception ex)
            {
                ConsoleLogger.Write("Exception: unable to CommonKitsuneDBGetQueryDetailsForAnyCollection" + ex.ToString());
            }
            return null;
        }

        #endregion

        #region KLM HELPERS

        internal static async Task<List<ProductionThemeModel>> GetAllThemesAsync(string category)
        {
            try
            {
                if (_server == null)
                    InitiateConnection();

                var themeCollection = _kitsuneDB.GetCollection<ProductionThemeModel>(Constants.ProductionThemeCollection);
                var productDefinitionBuilder = new ProjectionDefinitionBuilder<ProductionThemeModel>();
                var pd = productDefinitionBuilder.Include(x => x._id).Include(x => x.IsMobileResponsive).Include(x => x.ThemeId).Include(x => x.ThemeCode).Include(x => x.Category);
                List<ProductionThemeModel> result;
                if (category != null)
                    result = await themeCollection.Find(x => x.Category != null && x.Category.Contains(category), new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).Project<ProductionThemeModel>(pd).ToListAsync();
                else
                    result = await themeCollection.Find(x => x.Category == null, new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).Project<ProductionThemeModel>(pd).ToListAsync();

                if (result == null || !result.Any())
                    result = await themeCollection.Find(x => x.Category == null, new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).Project<ProductionThemeModel>(pd).ToListAsync();

                if (result != null && result.Any())
                {
                    var finalResult = result.ToList();
                    if (!String.IsNullOrEmpty(category))
                    {
                        var themeList = finalResult.Where(s => s.Category != null && s.Category.Contains(category)).ToList();
                        if (themeList != null && themeList.Any())
                        {
                            return themeList;
                        }
                    }
                    var themesList = finalResult.Where(s => s.Category == null).ToList();
                    if (themesList != null && themesList.Any())
                    {
                        return themesList;
                    }

                }
            }
            catch (Exception ex)
            {

            }
            return null;
        }

        internal static string GetViewNameFromDB(string themeId, string absoluteUri, string rootaliasurl, string urlParams = null)
        {
            try
            {
                if (_server == null)
                    InitiateConnection();

                var queryUrl = absoluteUri.Trim('/').Replace(rootaliasurl, "[[Business.rootaliasurl.url]]");
                var themeCollection = _kitsuneDB.GetCollection<ProductionPageModel>(Constants.ProductionPageCollection);
                var productDefinitionBuilder = new ProjectionDefinitionBuilder<ProductionPageModel>();
                var pd = productDefinitionBuilder.Include(x => x._id).Include(x => x.UrlPattern).Include(x => x.PageName).Include(x => x.IsDefault).Include(x => x.UrlPatternRegex).Include(x => x.IsStatic);
                var result = themeCollection.Find(x => x.ThemeId == themeId, new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).Project<ProductionPageModel>(pd).ToList();
                if (result != null)
                {
                    if (queryUrl.Equals("[[Business.rootaliasurl.url]]"))
                    {
                        var model = result.Where(s => s.UrlPattern.ToLower().Equals(queryUrl.ToLower())).FirstOrDefault();
                        if (model != null)
                        {
                            return model.PageName;
                        }
                        else
                        {
                            model = result.Where(s => s.IsDefault == true).FirstOrDefault();
                            if (model != null)
                            {
                                return model.PageName;
                            }
                        }
                    }
                    else
                    {

                        var model = result.Where(s => s.IsStatic == true && s.UrlPattern.ToLower().Equals(queryUrl.ToLower())).FirstOrDefault();
                        if (model != null)
                        {
                            return model.PageName;
                        }
                        else
                        {
                            queryUrl = queryUrl.Replace("[[Business.rootaliasurl.url]]/", "");
                            //   Match m = Regex.Match(input, @"/([a-z0-9\-]+)/b([0-9\-]+)");
                            var pageModel = result.Where(s => !string.IsNullOrEmpty(GetRegexMatchedValue(queryUrl, s.UrlPatternRegex)) && queryUrl.ToLower().Equals(GetRegexMatchedValue(queryUrl, s.UrlPatternRegex))).ToList();
                            if (pageModel != null)
                            {
                                return pageModel.FirstOrDefault().PageName;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return null;
        }

        internal static ProductionPageModel GetViewFromDB(string themeId, string absoluteUri, string rootaliasurl, string urlParams = null)
        {
            try
            {
                if (_server == null)
                    InitiateConnection();

                var queryUrl = absoluteUri.Trim('/').Replace(rootaliasurl.Trim('/'), "[[Business.rootaliasurl.url]]");
                var themeCollection = _kitsuneDB.GetCollection<ProductionPageModel>(Constants.ProductionPageCollection);
                var productDefinitionBuilder = new ProjectionDefinitionBuilder<ProductionPageModel>();
                var pd = productDefinitionBuilder.Include(x => x._id).Include(x => x.UrlPattern).Include(x => x.PageName).Include(x => x.IsDefault).Include(x => x.UrlPatternRegex).Include(x => x.IsStatic).Include(s => s.PageType);
                var result = themeCollection.Find(x => x.ThemeId == themeId, new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).Project<ProductionPageModel>(pd).ToList();
                if (result != null)
                {
                    if (queryUrl.Equals("[[Business.rootaliasurl.url]]"))
                    {
                        var model = result.Where(s => s.UrlPattern.ToLower().Equals(queryUrl.ToLower())).FirstOrDefault();
                        if (model != null)
                        {
                            return model;
                        }
                        else
                        {
                            model = result.Where(s => s.IsDefault == true).FirstOrDefault();
                            if (model != null)
                            {
                                return model;
                            }
                        }
                    }
                    else
                    {

                        var model = result.Where(s => s.IsStatic == true && s.UrlPattern.ToLower().Equals(queryUrl.ToLower())).FirstOrDefault();
                        if (model != null)
                        {
                            return model;
                        }
                        else
                        {
                            queryUrl = queryUrl.Replace("[[Business.rootaliasurl.url]]/", "");
                            //   Match m = Regex.Match(input, @"/([a-z0-9\-]+)/b([0-9\-]+)");
                            var pageModel = result.Where(s => !string.IsNullOrEmpty(GetRegexMatchedValue(queryUrl, s.UrlPatternRegex)) && queryUrl.ToLower().Replace(" ", "").Equals(GetRegexMatchedValue(queryUrl, s.UrlPatternRegex))).ToList();
                            if (pageModel != null && pageModel.Count > 0)
                            {
                                return pageModel.FirstOrDefault();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return null;
        }

        internal static bool DoesProjectExists(string themeId)
        {
            try
            {
                if (_server == null)
                    InitiateConnection();

                var themeCollection = _kitsuneDB.GetCollection<ProductionThemeModel>("ProductionThemes");
                return (themeCollection.Count(x => x.ThemeId == themeId) > 0);
            }
            catch (Exception ex) { throw ex; }
            return false;
        }

        internal static ProductionPageModel GetErrorViewFromDB(string themeId)
        {
            try
            {
                var themeCollection = _kitsuneDB.GetCollection<ProductionPageModel>(Constants.ProductionPageCollection);
                var result = themeCollection.Find(x => x.ThemeId == themeId && x.ClassName == "404", new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).ToList();
                if (result != null)
                {
                    return result.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {

            }

            return null;
        }

        private static string GetRegexMatchedValue(string input, string regex)
        {
            try
            {
                if (!String.IsNullOrEmpty(regex) && !String.IsNullOrEmpty(input))
                {
                    input = input.Replace(" ", "");

                    var match = Regex.Match(input, regex);
                    if (match.Success)
                        return match.Value;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return null;
        }

        internal static void saveErrorLog(string fpTag, string v1, object p, string v2)
        {
            throw new NotImplementedException();
        }

        internal static string GetThemeIdForUser(string ipAddress, string tag)
        {
            try
            {
                if (_server == null)
                    InitiateConnection();

                var auditLogCollection = _kitsuneDB.GetCollection<KLMAuditLogModel>(Constants.KLMAuditLogCollection);
                var cursor = auditLogCollection.Find(x => x.fpTag == tag.ToUpper() && x.ipAddress == ipAddress && x.createdOn >= DateTime.Now.AddDays(-30), new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) });
                var productDefinitionBuilder = new ProjectionDefinitionBuilder<KLMAuditLogModel>();
                var pd = productDefinitionBuilder.Include(x => x._id).Include(x => x.themeId).Include(x => x.createdOn);
                var sortDefinitionBuilder = new SortDefinitionBuilder<KLMAuditLogModel>();
                var sortDefinition = sortDefinitionBuilder.Descending(x => x.createdOn);
                var result = cursor.Project<KLMAuditLogModel>(pd).Limit(1).Sort(sortDefinition).FirstOrDefault();
                if (result != null)
                    return result.themeId;

            }
            catch (Exception ex)
            {
                // ExceptionHelper.SendNotification(ex.ToString(), "MongoHelper - GetThemeIdForUser:" + tag);
            }
            return null;
        }

        internal static ProductionThemeModel GetThemeDetails(string themeId)
        {
            try
            {
                if (_server == null)
                    InitiateConnection();

                var themeCollection = _kitsuneDB.GetCollection<ProductionThemeModel>(Constants.ProductionThemeCollection);

                var productDefinitionBuilder = new ProjectionDefinitionBuilder<ProductionThemeModel>();
                var pd = productDefinitionBuilder.Include(x => x._id).Include(x => x.IsMobileResponsive).Include(x => x.ThemeCode).Include(x => x.ThemeId);
                var result = themeCollection.Find(x => x.ThemeId == themeId, new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).Project<ProductionThemeModel>(pd).Limit(1).FirstOrDefault();

                if (result != null)
                    return result;
            }
            catch (Exception ex)
            {

            }
            return null;
        }

        internal static KLMAuditLogModel saveLog(string fpTag, ulong fpcode, string IPAddress, string city, string country, bool IsCrawler, bool ignoreThemePicking)
        {
            try
            {
                if (_server == null)
                    InitiateConnection();


                var auditLogCollection = _kitsuneDB.GetCollection<KLMAuditLogModel>(Constants.KLMAuditLogCollection);
                //var getExisting = auditLogCollection.Find(x => x.ipAddress == IPAddress).FirstOrDefault();

                var model = new KLMAuditLogModel()
                {
                    _id = ObjectId.GenerateNewId().ToString(),
                    createdOn = DateTime.Now,
                    fpCode = fpcode,
                    ipAddress = IPAddress,
                    isCrawler = IsCrawler,
                    userAgent = "",
                    ignoreInThemeSelection = ignoreThemePicking,
                    fpTag = fpTag.ToUpper(),
                    city = city,
                    country = country
                };

                auditLogCollection.InsertOne(model);

                return model;
            }
            catch (Exception ex)
            {
                var exx = ex.ToString();
            }

            return null;
        }

        internal static void UpdateCrawlerName(string id, string name)
        {
            try
            {
                if (_server == null)
                    InitiateConnection();

                var auditLogCollection = _kitsuneDB.GetCollection<KLMAuditLogModel>(Constants.KLMAuditLogCollection);
                UpdateDefinitionBuilder<KLMAuditLogModel> updateDefinitionBuilder = new UpdateDefinitionBuilder<KLMAuditLogModel>();
                auditLogCollection.UpdateOne<KLMAuditLogModel>((x => x._id == id), updateDefinitionBuilder.Set(x => x.userAgent, name));
            }
            catch (Exception ex)
            {

            }
        }

        public static string GetHtmlForViewFromTheme(string themeId, string view)
        {
            try
            {
                if (!string.IsNullOrEmpty(themeId) && !string.IsNullOrEmpty(view))
                {
                    if (_server == null)
                        InitiateConnection();

                    var themeCollection = _kitsuneDB.GetCollection<ProductionPageModel>(Constants.ProductionPageCollection);
                    var productDefinitionBuilder = new ProjectionDefinitionBuilder<ProductionPageModel>();
                    var pd = productDefinitionBuilder.Include(x => x._id).Include(x => x.HtmlCompiledString);
                    var result = themeCollection.Find(x => x.ThemeId == themeId && x.PageName == view,
                                                        new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) })
                                                        .Project<ProductionPageModel>(pd).Limit(1).FirstOrDefault();

                    if (result != null)
                        return result.HtmlCompiledString;
                }
            }
            catch (Exception ex)
            {

            }
            return null;
        }

        internal static void UpdateThemeDetails(string logId, string themeId = null, ulong themeCode = 0)
        {
            Task.Run(() =>
            {
                try
                {
                    if (_server == null)
                        InitiateConnection();

                    var auditLogCollection = _kitsuneDB.GetCollection<KLMAuditLogModel>(Constants.KLMAuditLogCollection);

                    if (themeCode != 0)
                    {
                        auditLogCollection.UpdateOne((x => x._id == logId), new UpdateDefinitionBuilder<KLMAuditLogModel>().Set(x => x.themeCode, themeCode));
                    }

                    if (!String.IsNullOrEmpty(themeId))
                    {
                        auditLogCollection.UpdateOne((x => x._id == logId), new UpdateDefinitionBuilder<KLMAuditLogModel>().Set(x => x.themeId, themeId));
                    }

                }
                catch (Exception ex)
                {
                    //Send mail
                }
            });
        }

        internal static void UpdateLoadTime(string auditId, long loadTime, Dictionary<string, long> functionLog)
        {
            try
            {
                if (_server == null)
                    InitiateConnection();

                var auditLogCollection = _kitsuneDB.GetCollection<KLMAuditLogModel>(Constants.KLMAuditLogCollection);
                var jsonDoc = Newtonsoft.Json.JsonConvert.SerializeObject(functionLog);
                var bsonDoc = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(jsonDoc);
                var result = auditLogCollection.UpdateOne((x => x._id == auditId), new UpdateDefinitionBuilder<KLMAuditLogModel>().Set(x => x.loadTime, loadTime).Set(x => x.functionalLog, functionLog));
            }
            catch (Exception ex)
            {
                //Send mail
            }
        }

        internal static List<ThemePerformanceModel> GetThemePerformanceStats(IEnumerable<string> themeIds, string fpId, string visitorCity, string categoryId, string visitorCountry)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fpId))
                    return new List<ThemePerformanceModel>();
                if (_kitsuneDB == null)
                    InitiateConnection();
                var visitorColl = _kitsuneDB.GetCollection<MongoVisitorData>(Constants.PiwikVisitorsData);
                var filter = Builders<MongoVisitorData>.Filter;
                var project = Builders<MongoVisitorData>.Projection.Include("actions").Include("visitDuration").Include("isBounced").Include("themeid");
                List<MongoVisitorData> perf = null;
                if (!string.IsNullOrEmpty(visitorCity))
                {
                    perf = visitorColl.Find(filter.Eq<string>(x => x.fpid, fpId)
                        & filter.Eq<string>(x => x.city, visitorCity)
                        & filter.In<string>(x => x.themeid, themeIds),
                        new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).Project<MongoVisitorData>(project).ToList();

                    if (perf == null || perf.Count < 100)
                    {
                        perf = visitorColl.Find(filter.Eq<string>(x => x.categoryid, categoryId)
                            & filter.Eq<string>(x => x.city, visitorCity)
                            & filter.In<string>(x => x.themeid, themeIds),
                            new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).Project<MongoVisitorData>(project).ToList();
                    }
                }
                if ((perf == null || perf.Count < 100) && !string.IsNullOrEmpty(visitorCountry))
                {
                    perf = visitorColl.Find(filter.Eq<string>(x => x.fpid, fpId)
                        & filter.Eq<string>(x => x.country, visitorCountry)
                        & filter.In<string>(x => x.themeid, themeIds),
                        new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).Project<MongoVisitorData>(project).ToList();

                    if (perf == null || perf.Count < 100)
                        perf = visitorColl.Find(filter.Eq<string>(x => x.categoryid, categoryId)
                            & filter.Eq<string>(x => x.country, visitorCountry)
                            & filter.In<string>(x => x.themeid, themeIds),
                            new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).Project<MongoVisitorData>(project).ToList();
                }

                if (perf == null || perf.Count < 100)
                    return new List<ThemePerformanceModel>();

                var grp = perf.GroupBy(x => x.themeid);
                var themeFilter = grp.Select(x => new ThemePerformanceModel
                {
                    BounceRate = x.Count(y => !y.isBounced) / x.Count(),
                    TotalActions = x.Sum(y => y.actions),
                    ThemeId = x.Key,
                    TotalTimeSpent = x.Sum(y => y.visitDuration)
                });
                return themeFilter.ToList();
            }
            catch (Exception ex)
            {

            }

            return new List<ThemePerformanceModel>();
        }

        #endregion

        #region DYNAMIC SITE 

        internal static ProductionKitsuneResource GetResourceForS3Url(string url, string projectId)
        {
            try
            {
                if (_server == null)
                    InitiateConnection();

                var projectsCollection = _kitsuneDB.GetCollection<ProductionKitsuneResource>("new_KitsuneResourcesProduction");
                return projectsCollection.Find(x => x.OptimizedPath == url && x.ProjectId == projectId,
                    new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).FirstOrDefault();
            }
            catch (Exception ex)
            {
                ConsoleLogger.Write($"ERROR: GetResourceForS3Url with exception {ex.ToString()}");
            }

            return null;
        }

        internal static ProductionKitsuneResource GetResourceForUrl(string url, string projectid, string domainUrl, bool isDefaultView = false)
        {
            try
            {
                if (_server == null)
                    InitiateConnection();

                var projectsCollection = _kitsuneDB.GetCollection<ProductionKitsuneResource>("new_KitsuneResourcesProduction");
                if (isDefaultView)
                {
                    return projectsCollection.Find(x => x.ProjectId == projectid && x.IsDefault == true,
                        new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).ToList().FirstOrDefault();
                }
                else
                {
                    domainUrl = domainUrl.ToLower();
                    url = url.ToLower();

                    //url.Replace(domainUrl, "")
                    var relativeUrl = new Uri(url).AbsolutePath.Trim('/');
                    var matchedResources = new List<ProductionKitsuneResource>();

                    #region Check if regex pattern is matching for Dynamic resources and return the resource
                    var allDynamicResources = projectsCollection.Find(x => x.ProjectId == projectid && x.IsStatic == false,
                        new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).ToList();

                    foreach (var resource in allDynamicResources)
                    {
                        var regexComparisionValue = GetRegexMatchedValue(relativeUrl, resource.UrlPatternRegex);
                        if (!String.IsNullOrEmpty(regexComparisionValue) && String.Compare(relativeUrl, regexComparisionValue) == 0)
                            return resource;
                    }
                    #endregion

                    #region Check if regex pattern is matching for static resources and return the resource
                    var allstaticResources = projectsCollection.Find(x => x.ProjectId == projectid && x.IsStatic == true,
                        new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).ToList();

                    foreach (var resource in allstaticResources)
                    {
                        var regexComparisionValue = GetRegexMatchedValue(relativeUrl, resource.UrlPatternRegex);
                        if (!String.IsNullOrEmpty(regexComparisionValue) && String.Compare(relativeUrl, regexComparisionValue) == 0)
                            return resource;
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return null;
        }

        internal static KitsuneWebsiteCollection GetCustomerDetailsFromDomain(string domainName)
        {
            try
            {
                if (_server == null)
                    InitiateConnection();

                domainName = domainName.ToUpper();

                var customerCollection = _kitsuneDB.GetCollection<KitsuneWebsiteCollection>("KitsuneWebsites");
                var cursor = customerCollection.Find(x => x.WebsiteUrl == domainName && x.IsActive == true,
                    new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).Limit(1).ToList();

                if (cursor.Count() > 0)
                    return cursor.FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return null;
        }

        internal static ProductionKitsuneProject GetKitsuneProductionProjectDetails(string projectId)
        {
            try
            {
                var projection = new[] { "BucketNames", "SchemaId", "Version", "Modules" };
                var filter = Builders<ProductionKitsuneProject>.Filter.Eq(document => document.ProjectId, projectId);

                var collectionName = KLM_Constants.KitsuneProjectProductionCollection;

                var result = CommonKitsuneDBGetQueryDetailsForAnyCollection<ProductionKitsuneProject>(collectionName, filter, projection, null, 1).FirstOrDefault();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region Helpers used by RIA

        public static WebsiteDetails GetWebsiteDetailsById(string websiteId)
        {
            try
            {
                if (_server == null)
                    InitiateConnection();

                var websiteCollection = _kitsuneDB.GetCollection<KitsuneWebsiteCollection>(KLM_Constants.KitsuneWebsiteCollection);
                var websiteUserCollection = _kitsuneDB.GetCollection<KitsuneWebsiteUserCollection>(KLM_Constants.KitsuneWebsiteUserCollection);

                var website = websiteCollection.Find(x => x._id == websiteId,
                    new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).FirstOrDefault();

                var user = websiteUserCollection.Find(x => x.WebsiteId == websiteId && x.AccessType == KitsuneWebsiteAccessType.Owner,
                    new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).FirstOrDefault();

                return new WebsiteDetails
                {
                    CreatedOn = website.CreatedOn,
                    WebsiteUrl = website.WebsiteUrl,
                    WebsiteTag = website.WebsiteTag,
                    IsActive = website.IsActive,
                    DeveloperId = website.DeveloperId,
                    ProjectId = website.ProjectId,
                    WebsiteId = website._id,
                    PublishedOn = website.CreatedOn,
                    UpdatedOn = website.UpdatedOn,
                    WebsiteOwner = new WebsiteUserDetails
                    {
                        AccessType = user.AccessType.ToString(),
                        Contact = user.Contact,
                        CreatedOn = user.CreatedOn.Date,
                        LastLoginTimeStamp = user.LastLoginTimeStamp,
                        UpdatedOn = user.UpdatedOn,
                        UserId = user._id,
                        UserName = user.UserName
                    }
                };
            }
            catch (Exception ex)
            {
                //TODO: Log Exceptions
            }
            return null;
        }

        #endregion

        #region collection keys
        private const string COLLECTION_KEY_ID = "_id";
        private const string COLLECTION_KEY_CREATED_ON = "createdon";
        private const string COLLECTION_KEY_UPDATED_ON = "updatedon";
        private const string COLLECTION_KEY_PARENT_CLASS_ID = "_parentClassId";
        private const string COLLECTION_KEY_PARENT_CLASS_NAME = "_parentClassName";
        private const string COLLECTION_KEY_PROPERTY_NAME = "_propertyName";
        private const string COLLECTION_KEY_KID = "_kid";
        private const string COLLECTION_KEY_WEBISTE_ID = "websiteid";
        private const string COLLECTION_KEY_IS_ARCHIVED = "isarchived";
        private const string COLLECTION_KEY_REFLECTION_ID = "_reflectionId";
        public static string KitsuneLanguageCollectionName = "KitsuneLanguages";
        private const string FUNCTION_NAME_LENGTH = "length";

        #endregion

        public static string GenerateSchemaName(string _schemaName)
        {
            try
            {
                return "k_" + _schemaName.ToLower().Trim().Replace(" ", "").Replace("-", "_");
            }
            catch
            {
                return null;
            }
        }

        private static dynamic ExtractDataFromBsonValue(BsonValue bsonValue)
        {
            dynamic data = null;
            switch (bsonValue.BsonType)
            {
                case BsonType.Array:
                    {
                        var arrayResult = new List<dynamic>();
                        foreach (var propRes in bsonValue.AsBsonArray)
                        {
                            arrayResult.Add(BsonSerializer.Deserialize<dynamic>((BsonDocument)propRes));
                        }
                        data = arrayResult;
                    }; break;
                case BsonType.Boolean: { data = bsonValue.AsBoolean; } break;
                case BsonType.Int32: case BsonType.Int64: case BsonType.Double: { data = bsonValue.AsDouble; } break;
                case BsonType.DateTime: { data = bsonValue.ToNullableUniversalTime(); } break;
                case BsonType.String: { data = bsonValue.AsString; } break;
            }
            return data;
        }

        private static long GetObjectArrayLength(KEntity entity, string baseCollectionName, string kClassName, string propertyName, string parentId, bool isCustomDatatype)
        {
            if (_kitsuneSchemaServer == null || _kitsuneSchemaDB == null)
            {
                InitiateConnection();
            }
            var kClass = entity.Classes.FirstOrDefault(x => x.Name.ToLower() == kClassName.ToLower());

            long documentsCount = 0;
            if (kClass != null)
            {
                var queryDoc = new BsonDocument();
                if (!isCustomDatatype)
                {
                    queryDoc.Add(COLLECTION_KEY_KID, parentId);
                    var projectDoc = new BsonDocument();
                    projectDoc.Add(propertyName, 1);
                    var collectionName = kClass.ClassType == KClassType.BaseClass ? baseCollectionName : $"{baseCollectionName}_{kClassName}";
                    var propertyValue = _kitsuneSchemaDB.GetCollection<BsonDocument>(collectionName).Find(queryDoc, new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).Project(projectDoc).FirstOrDefault();

                    if (propertyValue != null && propertyValue.Contains(propertyName) && propertyValue[propertyName].GetType() == typeof(BsonArray))
                        documentsCount = ((BsonArray)propertyValue[propertyName]).Count;
                    else
                        documentsCount = 0;
                }
                //if the base class then it will be always one document per website
                else if (kClass.ClassType == KClassType.BaseClass)
                    documentsCount = 1;
                else
                {
                    var filterBuilder = Builders<BsonDocument>.Filter;
                    var filter = filterBuilder.Eq(COLLECTION_KEY_PARENT_CLASS_ID, parentId) &
                        filterBuilder.Eq(COLLECTION_KEY_PROPERTY_NAME, propertyName) &
                        filterBuilder.Eq(COLLECTION_KEY_IS_ARCHIVED, false);

                    //queryDoc.Add(COLLECTION_KEY_PARENT_CLASS_ID, parentId);
                    //queryDoc.Add(COLLECTION_KEY_PROPERTY_NAME, propertyName);
                    //queryDoc.Add(COLLECTION_KEY_IS_ARCHIVED, false);
                    documentsCount = _kitsuneSchemaDB.GetCollection<BsonDocument>($"{baseCollectionName}_{kClassName}").CountDocuments(filter, new CountOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) });
                }
            }
            return documentsCount;
        }

        /// <summary>
        /// Get langauge schema by languageid and version (optional)
        /// </summary>
        /// <param name="requestModel"></param>
        /// <returns></returns>
        internal static async Task<KEntity> GetLanguageEntityAsync(string languageId)
        {
            if (string.IsNullOrEmpty(languageId))
            {
                throw new ArgumentNullException(nameof(languageId));
            }

            try
            {
                if (_kitsuneSchemaServer == null)
                    InitiateConnection();

                var LanguageCollection = _kitsuneSchemaDB.GetCollection<KLanguageModel>(KitsuneLanguageCollectionName);
                if (!string.IsNullOrEmpty(languageId))
                {
                    var filterDefinition = new FilterDefinitionBuilder<KLanguageModel>();
                    var fd = filterDefinition.Eq(q => q._id, languageId);

                    //To support clientid and userid both
                    //if (!string.IsNullOrEmpty(requestModel.UserId))
                    //    fd = filterDefinition.And(filterDefinition.Eq(q => q._id, requestModel.EntityId), filterDefinition.Eq(q => q.UserId, requestModel.UserId));

                    var result = await LanguageCollection.Find(fd, new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).FirstOrDefaultAsync();

                    if (result?.Entity != null)
                    {
                        return result?.Entity;
                    }

                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return null;
        }

        internal static async Task<GetWebsiteDataByPropertyResponseModel> GetWebsiteDataByPropertyPathAsync(List<PropertyPathSegment> PropertySegments,
            string SchemaId, string WebsiteId, KEntity entity, Dictionary<string, string> collectionKIDMap, int kobjectindex)
        {
            try
            {
                if (_kitsuneSchemaServer == null || _kitsuneSchemaDB == null)
                {
                    InitiateConnection();
                }
                if (entity == null)
                {
                    throw new Exception($"Language not found with id \"{SchemaId}\".");
                }


                //Get first parent _kid

                var baseCollectionName = GenerateSchemaName(entity.EntityName);
                BsonDocument baseObject;
                string collectionKIDMapKey = GetcollectionKIDMapKey(baseCollectionName, null, null, null);
                string parentId;
                if (collectionKIDMap.ContainsKey(collectionKIDMapKey))
                {
                    parentId = collectionKIDMap[collectionKIDMapKey];
                }
                else
                {
                    var filterDoc = new BsonDocument();
                    filterDoc.Add(COLLECTION_KEY_IS_ARCHIVED, false);
                    filterDoc.Add(COLLECTION_KEY_WEBISTE_ID, WebsiteId);
                    var projectDoc = new BsonDocument();
                    projectDoc.Add(COLLECTION_KEY_KID, 1);

                    baseObject = await _kitsuneSchemaDB.GetCollection<BsonDocument>(baseCollectionName).Find(filterDoc,
                        new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).Project(projectDoc).FirstOrDefaultAsync();

                    if (baseObject != null)
                        collectionKIDMap.Add(collectionKIDMapKey, baseObject[COLLECTION_KEY_KID].AsString);

                    if (baseObject == null)
                        throw new Exception($"No data found for website : \"{WebsiteId}\"");

                    parentId = baseObject[COLLECTION_KEY_KID].AsString;
                }

                bool returnLength = false;
                //foreach property in propertysegments
                if (PropertySegments.Count > 2 && PropertySegments[PropertySegments.Count - 1].Type == PropertyType.function && PropertySegments[PropertySegments.Count - 2].Type == PropertyType.str)
                {
                    PropertySegments.RemoveAt(PropertySegments.Count - 1);
                    returnLength = true;
                }

                //foreach property in propertysegments
                var end = PropertySegments.Count - 1;
                var start = kobjectindex != 0 ? kobjectindex - 1 : 1;
                PropertyPathSegment previousProp = null;
                PropertyPathSegment currentProp = null;
                PropertyPathSegment nextProp = null;
                int? startIndex = null;
                int? limit = null;

                GetWebsiteDataByPropertyResponseModel responseModel = new GetWebsiteDataByPropertyResponseModel() { };
                var dataTypeObjects = new string[] { "str", "number", "datetime", "boolean" };

                for (var i = start; i <= end; i++)
                {
                    bool isKobject = kobjectindex > 0 && i == kobjectindex - 1;

                    currentProp = PropertySegments[i];
                    nextProp = i < end ? PropertySegments[i + 1] : null;
                    previousProp = PropertySegments[i - 1];
                    startIndex = currentProp.Index;
                    limit = currentProp.Index == null ? null : (int?)1;
                    if (currentProp.Type == PropertyType.obj || currentProp.Type == PropertyType.kstring || currentProp.Type == PropertyType.phonenumber)
                    {
                        if (i < end)
                        {
                            if ((nextProp.Type == PropertyType.array && nextProp.Type == PropertyType.obj) || nextProp.Type == PropertyType.obj)
                            {
                                string collectionName = baseCollectionName + "_" + currentProp.PropertyDataType;
                                collectionKIDMapKey = GetcollectionKIDMapKey(collectionName, parentId, currentProp.PropertyName, currentProp.Index.ToString());
                                if (collectionKIDMap.ContainsKey(collectionKIDMapKey))
                                {
                                    parentId = collectionKIDMap[collectionKIDMapKey];
                                }
                                else
                                {
                                    List<BsonDocument> kid = await GetObjectAsync(isKobject, entity, currentProp.PropertyDataType, previousProp.PropertyDataType, currentProp.PropertyName, baseCollectionName, parentId, new List<string> { COLLECTION_KEY_KID }, currentProp.Sort, currentProp.Filter, startIndex, limit);
                                    if (kid != null && kid.Any() && kid[0].Contains(COLLECTION_KEY_KID))
                                    {
                                        parentId = kid.First()[COLLECTION_KEY_KID].AsString;
                                        collectionKIDMap.Add(collectionKIDMapKey, parentId);
                                    }
                                }
                            }
                            else if (i == (end - 1) && (nextProp.Type != PropertyType.array || (nextProp.Type == PropertyType.array && dataTypeObjects.Contains(nextProp.PropertyDataType)))
                                && nextProp.Type != PropertyType.obj
                                && nextProp.Type != PropertyType.phonenumber
                                && nextProp.Type != PropertyType.kstring
                                && nextProp.Type != PropertyType.function)//check if its second last property
                            {
                                List<BsonDocument> propertyResult;
                                string collectionName = baseCollectionName + "_" + currentProp.PropertyDataType;
                                collectionKIDMapKey = GetcollectionKIDMapKey(collectionName, parentId, currentProp.PropertyName, currentProp.Index.ToString());
                                if (collectionKIDMap.ContainsKey(collectionKIDMapKey))
                                {
                                    string kid = collectionKIDMap[collectionKIDMapKey];
                                    if (currentProp.Filter == null)
                                    {
                                        currentProp.Filter = new Dictionary<string, object>();
                                    }
                                    //currentProp.Filter.Add(COLLECTION_KEY_KID, kid);
                                    currentProp.Filter.Add(COLLECTION_KEY_ID, ObjectId.Parse(kid));
                                    //currentProp.Filter.Add(COLLECTION_KEY_WEBISTE_ID, WebsiteId);

                                    propertyResult = await GetObjectAsync(isKobject, entity, currentProp.PropertyDataType, previousProp.PropertyDataType, currentProp.PropertyName, baseCollectionName, parentId, new List<string> { nextProp.PropertyName }, currentProp.Sort, currentProp.Filter, null, null);
                                }
                                else
                                {
                                    propertyResult = await GetObjectAsync(isKobject, entity, currentProp.PropertyDataType, previousProp.PropertyDataType, currentProp.PropertyName, baseCollectionName, parentId, new List<string> { nextProp.PropertyName }, currentProp.Sort, currentProp.Filter, startIndex, limit);
                                    if (propertyResult != null)
                                    {
                                        collectionKIDMap.Add(collectionKIDMapKey, propertyResult.First()[COLLECTION_KEY_KID].AsString);
                                    }
                                }

                                //get the next property from database
                                if (propertyResult != null && propertyResult.Any() && propertyResult[0].Contains(nextProp.PropertyName.ToLower()))
                                {

                                    switch (nextProp.Type)
                                    {
                                        case PropertyType.array:
                                            {
                                                if (nextProp.Sort != null)
                                                {
                                                    if (nextProp.Sort.Values.First() == 1)
                                                        propertyResult[0][nextProp.PropertyName.ToLower()] = new BsonArray((propertyResult[0][nextProp.PropertyName.ToLower()].AsBsonArray).OrderBy(x => x));
                                                    else if (nextProp.Sort.Values.First() == -1)
                                                        propertyResult[0][nextProp.PropertyName.ToLower()] = new BsonArray((propertyResult[0][nextProp.PropertyName.ToLower()].AsBsonArray).OrderByDescending(x => x));

                                                }
                                                if (nextProp.Index == null)
                                                {
                                                    var arrayResult = new List<dynamic>();
                                                    foreach (var propRes in propertyResult[0][nextProp.PropertyName.ToLower()].AsBsonArray)
                                                    {
                                                        arrayResult.Add(ExtractDataFromBsonValue(propRes));
                                                    }
                                                    responseModel.Data = arrayResult;
                                                }
                                                else
                                                {
                                                    var propRes = propertyResult[0][nextProp.PropertyName.ToLower()].AsBsonArray;
                                                    if (propRes.Count >= nextProp.Index)
                                                    {
                                                        responseModel.Data = ExtractDataFromBsonValue(propRes[nextProp.Index ?? 0]);
                                                    }
                                                }

                                            }; break;
                                        case PropertyType.boolean: { responseModel.Data = propertyResult[0][nextProp.PropertyName.ToLower()].AsBoolean; } break;
                                        case PropertyType.number: { responseModel.Data = propertyResult[0][nextProp.PropertyName.ToLower()].AsDouble; } break;
                                        case PropertyType.date: { responseModel.Data = propertyResult[0][nextProp.PropertyName.ToLower()].ToNullableUniversalTime(); } break;
                                        case PropertyType.str: { responseModel.Data = propertyResult[0][nextProp.PropertyName.ToLower()].AsString; } break;
                                    }

                                }
                                if (returnLength)
                                    responseModel.Data = responseModel.Data != null ? ((string)responseModel.Data).Length : 0;

                                return responseModel;
                            }
                            else
                            {
                                string collectionName = baseCollectionName + "_" + currentProp.PropertyDataType;
                                collectionKIDMapKey = GetcollectionKIDMapKey(collectionName, parentId, currentProp.PropertyName, currentProp.Index.ToString());
                                if (collectionKIDMap.ContainsKey(collectionKIDMapKey))
                                {
                                    parentId = collectionKIDMap[collectionKIDMapKey];
                                }
                                else
                                {
                                    var propertyResult = await GetObjectAsync(isKobject, entity, currentProp.PropertyDataType, previousProp.PropertyDataType, currentProp.PropertyName, baseCollectionName, parentId, new List<string> { COLLECTION_KEY_KID }, currentProp.Sort, currentProp.Filter, currentProp.Index, 1);

                                    if (propertyResult != null && propertyResult.Any() && propertyResult[0].Contains(COLLECTION_KEY_KID))
                                    {
                                        parentId = propertyResult[0][COLLECTION_KEY_KID].AsString;
                                        collectionKIDMap.Add(collectionKIDMapKey, parentId);
                                    }
                                }
                            }
                        }
                        else if (i == end)
                        {
                            string collectionName = baseCollectionName + "_" + currentProp.PropertyDataType;
                            collectionKIDMapKey = GetcollectionKIDMapKey(collectionName, parentId, currentProp.PropertyName, currentProp.Index.ToString());
                            if (collectionKIDMap.ContainsKey(collectionKIDMapKey))
                            {
                                string kid = collectionKIDMap[collectionKIDMapKey];
                                if (currentProp.Filter == null)
                                {
                                    currentProp.Filter = new Dictionary<string, object>();
                                }
                                //currentProp.Filter.Add(COLLECTION_KEY_KID, kid);
                                currentProp.Filter.Add(COLLECTION_KEY_ID, ObjectId.Parse(kid));
                                //currentProp.Filter.Add(COLLECTION_KEY_WEBISTE_ID, WebsiteId);
                                var propertyResult = await GetObjectAsync(isKobject, entity, currentProp.PropertyDataType, previousProp.PropertyDataType, currentProp.PropertyName, baseCollectionName, parentId, null, currentProp.Sort, currentProp.Filter, null, null);
                                if (propertyResult != null && propertyResult.Any())
                                {
                                    responseModel.Data = BsonSerializer.Deserialize<dynamic>(propertyResult[0]);
                                    return responseModel;
                                }
                            }
                            else
                            {
                                var propertyResult = await GetObjectAsync(isKobject, entity, currentProp.PropertyDataType, previousProp.PropertyDataType, currentProp.PropertyName, baseCollectionName, parentId, null, currentProp.Sort, currentProp.Filter, startIndex, limit);

                                if (propertyResult != null && propertyResult.Any())
                                {
                                    if (propertyResult[0].Contains(COLLECTION_KEY_KID))
                                    {
                                        collectionKIDMap.Add(collectionKIDMapKey, propertyResult[0][COLLECTION_KEY_KID].AsString);
                                    }
                                    responseModel.Data = BsonSerializer.Deserialize<dynamic>(propertyResult[0]);
                                }
                                return responseModel;
                            }
                            //Get entire object
                        }

                    }
                    else if (currentProp.Type == PropertyType.array)
                    {
                        if (i < end)
                        {

                            if ((nextProp.Type == PropertyType.array && nextProp.Type == PropertyType.obj) || nextProp.Type == PropertyType.obj || nextProp.Type == PropertyType.kstring)
                            {
                                string collectionName = baseCollectionName + "_" + currentProp.PropertyDataType;
                                collectionKIDMapKey = GetcollectionKIDMapKey(collectionName, parentId, currentProp.PropertyName, currentProp.Index.ToString());
                                if (collectionKIDMap.ContainsKey(collectionKIDMapKey))
                                {
                                    parentId = collectionKIDMap[collectionKIDMapKey];
                                }
                                else
                                {
                                    List<BsonDocument> kid = await GetObjectAsync(isKobject, entity, currentProp.PropertyDataType, previousProp.PropertyDataType, currentProp.PropertyName, baseCollectionName, parentId, new List<string> { COLLECTION_KEY_KID }, currentProp.Sort, currentProp.Filter, startIndex, limit);
                                    if (kid != null && kid.Any() && kid[0].Contains(COLLECTION_KEY_KID))
                                    {
                                        parentId = kid.First()[COLLECTION_KEY_KID].AsString;
                                        collectionKIDMap.Add(collectionKIDMapKey, parentId);
                                    }
                                }
                            }
                            else if (i == (end - 1)
                                && (nextProp.Type != PropertyType.array || (nextProp.Type == PropertyType.array && dataTypeObjects.Contains(nextProp.PropertyDataType)))
                                && nextProp.Type != PropertyType.obj
                                && nextProp.Type != PropertyType.phonenumber
                                && nextProp.Type != PropertyType.kstring
                                && nextProp.Type != PropertyType.function)//check if its second last property
                            {
                                string collectionName = baseCollectionName + "_" + currentProp.PropertyDataType;
                                collectionKIDMapKey = GetcollectionKIDMapKey(collectionName, parentId, currentProp.PropertyName, currentProp.Index.ToString());
                                List<BsonDocument> propertyResult;
                                if (collectionKIDMap.ContainsKey(collectionKIDMapKey))
                                {
                                    string kid = collectionKIDMap[collectionKIDMapKey];
                                    if (currentProp.Filter == null)
                                    {
                                        currentProp.Filter = new Dictionary<string, object>();
                                    }
                                    currentProp.Filter.Add(COLLECTION_KEY_ID, ObjectId.Parse(kid));
                                    //currentProp.Filter.Add(COLLECTION_KEY_WEBISTE_ID, WebsiteId);
                                    propertyResult = await GetObjectAsync(isKobject, entity, currentProp.PropertyDataType, previousProp.PropertyDataType, currentProp.PropertyName, baseCollectionName, parentId, new List<string> { nextProp.PropertyName }, currentProp.Sort, currentProp.Filter, null, null);
                                }
                                else
                                {
                                    propertyResult = await GetObjectAsync(isKobject, entity, currentProp.PropertyDataType, previousProp.PropertyDataType, currentProp.PropertyName, baseCollectionName, parentId, new List<string> { nextProp.PropertyName }, currentProp.Sort, currentProp.Filter, startIndex, limit);
                                    if (propertyResult != null && propertyResult.Any() && propertyResult[0].Contains(COLLECTION_KEY_KID))
                                    {
                                        collectionKIDMap.Add(collectionKIDMapKey, propertyResult[0][COLLECTION_KEY_KID].AsString);
                                    }
                                }

                                //get the next property from database
                                if (propertyResult != null && propertyResult.Any() && propertyResult[0].Contains(nextProp.PropertyName.ToLower()))
                                {

                                    switch (nextProp.Type)
                                    {
                                        case PropertyType.array:
                                            {
                                                if (nextProp.Sort != null)
                                                {
                                                    if (nextProp.Sort.Values.First() == 1)
                                                        propertyResult[0][nextProp.PropertyName.ToLower()] = new BsonArray((propertyResult[0][nextProp.PropertyName.ToLower()].AsBsonArray).OrderBy(x => x));
                                                    else if (nextProp.Sort.Values.First() == -1)
                                                        propertyResult[0][nextProp.PropertyName.ToLower()] = new BsonArray((propertyResult[0][nextProp.PropertyName.ToLower()].AsBsonArray).OrderByDescending(x => x));

                                                }
                                                if (nextProp.Index == null)
                                                {
                                                    var arrayResult = new List<dynamic>();
                                                    foreach (var propRes in propertyResult[0][nextProp.PropertyName.ToLower()].AsBsonArray)
                                                    {
                                                        arrayResult.Add(ExtractDataFromBsonValue(propRes));
                                                    }
                                                    responseModel.Data = arrayResult;
                                                }
                                                else
                                                {
                                                    var propRes = propertyResult[0][nextProp.PropertyName.ToLower()].AsBsonArray;
                                                    if (propRes.Count >= nextProp.Index)
                                                    {
                                                        responseModel.Data = ExtractDataFromBsonValue(propRes[nextProp.Index ?? 0]);
                                                    }
                                                }

                                            }; break;
                                        case PropertyType.boolean: { responseModel.Data = propertyResult[0][nextProp.PropertyName.ToLower()].AsBoolean; } break;
                                        case PropertyType.number: { responseModel.Data = propertyResult[0][nextProp.PropertyName.ToLower()].AsDouble; } break;
                                        case PropertyType.date: { responseModel.Data = propertyResult[0][nextProp.PropertyName.ToLower()].ToNullableUniversalTime(); } break;
                                        case PropertyType.str: { responseModel.Data = propertyResult[0][nextProp.PropertyName.ToLower()].AsString; } break;
                                    }
                                }
                                if (returnLength)
                                    responseModel.Data = responseModel.Data != null ? ((string)responseModel.Data).Length : 0;

                                return responseModel;
                            }
                            else if (nextProp.Type != PropertyType.function)
                            {
                                string collectionName = baseCollectionName + "_" + currentProp.PropertyDataType;
                                collectionKIDMapKey = GetcollectionKIDMapKey(collectionName, parentId, currentProp.PropertyName, currentProp.Index.ToString());
                                if (collectionKIDMap.ContainsKey(collectionKIDMapKey))
                                {
                                    parentId = collectionKIDMap[collectionKIDMapKey];
                                }
                                else
                                {
                                    var propertyResult = await GetObjectAsync(isKobject, entity, currentProp.PropertyDataType, previousProp.PropertyDataType, currentProp.PropertyName, baseCollectionName, parentId, new List<string> { COLLECTION_KEY_KID }, currentProp.Sort, currentProp.Filter, currentProp.Index, 1);

                                    if (propertyResult != null && propertyResult.Any() && propertyResult[0].Contains(COLLECTION_KEY_KID))
                                    {
                                        parentId = propertyResult[0][COLLECTION_KEY_KID].AsString;
                                        collectionKIDMap.Add(collectionKIDMapKey, parentId);
                                    }
                                }
                            }
                        }
                        else if (end == 1 && dataTypeObjects.Contains(currentProp.PropertyDataType))
                        {
                            string collectionName = baseCollectionName + "_" + currentProp.PropertyDataType;
                            collectionKIDMapKey = GetcollectionKIDMapKey(collectionName, parentId, currentProp.PropertyName, currentProp.Index.ToString());
                            List<BsonDocument> propertyResult;
                            if (collectionKIDMap.ContainsKey(collectionKIDMapKey))
                            {
                                string kid = collectionKIDMap[collectionKIDMapKey];
                                if (currentProp.Filter == null)
                                {
                                    currentProp.Filter = new Dictionary<string, object>();
                                }
                                //currentProp.Filter.Add(COLLECTION_KEY_KID, kid);
                                currentProp.Filter.Add(COLLECTION_KEY_ID, ObjectId.Parse(kid));
                                //currentProp.Filter.Add(COLLECTION_KEY_WEBISTE_ID, WebsiteId);

                                propertyResult = await GetObjectAsync(isKobject, entity, currentProp.PropertyDataType, previousProp.PropertyDataType, currentProp.PropertyName, baseCollectionName, parentId, new List<string> { currentProp.PropertyName }, null, currentProp.Filter, null, null);
                            }
                            else
                            {
                                propertyResult = await GetObjectAsync(isKobject, entity, currentProp.PropertyDataType, previousProp.PropertyDataType, currentProp.PropertyName, baseCollectionName, parentId, new List<string> { currentProp.PropertyName }, null, currentProp.Filter, startIndex, limit);
                                if (propertyResult != null && propertyResult.Any() && propertyResult[0].Contains(COLLECTION_KEY_KID))
                                {
                                    collectionKIDMap.Add(collectionKIDMapKey, propertyResult[0][COLLECTION_KEY_KID].AsString);
                                }
                            }
                            if (propertyResult != null && propertyResult.Any() && propertyResult[0].Contains(currentProp.PropertyName.ToLower()))
                            {
                                if (currentProp.Sort != null)
                                {
                                    if (currentProp.Sort.Values.First() == 1)
                                        propertyResult[0][currentProp.PropertyName.ToLower()] = new BsonArray((propertyResult[0][currentProp.PropertyName.ToLower()].AsBsonArray).OrderBy(x => x));
                                    else if (currentProp.Sort.Values.First() == -1)
                                        propertyResult[0][currentProp.PropertyName.ToLower()] = new BsonArray((propertyResult[0][currentProp.PropertyName.ToLower()].AsBsonArray).OrderByDescending(x => x));

                                }
                                if (currentProp.Index == null)
                                {
                                    var arrayResult = new List<dynamic>();
                                    foreach (var propRes in propertyResult[0][currentProp.PropertyName.ToLower()].AsBsonArray)
                                    {
                                        arrayResult.Add(ExtractDataFromBsonValue(propRes));
                                    }
                                    responseModel.Data = arrayResult;
                                }
                                else
                                {
                                    var propRes = propertyResult[0][currentProp.PropertyName.ToLower()].AsBsonArray;
                                    if (propRes.Count >= currentProp.Index)
                                    {
                                        responseModel.Data = ExtractDataFromBsonValue(propRes[currentProp.Index ?? 0]);
                                    }
                                }

                            }
                        }
                        else if (i == end)
                        {
                            var newLimit = 5;
                            if (limit != null && limit.HasValue && limit.Value < 10)
                                newLimit = limit.Value;
                            else
                                newLimit = 5;

                            string collectionName = baseCollectionName + "_" + currentProp.PropertyDataType;
                            collectionKIDMapKey = GetcollectionKIDMapKey(collectionName, parentId, currentProp.PropertyName, currentProp.Index.ToString());
                            List<BsonDocument> propertyResult;
                            if (collectionKIDMap.ContainsKey(collectionKIDMapKey))
                            {
                                string kid = collectionKIDMap[collectionKIDMapKey];
                                if (currentProp.Filter == null)
                                {
                                    currentProp.Filter = new Dictionary<string, object>();
                                }
                                //currentProp.Filter.Add(COLLECTION_KEY_KID, kid);
                                currentProp.Filter.Add(COLLECTION_KEY_ID, ObjectId.Parse(kid));
                                //currentProp.Filter.Add(COLLECTION_KEY_WEBISTE_ID, WebsiteId);

                                propertyResult = await GetObjectAsync(isKobject, entity, currentProp.PropertyDataType, previousProp.PropertyDataType, currentProp.PropertyName, baseCollectionName, parentId, new List<string> { currentProp.PropertyName }, null, currentProp.Filter, null, null);
                            }
                            else
                            {

                                propertyResult = await GetObjectAsync(isKobject, entity, currentProp.PropertyDataType, previousProp.PropertyDataType, currentProp.PropertyName, baseCollectionName, parentId, null, currentProp.Sort, currentProp.Filter, startIndex, newLimit);
                            }
                            if (propertyResult != null && propertyResult.Any())
                            {
                                if (currentProp.Index != null)
                                {
                                    responseModel.Data = BsonSerializer.Deserialize<dynamic>(propertyResult[0]);
                                }
                                else
                                {
                                    var arrayResult = new List<dynamic>();
                                    foreach (var propRes in propertyResult)
                                    {
                                        arrayResult.Add(BsonSerializer.Deserialize<dynamic>(propRes));
                                    }
                                    responseModel.Data = arrayResult;
                                }

                            }
                            //Get entire list of array
                        }
                    }
                    else if (currentProp.Type == PropertyType.function)
                    {
                        if (currentProp.PropertyName.ToLower() == FUNCTION_NAME_LENGTH && previousProp.Type == PropertyType.array)
                        {
                            if (i > 1 && previousProp.PropertyDataType != null && dataTypeObjects.Contains(previousProp.PropertyDataType))
                            {
                                var objectCollectionProp = PropertySegments[i - 2];
                                var length = GetObjectArrayLength(entity, baseCollectionName, objectCollectionProp.PropertyDataType, previousProp.PropertyName, parentId, isCustomDatatype: false);
                                responseModel.Data = length;
                            }
                            else
                            {
                                var length = GetObjectArrayLength(entity, baseCollectionName, previousProp.PropertyDataType, previousProp.PropertyName, parentId, isCustomDatatype: true);
                                responseModel.Data = length;
                            }
                        }
                    }
                    else if (nextProp == null)
                    {

                        var propertyResult = await GetObjectAsync(isKobject, entity, end == 1 ? previousProp.PropertyDataType : currentProp.PropertyDataType, previousProp.PropertyDataType, currentProp.PropertyName, baseCollectionName, parentId, new List<string> { currentProp.PropertyName }, currentProp.Sort, currentProp.Filter, startIndex, limit);

                        if (propertyResult != null && propertyResult.Any() && propertyResult[0].Contains(currentProp.PropertyName))
                        {
                            responseModel.Data = ExtractDataFromBsonValue(propertyResult[0][currentProp.PropertyName]);

                        }
                        if (returnLength)
                            responseModel.Data = responseModel.Data != null ? ((string)responseModel.Data).Length : 0;

                        return responseModel;
                    }

                }


                //add default query
                //add additional custom query
                //add sort
                //add skip/limit for arrya
                //add include if the next segment is default property 
                //if next segment is object or array just get the _kid
                //

                return responseModel;

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private static string GetcollectionKIDMapKey(string collectionName, string _parentId, string _propertyName, string _index)
        {
            return collectionName + "_" + _parentId + "_" + _propertyName + "_" + _index;
        }

        public static async Task<string> PrefillKIDsAsync(List<PropertyPathSegment> PropertySegments,
            string SchemaId, string WebsiteId, KEntity entity, int arrayStartIndex, int arrayEndIndex, Dictionary<string, string> collectionKIDMap, bool isKobject = false, int? kobjectIndex = null)
        {
            var resultParentId = string.Empty;
            try
            {
                if (_kitsuneSchemaServer == null || _kitsuneSchemaDB == null)
                {
                    InitiateConnection();
                }
                if (entity == null)
                {
                    throw new Exception($"Language not found with id \"{SchemaId}\".");
                }

                //Get first parent _kid
                var baseCollectionName = GenerateSchemaName(entity.EntityName);
                BsonDocument baseObject;
                string collectionKIDMapKey = GetcollectionKIDMapKey(baseCollectionName, null, null, null);

                string parentId;
                if (collectionKIDMap.ContainsKey(collectionKIDMapKey))
                {
                    parentId = collectionKIDMap[collectionKIDMapKey];
                }
                else
                {
                    var filterDoc = new BsonDocument();
                    filterDoc.Add(COLLECTION_KEY_IS_ARCHIVED, false);
                    filterDoc.Add(COLLECTION_KEY_WEBISTE_ID, WebsiteId);
                    var projectDoc = new BsonDocument();
                    projectDoc.Add(COLLECTION_KEY_KID, 1);
                    baseObject = await _kitsuneSchemaDB.GetCollection<BsonDocument>(baseCollectionName).Find(filterDoc,
                        new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).Project(projectDoc).FirstOrDefaultAsync();

                    if (baseObject != null)
                        collectionKIDMap.Add(collectionKIDMapKey, baseObject[COLLECTION_KEY_KID].AsString);
                    if (baseObject == null)
                        throw new Exception($"No data found for website : \"{WebsiteId}\"");
                    parentId = baseObject[COLLECTION_KEY_KID].AsString;
                }

                //foreach property in propertysegments
                var end = PropertySegments.Count - 1;
                PropertyPathSegment previousProp = null;
                PropertyPathSegment currentProp = null;
                PropertyPathSegment nextProp = null;
                int? startIndex = null;
                int? limit = null;

                List<string> projectList = new List<string>();
                projectList.Add(COLLECTION_KEY_KID);
                projectList.Add(COLLECTION_KEY_PARENT_CLASS_ID);
                List<BsonDocument> propertyResult = null;
                var dataTypeObjects = new string[] { "str", "number", "datetime", "boolean" };
                if (isKobject)
                {
                    currentProp = PropertySegments[end];
                    previousProp = PropertySegments[end - 1];
                }
                else
                {
                    for (var i = 1; i < end; i++)
                    {
                        currentProp = PropertySegments[i];
                        nextProp = i < end ? PropertySegments[i + 1] : null;
                        previousProp = PropertySegments[i - 1];
                        propertyResult = await GetObjectAsync(isKobject, entity, currentProp.PropertyDataType, previousProp.PropertyDataType, currentProp.PropertyName, baseCollectionName, parentId, projectList, currentProp.Sort, currentProp.Filter, null, null);
                        if (propertyResult != null)
                        {
                            parentId = propertyResult.First()[COLLECTION_KEY_KID].AsString;
                        }
                        if (parentId == null)
                        {
                            return resultParentId;
                        }
                    }
                }

                if (currentProp == null)
                {
                    currentProp = PropertySegments[1];
                    previousProp = PropertySegments[0];
                }

                propertyResult = await GetObjectAsync(isKobject, entity, currentProp.PropertyDataType, previousProp.PropertyDataType, currentProp.PropertyName, baseCollectionName, parentId, projectList, currentProp.Sort, currentProp.Filter, arrayStartIndex, arrayEndIndex - arrayStartIndex);
                int index = isKobject && kobjectIndex != null ? kobjectIndex ?? 0 : arrayStartIndex;
                foreach (BsonDocument result in propertyResult)
                {
                    string collectionName = baseCollectionName + "_" + currentProp.PropertyDataType;
                    collectionKIDMapKey = GetcollectionKIDMapKey(collectionName, parentId, currentProp.PropertyName, index.ToString());
                    collectionKIDMap.Add(collectionKIDMapKey, result[COLLECTION_KEY_KID].AsString);
                    index++;
                    resultParentId = result[COLLECTION_KEY_PARENT_CLASS_ID].AsString;
                }
                return resultParentId;
            }
            catch (Exception e)
            {
            }
            return resultParentId;
        }

        /// <summary>
        ///   Get recursive object of the schema from the different collection
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="kClassName"></param>
        /// <param name="propertyName"></param>
        /// <param name="isArray"></param>
        /// <returns></returns>
        private static async Task<List<BsonDocument>> GetObjectAsync(bool isKobject, KEntity entity,
            string kClassName,
            string parentClassName,
            string propertyName,
            string baseCollectionName,
            string parentId,
            List<string> projectDefinition = null,
            Dictionary<string, int> sort = null,
            Dictionary<string, object> query = null,
            int? skip = null,
            int? limit = null)
        {
            parentClassName = parentClassName?.ToLower();
            kClassName = kClassName?.ToLower();
            propertyName = propertyName?.ToLower();
            var kClass = entity.Classes.FirstOrDefault(x => x.Name.ToLower() == kClassName.ToLower());
            var kParentClass = entity.Classes.FirstOrDefault(x => x.Name.ToLower() == parentClassName.ToLower());

            if (kClass != null)
            {
                var isBase = kClass.ClassType == KClassType.BaseClass;
                IMongoCollection<BsonDocument> classCollection = null;
                string collectionName = "";
                if (kClass.ClassType == KClassType.DataTypeClass && kClass.Name.ToLower() != "kstring" && kClass.Name.ToLower() != "phonenumber")
                {
                    collectionName = isBase
                        ? baseCollectionName
                        : string.Format("{0}_{1}", baseCollectionName, parentClassName.ToLower());
                }
                else
                {
                    collectionName = isBase
                    ? baseCollectionName
                    : string.Format("{0}_{1}", baseCollectionName, kClassName.ToLower());
                }

                classCollection = _kitsuneSchemaDB.GetCollection<BsonDocument>(collectionName);

                bool isCacheableQuery = IsCacheableCollection(collectionName, propertyName);
                string key = collectionName + "_" + parentId;

                if (isCacheableQuery)
                {
                    dynamic result = CacheHelper.GetExpression(key);
                    if (result != null)
                    {
                        return result;
                    }
                }

                var findOb = new BsonDocument();
                var defaultQueryParams = new Dictionary<string, object>();



                //defaultQueryParams.Add(COLLECTION_KEY_PARENT_CLASS_NAME, parentClassName);
                //added condition for nested kobject as they dont need parent reference
                if (!isKobject && !string.IsNullOrEmpty(parentId))
                    defaultQueryParams.Add(COLLECTION_KEY_PARENT_CLASS_ID, parentId);

                if (!string.IsNullOrEmpty(propertyName))
                    defaultQueryParams.Add(COLLECTION_KEY_PROPERTY_NAME, propertyName);
                defaultQueryParams.Add("isarchived", false);
                if (query != null)
                {
                    try
                    {
                        findOb = new BsonDocument(query);
                    }
                    catch (Exception ex)
                    {
                        if (ex.Data != null)
                            ex.Data.Add("Invalid query", JsonConvert.SerializeObject(query));
                        throw ex;
                    }
                }
                if (isBase)
                    findOb.Add(COLLECTION_KEY_KID, parentId);
                else
                    findOb.AddRange(defaultQueryParams);

                IFindFluent<BsonDocument, BsonDocument> ob = null;
                ob = classCollection
                   .Find(findOb, new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) });

                //Handle sorting 
                if (sort != null && sort.Any())
                {
                    ob = ob.Sort(new BsonDocument(sort));
                }
                //Ronak
                //sorting basedon _id, as its basedon created date only so we can use it without creating any additional index
                //TODO : test the performance without creating any index. if required create descending index 
                else
                {
                    ob = ob.Sort(new SortDefinitionBuilder<BsonDocument>().Descending("_id"));
                }
                //Handle query

                if (skip != null)
                    ob.Skip(skip);
                if (limit != null)
                    ob.Limit(limit);

                if (projectDefinition != null)
                {
                    var projDoc = new BsonDocument();
                    foreach (var proj in projectDefinition)
                    {
                        projDoc.Add(proj, 1);
                    }
                    if (!projDoc.Contains(COLLECTION_KEY_KID))
                        projDoc.Add(COLLECTION_KEY_KID, 1);
                    ob = ob.Project(projDoc);
                }

                var finalResult = new List<BsonDocument>();
                var listDocs = await ob.ToListAsync();
                //if (ob != null && ob.Any())
                if (listDocs?.Count > 0)
                {
                    var dataTypeObjects = new string[] { "str", "number", "datetime", "boolean" };

                    var classProperties = kClass.PropertyList.Where(x => (x.Type == PropertyType.array && !dataTypeObjects.Contains(x.DataType?.Name?.ToLower())) || x.Type == PropertyType.obj || x.Type == PropertyType.kstring || x.Type == PropertyType.phonenumber);
                    foreach (var item in listDocs)
                    {
                        foreach (var prop in classProperties)
                        {
                            if (projectDefinition == null || projectDefinition.Any(x => x.Contains($"{propertyName}.{prop.Name.ToLower()}")) || projectDefinition.Any(x => x.Contains($"{prop.Name.ToLower()}")))
                            {
                                var resob = await GetObjectAsync(isKobject, entity, prop.DataType.Name.Trim('[', ']'), kClassName, prop.Name, baseCollectionName, item[COLLECTION_KEY_KID].AsString);
                                if (prop.Type == PropertyType.array)
                                {
                                    var arr = new BsonArray();
                                    foreach (var arritem in resob)
                                    {
                                        arr.Add(arritem.AsBsonValue);
                                    }
                                    item.Add(prop.Name.ToLower(), arr);
                                }
                                else if (resob.Any())
                                    item.Add(prop.Name.ToLower(), resob.FirstOrDefault());
                            }
                        }
                        item.Remove(COLLECTION_KEY_ID);
                        finalResult.Add(item);
                    }
                }

                if (isCacheableQuery)
                {
                    CacheHelper.SaveExpression(key, finalResult);
                }

                return finalResult;
            }
            throw new Exception($"Class : {kClassName} does not found");
        }

        private static bool IsCacheableCollection(string collectionName, string propertyName)
        {
            if (collectionName.ToLower() == "k_business_link" && propertyName.ToLower() == "keywords")
            {
                return true;
            }
            return false;
        }

        #region Distributed Identifier
        public static List<ProjectComponent> GetProjectComponents(string projectId, int version)
        {
            try
            {
                var collectionName = KLM_Constants.KitsuneProjectProductionCollection;
                IMongoCollection<ProductionKitsuneProject> projectsCollection = _kitsuneDB.GetCollection<ProductionKitsuneProject>(collectionName);
                var result = projectsCollection.Find(x => x.ProjectId == projectId,
                    new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).Project(k => new { k.Components }).FirstOrDefault();

                return result.Components;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static KitsuneResource GetUrlPatternDetails(string projectId, int version, string sourcePath)
        {
            try
            {
                if (_server == null)
                    InitiateConnection();

                string collectionName = KLM_Constants.KitsuneResourcesProductionCollection;
                KitsuneResource response = null;

                IMongoCollection<KitsuneResource> projectsCollection = _kitsuneDB.GetCollection<KitsuneResource>(collectionName);
                //CHECK AND RETURN IF THE REQUEST TYPE IS DEFAULTVIEW
                if (string.IsNullOrEmpty(sourcePath) || sourcePath.Equals("/"))
                {
                    response = projectsCollection.Find(x => x.ProjectId == projectId && x.IsDefault == true && x.IsStatic == false
                                                && x.ResourceType == ResourceType.LINK, new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) })
                                                .Project(k => new KitsuneResource { UrlPattern = k.UrlPattern, UrlPatternRegex = k.UrlPatternRegex }).FirstOrDefault();
                }
                if (response == null)
                {
                    response = projectsCollection.Find(x => x.ProjectId == projectId && x.SourcePath == sourcePath).Project(k => new KitsuneResource { UrlPattern = k.UrlPattern, UrlPatternRegex = k.UrlPatternRegex }).FirstOrDefault();
                }
                return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return null;
        }


        #endregion

        #region NFX - Redirection

        internal static CommonBusinessSchemaNFXEntityModel GetProductDetails(string productId)
        {
            try
            {
                if (_kitsuneSchemaServer == null)
                    InitiateConnection();

                var productCollection = _kitsuneSchemaDB.GetCollection<BsonDocument>("k_business_product");

                var filter = Builders<BsonDocument>.Filter.Eq("_kid", productId)
                                & Builders<BsonDocument>.Filter.Eq("isarchived", false);

                var product = productCollection.Find(filter)?.FirstOrDefault();

                if (product != null)
                    return new CommonBusinessSchemaNFXEntityModel()
                    {
                        Content = product["name"].AsString,
                        Index = (long)product["index"].AsDouble,
                        _id = product["_kid"].AsString
                    };
            }
            catch (Exception ex)
            {
                ConsoleLogger.Write($"Unable to GetBizFloatsDetails({productId}) - {ex.ToString()}");
            }

            return null;
        }

        internal static CommonBusinessSchemaNFXEntityModel GetProductDetailsByIndex(string merchantId, string index)
        {
            try
            {
                if (_kitsuneSchemaServer == null)
                    InitiateConnection();

                var productCollection = _kitsuneSchemaDB.GetCollection<BsonDocument>("k_business_product");

                var filter = Builders<BsonDocument>.Filter.Eq("websiteid", merchantId)
                               & Builders<BsonDocument>.Filter.Eq("isarchived", false)
                               & Builders<BsonDocument>.Filter.Eq("index", Convert.ToDouble(index));

                var product = productCollection.Find(filter)?.FirstOrDefault();

                if (product != null)
                    return new CommonBusinessSchemaNFXEntityModel()
                    {
                        Content = product["name"].AsString,
                        Index = (long)product["index"].AsDouble,
                        _id = product["_kid"].AsString
                    };
            }
            catch (Exception ex)
            {
                ConsoleLogger.Write($"Unable to GetProductDetailsByIndex({merchantId}, {index}) - {ex.ToString()}");
            }

            return null;
        }

        public static CommonBusinessSchemaNFXEntityModel GetBizFloatsDetails(string dealId)
        {
            try
            {
                if (_kitsuneSchemaServer == null)
                    InitiateConnection();

                var dealCollection = _kitsuneSchemaDB.GetCollection<BsonDocument>("k_business_update");

                var filter = Builders<BsonDocument>.Filter.Eq("_kid", dealId)
                                & Builders<BsonDocument>.Filter.Eq("isarchived", false);

                var bizFloat = dealCollection.Find(filter)?.FirstOrDefault();

                if (bizFloat != null)
                    return new CommonBusinessSchemaNFXEntityModel()
                    {
                        Content = bizFloat["title"].AsString,
                        Index = (long)bizFloat["index"].AsDouble,
                        _id = bizFloat["_kid"].AsString
                    };
            }
            catch (Exception ex)
            {
                ConsoleLogger.Write($"Unable to GetBizFloatsDetails({dealId}) - {ex.ToString()}");
            }

            return null;
        }

        public static CommonBusinessSchemaNFXEntityModel GetBizFloatsDetailsByIndex(string merchantId, string index)
        {
            try
            {
                if (_kitsuneSchemaServer == null)
                    InitiateConnection();

                var dealCollection = _kitsuneSchemaDB.GetCollection<BsonDocument>("k_business_update");

                var filter = Builders<BsonDocument>.Filter.Eq("websiteid", merchantId)
                                & Builders<BsonDocument>.Filter.Eq("isarchived", false)
                                & Builders<BsonDocument>.Filter.Eq("index", Convert.ToDouble(index));

                var bizFloat = dealCollection.Find(filter)?.FirstOrDefault();

                if (bizFloat != null)
                    return new CommonBusinessSchemaNFXEntityModel()
                    {
                        Content = bizFloat["title"].AsString,
                        Index = (long)bizFloat["index"].AsDouble,
                        _id = bizFloat["_kid"].AsString
                    };
            }
            catch (Exception ex)
            {
                ConsoleLogger.Write($"Unable to GetBizFloatsDetailsByIndex({merchantId}, {index}) - {ex.ToString()}");
            }

            return null;
        }

        internal static string GetBizFloatUrlPattern(string themeId)
        {
            try
            {
                if (_server == null)
                    InitiateConnection();


                var temp_themeCollection = _kitsuneDB.GetCollection<ProductionKitsuneResource>(KLM_Constants.KitsuneResourcesProductionCollection);
                var temp_productDefinitionBuilder = new ProjectionDefinitionBuilder<ProductionKitsuneResource>();
                var temp_pd = temp_productDefinitionBuilder.Include(x => x._id).Include(x => x.UrlPattern).Include(x => x.IsDefault).Include(x => x.UrlPatternRegex).Include(x => x.IsStatic).Include(s => s.PageType);

                var temp_result = temp_themeCollection.Find(x => x.ProjectId == themeId && x.PageType == Kitsune.Models.Project.KitsunePageType.DETAILS && x.KObject.Contains("business.updates")).Project<ProductionKitsuneResource>(temp_pd)?.ToList();

                if (temp_result != null && temp_result.Count() > 0)
                {
                    return temp_result.FirstOrDefault().UrlPattern;
                }
                else
                {
                    temp_result = temp_themeCollection.Find(x => x.ProjectId == themeId
                                            && (x.UrlPatternRegex.ToLower().Contains("([a-zA-Z0-9\\-\\.,\\%]+)/b-([a-zA-Z0-9\\-\\.,\\%]+)") ||
                                            x.UrlPatternRegex.ToLower().Contains("([a-zA-Z0-9\\-\\.,\\%]+)/u-([a-zA-Z0-9\\-\\.,\\%]+)") ||
                                            x.UrlPatternRegex.ToLower().Contains("([a-zA-Z0-9\\-\\.,\\%]+)/u([a-zA-Z0-9\\-\\.,\\%]+)") ||
                                            (x.UrlPatternRegex.ToLower().Contains("([a-zA-Z0-9\\-\\.,\\%]+)/b([a-zA-Z0-9\\-\\.,\\%]+)")))).Project<ProductionKitsuneResource>(temp_pd)?.ToList();

                    if (temp_result != null && temp_result.Count() > 0)
                    {
                        return temp_result.FirstOrDefault().UrlPattern;
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleLogger.Write($"Exception: GetBizFloatUrlPattern({themeId}) - {ex.ToString()}");
            }

            return null;
        }

        internal static string GetProductUrlPattern(string themeId)
        {
            try
            {
                if (_server == null)
                    InitiateConnection();


                var temp_themeCollection = _kitsuneDB.GetCollection<ProductionKitsuneResource>(KLM_Constants.KitsuneResourcesProductionCollection);
                var temp_productDefinitionBuilder = new ProjectionDefinitionBuilder<ProductionKitsuneResource>();
                var temp_pd = temp_productDefinitionBuilder.Include(x => x._id).Include(x => x.UrlPattern).Include(x => x.IsDefault).Include(x => x.UrlPatternRegex).Include(x => x.IsStatic).Include(s => s.PageType);

                var temp_result = temp_themeCollection.Find(x => x.ProjectId == themeId && x.PageType == Kitsune.Models.Project.KitsunePageType.DETAILS && x.KObject.Contains("business.products")).Project<ProductionKitsuneResource>(temp_pd)?.ToList();

                if (temp_result != null && temp_result.Count() > 0)
                {
                    return temp_result.FirstOrDefault().UrlPattern;
                }
                else
                {
                    temp_result = temp_themeCollection.Find(x => x.ProjectId == themeId
                                    && (x.UrlPatternRegex.ToLower().Contains("([a-zA-Z0-9\\-\\.,\\%]+)/p([a-zA-Z0-9\\-\\.,\\%]+)")
                                    || (x.UrlPatternRegex.ToLower().Contains("([a-zA-Z0-9\\-\\.,\\%]+)/p-([a-zA-Z0-9\\-\\.,\\%]+)")))).Project<ProductionKitsuneResource>(temp_pd)?.ToList();

                    if (temp_result != null && temp_result.Count() > 0)
                    {
                        return temp_result.FirstOrDefault().UrlPattern;
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleLogger.Write($"Exception: GetProductUrlPattern({themeId}) - {ex.ToString()}");
            }

            return null;
        }

        #endregion
    }
}