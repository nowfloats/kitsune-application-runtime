using System.Collections.Generic;
using HtmlAgilityPack;
using Kitsune.Language.Models;
using Newtonsoft.Json.Linq;
using System;

namespace KitsuneLayoutManager.Helper.TagProcessors
{
    public class KScriptProcessor : TagProcessor
    {
        public KScriptProcessor()
        {
            TagProcessorIdentifier = "KScriptProcessor";
        }

        /// <summary>
        /// Process nodes with k-script tags.
        /// </summary>
        public override void Process(ref HtmlNode node, HtmlAttribute dynamicAttribute, Dictionary<string, AliasReference> classNameAlias, Dictionary<int, string> classNameAliasdepth, int depth, string websiteId, ExpressionEvaluator evaluator, KEntity entity, dynamic websiteData, Models.Pagination viewDetails, string queryString, Dictionary<string, long> functionLog, bool isDetailsView = false, bool isNFSite = false, string developerId = null)
        {
            try
            {
                var apiUrl = node.GetAttributeValue("get-api", null);
                var input = node.GetAttributeValue("input", null);
                var headers = node.GetAttributeValue("headers", null)?.Trim()?.Trim('[')?.Trim(']');
                string cacheEnabledStr = node.GetAttributeValue("cacheenabled", null);
                bool cacheEnabled = true;
                if (!string.IsNullOrEmpty(cacheEnabledStr))
                {
                    cacheEnabledStr = evaluator.EvaluateExpression(cacheEnabledStr, entity, viewDetails, classNameAlias, websiteData, websiteData?._system?.kresult, queryString, out bool hasData, functionLog, isDetailsView, isNFSite, developerId).ToString();
                    bool.TryParse(cacheEnabledStr, out cacheEnabled);
                }

                if (!string.IsNullOrEmpty(apiUrl))
                {
                    var tempUrl = apiUrl; var headerDict = new Dictionary<string, string>();

                    #region API ENDPOINT
                    if (!string.IsNullOrEmpty(input))
                    {
                        var tempInputArray = input.Split(',');
                        var inputArray = input.Split(',');
                        int count = 0;
                        foreach (var elem in inputArray)
                        {
                            if (elem.Contains("]") || elem.Contains("["))
                            {
                                var output = evaluator.EvaluateExpression(elem, entity, viewDetails, classNameAlias, websiteData, websiteData?._system?.kresult, queryString, out bool hasData, functionLog, isDetailsView, isNFSite, developerId).ToString();
                                node.SetAttributeValue("input", input.Replace(tempInputArray[count], output.Trim('\'')));
                                tempUrl = tempUrl.Replace("{" + count + "}", output.Trim('\''));
                            }
                            else
                            {
                                tempUrl = tempUrl.Replace("{" + count + "}", elem);
                            }
                            count++;
                        }
                    }
                    node.SetAttributeValue("get-api", tempUrl);
                    #endregion

                    if (!string.IsNullOrEmpty(headers))
                    {
                        var headersArray = Helper.TrimDelimiters(headers).Split(',');
                        foreach (var header in headersArray)
                        {
                            var separator = header.Split(':');
                            if (!headerDict.ContainsKey(separator[0].Trim()))
                            {
                                headerDict.Add(separator[0].Trim().Trim('\''), separator[1].Trim().Trim('\''));
                            }
                        }
                    }

                    var result = ApiHelper.GetResponseFromKScriptAsync(tempUrl, headerDict, cacheEnabled, websiteData?.rootaliasurl?.url?.Value, functionLog, websiteData?._id).GetAwaiter().GetResult();
                    //TODO: optimize this code
                    if (result != null)
                    {
                        try
                        {
                            if (websiteData["_system"] == null) {
                                websiteData["_system"] = new JObject();
                            }
                            websiteData["_system"]["kresult"] = result;
                            AliasReference aliasReference = new AliasReference
                            {
                                referenceObject = null,
                                iteration = -1,
                                maxIteration = -1
                            };

                            classNameAlias.Add("kresult", aliasReference);
                            classNameAliasdepth.Add(depth, "kresult");
                        }
                        catch { }
                        try
                        {
                            node.Attributes.Remove("get-api");
                            node.Attributes.Remove("input");
                            node.Attributes.Remove("headers");
                            node.Attributes.Remove("cacheenabled");
                        }
                        catch { }
                    }
                }
            }
            catch(Exception ex) {
            }
        }
    }
}
