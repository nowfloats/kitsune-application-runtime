using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using Kitsune.Server.Model.Kitsune;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using KitsuneLayoutManager.Models;
using System.Web;
using KitsuneLayoutManager.Models.Ria;
using Kitsune.Models;
using System.Diagnostics;
using Kitsune.Language.Models;
using System.Web.Script.Serialization;
using System.Threading.Tasks;

namespace KitsuneLayoutManager.Helper
{
    public class ApiHelper
    {
        private static string KitsuneServerUrl = Constant.KitsuneApiDomain;
     
        #region THIRD PARTY APIS

        internal static async Task<dynamic> GetResponseFromKScriptAsync(string endPoint, Dictionary<string, string> headers = null, bool isCacheEnabled = false, string rootAliasUri = null, Dictionary<string, long> functionLog = null, string websiteid = null)
		{
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
			try
			{
                string response = string.Empty;
                if (isCacheEnabled)
                {
                    response = CacheHelper.GetThirdPartyAPIResponseFromCache(endPoint, isCacheEnabled, rootAliasUri);
                }

                if (String.IsNullOrEmpty(response))
                {
                    HttpClient client = new HttpClient();
                    var headerString = string.Empty;
                    var credentials = "{username}:{password}";

                    try
                    {
                        var tempUri = new Uri(endPoint);
                        var dnsHost = tempUri.DnsSafeHost?.ToUpper();
                        if (dnsHost.Contains("KITSUNE.TOOLS"))
                        {
                            if (String.IsNullOrEmpty(tempUri.Query))
                            {
                                endPoint += $"?k_websiteid={websiteid}";
                            }
                            else if (!String.IsNullOrEmpty(tempUri.Query))
                            {
                                endPoint += $"&k_websiteid={websiteid}";
                            }
                        }
                    }
                    catch { }

                    client.DefaultRequestHeaders.Add("User-Agent", "Kitsune-HTMLGEN/1.0");

                    if (headers != null && headers.Any())
                    {
                        foreach (var key in headers.Keys)
                        {
                            if (key.ToLower().Contains("username"))
                            {
                                credentials = credentials.Replace("{username}", headers[key]);
                                headerString = headerString + headers[key];
                            }
                            else if (key.ToLower().Contains("password"))
                            {
                                credentials = credentials.Replace("{password}", headers[key]);
                                headerString = headerString + headers[key];
                            }
                            else
                            {
                                client.DefaultRequestHeaders.Add(key, headers[key]);
                            }

                        }
                    }

                    if (!string.IsNullOrEmpty(headerString))
                    {
                        var byteArray = new UTF8Encoding().GetBytes(credentials);
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                    }

                    var url = endPoint;
                    client.Timeout = TimeSpan.FromSeconds(30.0);

                    var result = await client.GetAsync(url);

                    if (result.StatusCode == HttpStatusCode.OK)
                    {
                        response = await result.Content.ReadAsStringAsync();

                        if (isCacheEnabled)
                        {
                            CacheHelper.UpdateThirdPartyAPI(endPoint, response, rootAliasUri);
                        }
                    }
                }

                var finalValue = JsonConvert.DeserializeObject<dynamic>(response);

                //Kstring result will be always wrapped inside the kresult object
                dynamic kresultDynamic = JObject.Parse("{kresult : null}");
                kresultDynamic.kresult = finalValue;
                return kresultDynamic;
            }
			catch (Exception ex)
			{

			}
            finally
            {
                stopwatch.Stop();
                Helper.UpdateFunctionLog(functionLog, string.Format(Constant.KSCRIPT_API_RESPONSE, endPoint), stopwatch.ElapsedMilliseconds);
            }

			return null;
		}


        #region KAppData 
        internal static dynamic GetComponentsData(List<Kitsune.Models.Project.ProjectComponent> components, string projectId, string websiteId, string requestUrl, string s3SettingsRootPath = null, string rootaliasurl = null, KitsuneRequestUrlType kitsuneRequestUrlType = KitsuneRequestUrlType.PRODUCTION)
        {
            dynamic kresultDynamic = JObject.Parse("{}");
           
            //var demoProject = MongoConnector.MongoHelper.GetProjectDetailsAsync(projectId, kitsuneRequestUrlType).Result;

            var riaArgsModel = ParseRiaSettingsJSON(requestUrl, s3SettingsRootPath);

            foreach (var component in components)
            {
                switch (component.ProjectId)
                {
                    //RIA App ID
                    case "5ab5190ba35c3b04e9817cb5":
                        {
                            try
                            {
                                var project = MongoConnector.MongoHelper.GetKitsuneProductionProjectDetails(projectId);
                                kresultDynamic["_" + component.SchemaId] = (JToken)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(RiaHelper.GetRIAAppData(project, websiteId, riaArgsModel, rootaliasurl)));
                            }
                            catch (Exception ex)
                            {
                               
                            }
                        }
                        break;
                        //super_app : dummy data
                    case "5ab5190ba35c3b04e9817cb7":
                        {
                            try
                            {
                                kresultDynamic["_" + component.SchemaId] = (JToken)JsonConvert.DeserializeObject("{'name' : 'jio_user_name', 'email' : 'jio_user_email', 'mobile_number' : '8XXXXXXX789', 'profile_pic' : 'https://xyz.com/user/zyx/profile.jpg', 'is_wallet_active' : true, 'jio_id' : '1234567890' }");
                            }
                            catch(Exception ex)
                            {

                            }
                        }
                        break;
                }
            }
            return kresultDynamic;
        }

        internal static RiaArgsModel ParseRiaSettingsJSON(string requestUrl, string s3SettingsRootPath)
        {
            try
            {

                Uri rUrl = new Uri(requestUrl);
                var args = HttpUtility.ParseQueryString(rUrl.Query);
                if (args.AllKeys.Contains("ria_args"))
                {
                    return JsonConvert.DeserializeObject<RiaArgsModel>(Encoding.UTF8.GetString(Convert.FromBase64String(args["ria_args"])));
                }
            }
            catch { }
            return null;
        }

        #endregion

        #endregion


		#region WEBACTION APIS

		private static string WebActionEndPoint = "https://webactions.kitsune.tools/api/v1/{0}/get-data?{1}";
		private static string WebActionListEndPoint = "https://webactions.kitsune.tools/api/v1/List";
		//TODO : Store user and theme mapping in cache 
		private static Dictionary<string, string> userMapping = new Dictionary<string, string>();
		internal static async System.Threading.Tasks.Task<dynamic> GetWebActionsDataAsync(string authId, string widgetName, string fpId, string themeid, bool isCacheEnabled = false)
		{
			try
			{
				if (!String.IsNullOrEmpty(widgetName))
				{
					//if (isCacheEnabled)
					//{
					//    var response = CacheHelper.GetThirdPartyAPIResponseFromCache("WEBACTIONS-" + widgetName.ToUpper() + fpId.ToUpper(), isCacheEnabled);
					//    if (!string.IsNullOrEmpty(response))
					//    {
					//        return JsonConvert.DeserializeObject<dynamic>(response);
					//    }
					//}
					//if (!userMapping.ContainsKey(themeid))
					//{
					//    HttpClient listclient = new HttpClient();
					//    listclient.DefaultRequestHeaders.Add("Authorization", userMapping[themeid]);
					//    var listresult = listclient.GetAsync(WebActionListEndPoint).Result;                        
					//    if (listresult.StatusCode == HttpStatusCode.OK)
					//    {
					//        var response = listresult.Content.ReadAsStringAsync().Result;
					//        if (response != null)
					//        {
					//            var finalValue = JObject.Parse(response);
					//            JToken value = null;
					//            if (finalValue.TryGetValue("Token", out value))
					//            {
					//                userMapping.Add(themeid, value.ToString());
					//            }

					//        }
					//    }
					//    else
					//        EventLogger.Write(System.Diagnostics.TraceLevel.Error, "GetWebActionsList api failure : " + listresult.Content.ReadAsStringAsync().Result);
					//}

					//if (userMapping.ContainsKey(themeid))
					if (!String.IsNullOrEmpty(authId))
					{
						HttpClient client = new HttpClient();
						client.DefaultRequestHeaders.Add("Authorization", authId);
						client.DefaultRequestHeaders.Add("ContentType", "application/json");
						var url = string.Format(WebActionEndPoint, widgetName.ToLower(), "query={WebsiteId:'" + fpId + "'}");
						var result = await client.GetAsync(url);
						if (result.StatusCode == HttpStatusCode.OK)
						{
							var response = await result.Content.ReadAsStringAsync();
							var finalValue = JsonConvert.DeserializeObject<dynamic>(response);

							//if (isCacheEnabled)
							//{
							//    CacheHelper.UpdateThirdPartyAPI("WEBACTIONS-" + widgetName.ToUpper() + fpId.ToUpper(), response);
							//}

							return finalValue;
						}
					}
				}
			}
			catch (Exception ex)
			{
				EventLogger.Write(ex, "KLM exception occured while GetWebActionsData: " + widgetName);
			}

			return null;
		}

		#endregion

		#region LANGUAGE API's

		private static string GetWebsiteDataEndPointFromOldAPI = "{0}/language/v1/{2}/get-data?website={1}";
		private static string GetSchemaForWebsiteEndPointFromOldAPI = "{0}/language/v1/GetWebsiteSchema?websiteid={1}";
        private static string GetBusinessDataWithMetaInfoAPI = "{0}/language/v1/{1}/get-file-data?projectId={2}&websiteId={3}&filePath={4}&currentPageNumber={5}";
        private static string GetMetaInfoAPI = "{0}/api/Project/v1/MetaInfo?projectId={1}&sourcePath={2}";
        private static string GetBusinessDataFromPropertyListAPI = "{0}/language/v1/{1}/get-data-by-property";

        internal static dynamic GetBusinessDataFromOldAPI(string websiteName, string schemaName)
		{
			try
			{
				var fpDataRequest = (HttpWebRequest)WebRequest.Create(new Uri(String.Format(GetWebsiteDataEndPointFromOldAPI, KitsuneServerUrl, websiteName.ToLower(), schemaName.ToLower())));
				fpDataRequest.Method = "GET";
				fpDataRequest.ContentType = "application/json";
				fpDataRequest.Headers.Add(HttpRequestHeader.Authorization, "");


				var ws = fpDataRequest.GetResponse();
				StreamReader sr = new StreamReader(ws.GetResponseStream());
				var response = sr.ReadToEnd().ToString();
				if (!String.IsNullOrEmpty(response))
				{
                    //JavaScriptSerializer responseJsonData = new JavaScriptSerializer();
                    //var output = responseJsonData.Deserialize<dynamic>(response);
                    var output = JsonConvert.DeserializeObject<dynamic>(response);
                    return output["Data"][0];
				}
			}
			catch (Exception ex)
			{
				EventLogger.Write(ex, "KLM exception occured while GetBusinessData: " + websiteName);
			}

			return null;
		}
        
        internal static dynamic GetBusinessDataWithMetaInfo(string projectId, string websiteName, string schemaName, string filePath, string currentPageNumber, string developerId)
        {
            try
            {
                var fpDataRequest = (HttpWebRequest)WebRequest.Create(new Uri(String.Format(GetBusinessDataWithMetaInfoAPI, KitsuneServerUrl, schemaName.ToLower(), projectId.ToLower(), websiteName.ToLower(), filePath.ToLower(), currentPageNumber.ToLower())));
                fpDataRequest.Method = "GET";
                fpDataRequest.ContentType = "application/json";
                fpDataRequest.Headers.Add(HttpRequestHeader.Authorization, developerId);


                var ws = fpDataRequest.GetResponse();
                StreamReader sr = new StreamReader(ws.GetResponseStream());
                var response = sr.ReadToEnd().ToString();
                if (!String.IsNullOrEmpty(response))
                {
                    //JavaScriptSerializer responseJsonData = new JavaScriptSerializer();
                    //var output = responseJsonData.Deserialize<dynamic>(response);
                    var output = JsonConvert.DeserializeObject<dynamic>(response);
                    return output["Data"][0];
                }
            }
            catch (Exception ex)
            {
                EventLogger.Write(ex, "KLM exception occured while GetBusinessData: " + websiteName);
            }

            return null;
        }

        internal static dynamic GetMetaInfoFromAPI(string projectId, string sourcePath, string developerId)
        {
            try
            {
                var fpDataRequest = (HttpWebRequest)WebRequest.Create(new Uri(String.Format(GetMetaInfoAPI, KitsuneServerUrl, projectId, sourcePath.ToLower())));
                fpDataRequest.Method = "GET";
                fpDataRequest.ContentType = "application/json";
                fpDataRequest.Headers.Add(HttpRequestHeader.Authorization, developerId);


                var ws = fpDataRequest.GetResponse();
                StreamReader sr = new StreamReader(ws.GetResponseStream());
                var response = sr.ReadToEnd().ToString();
                if (!String.IsNullOrEmpty(response))
                {
                    var output = JsonConvert.DeserializeObject<dynamic>(response);
                    return output;
                }
            }
            catch (Exception ex)
            {
                EventLogger.Write(ex, "KLM exception occured while GetMetaInfoFromAPI SourcePath: " + sourcePath + " , ProjectId: " + projectId);
            }

            return null;
        }

        internal static string GetSchemaForWebsiteFromOldAPI(string name)
		{
			try
			{
				var fpDataRequest = (HttpWebRequest)WebRequest.Create(new Uri(String.Format(GetSchemaForWebsiteEndPointFromOldAPI, KitsuneServerUrl, name)));
				fpDataRequest.Method = "GET";
				fpDataRequest.ContentType = "application/json";


				var ws = fpDataRequest.GetResponse();
				StreamReader sr = new StreamReader(ws.GetResponseStream());
				var response = sr.ReadToEnd().ToString();
				if (!String.IsNullOrEmpty(response))
				{
					return response;
				}
			}
			catch (Exception ex)
			{
				EventLogger.Write(ex, "KLM exception occured while GetSchemaForWebsite: " + name);
			}

			return null;
		}

        internal static dynamic GetBusinessDataFromPropertyList(List<PropertyPathSegment> propertyList, string schemaId, string WebsiteId, string developerId)
        {
            try
            {
                var uri = new Uri(String.Format(GetBusinessDataFromPropertyListAPI, KitsuneServerUrl, schemaId.ToLower()));
                DataApiRequestObject requestData = new DataApiRequestObject();
                requestData.WebsiteId = WebsiteId;
                requestData.PropertySegments = propertyList;
                string jsonString = new JavaScriptSerializer().Serialize(requestData);
                byte[] array = Encoding.ASCII.GetBytes(jsonString);

                var request = (HttpWebRequest)WebRequest.Create(uri);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Headers.Add(HttpRequestHeader.Authorization, developerId);

                Stream stream = request.GetRequestStream();
                stream.Write(array, 0, array.Length);
                stream.Close();

                using (var response = (HttpWebResponse) request.GetResponse())
                {
                    if (response == null)
                        throw new Exception($"schemaId:{schemaId} and propertyList:{jsonString}, API call failed");
                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new Exception($"schemaId:{schemaId} and propertyList:{jsonString}, API call response status : {response.StatusDescription}");

                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        string objText = reader.ReadToEnd();
                        JavaScriptSerializer responseJsonData = new JavaScriptSerializer();
                        return responseJsonData.Deserialize<dynamic>(objText);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

		private static string GetWebsiteDataEndPointFromNewAPI = "{0}/language/v1/{2}/get-data?website={1}";
		private static string GetSchemaForWebsiteEndPointFromNewAPI = "{0}/language/v1/GetWebsiteSchema?websiteid={1}";

		internal static dynamic GetBusinessDataFromNewAPI(string websiteName, string schemaName, string customerId, string developerId)
		{
			try
			{
				var fpDataRequest = (HttpWebRequest)WebRequest.Create(new Uri(String.Format(GetWebsiteDataEndPointFromNewAPI, KitsuneServerUrl, customerId, schemaName.ToLower())));
				fpDataRequest.Method = "GET";
				fpDataRequest.ContentType = "application/json";
				fpDataRequest.Headers.Add(HttpRequestHeader.Authorization, developerId);


                var ws = fpDataRequest.GetResponse();
                StreamReader sr = new StreamReader(ws.GetResponseStream());
                var response = sr.ReadToEnd().ToString();
                if (!String.IsNullOrEmpty(response))
                {                    
                    var output = JsonConvert.DeserializeObject<dynamic>(response);
                    return output["Data"][0];
                }
            }
            catch (Exception ex)
            {
                EventLogger.Write(ex, "KLM exception occured while GetBusinessData: " + websiteName);
            }

			return null;
		}

		internal static string GetSchemaForWebsiteFromNewAPI(string name)
		{
			try
			{
				var fpDataRequest = (HttpWebRequest)WebRequest.Create(new Uri(String.Format(GetSchemaForWebsiteEndPointFromNewAPI, KitsuneServerUrl, name)));
				fpDataRequest.Method = "GET";
				fpDataRequest.ContentType = "application/json";


				var ws = fpDataRequest.GetResponse();
				StreamReader sr = new StreamReader(ws.GetResponseStream());
				var response = sr.ReadToEnd().ToString();
				if (!String.IsNullOrEmpty(response))
				{
					return response;
				}
			}
			catch (Exception ex)
			{
				EventLogger.Write(ex, "KLM exception occured while GetSchemaForWebsite: " + name);
			}

			return null;
		}

		#endregion

		#region K-Pay APIs

		internal static KPayCheckSumAPIResponse GetKPayEncodedCheckSum(string websiteId, List<string> amount)
		{
			if (String.IsNullOrEmpty(websiteId))
				throw new ArgumentNullException(nameof(websiteId));
			if (amount == null)
				throw new ArgumentNullException(nameof(amount));

			try
			{
				string url = String.Format(Constant.KPayEncodedCheckSumApi, Constant.KitsunePaymentDomain);
				Uri uri = new Uri(url);

				var postData = new { pepper = websiteId, amounts = amount };
                //string jsonString = new JavaScriptSerializer().Serialize(postData);
                string jsonString = JsonConvert.SerializeObject(postData);
                byte[] array = Encoding.ASCII.GetBytes(jsonString);

				var request = (HttpWebRequest)WebRequest.Create(uri);
				request.Method = "POST";
				request.ContentType = "application/json";

				Stream stream = request.GetRequestStream();
				stream.Write(array, 0, array.Length);
				stream.Close();

				using (var response = (HttpWebResponse)request.GetResponse())
				{
					if (response == null)
						throw new Exception($"WebsiteId:{websiteId} and amount:{amount}, CheckSum API call failed");
					if (response.StatusCode != HttpStatusCode.OK)
						throw new Exception($"WebsiteId:{websiteId} and amount:{amount}, CheckSum API call response status : {response.StatusDescription}");

					using (var reader = new StreamReader(response.GetResponseStream()))
					{
						string objText = reader.ReadToEnd();
                        //JavaScriptSerializer responseJsonData = new JavaScriptSerializer();
                        //var output = responseJsonData.Deserialize<KPayCheckSumAPIResponse>(objText);
                        var output = JsonConvert.DeserializeObject<KPayCheckSumAPIResponse>(objText);
                        if (output != null)
						{
							if (output.checksums != null)
							{
								return output;
							}
							else
							{
								throw new Exception($"WebsiteId:{ websiteId } and amount:{ amount}, CheckSum value was empty in API response");
							}
						}
						else
						{
							throw new Exception($"WebsiteId:{ websiteId } and amount:{ amount},API response data was null");
						}
					}
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}
        internal async static Task<KPayCheckSumAPIResponse>  GetKPayEncodedCheckSumAsync(string websiteId, List<string> amount)
        {
            if (String.IsNullOrEmpty(websiteId))
                throw new ArgumentNullException(nameof(websiteId));
            if (amount == null)
                throw new ArgumentNullException(nameof(amount));

            try
            {
                string url = String.Format(Constant.KPayEncodedCheckSumApi, Constant.KitsunePaymentDomain);
                Uri uri = new Uri(url);

                var postData = new { pepper = websiteId, amounts = amount };
                //string jsonString = new JavaScriptSerializer().Serialize(postData);
                string jsonString = JsonConvert.SerializeObject(postData);
                byte[] array = Encoding.ASCII.GetBytes(jsonString);

                var request = (HttpWebRequest)WebRequest.Create(uri);
                request.Method = "POST";
                request.ContentType = "application/json";

                Stream stream = await request.GetRequestStreamAsync();
                stream.Write(array, 0, array.Length);
                stream.Close();

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    if (response == null)
                        throw new Exception($"WebsiteId:{websiteId} and amount:{amount}, CheckSum API call failed");
                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new Exception($"WebsiteId:{websiteId} and amount:{amount}, CheckSum API call response status : {response.StatusDescription}");

                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        string objText = reader.ReadToEnd();
                        //JavaScriptSerializer responseJsonData = new JavaScriptSerializer();
                        //var output = responseJsonData.Deserialize<KPayCheckSumAPIResponse>(objText);
                        var output = JsonConvert.DeserializeObject<KPayCheckSumAPIResponse>(objText);
                        if (output != null)
                        {
                            if (output.checksums != null)
                            {
                                return output;
                            }
                            else
                            {
                                throw new Exception($"WebsiteId:{ websiteId } and amount:{ amount}, CheckSum value was empty in API response");
                            }
                        }
                        else
                        {
                            throw new Exception($"WebsiteId:{ websiteId } and amount:{ amount},API response data was null");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

    }
}