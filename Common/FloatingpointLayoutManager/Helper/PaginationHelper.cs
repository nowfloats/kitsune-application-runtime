using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Kitsune.Server.Model;
using KitsuneLayoutManager.Models;
using Kitsune.Server.Model.Kitsune;
using System.Text.RegularExpressions;
using Kitsune.Models;

namespace KitsuneLayoutManager.Helper
{
    public class PaginationHelper
    {
        internal static Models.Pagination GetViewDetails(string actualUrl, string viewUrl, string rootAliasUrl, bool isDetailsView, bool isSearchView = false)
        {
            var model = new Models.Pagination()
            {
                currentpagenumber = "1",
                nextpage = new Kitsune.Server.Model.Kitsune.Link() { url = "#" },
                prevpage = new Kitsune.Server.Model.Kitsune.Link() { url = "#" },
                totalpagescount = "1"
            };
            try
            {
               
                var tempUrl = viewUrl.ToLower();
                int index = tempUrl.IndexOf('?');
                if(index != -1)
                {
                    var queryKey = GetQueryString(tempUrl, "currentpagenumber");
                    if(!string.IsNullOrEmpty(queryKey))
                    {
                        model.currentpagenumber = GetQueryValue(actualUrl, queryKey);
                        model.nextpage.url = ReplaceQueryValue(actualUrl, queryKey, (Convert.ToInt32(model.currentpagenumber) + 1).ToString());
                        if(Convert.ToInt32(model.currentpagenumber) != 1)
                        {
                            model.prevpage.url = ReplaceQueryValue(actualUrl, queryKey, (Convert.ToInt32(model.currentpagenumber) - 1).ToString());
                        }
                        
                    }
                }
                else
                {
                    if(isDetailsView)
                    {
                        var urlParamString = new Uri(actualUrl).AbsolutePath.Trim('/').Split('/');
                        var lastUrlParam = urlParamString[urlParamString.Length - 1];
                        if (char.IsDigit(lastUrlParam[0]))
                        {
                            model.currentpagenumber = lastUrlParam;
                        }
                        else
                        {
                            var pageNumberString = Regex.Match(lastUrlParam, @"\d+").Value;
                            if (pageNumberString.ToString() != lastUrlParam && !viewUrl.ToLower().Contains("currentpagenumber") && !viewUrl.ToLower().Contains("index") && !viewUrl.ToLower().Contains("kid"))
                            {
                                model.currentpagenumber = lastUrlParam;
                            }
                            else
                            {
                                model.currentpagenumber = pageNumberString;
                            }
                        }
                    }
                    else if (isSearchView)
                    {
                        var startIndex = actualUrl.IndexOf("search/") + 7;
                        var searchText = actualUrl.Substring(startIndex, (actualUrl.LastIndexOf('/') - startIndex));
                        var urlParamString = new Uri(actualUrl).AbsolutePath.Trim('/').Split('/');
                        var lastUrlParam = urlParamString[urlParamString.Length - 1];
                        if (char.IsDigit(lastUrlParam[0]))
                        {
                            model.currentpagenumber = lastUrlParam;
                            int i = actualUrl.LastIndexOf(lastUrlParam);
                            if (i >= 0)
                            {
                                actualUrl = actualUrl.Substring(0, i) + actualUrl.Substring(i + lastUrlParam.Length);
                            }

                            model.nextpage.url = actualUrl + (Convert.ToInt32(model.currentpagenumber) + 1).ToString();
                            if (Convert.ToInt32(model.currentpagenumber) != 1)
                            {
                                model.prevpage.url = actualUrl + (Convert.ToInt32(model.currentpagenumber) - 1).ToString();
                            }
                        }
                        else
                        {
                            var pageNumberString = Regex.Match(lastUrlParam, @"\d+").Value;
                            model.currentpagenumber = pageNumberString;
                        }

                        model.searchtext = searchText.Replace("-", " ");
                    }
                    else
                    {
                        var lcs = FindLongestCommonSubstring(tempUrl, actualUrl.Replace(rootAliasUrl, ""));

                        string pageNumber = String.Empty;

                        if (String.IsNullOrEmpty(lcs))
                            pageNumber = "1";
                        else
                            pageNumber = actualUrl.Replace(rootAliasUrl, "").Replace(lcs, "");

                        int n;
                        bool isNumeric = int.TryParse(pageNumber, out n);
                        if (isNumeric)
                        {
                            model.currentpagenumber = pageNumber;
                            int i = actualUrl.LastIndexOf(pageNumber);
                            if (i >= 0)
                            {
                                actualUrl = actualUrl.Substring(0, i) + actualUrl.Substring(i + pageNumber.Length);
                            }

                            if(actualUrl.Equals(rootAliasUrl,StringComparison.InvariantCultureIgnoreCase))
                            {
                                model.nextpage.url = actualUrl.TrimEnd('/') + "/" + (Convert.ToInt32(model.currentpagenumber) + 1).ToString();
                            }
                            else
                            {
                                model.nextpage.url = actualUrl + (Convert.ToInt32(model.currentpagenumber) + 1).ToString();
                            }
                            
                            if (Convert.ToInt32(model.currentpagenumber) != 1)
                            {
                                model.prevpage.url = actualUrl + (Convert.ToInt32(model.currentpagenumber) - 1).ToString();
                            }
                        }
                        else
                        {
                            var urlParamString = new Uri(actualUrl).AbsolutePath.Trim('/').Split('/');
                            var lastUrlParam = urlParamString[urlParamString.Length - 1];
                            var pageNumberString = Regex.Match(lastUrlParam, @"\d+").Value;
                            if (string.IsNullOrEmpty(pageNumberString))
                            {
                                model.currentpagenumber = lastUrlParam;
                            }
                            else
                            {
                                string[] urlParts = actualUrl.Split('?');
                                actualUrl = urlParts[0];
                                string queryParam = "";
                                if (urlParts.Length > 1)
                                {
                                    queryParam = urlParts[1];
                                }
                                model.currentpagenumber = pageNumberString;
                                isNumeric = (int.TryParse(pageNumberString, out n) && pageNumberString == n.ToString());
                                if (isNumeric)
                                {
                                    int i = actualUrl.LastIndexOf(pageNumberString);
                                    if (i >= 0)
                                    {
                                        actualUrl = actualUrl.Substring(0, i) + actualUrl.Substring(i + pageNumberString.Length);
                                    }

                                    if (actualUrl.Equals(rootAliasUrl, StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        model.nextpage.url = actualUrl.TrimEnd('/') + "/" + (Convert.ToInt32(model.currentpagenumber) + 1).ToString() + (queryParam == "" ? "" : "?" + queryParam);
                                    }
                                    else
                                    {
                                        model.nextpage.url = actualUrl + (Convert.ToInt32(model.currentpagenumber) + 1).ToString() + (queryParam == "" ? "" : "?" + queryParam);
                                    }

                                    if (Convert.ToInt32(model.currentpagenumber) != 1)
                                    {
                                        model.prevpage.url = actualUrl + (Convert.ToInt32(model.currentpagenumber) - 1).ToString() + (queryParam == "" ? "" : "?" + queryParam);
                                    }
                                }
                            }
                        }
                    }  
                }
            }
            catch(Exception ex)
            {
            }

            return model;
        }

        public static string FindLongestCommonSubstring(string s1, string s2)
        {
            int[,] a = new int[s1.Length + 1, s2.Length + 1];
            int row = 0;    // s1 index
            int col = 0;    // s2 index

            for (var i = 0; i < s1.Length; i++)
                for (var j = 0; j < s2.Length; j++)
                    if (s1[i] == s2[j])
                    {
                        int len = a[i + 1, j + 1] = a[i, j] + 1;
                        if (len > a[row, col])
                        {
                            row = i + 1;
                            col = j + 1;
                        }
                    }

            return s1.Substring(row - a[row, col], a[row, col]);
        }

        private static string GetQueryString(string url, string value)
        {
            try
            {
                string query_string = url.Split('?')[1];
                var expressionArray = query_string.Split('&');
                if(expressionArray != null && expressionArray.Length > 0)
                {
                    foreach(var expression in expressionArray)
                    {
                        if(expression.ToLower().Contains(value))
                        {
                            return expression.Split('=')[0];
                        }
                    }
                }

               
            }
            catch(Exception ex)
            {

            }

            return null;
        }

        private static string GetQueryValue(string url, string key)
        {
            try
            {
                string query_string = string.Empty;

                var uri = new Uri(url);
                var newQueryString = HttpUtility.ParseQueryString(uri.Query);
                query_string = newQueryString[key].ToString();

                return query_string;
            }
            catch(Exception ex)
            {

            }

            return null;
            
        }

        private static string ReplaceQueryValue(string url, string key, string Value)
        {
            try
            {
                string query_string = string.Empty;

                var uri = new Uri(url);
                var newQueryString = HttpUtility.ParseQueryString(uri.Query);
                newQueryString.Set(key, Value);

                var uriBuilder = new UriBuilder(url);
                uriBuilder.Query = newQueryString.ToString();
                var newUri = uriBuilder.Uri.AbsoluteUri;

                return newUri;
            }
            catch (Exception ex)
            {

            }

            return null;

        }

      
    }
}
