using Amazon;
using Kitsune.Identifier.Constants;
using Kitsune.Identifier.Models;
using KitsuneLayoutManager.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Kitsune.Identifier.Helpers
{
	public static class APIHelper
	{
		private static string EmailAPI { get { return "https://api.withfloats.com/Internal/v1/PushEmailToQueue/4C627432590419E9CF79252291B6A3AE25D7E2FF13347C6ACD1587CC93FC322"; } }
		private static string ClientId { get { return "4C627432590419E9CF79252291B6A3AE25D7E2FF13347C6ACD1587CC93FC322"; } }

		public static string getSitemapUri = Constants.KitsuneAPIDomain + "/api/Conversion/v1/GetSiteMapOfWebite?domain={0}&projectId={1}&websiteId={2}";
		public static string getKSearch = Constants.KitsuneAPIDomain + "/api/Conversion/v1/GetUrlsForKeywords?domain={0}&keyword={1}";
		private static string getRoutingObjectEndpoint = Constants.KitsuneRoutingApiDomain + "/match";
		private static string createRoutingTreeEndpoint = Constants.KitsuneRoutingApiDomain + "/update";
		private static string RIAUnSubscribeEndpoint = Constants.RIAAPIDomain + "/service/kitsuneUnsubscribe?websiteUserId={0}&channel={1}";
		private static string getDyanmicKSearchEndpoint = Constants.KitsuneAPIDomain + "/language/v1/ksearch/{0}?websiteid={1}&skip={2}&limit=20";
		private static string getKitsuneSettingsEndPoint = Constants.KitsuneAPIDomain + "/api/project/v1/getkitsunesettings?projectid={0}&version={1}";

		internal static FileContent GetByteStreamFromKitsuneStorage(string storageUrl)
		{
			try
			{
				if (!String.IsNullOrEmpty(storageUrl))
				{
					byte[] byteArray = null;
					var contentType = string.Empty;
					var contentEncoding = string.Empty;

					using (var wc = new WebClient())
					{
						byteArray = wc.DownloadData(storageUrl);
						contentType = wc.ResponseHeaders["Content-Type"];
						contentEncoding = wc.ResponseHeaders["Content-Encoding"];
					}

					return new FileContent()
					{
						contentStream = byteArray,
						contentEncoding = contentEncoding,
						contentType = contentType
					};
				}
			}
			catch (Exception ex)
			{
				//  EventLogger.Write(ex, String.Format("Error in Identifier-GetFileStreamFromKitsuneStorage : {0}", ex.Message), requestUrl: storageUrl);
			}
			return null;
		}

		internal static string GetSitemap(string projectId, string websiteId, string domain)
		{
			if (String.IsNullOrEmpty(projectId) || String.IsNullOrEmpty(websiteId) || String.IsNullOrEmpty(domain))
				return null;//TODO: Return an empty sitemap 

			try
			{
				var request = (HttpWebRequest)WebRequest.Create(String.Format(getSitemapUri, domain, projectId, websiteId));
				request.Method = "GET";
				string sitemap = null;
				var response = request.GetResponse();

				using (var reader = new StreamReader(response.GetResponseStream(), System.Text.Encoding.ASCII))
				{
					sitemap = reader.ReadToEnd();
				}

				//Ensuring that the API returns a valid crawlId, so checking the length to atleast 2
				if (sitemap.Length > 2)
				{
					sitemap = Regex.Unescape(sitemap);
					sitemap = sitemap.Trim('\"');
					return sitemap;
				}
				else
					return null;
			}
			catch { }
			return null;
		}

		public static KSearchModel GetKSearchObject(string domain, string queryString)
		{
			if (String.IsNullOrEmpty(domain) || String.IsNullOrEmpty(queryString))
				return null;

			try
			{
				var request = (HttpWebRequest)WebRequest.Create(String.Format(getKSearch, domain, queryString));
				request.Method = "GET";
				string kSearchString = null;
				var response = request.GetResponse();

				using (var reader = new StreamReader(response.GetResponseStream(), Encoding.ASCII))
				{
					kSearchString = reader.ReadToEnd();
				}
				if (String.IsNullOrEmpty(kSearchString))
					return null;

				KSearchModel ksearchJsonObject = JsonConvert.DeserializeObject<KSearchModel>(kSearchString);
				return ksearchJsonObject;
			}
			catch { }
			return null;
		}

		public static KDynamicSearch GetKDynamicSearchObject(string websiteId, string queryString, string developerid, int skipBy = 0)
		{
			if (String.IsNullOrEmpty(websiteId) || String.IsNullOrEmpty(queryString))
				return null;

			try
			{
				skipBy = skipBy * 20;
				var request = (HttpWebRequest)WebRequest.Create(String.Format(getDyanmicKSearchEndpoint, queryString, websiteId, skipBy));
				request.Method = "GET";
				request.Headers.Add(HttpRequestHeader.Authorization, developerid);

				string kSearchString = null;
				var response = request.GetResponse();

				using (var reader = new StreamReader(response.GetResponseStream(), Encoding.ASCII))
				{
					kSearchString = reader.ReadToEnd();
				}
				if (String.IsNullOrEmpty(kSearchString))
					return null;

				var ksearchJsonObject = JsonConvert.DeserializeObject<KDynamicSearch>(kSearchString);
				return ksearchJsonObject;
			}
			catch { }
			return null;
		}

		public static string SendEmail(KitsuneConvertMailRequest req)
		{
			try
			{
				var request = (HttpWebRequest)WebRequest.Create(EmailAPI);
				request.Method = "POST";
				request.ContentType = "application/json";

				var jsonSerializer = new DataContractJsonSerializer(typeof(KitsuneConvertMailRequest));
				var mem = new MemoryStream();
				jsonSerializer.WriteObject(mem, req);

				string finalData = Encoding.UTF8.GetString(mem.ToArray(), 0, (int)mem.Length);
				var bytes = new UTF8Encoding().GetBytes(finalData);

				using (Stream stream = request.GetRequestStream())
				{
					stream.Write(bytes, 0, bytes.Length);
				}

				WebResponse ws = request.GetResponse();
				var sr = new StreamReader(ws.GetResponseStream());
				var rs = sr.ReadToEnd();
				return rs;
			}
			catch (Exception ex)
			{
				return null;
				//EventLogger.Write(ex, "NowFloats.Boost Exception: Unable to SendMailToServer", null);
			}
		}

		/// <summary>
		/// Gets the resource details for a given url
		/// </summary>
		/// <param name="projectId">ProjectId</param>
		/// <param name="path">Only path of the url is being sent not the entire url </param>
		/// <param name="kitsuneRequestUrlType">To Identify DEMO | PREVIEW | PRODUCTION</param>
		/// <returns>
		/// Return null if file not found
		/// Throws error if Failed fetching route
		/// </returns>
		public static RoutingObjectModel GetRoutingObject(string projectId, string path, KitsuneRequestUrlType kitsuneRequestUrlType = KitsuneRequestUrlType.PRODUCTION, bool createIfNotExists = false)
		{
			if (String.IsNullOrEmpty(projectId))
				throw new ArgumentNullException(nameof(projectId));

			if (String.IsNullOrEmpty(path))
				throw new ArgumentNullException(nameof(path));

			try
			{
				var requestType = kitsuneRequestUrlType.Equals(KitsuneRequestUrlType.PRODUCTION) ? 1 : 0;
				var routingRequestParams = new String[] { requestType.ToString(), projectId, path };

				var response = AWSLambdaHelpers.InvokeAWSLambda(Constants.RoutingMatcherFunctionName, string.Join("\n", routingRequestParams), RegionEndpoint.GetBySystemName(IdentifierEnvironmentConstants.IdentifierConfigurations.RoutingLambdaCredentials.Region)).Result;
				var responseObject = JsonConvert.DeserializeObject<dynamic>(response);
				if (responseObject["body"] != null)
				{
					var responseData = ((string)responseObject["body"]).Split('\n');
					var resp = new RoutingObjectModel()
					{
						File = responseData[2],
						ResourceId = responseData[1],
						RedirectPath = responseData[3],
						StatusCode = int.Parse(responseData[0])
					};
					if (resp.StatusCode == 500 && createIfNotExists == true) //500 means routing tree not found. 
					{
						CreateRouteTree(projectId, path, kitsuneRequestUrlType);
						return GetRoutingObject(projectId, path, kitsuneRequestUrlType, false);
					}
					return resp;
				}

				throw new Exception($"ProjectId : {projectId} and SourcePath : {path}, Error : Unable to Retrieve the Message");
			}
			catch (Exception ex)
			{
				throw new Exception($"ProjectId : {projectId} and SourcePath : {path}, Error : Error from server with message : {ex.Message}");
			}
		}

		/// <summary>
		/// Gets the resource details for a given url
		/// </summary>
		/// <param name="projectId">ProjectId</param>
		/// <param name="path">Only path of the url is being sent not the entire url </param>
		/// <param name="kitsuneRequestUrlType">To Identify DEMO | PREVIEW | PRODUCTION</param>
		/// <returns>
		/// Return null if file not found
		/// Throws error if Failed fetching route
		/// </returns>
		public static void CreateRouteTree(string projectId, string path, KitsuneRequestUrlType kitsuneRequestUrlType = KitsuneRequestUrlType.PRODUCTION)
		{
			if (String.IsNullOrEmpty(projectId))
				throw new ArgumentNullException(nameof(projectId));

			if (String.IsNullOrEmpty(path))
				throw new ArgumentNullException(nameof(path));

			try
			{

				var requestType = kitsuneRequestUrlType.Equals(KitsuneRequestUrlType.PRODUCTION) ? 1 : 0;
				var temp = "{\"ProjectId\":\"" + projectId + "\"}";

				var routingRequestParams = new String[] { requestType.ToString(), projectId, "new_KitsuneResourcesProduction", temp };

				var response = AWSLambdaHelpers.InvokeAWSLambda(Constants.RoutingCreateFunctionName, string.Join("\n", routingRequestParams), RegionEndpoint.GetBySystemName(IdentifierEnvironmentConstants.IdentifierConfigurations.RoutingLambdaCredentials.Region)).Result;
				var responseObject = JsonConvert.DeserializeObject<dynamic>(response);
				if (responseObject["body"] != null)
				{
					var responseData = ((string)responseObject["body"]).Split('\n');
					if (responseData.Length <= 0 || responseData[0] != "200")
						throw new Exception(message: $"ProjectId : {projectId} and SourcePath : {path}, Error : Unable to create routing tree with message {(responseData.Length > 1 ? responseData[1] : "")}");
				}
				else
				{
					throw new Exception($"ProjectId : {projectId} and SourcePath : {path}, Error : Unable to create routing tree");
				}
			}
			catch (WebException webException)
			{
				HttpWebResponse response = (HttpWebResponse)webException.Response;
				switch (response.StatusCode)
				{
					case HttpStatusCode.NotFound:
						break;
				}
				throw new Exception($"ProjectId : {projectId} and SourcePath : {path}, Error : Error from server with ResponseCode : {response.StatusCode}");
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		/// <summary>
		/// RIA api to unsubscribe Kitsune
		/// </summary>
		/// <param name="websiteUserId"></param>
		/// <param name="channel"></param>
		/// <returns></returns>
		public static bool UnSubscribeRIA(string websiteUserId, string channel)
		{
			if (String.IsNullOrEmpty(websiteUserId))
				throw new ArgumentNullException(nameof(websiteUserId));

			if (String.IsNullOrEmpty(channel))
				throw new ArgumentNullException(nameof(channel));

			try
			{
				var request = (HttpWebRequest)WebRequest.Create(String.Format(RIAUnSubscribeEndpoint, websiteUserId, channel));
				request.Method = "GET";
				request.Timeout = 1000;

				var response = (HttpWebResponse)request.GetResponse();
				if (response.StatusCode == HttpStatusCode.OK)
				{
					return true;
				}
				return false;
			}
			catch (WebException webException)
			{
				return false;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public static dynamic GetKitsuneResources(string projectId, int version)
		{
			if (String.IsNullOrEmpty(projectId))
				return null;

			try
			{
				var request = (HttpWebRequest)WebRequest.Create(String.Format(getKitsuneSettingsEndPoint, projectId, version.ToString()));
				request.Method = "GET";

				var response = request.GetResponse();
				string result;
				using (var reader = new StreamReader(response.GetResponseStream(), Encoding.ASCII))
				{
					result = reader.ReadToEnd();
				}
				if (String.IsNullOrEmpty(result))
					return null;

				var settingsJsonObject = JsonConvert.DeserializeObject<dynamic>(result);
				return settingsJsonObject;
			}
			catch { }
			return null;
		}
	}
}
