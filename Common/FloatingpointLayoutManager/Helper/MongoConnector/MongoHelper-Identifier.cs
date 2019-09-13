using Kitsune.Models;
using Kitsune.Models.Project;
using Kitsune.Models.WebsiteModels;
using KitsuneLayoutManager.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KitsuneLayoutManager.Helper.MongoConnector
{
    public static partial class MongoHelper
    {
        #region Project and Website Details

        public static async Task<KitsuneDomainDetails> GetCustomerDetailsFromDomainAsync(string domainUrl, KitsuneRequestUrlType kitsuneRequestUrlType = KitsuneRequestUrlType.PRODUCTION)
        {
            try
            {
                if (_server == null)
                    InitiateConnection();
                if (String.IsNullOrEmpty(domainUrl))
                    return null;

                var websiteCollection = _kitsuneDB.GetCollection<KitsuneWebsiteCollection>(KLM_Constants.KitsuneWebsiteCollection);
                var websiteDNSCollection = _kitsuneDB.GetCollection<WebsiteDNSInfo>(KLM_Constants.KitsuneDNSCollection);

                var result = new KitsuneDomainDetails();
                var tempDomainUrl = domainUrl.ToUpper();

                var filter = (Builders<KitsuneWebsiteCollection>.Filter.Eq(document => document.WebsiteUrl, tempDomainUrl)) &
                                Builders<KitsuneWebsiteCollection>.Filter.Eq(document => document.IsActive, true);

                if (kitsuneRequestUrlType == KitsuneRequestUrlType.DEMO || kitsuneRequestUrlType == KitsuneRequestUrlType.PREVIEW)
                    filter = Builders<KitsuneWebsiteCollection>.Filter.Eq(document => document._id, domainUrl);


                var customer = websiteCollection.Find(filter, new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).FirstOrDefault();
                if (customer != null)
                {
                    result.Domain = customer.WebsiteUrl;
                    result.ProjectId = customer.ProjectId;
                    result.Version = customer.KitsuneProjectVersion;
                    result.CustomerId = customer._id;
                    result.WebsiteTag = customer.WebsiteTag;
                    result.DeveloperId = customer.DeveloperId;
                    result.ClientId = customer.ClientId;

                    try
                    {
                        var filterDNS = Builders<WebsiteDNSInfo>.Filter.Eq(document => document.DomainName, customer.WebsiteUrl) 
                                        & Builders<WebsiteDNSInfo>.Filter.Eq(document => document.DNSStatus, DNSStatus.Active);

                        var dnsProjectionBuilder = new ProjectionDefinitionBuilder<WebsiteDNSInfo>();
                        var dnsProjection = dnsProjectionBuilder.Include(x => x.WebsiteId).Include(x => x.IsSSLEnabled)
                            .Include(x => x._id);

                        var dnsInfo = websiteDNSCollection.Find(filterDNS, 
                            new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).Project<WebsiteDNSInfo>(dnsProjection)
                            .Sort(Builders<WebsiteDNSInfo>.Sort.Descending(x => x.CreatedOn))
                            ?.FirstOrDefault();

                        if (dnsInfo == null)
                        {
                            Console.WriteLine($"No active DNS record found for {tempDomainUrl}");
                            throw new Exception($"No active DNS record found for {tempDomainUrl}");
                        }

                        if (dnsInfo.IsSSLEnabled)
                            result.isSSLEnabled = true;
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
                else
                {
                    #region Handle .getkitsune.com subdomain redirection

                    var filterDNS = Builders<WebsiteDNSInfo>.Filter.Eq(document => document.DomainName, tempDomainUrl)
                                    & Builders<WebsiteDNSInfo>.Filter.Eq(document => document.DNSStatus, DNSStatus.Active);
                    
                    var dnsProjectionBuilder = new ProjectionDefinitionBuilder<WebsiteDNSInfo>();

                    var dnsInfo = websiteDNSCollection.Find(filterDNS, new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).FirstOrDefault();
                    if (dnsInfo != null)
                    {
                        filter = (Builders<KitsuneWebsiteCollection>.Filter.Eq(document => document._id, dnsInfo.WebsiteId));

                        //customerDetails = CommonKitsuneDBGetQueryDetailsForAnyCollection<KitsuneWebsiteCollection>(Identifier_Constants.KitsuneWebsiteCollection, filter, projec, null, 1).FirstOrDefault();
                        customer = await websiteCollection.Find(filter, new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).FirstOrDefaultAsync();

                        try
                        {
                            filterDNS = Builders<WebsiteDNSInfo>.Filter.Eq(document => document.DomainName, customer.WebsiteUrl)
                                            & Builders<WebsiteDNSInfo>.Filter.Eq(document => document.DNSStatus, DNSStatus.Active);
                            
                            var newDNSInfo = websiteDNSCollection.Find(filterDNS, new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) })
                                .Sort(Builders<WebsiteDNSInfo>.Sort.Descending(x => x.CreatedOn))
                                ?.FirstOrDefault();

                            if (customer != null)
                            {
                                result.Domain = tempDomainUrl;
                                result.ProjectId = customer.ProjectId;
                                result.Version = customer.KitsuneProjectVersion;
                                result.CustomerId = customer._id;
                                result.RedirectUrl = customer.WebsiteUrl;
                                result.IsRedirect = true;
                                result.WebsiteTag = customer.WebsiteTag;
                                result.DeveloperId = customer.DeveloperId;
                                result.ClientId = customer.ClientId;
                                result.isSSLEnabled = newDNSInfo.IsSSLEnabled;
                            }
                        }
                        catch
                        {
                        }
                    }
                    else
                    {
                        return null;
                    }
                    #endregion
                }

                //TO-Do update to BasePlugin
                if ((kitsuneRequestUrlType == KitsuneRequestUrlType.DEMO || kitsuneRequestUrlType == KitsuneRequestUrlType.PREVIEW) || (customer != null && !customer.IsActive && customer.WebsiteUrl.ToLower().EndsWith("getkitsune.com")))
                    result.IsRedirect = false;

                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static async Task<ProjectDetails> GetProjectDetailsAsync(string projectId, KitsuneRequestUrlType kitsuneRequestUrlType = KitsuneRequestUrlType.PRODUCTION)
        {
            try
            {
                if (_server == null)
                    InitiateConnection();

                var projectionBuilder = new ProjectionDefinitionBuilder<ProductionKitsuneProject>();
                var projection = projectionBuilder.Include(x => x.BucketNames)
                                                  .Include(x => x.SchemaId)
                                                  .Include(x => x.Version)
                                                  .Include(x => x.Components)
                                                  .Include(x => x.RuntimeOptimization)
                                                  .Include(x => x._id)
                                                  .Include(x => x.ProjectId)
                                                  .Include(x => x.UserEmail)
                                                  .Include(x => x.CompilerVersion);

                var filter = Builders<ProductionKitsuneProject>.Filter.Eq(document => document.ProjectId, projectId);

                var collectionName = KLM_Constants.KitsuneProjectProductionCollection;
                if (kitsuneRequestUrlType == KitsuneRequestUrlType.DEMO || kitsuneRequestUrlType == KitsuneRequestUrlType.PREVIEW)
                    collectionName = KLM_Constants.KitsuneProjectCollection;

                var productionKitsuneProjectsCOllection = _kitsuneDB.GetCollection<ProductionKitsuneProject>(collectionName);
                var result = await productionKitsuneProjectsCOllection.Find(filter,
                    new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).Project<ProductionKitsuneProject>(projection).FirstOrDefaultAsync();

                return new ProjectDetails()
                {
                    BucketNames = result.BucketNames,
                    SchemaId = result.SchemaId,
                    Version = result.Version,
                    Components = result.Components,
                    DeveloperEmail = result.UserEmail,
                    RuntimeOptimization = result.RuntimeOptimization,
                    ProjectId = result.ProjectId,
                    CompilerVersion = result.CompilerVersion
                };
            }
            catch (Exception ex)
            {
            }
            return null;
        }

        public static async Task<KitsuneResource> GetProjectResourceDetailsAsync(string projectid, string sourcePath, bool isDefaultView = false, KitsuneRequestUrlType kitsuneRequestUrlType = KitsuneRequestUrlType.PRODUCTION, bool isStaticFile = false)
        {
            try
            {
                if (_server == null)
                    InitiateConnection();

                string collectionName = KLM_Constants.KitsuneResourcesProductionCollection;
                IMongoCollection<KitsuneResource> projectsCollection;
                KitsuneResource response = null;

                if (kitsuneRequestUrlType == KitsuneRequestUrlType.DEMO || kitsuneRequestUrlType == KitsuneRequestUrlType.PREVIEW)
                    collectionName = KLM_Constants.KitsuneResourcesCollection;

                projectsCollection = _kitsuneDB.GetCollection<KitsuneResource>(collectionName);

                //optimized resource fatching
                var projection = new ProjectionDefinitionBuilder<KitsuneResource>()
                    .Include(x => x.OptimizedPath)
                    .Include(x => x.SourcePath)
                    .Include(x => x.UrlPattern)
                    .Include(x => x.UrlPatternRegex)
                    .Include(x => x.PageType)
                    .Include(x => x.IsStatic);
                //CHECK AND RETURN IF THE REQUEST TYPE IS DEFAULTVIEW
                if (isDefaultView)
                    response = await projectsCollection.Find(x => x.ProjectId == projectid
                                                        && x.IsDefault == true  && x.IsStatic == false
                                                        && x.ResourceType == ResourceType.LINK,
                                                        new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) })
                                                        .Project<KitsuneResource>(projection).FirstOrDefaultAsync();

                if (response == null)
                {
                    var requestUrl = sourcePath.ToLower();

                    #region Mimetype detection

                    var tempRequestUrl = requestUrl;
                    if (((String.Compare(tempRequestUrl, "/") == 0) && isDefaultView) || (tempRequestUrl.EndsWith("/")))
                        tempRequestUrl += "index.html";

                    //check if it is static file
                    var filter = (Builders<KitsuneResource>.Filter.Eq(document => document.ProjectId, projectid) &
                                Builders<KitsuneResource>.Filter.Eq(document => document.IsStatic, true)) &
                                (Builders<KitsuneResource>.Filter.Where(document => document.OptimizedPath.ToLower() == tempRequestUrl) |
                                Builders<KitsuneResource>.Filter.Where(document => document.SourcePath.ToLower() == tempRequestUrl));

                    var tempCheck = projectsCollection.Find(filter, new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) })
                        .Project<KitsuneResource>(projection).Limit(1).ToList();

                    if (tempCheck != null && tempCheck.Count() > 0 && !String.IsNullOrEmpty(tempCheck.FirstOrDefault()._id))
                        return tempCheck.FirstOrDefault();

                    if ((Helper.IsStaticFile(tempRequestUrl)) || (tempCheck != null && tempCheck.Count > 0 && tempCheck.FirstOrDefault().IsStatic))
                        return null;

                    #endregion

                    var allResources = await projectsCollection.Find(x => x.ProjectId == projectid && x.ResourceType == ResourceType.LINK, 
                                                                new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) })
                                                                .Project<KitsuneResource>(projection).ToListAsync();

                    var tempResourceDictionary = new Dictionary<string, int>();
                    foreach (var resource in allResources)
                    {
                        //To-Do re-optimize this case
                        if (resource.UrlPatternRegex == null)
                            resource.UrlPatternRegex = resource.OptimizedPath;

                        if (resource.UrlPatternRegex != null)
                        {
                            var pathMatchWeight = 0;
                            var pathMatchPercentage = 0;
                            var regexMatch = Helper.GetRegexValue(requestUrl, resource.UrlPatternRegex);
                            if (regexMatch.Success)
                                pathMatchWeight += regexMatch.Length;

                            var urlPatternRegexPathSplit = resource.UrlPatternRegex.TrimStart('/').Split('/');
                            var urlPatternRegexPathSplitCount = urlPatternRegexPathSplit.Count();

                            var urlPathSplit = requestUrl.TrimStart('/').Split('/');
                            var urlPathSplitCount = urlPathSplit.Count();

                            for (var index = 0; index < urlPatternRegexPathSplitCount; index++)
                            {
                                try
                                {
                                    if (index < urlPathSplitCount)
                                    {
                                        var regexPathValue = urlPatternRegexPathSplit[index];
                                        var urlPathValue = urlPathSplit[index];

                                        if ((!String.IsNullOrEmpty(regexPathValue) && !String.IsNullOrEmpty(urlPathValue)) && String.Compare(regexPathValue, urlPathValue, StringComparison.InvariantCultureIgnoreCase) == 0)
                                        {
                                            pathMatchWeight += (urlPatternRegexPathSplitCount - index);
                                        }
                                    }
                                }
                                catch { }
                            }

                            pathMatchPercentage = (pathMatchWeight / resource.UrlPatternRegex.Length) * 100;
                            tempResourceDictionary.Add(resource._id, pathMatchWeight);
                        }
                    }

                    try
                    {
                        var bestMatchValue = tempResourceDictionary.OrderByDescending(val => val.Value).FirstOrDefault();
                        if (bestMatchValue.Value > 0)
                        {
                            return allResources.Find(x => (x._id == bestMatchValue.Key));
                        }

                    }
                    catch
                    {
                        var bestMatchValue = tempResourceDictionary.OrderByDescending(val => val.Value).FirstOrDefault();
                        if (bestMatchValue.Value > 0)
                        {
                            return allResources.Find(x => (x._id == bestMatchValue.Key));
                        }
                    }
                }
                else
                    return response;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return null;
        }

        public static async Task<KitsuneResource> GetProjectResourceAsync(string projectid, string resourceId, KitsuneRequestUrlType kitsuneRequestUrlType = KitsuneRequestUrlType.PRODUCTION)
        {
            try
            {
                if (_server == null)
                    InitiateConnection();

                string collectionName = KLM_Constants.KitsuneResourcesProductionCollection;
                if (kitsuneRequestUrlType == KitsuneRequestUrlType.DEMO || kitsuneRequestUrlType == KitsuneRequestUrlType.PREVIEW)
                    collectionName = KLM_Constants.KitsuneResourcesCollection;

                var resourceCollection = _kitsuneDB.GetCollection<KitsuneResource>(collectionName);
                //optimized resource fatching
                var projection = new ProjectionDefinitionBuilder<KitsuneResource>()
                    .Include(x => x.OptimizedPath)
                    .Include(x => x.SourcePath)
                    .Include(x => x.UrlPattern)
                    .Include(x => x.UrlPatternRegex)
                    .Include(x => x.PageType)
                    .Include(x => x.IsStatic);
                var resource = await resourceCollection.Find(x => x._id.Equals(resourceId), new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).Project<KitsuneResource>(projection).FirstOrDefaultAsync();

                
                if (resource != null)
                    return resource;
                else
                    throw new Exception($"ResourceId : {resourceId} not found in DB");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static async Task<KitsuneResource> GetProjectDefaultResourceAsync(string projectid, KitsuneRequestUrlType kitsuneRequestUrlType = KitsuneRequestUrlType.PRODUCTION)
        {
            try
            {
                if (_server == null)
                    InitiateConnection();

                string collectionName = KLM_Constants.KitsuneResourcesProductionCollection;
                IMongoCollection<KitsuneResource> resourceCollection;

                if (kitsuneRequestUrlType == KitsuneRequestUrlType.DEMO || kitsuneRequestUrlType == KitsuneRequestUrlType.PREVIEW)
                    collectionName = KLM_Constants.KitsuneResourcesCollection;

                resourceCollection = _kitsuneDB.GetCollection<KitsuneResource>(collectionName);
                //optimized resource fatching
                var projection = new ProjectionDefinitionBuilder<KitsuneResource>()
                    .Include(x => x.OptimizedPath)
                    .Include(x => x.SourcePath)
                    .Include(x => x.UrlPattern)
                    .Include(x => x.UrlPatternRegex)
                    .Include(x => x.PageType)
                    .Include(x => x.IsStatic);
                KitsuneResource resource = await resourceCollection.Find(x => x.IsDefault && x.ProjectId.Equals(projectid), new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) })
                    .Project<KitsuneResource>(projection).FirstOrDefaultAsync();

                if (resource != null)
                    return resource;
                else
                    throw new Exception($"Default resource for ProjectId : {projectid} and not found in DB");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static async Task<string> GetDeveloperIdFromProjectIdAsync(string projectId, KitsuneRequestUrlType kitsuneRequestUrlType = KitsuneRequestUrlType.PRODUCTION)
        {
            try
            {
                if (_server == null)
                    InitiateConnection();

                var collectionName = KLM_Constants.KitsuneProjectProductionCollection;
                if (kitsuneRequestUrlType == KitsuneRequestUrlType.DEMO || kitsuneRequestUrlType == KitsuneRequestUrlType.PREVIEW)
                    collectionName = KLM_Constants.KitsuneProjectCollection;

                var collection = _kitsuneDB.GetCollection<ProductionKitsuneProject>(collectionName);
                var project = Builders<ProductionKitsuneProject>.Projection;
                var cursor = await collection.Find(x => x.ProjectId == projectId, 
                                                new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) })
                                                .Project<ProductionKitsuneProject>(project.Include(x => x.UserEmail)).Limit(1).FirstAsync();

                if (!String.IsNullOrEmpty(cursor.UserEmail))
                {
                    var usersCollection = _kitsuneDB.GetCollection<UserModel>("users");

                    var usersProject = Builders<UserModel>.Projection;
                    var usersCursor = await usersCollection.Find(x => x.Email == cursor.UserEmail, 
                                                new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) })
                                                .Project<UserModel>(usersProject.Include(x => x._id)).Limit(1).FirstAsync();

                    return usersCursor._id;
                }
            }
            catch (Exception ex)
            {
            }
            return null;
        }

        public static async Task<string> GetCustomerEmailAsync(string domain)
        {
            try
            {
                if (_server == null)
                    InitiateConnection();

                var websiteCollection = _kitsuneDB.GetCollection<KitsuneWebsiteCollection>(KLM_Constants.KitsuneWebsiteCollection);
                var websiteUserCollection = _kitsuneDB.GetCollection<KitsuneWebsiteUserCollection>(KLM_Constants.KitsuneWebsiteUserCollection);

                var project = new ProjectionDefinitionBuilder<KitsuneWebsiteUserCollection>();
                domain = domain.Trim(' ').ToUpper();

                var websiteId = await websiteCollection.Find(x => x.WebsiteUrl.Equals(domain), 
                    new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).Project(x => x._id).FirstOrDefaultAsync();

                if (websiteId != null)
                {
                    var user = await websiteUserCollection.Find(x => x.WebsiteId == websiteId, 
                        new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).FirstOrDefaultAsync();

                    return user?.Contact?.Email;
                }
            }
            catch (Exception ex)
            {
            }
            return null;

        }

        #endregion

        #region Schema Related Query

        public static async Task<string> GetDeveloperIdFromSchemaIdAsync(string schemaId)
        {
            try
            {
                if (_kitsuneSchemaServer == null)
                    InitiateConnection();

                var customerCollection = _kitsuneSchemaDB.GetCollection<KLanguageModel>("KitsuneLanguages");

                var project = Builders<KLanguageModel>.Projection;
                var cursor = await customerCollection.Find(x => x._id == schemaId, new FindOptions { MaxTime = TimeSpan.FromMilliseconds(KLM_Constants.MongoQueryMaxtimeOut) }).Project<KLanguageModel>(project.Include(x => x.UserId)).Limit(1).FirstAsync();

                return cursor.UserId;
            }
            catch (Exception ex)
            {
            }
            return null;
        }
        
        #endregion

    }
}
