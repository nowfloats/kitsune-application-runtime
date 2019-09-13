using Kitsune.Identifier.Constants;
using Kitsune.Identifier.DataHandlers.Mongo;
using Kitsune.Identifier.Models;
using Kitsune.Models.KLM;
using Kitsune.Models.Project;
using KitsuneLayoutManager;
using KitsuneLayoutManager.Helper;
using KitsuneLayoutManager.Helper.MongoConnector;
using KitsuneLayoutManager.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using KitsuneLayoutManager.Helper.CacheHandler;
using static Kitsune.Identifier.Helpers.AWSHelper;

namespace Kitsune.Identifier.Helpers
{
	public class MiddlewareHelper
	{
		private const String ErrorPageContent = "The page you are requesting for, is not valid.";
		private readonly RequestDelegate _next;

		//TODOC : Get value from config file
		private static bool IsKLMWebCacheEnabled = IdentifierEnvironmentConstants.IdentifierConfigurations.KLMConfigurations.WebCache.IsEnabled;

		public MiddlewareHelper(RequestDelegate next)
		{
			_next = next ?? throw new ArgumentNullException(nameof(next));
		}

		public async Task InvokeAsync(HttpContext context)
		{
			if (context == null)
				throw new ArgumentNullException(nameof(context));

			try
			{
				if (context.Request.Path.Value.EndsWith("Home/Execute", StringComparison.InvariantCultureIgnoreCase))
				{
					context.Request.Path = "/Home/Execute";
					await _next.Invoke(context);
					return;
				}

				if (context.Request.Path.Value.EndsWith("home/kpreview", StringComparison.InvariantCultureIgnoreCase))
				{
					context.Request.Path = "/Home/kpreview";
					await _next.Invoke(context);
					return;
				}


				//TODOC : put in a proper place
				context.Response.Headers.Remove("Content-Encoding");
				context.Response.Headers.Remove("X-Powered-By");

				var request = context.Request;
                //TODO : Create the required url (Verify the hash values)
                //if (request.Query.ContainsKey("k-debug"))
                //{
                //    context.Response.Headers["Content-Type"] = "text";
                //    if (!context.Response.HasStarted)
                //        context.Response.StatusCode = 404;
                //    var tempResponse2 = new StringBuilder();
                //    foreach (var header in context.Request.Headers)
                //    {
                //        try
                //        {
                //            tempResponse2.Append(header + " : " + request.Headers[header.ToString()] + "\n");
                //        }
                //        catch { }
                //    }

                //    tempResponse2.Append("Server variables " + "\n");
                //    foreach (var variables in request.Headers)
                //    {
                //        try
                //        {
                //            tempResponse2.Append(variables + " : " + request.Headers[variables.ToString()] + "\n");
                //        }
                //        catch { }
                //    }
                //    await context.Response.WriteAsync(ErrorPageContent + "\n" + String.Join("", tempResponse2));
                //    return;
                //}

                Uri appRequestUri = null;
                if (request.Headers.ContainsKey("K_URL"))
                {
                    try
                    {
                        appRequestUri = new Uri(request.Headers["K_URL"]);
                        Console.WriteLine($"HEADER: k_url: {appRequestUri}");
                        Console.WriteLine($"HEADER: Host: {request.Headers["Host"]}");
                    }
                    catch { }
                }
                //FOR LOCAL DEBUGGING REQUESTS ONLY
                else if (request.Query.ContainsKey("k-url"))
                {
                    appRequestUri = request.Query.ContainsKey("isNFSite") ?
                        new Uri(request.Query["url"]) :
                        new Uri(request.Query["k-url"]);
                }
                //FOR AZURE
                else if (request.Headers.ContainsKey("X-HOST"))
                {
                    appRequestUri = new Uri($"{request.Scheme}://{request.Headers["x-host"]}{request.Path}{request.QueryString}");
                }
                else if (request.Headers.ContainsKey("x-host"))
                {
                    appRequestUri = new Uri($"{request.Scheme}://{request.Headers["x-host"]}{request.Path}{request.QueryString}");
                }
                else
                {
                    appRequestUri = new Uri($"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}");
                }

                if (request.Query.ContainsKey("isNFSite"))
                {
                    appRequestUri = new Uri(request.Query["url"]);
                }

                var absoluteUrl = appRequestUri.AbsoluteUri;
				var domainName = appRequestUri.DnsSafeHost.ToLower();
				var absolutePath = appRequestUri.AbsolutePath;
				var appRequestQuery = HttpUtility.ParseQueryString(appRequestUri.Query);

				//domainName = "redtim.com";

				//TODO: Re-Construct the absoluteUrlWithOutQueryParams http://abc.com
				var absoluteUrlWithoutQueryParameters = appRequestUri.GetLeftPart(UriPartial.Path);
				var kitsuneRequestUrlType = IdentifyRequestUrlType(domainName);

				//(Commenting this section as it has moved to Edge Lambda)
				#region LEGACY DOMAIN REQUEST MAPPER 
				//try
				//{
				//	if (LDRMProcessor.AllDomainOriginStrings.Contains(appRequestUri.Authority.ToUpper()))
				//	{
				//		var ldrmResponse = LDRMProcessor.HasLegacyMapping(appRequestUri.Authority, appRequestUri);
				//		if (ldrmResponse.match)
				//		{
				//			var isLDRMSuccessfull = await ProxyHelper.ProcessProxyHttpRequest(context, appRequestUri, ldrmResponse);
				//			if (isLDRMSuccessfull)
				//				return;
				//		}
				//	}
				//}
				//catch { }
				#endregion

				#region INITIALIZE PARAMETERS
				var paramList = new List<string>();
				var webCacheFileModel = new WebCacheFileModel();
				var projectId = String.Empty;
				var demoOrPreviewDomainId = String.Empty;
				var projectDetails = new ProjectDetails();
				var kitsuneDomainDetails = new KitsuneDomainDetails();
				var s3folderName = String.Empty;
				var s3ResourcePathUrl = String.Empty;
				var themePreviewRequest = false;

				//TODO: optimize to boolean
				var isNoCacheRequestEnabled = (kitsuneRequestUrlType == KitsuneRequestUrlType.PRODUCTION) ? appRequestQuery["no-cache"] : "true";
				var fptag = String.Empty;
				var httpProtocol = (request.IsHttps) ? "https://" : "http://";
				var rootaliasurl = String.Format("{0}{1}", httpProtocol, domainName);

				var htmlCode = string.Empty;
				bool isCacheContentAvailable = false;

				#endregion

				#region Clear the webcache of a website
				if (appRequestUri.AbsolutePath.EndsWith("k-clear-cache", StringComparison.InvariantCultureIgnoreCase) || appRequestUri.AbsolutePath.EndsWith("k-api/clear-cache", StringComparison.InvariantCultureIgnoreCase))
				{
					if (IsKLMWebCacheEnabled)
					{
						try
						{
							CacheHelper.DeleteCacheEntityWithUrl(absoluteUrl);
						}
						catch (Exception ex)
						{
							context.Response.WriteAsync(ex.ToString()).Wait();
						}

						context.Response.WriteAsync("true").Wait();
						return;
					}
				}
				#endregion

				#region CHECK IF CACHE IS ENABLED AND GET THE CACHE

				if (String.Compare(isNoCacheRequestEnabled, "true", true) != 0 && IsKLMWebCacheEnabled)
				{
					try
					{
						//TODO: review the cache
						var cacheResponse = CacheHelper.GetCacheEntityFromUrl(absoluteUrl, out string cacheKey);
						if (cacheResponse != null)
						{
							webCacheFileModel = cacheResponse;

							context.Response.Headers.TryAdd("x-kitsune-module", "web-cache");
							isCacheContentAvailable = true;

                            if (webCacheFileModel.IsStaticFile)
                            {
                                var staticRespCacheSettings = IdentifierEnvironmentConstants.IdentifierConfigurations.StaticResponse;
                                if (staticRespCacheSettings != null && staticRespCacheSettings.CacheEnabled)
                                {
                                    context.Response.Headers.TryAdd("Cache-Control", staticRespCacheSettings.CacheControlValue);
                                    context.Response.Headers.TryAdd(HttpResponseHeader.Expires.ToString(), DateTime.UtcNow.AddSeconds(staticRespCacheSettings.ExpiresInSecondsValue).ToString("R"));
                                }
                                else
                                {
                                    context.Response.Headers.TryAdd("Cache-Control", "no-cache");
                                    context.Response.Headers.TryAdd(HttpResponseHeader.Expires.ToString(), DateTime.UtcNow.AddSeconds(0).ToString("R"));
                                }
                            }
                        }
                        
                        
#if DEBUG
                        context.Response.Headers.TryAdd("x-kitsune-info", $"web-cache key at get:  {cacheKey}, {(cacheResponse == null ? "cache value null" : "cache value not null")}");
#endif
					}
					catch { }
				}

				#endregion

				if (!isCacheContentAvailable)
				{
					string websiteProtocol = string.Empty;

					#region GET DOMAIN DETAILS AND PROCESS THE REQUEST
					try
					{
						#region GETTING DOMAIN DETAILS
						if (kitsuneRequestUrlType == KitsuneRequestUrlType.DEMO || kitsuneRequestUrlType == KitsuneRequestUrlType.PREVIEW)
						{
							demoOrPreviewDomainId = domainName.Split('.')?.First();
						}

						//sub-path validation check for websites http://abc.com/hyd - 
						//this has to be here, because kitsuneDomainDetails will never be empty if GetDomainDetailsSync executes first
						if ((kitsuneDomainDetails == null || String.IsNullOrEmpty(kitsuneDomainDetails.Domain)) && appRequestUri.Segments != null && appRequestUri.Segments.Length > 1)
						{
							foreach (var segment in appRequestUri.Segments)
							{
								var tempSegment = segment.Replace("/", "");
								if (!String.IsNullOrEmpty(tempSegment))
								{
									kitsuneDomainDetails = await Utils.GetDomainDetailsAsync(String.Format("{0}/{1}", appRequestUri.DnsSafeHost.ToUpper(), tempSegment.ToUpper()), kitsuneRequestUrlType);

									if (kitsuneDomainDetails != null && !String.IsNullOrEmpty(kitsuneDomainDetails.Domain))
									{
										rootaliasurl = String.Format("{0}{1}/{2}", httpProtocol, appRequestUri.DnsSafeHost.ToLower(), tempSegment.ToLower());

										var tempAbsoluteUri = new Uri(rootaliasurl);
										if (!String.IsNullOrEmpty(absolutePath) && !String.IsNullOrEmpty(tempAbsoluteUri.AbsolutePath) && String.Compare(tempAbsoluteUri.AbsolutePath, "/") != 0)
											absolutePath = absolutePath.ToLower().Replace(tempAbsoluteUri.AbsolutePath.ToLower(), "");
									}

									break;
								}
							}
						}

						if (kitsuneDomainDetails == null || String.IsNullOrEmpty(kitsuneDomainDetails.Domain))
						{
							kitsuneDomainDetails = await Utils.GetDomainDetailsAsync(kitsuneRequestUrlType ==  KitsuneRequestUrlType.DEMO ? domainName.Split('.')[0] : domainName, kitsuneRequestUrlType);
						}

                        if (kitsuneDomainDetails != null)
                        {
                            try
                            {
                                bool? sslProtocol = null;
                                try
                                {
                                    if (request.Headers.ContainsKey("CloudFront-Forwarded-Proto"))
                                    {
                                        sslProtocol = (String.Compare(request.Headers["CloudFront-Forwarded-Proto"], "http") == 0) ? false : true;
                                    }
                                    else if (request.Headers.ContainsKey("cloudfront-forwarded-proto"))
                                    {
                                        sslProtocol = (String.Compare(request.Headers["cloudfront-forwarded-proto"], "http") == 0) ? false : true;
                                    }
                                    else if (request.Headers.ContainsKey("x-forwarded-proto"))
                                    {
                                        sslProtocol = (String.Compare(request.Headers["x-forwarded-proto"], "http") == 0) ? false : true;
                                    }
                                    else if (request.Headers.ContainsKey("X-Forwarded-Proto"))
                                    {
                                        sslProtocol = (String.Compare(request.Headers["X-Forwarded-Proto"], "http") == 0) ? false : true;
                                    }
                                }
                                catch { }

                                if (!sslProtocol.HasValue)
                                {
                                    sslProtocol = request.IsHttps;
                                }

                                if (kitsuneRequestUrlType == KitsuneRequestUrlType.PRODUCTION)
                                {
#if !DEBUG
                                    if (kitsuneDomainDetails.IsRedirect)
                                    {
                                        var redirectionUrlHTTPProtocol = (kitsuneDomainDetails.isSSLEnabled) ? "https://" : "http://";
                                        context.Response.StatusCode = 301;
                                        context.Response.Redirect($"{redirectionUrlHTTPProtocol}{kitsuneDomainDetails.RedirectUrl}", true);
                                        return;
                                    }
                                    else if (kitsuneDomainDetails.isSSLEnabled && !sslProtocol.Value)
									{
										//Redirect to HTTPS Endpoint
										context.Response.StatusCode = 301;
										context.Response.Redirect($"{absoluteUrl.Replace("http://", "https://")}", true);
										return;
									}
                                    else if (!kitsuneDomainDetails.isSSLEnabled && sslProtocol.Value)
									{
										//Redirect to HTTP Endpoint
										context.Response.StatusCode = 301;
										context.Response.Redirect($"{absoluteUrl.Replace("https://", "http://")}", true);
										return;
									}									
#endif

								}
                                if (sslProtocol.Value == false)
								{
									websiteProtocol = "http://";
									absoluteUrl = absoluteUrl.Replace("https://", "http://");
								}
								else
								{
									websiteProtocol = "https://";
								}

								//TO-DO re-check the parameters and remove this
								//                        var forwardedProtocol = string.Empty;
								//if (!String.IsNullOrEmpty(request.Headers["HTTP_CLOUDFRONT_FORWARDED_PROTO"]))
								//{
								//	forwardedProtocol = request.Headers["HTTP_CLOUDFRONT_FORWARDED_PROTO"];
								//}
								//else if (!String.IsNullOrEmpty(request.Headers["CloudFront-Forwarded-Proto"]))
								//{
								//	forwardedProtocol = request.Headers["CloudFront-Forwarded-Proto"];
								//}
								//else if (!String.IsNullOrEmpty(absoluteUrl) && absoluteUrl.StartsWith("https://"))
								//{
								//	forwardedProtocol = "https";
								//}

								//if (!String.IsNullOrEmpty(forwardedProtocol) && String.Compare(forwardedProtocol, "https", true) == 0)
								//{
								//	httpProtocol = "https://";
								//	absoluteUrl = absoluteUrl.Replace("http://", httpProtocol);
								//}
							}
							catch (Exception ex)
							{
								context.Response.WriteAsync(String.Format("<!-- HEADERS VALIDATION IN KLM() - Exception message: {0} \n Stacktrace: {1} -->", ex.Message, ex.StackTrace)).Wait();
							}

							#region CHECK FOR OLD NF URL AND REDIRECT TO NEW ONE (NFX)

							//if ((!String.IsNullOrEmpty(kitsuneDomainDetails.ClientId) && String.Compare(kitsuneDomainDetails.ClientId, "AC16E0892F2F45388F439BDE9F6F3FB5C31F0FAA628D40CD9814A79D884139E") == 0))
							//{
							//	try
							//	{
							//		fptag = kitsuneDomainDetails.WebsiteTag;

							//		if (!String.IsNullOrEmpty(kitsuneDomainDetails.WebsiteTag))
							//		{
							//			var redirectUrl = UrlHandler.GetRedirectionUrlForV6LinksViaAPI(kitsuneDomainDetails.WebsiteTag, appRequestUri.GetLeftPart(UriPartial.Path));
							//			if (!string.IsNullOrEmpty(redirectUrl))
							//			{
							//				var urlRightPart = appRequestUri.Query;
							//				if (!String.IsNullOrEmpty(urlRightPart?.Trim()))
							//				{
							//					redirectUrl += urlRightPart;
							//				}

							//				context.Response.StatusCode = 301;
							//				context.Response.Redirect(redirectUrl, true);
							//				return;
							//			}
							//		}
							//	}
							//	catch { }
							//}

							#endregion

							projectId = kitsuneDomainDetails.ProjectId;
							domainName = kitsuneDomainDetails.Domain;
							try
							{
								if (!string.IsNullOrWhiteSpace(appRequestUri.Query))
								{
									if (appRequestQuery.AllKeys.Contains("theme") && !string.IsNullOrWhiteSpace(appRequestQuery["theme"]))
									{
#if DEBUG
										context.Response.Headers.TryAdd("x-kitsune-theme", appRequestQuery["theme"]);
										context.Response.Headers.TryAdd("x-kitsune-uri", appRequestUri.ToString());
#endif
										projectId = GetThemeIdFromThemeName(appRequestQuery["theme"]) ?? kitsuneDomainDetails.ProjectId;
										themePreviewRequest = true;
									}
								}
							}
							catch { }

							s3folderName = $"{projectId}/v{kitsuneDomainDetails.Version}";
						}
						else
						{
							context.Response.Headers["Content-Type"] = "text";
							if (!context.Response.HasStarted)
								context.Response.StatusCode = 404;

							await context.Response.WriteAsync(ErrorPageContent);
							return;
						}
						#endregion
					}
					catch (Exception)
					{
						//TODOC : Chekck what to do
						//app.Context.Response.Write(String.Format("<!-- GET DOMAIN DETAILS() - Exception message: {0} \n Stacktrace: {1} -->", ex.Message, ex.StackTrace));
					}

					#endregion

					if (String.IsNullOrEmpty(projectId))
					{
						context.Response.Headers["Content-Type"] = "text";
						if (!context.Response.HasStarted)
							context.Response.StatusCode = 404;

						await context.Response.WriteAsync(ErrorPageContent);
						return;
					}

					#region EXTRACT PARAMETERS FROM URI

					if (appRequestUri.Segments != null && appRequestUri.Segments.Length > 1)
					{
						foreach (var segment in appRequestUri.Segments)
						{
							var tempSegment = segment.Replace("/", "");
							if (!String.IsNullOrEmpty(tempSegment))
							{
								paramList.Add(tempSegment.Trim().ToUpper());
							}
						}
					}

					#endregion

					projectDetails = await MongoHelper.GetProjectDetailsAsync(projectId, kitsuneRequestUrlType);

					if (projectDetails == null || projectDetails.BucketNames == null)
						throw new Exception($"Error:projectDetails was null for domain name:{domainName}");

					if (themePreviewRequest)
						s3folderName = $"{projectId}/v{projectDetails.Version}";

					var projectResourceDetails = new ResourceDetails();
					var bucketUrl = projectDetails.BucketNames.production;

					switch (kitsuneRequestUrlType)
					{
						case KitsuneRequestUrlType.DEMO:
							bucketUrl = projectDetails.BucketNames.demo;
							s3folderName = $"{projectId}/cwd";
							rootaliasurl = String.Format("{0}{1}.demo.getkitsune.com", httpProtocol, demoOrPreviewDomainId);
							domainName = String.Format("{0}.demo.getkitsune.com", demoOrPreviewDomainId);
							break;

						case KitsuneRequestUrlType.PREVIEW:
							bucketUrl = projectDetails.BucketNames.source;
							s3folderName = $"{projectId}";
							rootaliasurl = String.Format("{0}{1}.preview.getkitsune.com", httpProtocol, demoOrPreviewDomainId);
							domainName = String.Format("{0}.preview.getkitsune.com", demoOrPreviewDomainId);
							break;

						default:
							bucketUrl = projectDetails.BucketNames.production;
							break;
					}

					var s3FolderUrl = $"https://{bucketUrl}.s3-accelerate.amazonaws.com/{s3folderName}";

					#region REWRITE TO CONTROLLER IF PARAM == (K-SEARCH, K-CONTACT, K-SUBMITFORM, SITEMAP.XML)

					if (paramList.Count > 0)
					{
						switch (paramList[paramList.Count - 1])
						{
							case "KCSRF.JS":
								var fileContent = APIHelper.GetByteStreamFromKitsuneStorage("http://cdn.kitsune.tools/libs/kcsrf.js");

								if (fileContent != null && fileContent.contentStream != null && fileContent.contentStream.Length > 0)
								{
									context.Response.ContentType = (!String.IsNullOrEmpty(fileContent.contentType)) ? fileContent.contentType : "text";
									context.Response.Headers.Remove("Content-Encoding");
									context.Response.Body.Write(fileContent.contentStream);
									return;
								}
								break;

							case "SITEMAP.XML":
								#region SITEMAP
								string websiteId = kitsuneDomainDetails.CustomerId;

								var sitemapUrl = String.Format("{0}/{1}/{2}/sitemap.xml", projectId, "websiteresources", websiteId);
								var tempFileContentResponse = AWSS3Helper.GetAssetFromS3(bucketUrl, sitemapUrl);

								if (tempFileContentResponse != null && tempFileContentResponse.IsSuccess)
								{
									Byte[] binaryData = tempFileContentResponse.File.Content;
									context.Response.ContentType = "text/xml";
									context.Response.Headers.Remove("Content-Encoding");
									context.Response.Body.Write(binaryData, 0, binaryData.Length);
									return;
								}

								string siteMap = APIHelper.GetSitemap(projectId, websiteId, domainName);
								if (siteMap != null)
								{
									context.Response.WriteAsync(siteMap).Wait();
									context.Response.ContentType = "text/xml";
									context.Response.Headers.Remove("Content-Encoding");
									return;
								}
								else
								{
									//redirect to the homepage if the file is not found
									context.Response.StatusCode = 301;
									context.Response.Redirect(String.Format("{0}{1}", httpProtocol, domainName));
									return;
								}

								#endregion
						}

						switch (paramList[0])
						{
							//TODOD : Check how to add headers
							case "K-HEADERS":

                                var tempResponse = new StringBuilder();
                                foreach (var header in context.Request.Headers)
                                {
                                    try
                                    {
                                        tempResponse.Append(header + " : " + request.Headers[header.ToString()] + "\n");
                                    }
                                    catch { }
                                }

                                tempResponse.Append("Server variables " + "\n");
                                foreach (var variables in request.Headers)
                                {
                                    try
                                    {
                                        tempResponse.Append(variables + " : " + request.Headers[variables.ToString()] + "\n");
                                    }
                                    catch { }
                                }
                                context.Response.ContentType = "text";
                                if (!context.Response.HasStarted)
                                    context.Response.StatusCode = 404;

                                await context.Response.WriteAsync(String.Join("", tempResponse));
								return;

							case "K-SEARCH":
								#region Process k-search

								if (paramList.Count > 1)
								{
									var tempSegment = paramList[1];
									if (!String.IsNullOrEmpty(tempSegment))
									{
										var tempSegment2 = (paramList != null && paramList.Count() > 2) ? Convert.ToInt32(paramList[2]) : 0;

										var authorizationId = await MongoHelper.GetDeveloperIdFromSchemaIdAsync(projectDetails.SchemaId);

										//TODOD : Check the rewritepath
										context.Request.Path = "/KitsuneSearch";
										context.Request.QueryString = new QueryString($"?domain={kitsuneDomainDetails.Domain}&queryString={tempSegment?.ToLower()?.Replace("+", " ").Replace("-", " ")}&websiteId={kitsuneDomainDetails.CustomerId}&id={authorizationId}&skip={tempSegment2}");
										await _next.Invoke(context);
										return;
									}
								}
								else
								{
									context.Response.StatusCode = 301;
									context.Response.Redirect(String.Format("{0}{1}", httpProtocol, domainName));
									return;
								}

								#endregion
								break;

							case "K-UNSUBSCRIBE":
								#region K-UNSUBSCRIBE

								if (paramList.Count > 1)
								{
									var id = paramList[1];
									if (!String.IsNullOrEmpty(id))
									{
										var websiteUserId = request.Query["websiteUserId"];
										if (String.IsNullOrEmpty(websiteUserId))
										{
											context.Response.Headers["Content-Type"] = "text";
											if (!context.Response.HasStarted)
												context.Response.StatusCode = 404;

											await context.Response.WriteAsync(ErrorPageContent);
											return;
										}

										//TODOD : Check how to rewrite path
										context.Request.Path = "/UnSubscribe";
										context.Request.QueryString = new QueryString($"?_id={id}&websiteUserId={websiteUserId}&channel=EMAIL");
										await _next.Invoke(context);
										//app.Context.RewritePath(String.Format("/UnSubscribe?_id={0}&websiteUserId={1}&channel={2}", id, websiteUserId, "EMAIL"), true);
										return;
									}
									else
									{
										context.Response.Headers["Content-Type"] = "text";
										if (!context.Response.HasStarted)
											context.Response.StatusCode = 404;

										await context.Response.WriteAsync(ErrorPageContent);
										return;
									}
								}

								#endregion
								break;

							default:

								#region GOOGLE VERIFICATION HTML
								if (System.Text.RegularExpressions.Regex.IsMatch(paramList[0].Trim(), @"google([A-Za-z0-9\-]+)\.html", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
								{
									var s3ResourcesPath = String.Format("{0}/{1}/{2}/{3}", projectId, "websiteresources", kitsuneDomainDetails.CustomerId, paramList[0].Trim().ToLower());
									var tempContentResponse = AWSS3Helper.GetAssetFromS3(bucketUrl, s3ResourcesPath);

									if (tempContentResponse != null && tempContentResponse.IsSuccess)
									{
										Byte[] data = tempContentResponse.File.Content;
										if (!String.IsNullOrEmpty(tempContentResponse.File.ContentType))
											context.Response.ContentType = tempContentResponse.File.ContentType;
										context.Response.Headers.Remove("Content-Encoding");
										context.Response.Body.Write(data, 0, data.Length);
										return;
									}
									else
									{
										context.Response.Headers["Content-Type"] = "text";
										if (!context.Response.HasStarted)
											context.Response.StatusCode = 404;

										await context.Response.WriteAsync(ErrorPageContent);
										return;
									}
								}
								#endregion

								break;
						}
					}

					#endregion

					#region GET RESOURCE DETAILS FROM URL

					bool isDefaultView = false;
					if ((String.IsNullOrEmpty(absolutePath) || String.Compare(absolutePath, "/") == 0) ||
						(!String.IsNullOrEmpty(rootaliasurl) && rootaliasurl.Trim('/').ToLower().Equals(absoluteUrlWithoutQueryParameters?.ToLower()?.Trim('/'))))
					{
						isDefaultView = true;
					}

					try
					{
						string routeUrlEncodedPath = string.Empty;
						try
						{
							foreach (var param in absolutePath.Split('/'))
							{
								if (!String.IsNullOrEmpty(param))
								{
									routeUrlEncodedPath += String.Format("/{0}", HttpUtility.UrlEncode(param.ToLower()));
								}
							}

							if (absolutePath.EndsWith("/"))
								routeUrlEncodedPath += "/";
						}
						catch { routeUrlEncodedPath = absolutePath; }

						//Ronak - Change this code to regional-Lambda
						projectResourceDetails = await Utils.GetResourceDetailsAsync(projectId, routeUrlEncodedPath, absolutePath, kitsuneRequestUrlType, isDefaultView);

						if (projectResourceDetails != null && projectResourceDetails.IsRedirect)
						{
							string redirectUrl = $"{rootaliasurl.Trim('/')}/{projectResourceDetails.RedirectPath.TrimStart('/')}";
							context.Response.StatusCode = 301;
							context.Response.Redirect(redirectUrl);
							return;
						}

						if (projectResourceDetails != null && projectResourceDetails.StatusCode > 400 & projectResourceDetails.StatusCode < 500)
						{
							if (absolutePath.EndsWith("robots.txt", StringComparison.InvariantCultureIgnoreCase) && (kitsuneRequestUrlType == KitsuneRequestUrlType.PRODUCTION))
							{
								context.Response.WriteAsync("User-agent: *\nDisallow:").Wait();
								context.Response.ContentType = "text";
								context.Response.StatusCode = 200;
								return;
							}

							if (absolutePath.EndsWith("robots.txt", StringComparison.InvariantCultureIgnoreCase) && (kitsuneRequestUrlType != KitsuneRequestUrlType.PRODUCTION))
							{
								context.Response.WriteAsync("User-agent: *\nDisallow: /").Wait();
								context.Response.StatusCode = 200;
								return;
							}

                            #region LEGACY DOMAIN REQUEST MAPPER
                            //try
                            //{
                            //	//TODO: Optimize this OriginDomainArrayStrings()
                            //	if (LDRMProcessor.AllDomainOriginStrings.Contains(appRequestUri.Authority.ToUpper()))
                            //	{
                            //		var ldrmResponse = LDRMProcessor.MappingDetails(appRequestUri.Authority);
                            //		if (ldrmResponse.match)
                            //		{
                            //			var isLDRMSuccessfull = await ProxyHelper.ProcessProxyHttpRequest(context, appRequestUri, ldrmResponse);
                            //			if (isLDRMSuccessfull)
                            //				return;
                            //		}
                            //	}
                            //}
                            //catch { }
                            #endregion

                            //Redirect to home page if 404 based on configuration
                            try
                            {
                                var config = APIHelper.GetKitsuneResources(projectId, kitsuneDomainDetails.Version);
                                if (config != null && config.custom_error != null && ((Newtonsoft.Json.Linq.JObject)config)["custom_error"].GetType() == typeof(Newtonsoft.Json.Linq.JArray))
                                {
                                    var redirectPath = ((Newtonsoft.Json.Linq.JArray)((Newtonsoft.Json.Linq.JObject)config)["custom_error"]).Where(x => (int)((Newtonsoft.Json.Linq.JObject)x)["status_code"] == 404).FirstOrDefault();
                                    if (redirectPath != null && ((Newtonsoft.Json.Linq.JObject)redirectPath)["redirect_path"] != null)
                                    {
                                        //redirect to the homepage if the file is not found
                                        var redirectStatusPath = (string)(((Newtonsoft.Json.Linq.JObject)redirectPath)["redirect_path"]);
                                        context.Response.StatusCode = 301;
                                        context.Response.Redirect(String.Format("{0}{1}", kitsuneDomainDetails.isSSLEnabled ? "https://" : "http://", domainName + redirectStatusPath));
                                        return;
                                    }
                                }

                            }
                            catch (Exception)
                            {

                            }

                            context.Response.Headers["Content-Type"] = "text";
							if (!context.Response.HasStarted)
								context.Response.StatusCode = 404;

							await context.Response.WriteAsync(ErrorPageContent);
							return;
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine("Crash in Middleware - 1");
						Console.WriteLine(ex.ToString());
						//app.Context.Response.Write(String.Format("<!-- RESOURCE Get - Exception message: {0} \n Stacktrace: {1} -->", ex.Message, ex.StackTrace));
					}

					webCacheFileModel.IsStaticFile = (projectResourceDetails != null) ? projectResourceDetails.isStatic : true;

					#endregion

					#region PROCESS STATIC FILE REQUEST

					if (webCacheFileModel.IsStaticFile)
					{
						//Generate S3 download Url
						s3ResourcePathUrl = $"{s3FolderUrl}{absolutePath}";

						if (projectResourceDetails != null)
						{
							var requestedFilePath = (!String.IsNullOrEmpty(projectResourceDetails.OptimizedPath)) ? projectResourceDetails.OptimizedPath : null;
							s3ResourcePathUrl = (!String.IsNullOrEmpty(requestedFilePath)) ? $"{s3FolderUrl}{requestedFilePath}" : $"{s3FolderUrl}{absolutePath}";
						}

						if (s3ResourcePathUrl.EndsWith("/"))
						{
							s3ResourcePathUrl = String.Format("{0}index.html", s3ResourcePathUrl);
						}
					}

					#endregion

					#region PROCESS DYNAMIC SITE CONTENT REQUEST

					else if (projectResourceDetails != null)
					{

						var developerId = kitsuneDomainDetails.DeveloperId;
						s3ResourcePathUrl = $"{s3FolderUrl}{projectResourceDetails.OptimizedPath}";

						if (!String.IsNullOrEmpty(projectDetails.SchemaId) && String.IsNullOrEmpty(developerId))
						{
							developerId = await MongoHelper.GetDeveloperIdFromSchemaIdAsync(projectDetails.SchemaId);
						}

						try
						{
							//string websiteHttpProtocol = "http://";
							//try
							//{
							//	var forwardedProtocol = string.Empty;
							//	if (!String.IsNullOrEmpty(request.Headers["HTTP_CLOUDFRONT_FORWARDED_PROTO"]))
							//	{
							//		forwardedProtocol = request.Headers["HTTP_CLOUDFRONT_FORWARDED_PROTO"];
							//	}
							//	else if (!String.IsNullOrEmpty(request.Headers["CloudFront-Forwarded-Proto"]))
							//	{
							//		forwardedProtocol = request.Headers["CloudFront-Forwarded-Proto"];
							//	}

							//	if (!String.IsNullOrEmpty(forwardedProtocol) && String.Compare(forwardedProtocol, "https", true) == 0)
							//	{
							//		websiteHttpProtocol = "https://";
							//		absoluteUrl = absoluteUrl.Replace("http://", websiteHttpProtocol);
							//	}


							//}
							//catch (Exception ex)
							//{
							//	context.Response.WriteAsync(String.Format("<!-- HEADERS VALIDATION IN KLM() - Exception message: {0} \n Stacktrace: {1} -->", ex.Message, ex.StackTrace)).Wait();
							//}

							//var domainEntity = domainName.ToLower();

							RequestDetails requestDetails = new RequestDetails();
							var ipAddress = request.Headers["HTTP_X_FORWARDED_FOR"];
							try
							{
								if (string.IsNullOrEmpty(ipAddress))
								{
									ipAddress = request.Headers["REMOTE_ADDR"];
								}
								string perfLog = request.Headers["perflog"];
								string referenceQuery = request.Headers["ref"];
								string referer = request.Headers["Referer"];
								string agent = request.Headers["User-Agent"];
								bool isCrawler = HttpRequestHelper.IsCrawler(agent);

								requestDetails = new RequestDetails()
								{
									IPAddress = ipAddress,
									Perflog = perfLog,
									ReferenceQuery = referenceQuery,
									Referer = referer,
									UserAgent = agent,
									IsCrawler = isCrawler
								};
							}
							catch { }

							var klmResponse = await RequestHandler.GetHtmlFromKlmAsync(requestDetails, domainName,
								absoluteUrl, projectId, projectDetails.SchemaId, s3ResourcePathUrl, isNoCacheRequestEnabled,
								developerId, projectResourceDetails.UrlPattern, kitsuneDomainDetails.CustomerId, projectResourceDetails.PageType,
								kitsuneRequestUrlType, websiteProtocol, projectDetails.Components, s3FolderUrl, projectResourceDetails.OptimizedPath,
								projectResourceDetails.UrlPatternRegex, projectDetails.CompilerVersion, kitsuneDomainDetails.WebsiteTag);

							htmlCode = klmResponse.HtmlCode;
							//if (!klmResponse.CacheableResult)
							//{
							//	context.Response.Headers.TryAdd("cache-control", "no-cache");
							//	IsKLMWebCacheEnabled = false;
							//}


							#region RETURN HTML IF DEBUG
#if DEBUG
							//context.Response.ContentType = "text/html";
							//context.Response.StatusCode = 200;
							//await context.Response.WriteAsync(htmlCode);
							//return;
#endif
							#endregion

							//htmlCode = RequestHandler.GetHtmlFromKlmV2(new Kitsune.Models.KitsuneV2KLMRequestModel()
							//{
							//    DeveloperId = developerId,
							//    HostedFilePath = s3ResourcePathUrl,
							//    IncomingUrl = absoluteUrl,
							//    PageType = projectResourceDetails.PageType,
							//    ProjectId = projectId,
							//    Protocol = websiteHttpProtocol,
							//    SchemaId = projectDetails.SchemaId,
							//    WebsiteId = kitsuneDomainDetails.CustomerId
							//}, (isNoCacheRequestEnabled == "false"), ipAddress);
						}
						catch (Exception ex)
						{
							Console.WriteLine("Error in KLM hit");
							Console.WriteLine(ex.ToString());
							//context.Response.WriteAsync(String.Format("<!-- FETCH HTML FROM KLM() - Exception message: {0} \n Stacktrace: {1} -->", ex.Message, ex.StackTrace)).Wait();
						}
					}

					#endregion

					#region DOWNLOAD STATIC FILE FROM S3

					if (webCacheFileModel.IsStaticFile)
					{
						var fileContentResponse = APIHelper.GetByteStreamFromKitsuneStorage(s3ResourcePathUrl);
						if (fileContentResponse != null)
						{
							webCacheFileModel.ContentBody = fileContentResponse.contentStream;
							webCacheFileModel.ContentType = fileContentResponse.contentType;

							var staticRespCacheSettings = IdentifierEnvironmentConstants.IdentifierConfigurations.StaticResponse;
							if (staticRespCacheSettings != null && staticRespCacheSettings.CacheEnabled)
							{
								context.Response.Headers.TryAdd("Cache-Control", staticRespCacheSettings.CacheControlValue);
								context.Response.Headers.TryAdd(HttpResponseHeader.Expires.ToString(), DateTime.UtcNow.AddSeconds(staticRespCacheSettings.ExpiresInSecondsValue).ToString("R"));
							}
							else
							{
								context.Response.Headers.TryAdd("Cache-Control", "no-cache");
								context.Response.Headers.TryAdd(HttpResponseHeader.Expires.ToString(), DateTime.UtcNow.AddSeconds(0).ToString("R"));
							}
						}
						else
						{
							context.Response.Headers["Content-Type"] = "text";
							if (!context.Response.HasStarted)
								context.Response.StatusCode = 404;

							await context.Response.WriteAsync(ErrorPageContent);
							return;
						}
					}

					#endregion
				}

				#region ADDING REQUIRED HEADERS FOR CSRF TOKEN

				try
				{
					if ((webCacheFileModel.ContentType.Contains("html")) && (!String.IsNullOrEmpty(htmlCode) && htmlCode.Contains("[[___KIT_CSRF_TOKEN___]]")))
					{
						context.Response.Headers["X-KIT-CSRF-TOKEN"] = "ENABLE";

						//if (app.Context.Request.Headers.AllKeys.Contains("X-KIT-CSRF-TOKEN-ID"))
						//    app.context.Response.Headers.TryAdd("X-KIT-CSRF-TOKEN-ID", app.Context.Request.Headers["X-KIT-CSRF-TOKEN-ID"]);

					}
				}
				catch { }

				#endregion

				bool isGzipHeaderAvailable = false;
				try
				{
					isGzipHeaderAvailable = request.Headers.ContainsKey("Accept-Encoding") &&
											request.Headers["Accept-Encoding"].ToString().Contains("gzip");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"ERROR: Trying to get Accep-Encoding header {ex.ToString()}");
				}

				if ((!String.IsNullOrEmpty(htmlCode) || (webCacheFileModel != null
					  && webCacheFileModel.ContentBody != null && IsContentTypeIsOfTypeText(webCacheFileModel.ContentType))))
				{
					try
					{
						if (!isGzipHeaderAvailable && isCacheContentAvailable)
						{
							webCacheFileModel.ContentBody = GZipCompressor.Bas64DecodeIfNeededAndDecompress(webCacheFileModel.ContentBody);
						}
						else if (!isGzipHeaderAvailable && !String.IsNullOrEmpty(htmlCode))
						{
							webCacheFileModel.ContentBody = Encoding.UTF8.GetBytes(htmlCode);
						}
					}
					catch (Exception ex)
					{
						Console.WriteLine($"ERROR: Trying to Decompress {ex.ToString()}");
					}

					#region SAVE HTML IN CACHE

					if ((String.IsNullOrEmpty(isNoCacheRequestEnabled) ||
						 String.Compare(isNoCacheRequestEnabled, "true", true) != 0) && IsKLMWebCacheEnabled && !isCacheContentAvailable)
					{
						try
						{
							var redisCacheModel = new WebCacheFileModel()
							{
								ContentEncoding = (isGzipHeaderAvailable) ? "gzip" : null,
								ContentType = webCacheFileModel.ContentType,
								ContentHeaders =
									context.Response.Headers.ToDictionary(x => x.Key, x => x.Value.ToString()),
								IsStaticFile = webCacheFileModel.IsStaticFile
							};

							if (!String.IsNullOrEmpty(htmlCode))
							{
								var bytes = Encoding.UTF8.GetBytes(htmlCode);
								redisCacheModel.ContentBody = (GZipCompressor.CompressAndBase64EncodeIfNeeded(bytes));

								if (isGzipHeaderAvailable)
								{
									webCacheFileModel.ContentBody = redisCacheModel.ContentBody;
									context.Response.Headers.TryAdd("Content-Encoding", "gzip");
								}
							}
							else if (webCacheFileModel.ContentBody != null)
							{
								redisCacheModel.ContentBody = (GZipCompressor.CompressAndBase64EncodeIfNeeded(webCacheFileModel.ContentBody));

								if (isGzipHeaderAvailable)
								{
									webCacheFileModel.ContentBody = redisCacheModel.ContentBody;
									context.Response.Headers.TryAdd("Content-Encoding", "gzip");
								}
							}

							var cacheKey = await CacheHelper.SaveCacheEntity(absoluteUrl, redisCacheModel);
#if DEBUG
							context.Response.Headers.TryAdd("x-kitsune-info-2", $"web-cache key at save:  {cacheKey}");
#endif
						}
						catch (Exception ex)
						{
							Console.WriteLine($"ERROR: Trying to save in cache {ex.ToString()}");
						}
					}
					else if (isGzipHeaderAvailable && !IsKLMWebCacheEnabled)
					{
						if (!String.IsNullOrEmpty(htmlCode))
						{
							var bytes = Encoding.UTF8.GetBytes(htmlCode);
							webCacheFileModel.ContentBody = GZipCompressor.CompressAndBase64EncodeIfNeeded(bytes);
							context.Response.Headers.TryAdd("Content-Encoding", "gzip");// + base64
						}
						else
						{
							webCacheFileModel.ContentBody = GZipCompressor.CompressAndBase64EncodeIfNeeded(webCacheFileModel.ContentBody);
							context.Response.Headers.TryAdd("Content-Encoding", "gzip");// + base64
						}
					}
					else
					{
						if (!String.IsNullOrEmpty(htmlCode))
						{
							var respBytes = Encoding.UTF8.GetBytes(htmlCode);
							if (isGzipHeaderAvailable)
							{
								webCacheFileModel.ContentBody = GZipCompressor.CompressAndBase64EncodeIfNeeded(respBytes);
							}
							else
							{
								webCacheFileModel.ContentBody = respBytes;
							}
						}
					}

					#endregion
				}

				if (webCacheFileModel != null && IsContentTypeIsOfTypeText(webCacheFileModel.ContentType)
					&& (isGzipHeaderAvailable || (webCacheFileModel.ContentEncoding != null && webCacheFileModel.ContentEncoding.Contains("gzip"))))
				{
					context.Response.Headers.TryAdd("Content-Encoding", "gzip");
				}

				if (KLM_Constants.DisableGZipAndDisableBase64Response)
				{
					try
					{
						context.Response.Headers.Remove("Content-Encoding");
						webCacheFileModel.ContentBody = GZipCompressor.Bas64DecodeIfNeededAndDecompress(webCacheFileModel.ContentBody);
					}
					catch
					{
						//Suppress exceptions
					}
				}

				context.Response.Headers["server"] = IdentifierEnvironmentConstants.IdentifierConfigurations.ServerHeader;
				context.Response.Headers["x-powered-by"] = IdentifierEnvironmentConstants.IdentifierConfigurations.XPoweredByHeader;

                if (IdentifierEnvironmentConstants.IdentifierConfigurations.EnableCacheTagHeader
                    && !string.IsNullOrEmpty(kitsuneDomainDetails?.CustomerId)
                    && !string.IsNullOrEmpty(IdentifierEnvironmentConstants.IdentifierConfigurations.CacheTagHeader))
                {
                    try
                    {
                        context.Response.Headers.TryAdd(IdentifierEnvironmentConstants.IdentifierConfigurations.CacheTagHeader, $"{kitsuneDomainDetails?.CustomerId},{kitsuneDomainDetails?.ProjectId}");
                    }
                    catch
                    {
                        context.Response.Headers[IdentifierEnvironmentConstants.IdentifierConfigurations.CacheTagHeader] = $"{kitsuneDomainDetails?.CustomerId},{kitsuneDomainDetails?.ProjectId}";
                    }
                }

                if (webCacheFileModel.ContentType.Contains("html"))
				{
					var dynamicRespCacheSettings = IdentifierEnvironmentConstants.IdentifierConfigurations.DynamicResponse;
					if (dynamicRespCacheSettings != null && dynamicRespCacheSettings.CacheEnabled)
					{
						context.Response.Headers.TryAdd("cache-control", dynamicRespCacheSettings.CacheControlValue);
						context.Response.Headers.TryAdd(HttpResponseHeader.Expires.ToString(), DateTime.UtcNow.AddSeconds(dynamicRespCacheSettings.ExpiresInSecondsValue).ToString("R"));
					}
					else
					{
						context.Response.Headers.TryAdd("cache-control", "no-cache");
						context.Response.Headers.TryAdd(HttpResponseHeader.Expires.ToString(), DateTime.UtcNow.AddSeconds(0).ToString("R"));
					}

					var fixedCacheSettings = IdentifierEnvironmentConstants.IdentifierConfigurations.AllResponseFixedDurationCache;
					if (fixedCacheSettings != null && fixedCacheSettings.Enabled && fixedCacheSettings.StartHour_IST != 0 && fixedCacheSettings.EndHour_IST != 0)
					{
						var currentISTTime = DateTime.UtcNow.AddHours(5).AddMinutes(30);
						var isIntraDay = fixedCacheSettings.EndHour_IST >= fixedCacheSettings.StartHour_IST;

						if (isIntraDay ? (currentISTTime.Hour >= fixedCacheSettings.StartHour_IST && currentISTTime.Hour < fixedCacheSettings.EndHour_IST) :
							currentISTTime.Hour >= fixedCacheSettings.StartHour_IST || currentISTTime.Hour < fixedCacheSettings.EndHour_IST) //fixed cache duration start & end
						{
							try
							{
								var constantDateTime = (currentISTTime.Hour < fixedCacheSettings.EndHour_IST) ?
									(new DateTime(currentISTTime.Year, currentISTTime.Month, currentISTTime.Day, fixedCacheSettings.EndHour_IST, 0, 0, 0))
									: (new DateTime(currentISTTime.Year, currentISTTime.Month, (currentISTTime.Day + 1), fixedCacheSettings.EndHour_IST, 0, 0, 0));

								var expiryTimeInSeconds = Math.Floor((constantDateTime - currentISTTime).TotalSeconds);

								context.Response.Headers["cache-control"] = $"public, max-age={expiryTimeInSeconds}";
								context.Response.Headers[HttpResponseHeader.Expires.ToString()] = currentISTTime.AddSeconds(expiryTimeInSeconds).ToString("R");
							}
							catch { }
						}
					}
				}

				//To-Do review
				try
				{
					var userAgent = request.Headers["User-Agent"];
					var botRespSettings = IdentifierEnvironmentConstants.IdentifierConfigurations.BotResponse;
					if (botRespSettings != null && botRespSettings.CacheEnabled)
					{
						if (Utils.IsBot(userAgent))
						{
							if (context.Response.Headers.ContainsKey("Cache-Control"))
							{
								try
								{
									context.Response.Headers["Cache-Control"] = botRespSettings.CacheControlValue;
								}
								catch (Exception ex)
								{
									Console.WriteLine($"ERROR: Unable to modify the Cache-Control header" + ex.ToString());
									context.Response.Headers.TryAdd("Cache-Control", botRespSettings.CacheControlValue);
								}

								try
								{
									context.Response.Headers[HttpResponseHeader.Expires.ToString()] = DateTime.UtcNow.AddSeconds(botRespSettings.ExpiresInSecondsValue).ToString("R");
								}
								catch (Exception ex)
								{

									Console.WriteLine($"ERROR: Unable to modify the Cache-Expiry header" + ex.ToString());
									context.Response.Headers.TryAdd(HttpResponseHeader.Expires.ToString(), DateTime.UtcNow.AddSeconds(botRespSettings.ExpiresInSecondsValue).ToString("R"));
								}
							}
							else
							{
								context.Response.Headers.TryAdd("Cache-Control", botRespSettings.CacheControlValue);
								context.Response.Headers.TryAdd(HttpResponseHeader.Expires.ToString(), DateTime.UtcNow.AddSeconds(botRespSettings.ExpiresInSecondsValue).ToString("R"));
							}
						}
					}
				}
				catch { }

				context.Response.ContentType = webCacheFileModel.ContentType;

				//String vs Binary - to handle the content encoding types
				if (webCacheFileModel.ContentBody != null)
				{
					await context.Response.Body.WriteAsync(webCacheFileModel.ContentBody, 0, webCacheFileModel.ContentBody.Length);
					return;
				}

				//Duplicate code of LDRM - Commenting it as this logic has been moved to Edge Lambda
				//#region LEGACY DOMAIN REQUEST MAPPER
				//try
				//{
				//TODO: Optimize this OriginDomainArrayStrings()
				//	if (LDRMProcessor.AllDomainOriginStrings.Contains(appRequestUri.Authority.ToUpper()))
				//	{
				//		var ldrmResponse = LDRMProcessor.MappingDetails(appRequestUri.Authority);
				//		if (ldrmResponse.match)
				//		{
				//			var isLDRMSuccessfull = await ProxyHelper.ProcessProxyHttpRequest(context, appRequestUri, ldrmResponse);
				//			if (isLDRMSuccessfull)
				//				return;
				//		}
				//	}
				//}
				//catch { }
				//#endregion
			}
			catch (Exception ex)
			{
				Console.WriteLine("Crash in Middleware");
				Console.WriteLine(ex.ToString());
			}

			context.Response.Headers["Content-Type"] = "text";
			if (!context.Response.HasStarted)
				context.Response.StatusCode = 404;

			await context.Response.WriteAsync(ErrorPageContent);
			return;
		}

		private KitsuneRequestUrlType IdentifyRequestUrlType(string dnsHostName)
		{
			if ((dnsHostName.EndsWith(Constants.KitsuneDemoSubDomain, StringComparison.InvariantCultureIgnoreCase)))
				return KitsuneRequestUrlType.DEMO;

			if ((dnsHostName.EndsWith(Constants.KitsunePreviewSubDomain, StringComparison.InvariantCultureIgnoreCase)))
				return KitsuneRequestUrlType.PREVIEW;

			return KitsuneRequestUrlType.PRODUCTION;
		}

		internal bool IsContentTypeIsOfTypeText(string contentType)
		{
			try
			{
				if (contentType.Contains("text") || contentType.Contains("javascript"))
					return true;
			}
			catch { }
			return false;
		}

		// ThemeId mapping taken from https://bitbucket.org/anwesh_mohanty/nds-documentation/src/master/FloatingPoints/theme_id_package_mapping.md
		internal string GetThemeIdFromThemeName(string themeName)
		{
			if (!string.IsNullOrWhiteSpace(themeName))
			{
				switch (themeName.ToLower())
				{
					case "bnb":
						return "575bfec79bfed51e10df0e5d";
					case "fml":
						return "57c3c1a65d64370d7cf4eb17";
					case "tff":
						return "571f7f789bfed52c543d888d";
					case "cro":
						return "5b864dd931bfd4054774ec1b";
					case "alx":
						return "5b7c65a512416106678dfbe7";
					case "lxr":
						return "5b3ce6311a6cfc051477e5bc";

					case "mnm":
						return "590b3f09ee786c1d88879129";
					case "ppe":
						return "59d74e153872831a6483491e";
					case "hnc":
						return "597f48fd38728384d4b85cdd";
					case "cnc":
						return "5a952f3dac626704fc9b6d86";
					case "fff":
						return "5ad9c409889084051f87a4b6";
					case "bey":
						return "5b34bd254030c804fbbd8414";
					case "srd":
						return "59fb07a43872830d70b3d1a4";
					case "ehl":
						return "5b3b2b6aec3c7704fee7ae93";
					case "bct":
						return "5b56daca5d8dff0509c381ee";
					// default theme : cairo
					default:
						return "5b864dd931bfd4054774ec1b";
				}
			}
			return null;
		}
	}
}
