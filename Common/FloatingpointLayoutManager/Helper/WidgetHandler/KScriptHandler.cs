using HtmlAgilityPack;
using Kitsune.Helper;
using Kitsune.SyntaxParser;
using KitsuneLayoutManager.Models;
using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace KitsuneLayoutManager.Helper.WidgetHandler
{
    public class KScriptHandler
    {
        public static void KScriptReplacer(HtmlNode documentNode, dynamic websiteDetails, string rootaliasurl, Pagination ViewDetails,string themeId, string view, string queryString, Dictionary<string, long> functionLog = null, bool isDetailsView = false,bool isNFSite=false, bool isCacheEnabled = false)
        {
            try
            {
                var scriptNodes = documentNode.DescendantsAndSelf().Where(x => x.Attributes["get-api"] != null).ToList();
                if(scriptNodes != null && scriptNodes.Any())
                {
                    foreach(var scriptNode in scriptNodes)
                    {
                        ScriptDataHandlerAsync(scriptNode, websiteDetails, rootaliasurl, ViewDetails, themeId, view, isDetailsView, queryString, functionLog, isNFSite, isCacheEnabled);
                    }
                }
            }
            catch(Exception ex)
            {

            }

        }

        private static async Task ScriptDataHandlerAsync(HtmlNode widgetNode, dynamic websiteDetails, string rootaliasurl, Pagination ViewDetails, string themeId, string view, bool isDetailsView, string queryString, Dictionary<string, long> functionLog = null,bool isNFSite=false, bool isCacheEnabled = false)
        {
            try
            {
                var apiUrl = widgetNode.GetAttributeValue("get-api", null);
                var input = widgetNode.GetAttributeValue("input", null);
                var headers = widgetNode.GetAttributeValue("headers", null);

                var webactionsApiDictionary = new Dictionary<string, dynamic>();
                var searchApiDictionary = new Dictionary<string, dynamic>();

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
                            if(elem.Contains("]") || elem.Contains("["))
                            {
                                var output = await DataHandler.ExpressionEvaluatorAsync(elem, rootaliasurl, websiteDetails, queryString, ViewDetails, view, webactionsApiDictionary, searchApiDictionary, functionLog, themeId, false, isDetailsView, isNFSite);
                                widgetNode.SetAttributeValue("input", input.Replace(tempInputArray[count], output.Trim('\'')));
                                tempUrl = tempUrl.Replace("{" + count + "}", output.Trim('\''));
                            }
                            else
                            {
                                tempUrl = tempUrl.Replace("{" + count + "}", elem);
                            }

                            count++;
                        }

                        
                    }

                    widgetNode.SetAttributeValue("get-api", tempUrl);

                    #endregion

                    if(!string.IsNullOrEmpty(headers))
                    {
                        var headersArray = headers.Trim('[').Trim(']').Split(',');
                        foreach(var header in headersArray)
                        {
                            var separator = header.Split(':');
                            headerDict.Add(separator[0].Trim(), separator[1].Trim());
                        }
                    }

                    var result = await ApiHelper.GetResponseFromKScriptAsync(tempUrl, headerDict, isCacheEnabled, rootaliasurl, websiteDetails?._id);
                    //TO-DO optimize this code
                    if(result != null)
                    {
                        try
                        {
                            result["_system"] = websiteDetails["_system"];
                        }
                        catch { }
                        try
                        {
                            widgetNode.Attributes.Remove("get-api");
                            widgetNode.Attributes.Remove("input");
                            widgetNode.Attributes.Remove("headers");
                        }
                        catch { }
                        RepeatHelper.RepeatHtmlNodesAsync(widgetNode, result, null, rootaliasurl, null, ViewDetails, themeId, false, false, true,false);
                        var innerHtml = DataReplacer(widgetNode.InnerHtml, result);
                        if(!string.IsNullOrEmpty(innerHtml))
                        {
                            widgetNode.InnerHtml = innerHtml;
                        }
                        else
                        {
                            widgetNode.ParentNode.RemoveChild(widgetNode);
                        }
                    }
                    else
                    {
                        widgetNode.ParentNode.RemoveChild(widgetNode);
                    }

                }
            }
            catch(Exception ex)
            {

            }
        }

        private static bool DynamicFieldExists(dynamic obj, int field)
        {
            bool retval = false;
            try
            {
                // can't write the following:
                var temp = obj[field];
                retval = true;
            }
            catch (RuntimeBinderException) { }
            catch (Exception ex)
            {
                EventLogger.Write(ex, "Kitsune Helper :: DynamicFieldExists ", null);
            }
            return retval;
        }

        private static object GetElementFromArray(dynamic arrayObject, string elementIndex)
        {
            try
            {
                if (arrayObject != null)
                {
                    var valueExist = DynamicFieldExists(arrayObject, Convert.ToInt32(elementIndex));
                    if (valueExist)
                    {
                        return GetValueFromArray(arrayObject, Convert.ToInt32(elementIndex));
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return null;
        }

        private static dynamic GetValueFromArray(dynamic obj, int field)
        {
            try
            {
                return obj[field];
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return null;
        }

        private static string ExpressionEvaluator(string expressionString, dynamic data, bool isArray = false)
        {
            try
            {
                var expressionArray = expressionString.Split('.');
                var dataObject = data; var count = 0;
                foreach (var expressionValue in expressionArray)
                {
                    var value = expressionValue.Trim('[', ']');

                    if(count == 0 && isArray)
                    {
                        var dataElement = value.Split('[');
                        var index = dataElement[1].Replace("]", "");
                        int numValue;
                        bool parsed = Int32.TryParse(index, out numValue);
                        if (!parsed)
                            numValue = 0;

                        dataObject = GetElementFromArray(dataObject, numValue.ToString());
                    }
                    else
                    {
                        if (value.Contains("["))
                        {
                            var dataElement = value.Split('[');
                            dataObject = dataObject[dataElement[0]];
                            var index = dataElement[1].Replace("]", "");
                            int numValue;
                            bool parsed = Int32.TryParse(index, out numValue);
                            if (!parsed)
                                numValue = 0;

                            dataObject = GetElementFromArray(dataObject, numValue.ToString());
                        }
                        else
                        {
                            if (value.Contains("length"))
                            {
                                if (dataObject.GetType().Name == "String")
                                    return dataObject.Length.ToString();
                                else
                                    return dataObject.Count.ToString();
                            }
                            else
                            {
                                object tempDataObject = dataObject[value];
                                var type = tempDataObject.GetType();
                                if (type.Name.Equals("JValue"))
                                {
                                    dataObject = dataObject[value].Value;
                                }
                                else
                                {
                                    dataObject = dataObject[value];
                                }
                            }
                        }
                    }
                  
                    count++;
                }

                decimal decimalvalue = 0;
                if (decimal.TryParse(dataObject.ToString(), out decimalvalue))
                    return decimalvalue.ToString();
                else
                    return "'" + dataObject.ToString() + "'";
            }



            catch (Exception ex)
            {

            }

            return "''";
        }

        private static List<string> validationPointsForKscriptReplacer = new List<string>() { "kresult", "_system" };
        /// <summary>
        /// Data replacer for only k-script
        /// </summary>
        /// <param name="htmlString"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private static string DataReplacer(string htmlString, dynamic data)
        {
            try
            {
                string[] lines = htmlString.Split('\n');

                bool isArray = false;
                object tempDataObject = data;
                var type = tempDataObject.GetType();

                if(type.Name.Equals("JArray"))
                {
                    isArray = true;   
                }
                for (int index = 0; index < lines.Length; index++)
                {
                    var line = lines[index];
                    var attributeValue = HtmlHelper.GetExpressionFromElement(line, index);
                    if (attributeValue != null && attributeValue.Any())
                    {
                        if (attributeValue != null && attributeValue.Any())
                        {
                            foreach (var attr in attributeValue)
                            {
                                var expression = string.Empty;
                                var tempVal = WebUtility.HtmlDecode(attr.Value);
                                var tempMatch = WebUtility.HtmlDecode(attr.Value.Trim('[', ']'));
                                var matches = Parser.GetObjects(tempMatch);
                                var kresultFound = false;
                                foreach (var mat in matches)
                                {
                                    if (attr.Value != null && validationPointsForKscriptReplacer.FindIndex(x => mat.Trim().ToLower().StartsWith(x)) > -1)
                                    {
                                        expression = mat.ToString().Replace("[[", "").Replace("]]", "");
                                        var expressionValue = ExpressionEvaluator(expression, data, isArray);

                                        tempVal = tempVal.Replace(expression, expressionValue);
                                        kresultFound = true;
                                    }
                                }

                                if (String.Compare(tempVal, attr.Value) != 0 || kresultFound)
                                {
                                    var expressionValue2 = !string.IsNullOrEmpty(tempVal.Trim('[', ']')) ? Parser.Execute(tempVal.Trim('[', ']')) : "";
                                    line = line.Replace(attr.Value, WebUtility.HtmlDecode(expressionValue2?.ToString()));
                                }

                                lines[index] = line;
                            }
                        }

                    }
                }
               
                var outerHtml = (string.Join("\n", lines.ToArray()));
                return outerHtml;
            }
            catch (Exception ex)
            {

            }

            return null;

        }
    }
}
