using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Kitsune.Server.Model.Kitsune;
using System.Text.RegularExpressions;
using Kitsune.SyntaxParser;

namespace KitsuneLayoutManager.Helper.WidgetHandler
{
    public class RepeatHelper
    {
        internal static void RepeatNodesWithObject(HtmlNode repeatNode, int repeatCount, string ObjectValue, string verb)
        {
            try
            {
                var initialHtml = repeatNode.InnerHtml;

                #region NO REPEAT
                var tempNode = HtmlNode.CreateNode(repeatNode.OuterHtml);
                var norepeatNodes = tempNode.DescendantsAndSelf().Where(s => s.Attributes["k-norepeat"] != null).ToList();
                if (norepeatNodes != null && norepeatNodes.Any())
                {
                    foreach (var norepeatNode in norepeatNodes)
                    {
                        if (norepeatNode.ParentNode != null)
                        {
                            norepeatNode.ParentNode.RemoveChild(norepeatNode);
                        }
                    }

                    initialHtml = tempNode.InnerHtml;
                }

                #endregion

                if (repeatCount > 1)
                {
                    for (int i = 0; i < repeatCount; i++)
                    {
                        var htmlString = initialHtml.Trim();
                        htmlString = htmlString.Replace("[i]", "[" + i.ToString() + "]");
                        if (i == 0)
                        {
                            repeatNode.InnerHtml = repeatNode.InnerHtml.Replace("[i]", "[" + i.ToString() + "]");
                        }
                        else
                        {
                            repeatNode.InnerHtml += htmlString;
                        }
                    }
                }
                else
                {
                    var htmlString = initialHtml;
                    htmlString = htmlString.Replace("[i]", "[0]");
                    repeatNode.InnerHtml = htmlString;
                }
            }
            catch (Exception ex)
            {

            }
        }

        internal static void RepeatNodeWithNoVerb(HtmlNode repeatNode, int repeatCount)
        {
            try
            {
                var initialHtml = repeatNode.InnerHtml;

                #region NO REPEAT
                var tempNode = HtmlNode.CreateNode(repeatNode.OuterHtml);
                var norepeatNodes = tempNode.DescendantsAndSelf().Where(s => s.Attributes["k-norepeat"] != null).ToList();
                if (norepeatNodes != null && norepeatNodes.Any())
                {
                    foreach (var norepeatNode in norepeatNodes)
                    {
                        if (norepeatNode.ParentNode != null)
                        {
                            norepeatNode.ParentNode.RemoveChild(norepeatNode);
                        }
                    }

                    initialHtml = tempNode.InnerHtml;
                }

                #endregion

                if (repeatCount > 1)
                {
                    for (int i = 0; i < repeatCount; i++)
                    {
                        var htmlString = initialHtml.Trim();
                        if (i == 0)
                        {
                            repeatNode.InnerHtml = repeatNode.InnerHtml;
                        }
                        else
                        {
                            repeatNode.InnerHtml += htmlString;
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        internal static void RepeatNodesWithIndex(HtmlNode repeatNode, int repeatCount, string verb, int index = 0)
        {
            try
            {
                var initialHtml = repeatNode.InnerHtml;
                var tempNode = HtmlNode.CreateNode(repeatNode.OuterHtml);

                #region NO REPEAT
                var norepeatNodes = tempNode.DescendantsAndSelf().Where(s => s.Attributes["k-norepeat"] != null).ToList();
                if (norepeatNodes != null && norepeatNodes.Any())
                {
                    foreach (var norepeatNode in norepeatNodes)
                    {
                        if (norepeatNode.ParentNode != null)
                        {
                            norepeatNode.ParentNode.RemoveChild(norepeatNode);
                        }
                    }

                    initialHtml = tempNode.InnerHtml;
                }

                #endregion

                var count = index;
                var regexKitsuneTag = @"\[\[(.*?)\]\]";
                var regexVerb = $@"([\s\(\+\-\*\/\=\[\>\<\%])({verb})([\s\+\-\*\/\=\)\]\<\>\%])";
                var regexVerbRegex = new Regex(regexVerb);
                var regexKitsuneTagRegex = new Regex(regexKitsuneTag);


                if (repeatCount > 1)
                {
                    for (int intnum = 0; intnum < repeatCount; intnum++)
                    {
                        var htmlString = initialHtml.Trim();
                        var kitsuneTags = regexKitsuneTagRegex.Matches(htmlString);
                        if (kitsuneTags != null && kitsuneTags.Count > 0)
                        {
                            for (var t = 0; t < kitsuneTags.Count; t++)
                            {
                                htmlString = htmlString.Replace(kitsuneTags[t].Value, ReplaceIteratorValue(kitsuneTags[t].Value, regexVerbRegex, verb, count.ToString()));
                            }
                        }

                        if (intnum != 0)
                        {
                            repeatNode.InnerHtml += htmlString;
                        }
                        else
                        {

                            kitsuneTags = regexKitsuneTagRegex.Matches(repeatNode.InnerHtml);
                            if (kitsuneTags != null && kitsuneTags.Count > 0)
                            {
                                for (var t = 0; t < kitsuneTags.Count; t++)
                                {
                                    repeatNode.InnerHtml = repeatNode.InnerHtml.Replace(kitsuneTags[t].Value, ReplaceIteratorValue(kitsuneTags[t].Value, regexVerbRegex, verb, count.ToString()));
                                }
                            }
                        }
                        count++;
                    }
                }
                else
                {

                    var htmlString = initialHtml;

                    var kitsuneTags = regexKitsuneTagRegex.Matches(htmlString);
                    if (kitsuneTags != null && kitsuneTags.Count > 0)
                    {
                        for (var t = 0; t < kitsuneTags.Count; t++)
                        {
                            htmlString = htmlString.Replace(kitsuneTags[t].Value, ReplaceIteratorValue(kitsuneTags[t].Value, regexVerbRegex, verb, index.ToString()));
                        }
                    }

                    repeatNode.InnerHtml = htmlString;
                }

            }
            catch (Exception ex)
            {

            }

        }
        internal static string ReplaceIteratorValue(string inputHtml, Regex regex, string iterator, string iteratorValue)
        {
            var matches = regex.Matches(inputHtml);
            if (matches != null && matches.Count > 0)
                for (var mat = 0; mat < matches.Count; mat++)
                {
                    inputHtml = inputHtml.Replace(matches[mat].Value, matches[mat].Value.Replace(matches[mat].Groups[2].Value, iteratorValue));
                }
            return inputHtml;
        }
        internal static async Task RepeatHtmlNodesAsync(HtmlNode documentNode, dynamic business, string queryString, string rootAliasUrl, string[] urlParams, Models.Pagination viewDetails, string themeid, bool isCacheEnabled = false, bool isDetailsView = false, bool isKScriptRepeat = false,bool isNFSite=false, string developerId = null)
        {
            try
            {

                var repeatNodeList = documentNode.DescendantsAndSelf().Where(s => s.Attributes["k-Repeat"] != null).ToList();
                var removeNodeList = new List<HtmlNode>();
                for (var i = 0; i < repeatNodeList.Count; i++)
                {
                    removeNodeList.AddRange(repeatNodeList[i].Descendants().Where(x => x.Attributes["k-Repeat"] != null));
                }

                if (removeNodeList.Any())
                {
                    foreach (var node in removeNodeList)
                    {
                        repeatNodeList.Remove(node);
                    }
                }
                if (!isKScriptRepeat)
                {
                    removeNodeList = new List<HtmlNode>();
                    for (var i = 0; i < repeatNodeList.Count; i++)
                    {
                        try
                        {
                            if (repeatNodeList[i].GetAttributeValue("k-Repeat", null).Trim('[', ']').ToLower().StartsWith("kresult"))
                                removeNodeList.Add(repeatNodeList[i]);
                        }
                        catch { }
                    }
                    if (removeNodeList.Any())
                    {
                        foreach (var node in removeNodeList)
                        {
                            repeatNodeList.Remove(node);
                        }
                    }
                }
                

                if (repeatNodeList != null && repeatNodeList.Count() > 0)
                {
                    foreach (var repeatNode in repeatNodeList)
                    {
                        //Putting try for each k-repeat node... in case of any exception in any k-repeat it should not impact other k-repeat
                        try
                        {
                            var expression = repeatNode.GetAttributeValue("k-Repeat", null);
                            repeatNode.Attributes.Remove("k-repeat");
                            expression = expression.Trim('[', ']');
                            int expressionNumber;
                            var isNumeric = int.TryParse(expression, out expressionNumber);
                            var repeatCount = 1;

                            if (isNumeric)
                            {
                                RepeatNodeWithNoVerb(repeatNode, expressionNumber);
                            }
                            else if (!expression.Contains(','))
                            {
                                var expressionValues = expression.Split(new string[] { "in " }, StringSplitOptions.None);
                                var expressionArray = expressionValues[1].Split('.');
                                var dataObject = await GetObjectDetailsAsync(expressionArray, business, queryString, viewDetails, themeid, isCacheEnabled, isDetailsView, isNFSite, developerId);
                                var objectLength = DataHandler.GetObjectSize(dataObject);
                                if (expressionValues.Length == 2)
                                {
                                    repeatCount = Convert.ToInt32(objectLength);
                                    RepeatNodesWithObject(repeatNode, repeatCount, expressionValues[1], expressionValues[0].Trim());
                                }
                            }
                            else
                            {
                                var expressionValues = expression.Split(',');
                                var expressionArray = expressionValues[0].Split('.');
                                object dataObject = null; long objectLength = 0;

                                dataObject = await GetObjectDetailsAsync(expressionArray, business, queryString, viewDetails, themeid, isCacheEnabled, isDetailsView, isNFSite, developerId);
                                objectLength = DataHandler.GetObjectSize(dataObject);
                                var verb = expressionValues[1].Trim();
                                var indexes = expressionValues[2].Split(':');
                                var firstIndex = indexes[0]; var finalIndex = indexes[1];
                                bool isPaginationView = false;


                                //Check for the current view offset. 
                                if (firstIndex.ToLower().Contains("offset"))
                                {
                                    var offset = viewDetails.currentpagenumber;
                                    int numValue = 0;
                                    bool parsed = Int32.TryParse(offset, out numValue);
                                    if (parsed)
                                    {
                                        isPaginationView = true;
                                        firstIndex = (numValue - 1).ToString();
                                    }
                                    else
                                    {
                                        firstIndex = "0";
                                    }
                                }
                                else
                                {
                                    var firstIndexValue =await EvaluateRepeatCountAsync(firstIndex, rootAliasUrl, business, queryString, viewDetails, themeid, isCacheEnabled, isDetailsView, isNFSite, developerId);
                                    firstIndex = firstIndexValue?.ToString();
                                }
                                //Evaluate repeat count and index
                                var finalIndexValue = await EvaluateRepeatCountAsync(finalIndex, rootAliasUrl, business, queryString, viewDetails, themeid, isCacheEnabled, isDetailsView, isNFSite, developerId).ToString();
                                finalIndex = finalIndexValue?.ToString();

                                if (Convert.ToInt32(firstIndex) <= objectLength)
                                {
                                    int firstIndexNum = Convert.ToInt32(firstIndex);
                                    int finalIndexNum = 0;
                                    bool parsed = Int32.TryParse(finalIndex, out finalIndexNum);

                                    if (isPaginationView)
                                    {
                                        firstIndexNum = firstIndexNum * finalIndexNum;
                                        if (objectLength <= (firstIndexNum + finalIndexNum))
                                        {
                                            viewDetails.nextpage.url = "#";
                                        }

                                        viewDetails.pagesize = finalIndexNum;
                                    }

                                    if (!parsed)
                                    {
                                        finalIndexNum = Convert.ToInt32(objectLength);
                                    }
                                    else
                                    {
                                        finalIndexNum = Convert.ToInt32(objectLength) > (firstIndexNum + finalIndexNum) ? finalIndexNum : (Convert.ToInt32(objectLength) - firstIndexNum);
                                    }


                                    RepeatNodesWithIndex(repeatNode, Convert.ToInt32(finalIndexNum), verb, firstIndexNum);
                                    if (repeatNode.Descendants().Any(x => x.Attributes["k-repeat"] != null))
                                    {
                                        foreach (var node in repeatNode.Descendants().Where(s => s.Attributes["k-Repeat"] != null))
                                        {
                                            RepeatHtmlNodesAsync(node, business, queryString, rootAliasUrl, urlParams, viewDetails, themeid, isCacheEnabled, isDetailsView,false, isNFSite, developerId);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                        
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
        private static async Task<object> EvaluateRepeatCountAsync(string repeatIndex, string rootAliasUrl, dynamic business, string queryString, Models.Pagination viewDetails, string themeid, bool isCacheEnabled = false, bool isDetailsView = false,bool isNFSite=false, string developerId = null)
        {
            var webactionsApiDictionary = new Dictionary<string, dynamic>();
            var functionLog = new Dictionary<string, long>();
            var searchApiDictionary = new Dictionary<string, dynamic>();
            var expression = string.Empty;

            var matches = Parser.GetObjects(repeatIndex);
            var tempVal = repeatIndex;
            foreach (var mat in matches)
            {
                expression = mat.ToString().Replace("[[", "").Replace("]]", "");
                var expressionValue = await DataHandler.ExpressionEvaluatorAsync(expression, rootAliasUrl, business, queryString, viewDetails, "", webactionsApiDictionary, searchApiDictionary, functionLog, themeid, isCacheEnabled, isDetailsView, isNFSite, developerId);

                tempVal = DataHandler.ReplaceFirstOccurrence(tempVal, expression, expressionValue);
            }
            var expressionValue2 = !string.IsNullOrEmpty(tempVal.Trim('[', ']')) ? Parser.Execute(tempVal.Trim('[', ']')) : "";
            return expressionValue2;
        }
        private static async Task<object> GetObjectDetailsAsync(string[] expressionArray, dynamic business, string queryString, Models.Pagination viewDetails, string themeid, bool isCacheEnabled = false, bool isDetailsView = false, bool isNFSite = false, string developerId = null)
        {
            try
            {
                dynamic dataObject = null;

                if (expressionArray != null && expressionArray.Length > 0 && expressionArray[0].ToUpper().Equals("WEBACTIONS"))
                {
                    var webactionwidget = expressionArray[1].Split('[')[0];
                    var webactionsdata = await ApiHelper.GetWebActionsDataAsync(developerId, webactionwidget, isNFSite ? business.tag.Value : business.websiteid.Value, themeid, isCacheEnabled);
                    if (webactionsdata != null)
                    {
                        return webactionsdata.Data;
                    }

                }
                else if (expressionArray != null && expressionArray.Length > 0)
                {
                    var count = 0;
                    foreach (var value in expressionArray)
                    {
                        if (count != 0)
                        {
                            if (value.Contains("["))
                            {

                                var dataElement = value.ToLower().Split('[');
                                dataObject = dataObject[dataElement[0]];
                                var index = dataElement[1].Replace("]", "");
                                int numValue; var detailValue = string.Empty;
                                bool parsed = Int32.TryParse(index, out numValue);
                                bool isDetailViewObject = false;
                                if (!parsed)
                                {
                                    isDetailViewObject = true;
                                    if (isDetailsView)
                                        detailValue = viewDetails.currentpagenumber;
                                    else
                                    {
                                        numValue = 0;
                                        detailValue = numValue.ToString();
                                    }

                                }
                                else
                                {
                                    detailValue = numValue.ToString();
                                }
                                var tempExpression = string.Join(".", expressionArray);
                                dataObject = DataHandler.GetElementFromArray(dataObject, detailValue, queryString, tempExpression, viewDetails, isDetailsView, isDetailViewObject, isNFSite);

                            }
                            else
                            {
                                dataObject = dataObject[value];
                                var tempData = (object)dataObject;
                                if (tempData.GetType().Name.Equals("JValue"))
                                {
                                    dataObject = dataObject.Value;
                                }
                            }

                        }
                        else
                        {
                            if(business.GetType() == typeof(Newtonsoft.Json.Linq.JObject) && ((Newtonsoft.Json.Linq.JObject)business).SelectToken(value) != null)
                            {
                                if(((Newtonsoft.Json.Linq.JObject)business).SelectToken(string.Join(".", expressionArray)) != null)
                                {
                                    return ((Newtonsoft.Json.Linq.JObject)business).SelectToken(string.Join(".", expressionArray));
                                }
                                dataObject = ((Newtonsoft.Json.Linq.JObject)business).SelectToken(value);
                            }
                            else
                            {
                                dataObject = business;
                            }
                        }

                        count++;
                    }

                }
                else
                {
                    dataObject = expressionArray[0];
                }

                return dataObject;
            }
            catch (Exception ex)
            {

            }

            return null;
        }
    }
}
