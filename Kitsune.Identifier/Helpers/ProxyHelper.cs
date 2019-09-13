using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace Kitsune.Identifier.Helpers
{
    internal static class ProxyHelper
    {
        public static async Task<bool> ProcessProxyHttpRequest(HttpContext context, Uri appRequestUri, LDRMResponse ldrmResponse)
        {
            try
            {
                string requestId = DateTime.Now.GetHashCode().ToString();

                Uri originalUri = appRequestUri;
                var builder = new UriBuilder(appRequestUri);
                try
                {
                    builder.Host = ldrmResponse.destination_domain;
                    try
                    {
                        builder.Port = ldrmResponse.destination_domain_port;
                        if (builder.Port == 443)
                            builder.Scheme = "https";

                        appRequestUri = builder.Uri;
                    }
                    catch { }
                }
                catch { }

                var httpResponseMessage = await ProxyHelper.CreateProxyHttpRequest(context.Request, appRequestUri);

                if (httpResponseMessage.StatusCode == HttpStatusCode.Redirect || httpResponseMessage.StatusCode == HttpStatusCode.Moved)
                {
                    var redirectUri = httpResponseMessage.Headers.Location;
                    if (!redirectUri.IsAbsoluteUri)
                    {
                        redirectUri = new Uri(originalUri, redirectUri);
                    }

                    if (httpResponseMessage.StatusCode == HttpStatusCode.Moved)
                    {
                        context.Response.Redirect(redirectUri.AbsoluteUri);
                        return true;
                    }
                    else
                    {
                        context.Response.Redirect(redirectUri.AbsoluteUri);
                        return true;
                    }
                }
                else
                {

                    // Preserve Origin Headers
                    var headers = httpResponseMessage.Headers.Concat(httpResponseMessage.Content.Headers);
                    foreach (var header in headers)
                    {
                        context.Response.Headers[header.Key] = header.Value.ToArray();
                    }
                    context.Response.StatusCode = (int)httpResponseMessage.StatusCode;
                    context.Response.Headers.Remove("Transfer-Encoding");
                    context.Response.Headers.TryAdd("x-kitsune-module", "ldrm");

                    Byte[] data = httpResponseMessage.Content.ReadAsByteArrayAsync().Result;
                    
                    //Update contentLength if updating or converting the body content
                    //context.Response.ContentLength = data.Length;

                    context.Response.Body.Write(data, 0, data.Length);
                    
                    return true;
                }

            }
            catch { }
            return false;
        }

        public static async Task<HttpResponseMessage> CreateProxyHttpRequest(HttpRequest originalRequest, Uri uri, string requestId = null)
        {
            try
            {
                var proxyRequest = new HttpRequestMessage();
                var requestMethod = originalRequest.Method;

                var cookieContainer = new CookieContainer();
                var headers = originalRequest.HttpContext.Request.Headers;
                foreach (var headerKey in headers.Keys)
                {
                    var headerValues = headers[headerKey];
                    foreach(var headerValue in headerValues)
                    {
                        if (headerKey == "Cookie")
                        {
                            try
                            {
                                string[] values = headerValue?.Split(';');
                                foreach (string cookieValue in values)
                                {
                                    string[] cookieParts = cookieValue?.Split('=');
                                    if (cookieParts.Length == 2)
                                    {
                                        cookieContainer.Add(uri, new Cookie(cookieParts[0].Trim(), HttpUtility.UrlEncode(cookieParts[1].Trim())));
                                    }
                                }
                            }
                            catch { }
                        }
                        else if (!proxyRequest.Headers.TryAddWithoutValidation(headerKey, headerValue) && proxyRequest.Content != null)
                        {
                            proxyRequest.Content?.Headers.TryAddWithoutValidation(headerKey, headerValue);
                        }
                    }
                }


                if (headers["Content-Type"].ToString().Contains("application/x-www-form-urlencoded") || headers["Content-Type"].ToString().Contains("multipart/form-data"))
                {
                    try
                    {
                        var formValues = new List<KeyValuePair<string, string>>();
                        var requestForm = originalRequest.Form;
                        foreach (var form in requestForm)
                        {
                            try
                            {
                                var values = form.Value;
                                foreach(var value in values)
                                {
                                    formValues.Add(new KeyValuePair<string, string>(form.Key, value));
                                }
                            }
                            catch { }
                        }
                        proxyRequest.Content = new FormUrlEncodedContent(formValues);
                    }
                    catch { }
                }
                else
                {
                    var httpMethodCheckList = new List<string>() { "GET", "DELETE", "HEAD", "TRACE" };
                    if (!httpMethodCheckList.Contains(requestMethod?.ToUpper()))
                    {
                        proxyRequest.Content = new StreamContent(originalRequest.Body);
                    }
                }

                var ipAddress = originalRequest.Headers["HTTP_X_FORWARDED_FOR"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(ipAddress) || string.Equals(ipAddress, "unknown", StringComparison.OrdinalIgnoreCase))
                    ipAddress = originalRequest.Headers["REMOTE_ADDR"];

                proxyRequest.Headers.TryAddWithoutValidation("X-Forwarded-For", ipAddress);
                proxyRequest.Headers.Host = uri.Authority;
                proxyRequest.RequestUri = uri;
                proxyRequest.Method = new HttpMethod(requestMethod);

                HttpClientHandler handler = new HttpClientHandler()
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    AllowAutoRedirect = false,
                    CookieContainer = cookieContainer
                };
                var client = new HttpClient(handler);
                return await client.SendAsync(proxyRequest);
            }
            catch (Exception ex)
            {
                //Handle the exception and respond with proper errorCode and message
            }
            return null;
        }
    }
}
