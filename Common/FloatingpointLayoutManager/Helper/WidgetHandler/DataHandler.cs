using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Kitsune.Server.Model;
using Kitsune.Server.Model.Kitsune;
using Kitsune.Helper;
using Kitsune.Language.Models;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.CSharp.RuntimeBinder;
using System.Net;
using System.Reflection;
using KitsuneLayoutManager.Models;
using Newtonsoft.Json;
using System.Collections;
using System.Diagnostics;
using Kitsune.SyntaxParser;
using System.Dynamic;

namespace KitsuneLayoutManager.Helper.WidgetHandler
{
    public class DataHandler
    {
        internal static List<string> ignoreTags = new List<string>() { "[[___KIT_CSRF_TOKEN___]]" };
        internal static async Task<string> ReplaceWidgetDataAsync(string rootAliasUri, bool isCacheEnabled, HtmlNode documentNode, List<string> lines, dynamic fpModel, string themeid, string queryString, Models.Pagination viewDetails, string view = null, Dictionary<string, long> functionLog = null, bool isDetailsView = false,bool isNFSite=false)
        {
            try
            {
                var exceptionWidget = string.Empty;
                var webactionsApiDictionary = new Dictionary<string, dynamic>();
                var searchApiDictionary = new Dictionary<string, dynamic>();
                var functionStopWatch = new Stopwatch();

                for (int index = 0; index < lines.Count; index++)
                {
                    var line = lines[index];
                    var attributeValue = HtmlHelper.GetExpressionFromElement(line, index);
                    if (attributeValue != null && attributeValue.Any())
                    {
                        foreach (var attr in attributeValue)
                        //Parallel.ForEach(attributeValue, attr =>
                        {
                            if (!ignoreTags.Contains(attr.Value))
                            {
                                var expression = string.Empty;
                                var tempVal = attr.Value;
                                var tempMatch = attr.Value.Trim('[', ']');
                                var matches = Parser.GetObjects(tempMatch);

                                //foreach (var mat in matches)
                                //Parallel.ForEach(matches, mat =>
                                for (int i = 0; i < matches.Count; i++)
                                {
                                    var mat = matches[i];

                                    expression = mat.ToString().Replace("[[", "").Replace("]]", "");
                                    var expressionValue = await ExpressionEvaluatorAsync(expression, rootAliasUri, fpModel, queryString, viewDetails, view, webactionsApiDictionary, searchApiDictionary, functionLog, themeid, isCacheEnabled, isDetailsView, isNFSite);

                                    tempVal = ReplaceFirstOccurrence(tempVal, expression, expressionValue);
                                } //);

                                var expressionValue2 = !string.IsNullOrEmpty(tempVal.Trim('[', ']')) ? Parser.Execute(tempVal.Trim('[', ']')) : "";
                                line = ReplaceFirstOccurrence(line, attr.Value, WebUtility.HtmlDecode(expressionValue2?.ToString()));
                                lines[index] = line;
                            }
                        } //);
                    }
                }

                var newDocument = new HtmlDocument();
                newDocument.LoadHtml(string.Join("\n", lines.ToArray()));

                functionStopWatch.Start();
                List<HtmlNode> widgetsToRemove = new List<HtmlNode>();
                foreach (var kshowWidget in newDocument.DocumentNode.Descendants().Where(x => x.Attributes["k-show"] != null && !string.IsNullOrEmpty(x.Attributes["k-show"].Value)))
                {
                    if (KSelectHandle(rootAliasUri, newDocument.DocumentNode, kshowWidget, fpModel))
                        widgetsToRemove.Add(kshowWidget);
                }

                foreach (var khideWidget in newDocument.DocumentNode.Descendants().Where(x => x.Attributes["k-hide"] != null && !string.IsNullOrEmpty(x.Attributes["k-hide"].Value)))
                {
                    if (!KSelectHandle(rootAliasUri, newDocument.DocumentNode, khideWidget, fpModel))
                        widgetsToRemove.Add(khideWidget);
                }

                if (widgetsToRemove.Any())
                {
                    for (int i = 0; i < widgetsToRemove.Count; i++)
                    {
                        widgetsToRemove[i].ParentNode.RemoveChild(widgetsToRemove[i]);
                    }
                }
                if (functionLog != null)
                    functionLog.Add("K SHOW LOGIC", functionStopWatch.ElapsedMilliseconds);

                functionStopWatch.Reset();


                var attrList = newDocument.DocumentNode.SelectNodes("//*/@*[starts-with(local-name(), 'k-')]");
                if (attrList != null && attrList.Count() > 0)
                {
                    foreach (var tempNode in attrList)
                    {
                        var attrName = new List<String>();
                        foreach (var attr in tempNode.Attributes)
                        {
                            if (!attr.Name.StartsWith("k-pay",StringComparison.InvariantCultureIgnoreCase) && attr.Name.StartsWith("k-"))
                            {
                                attrName.Add(attr.Name);
                            }
                        }

                        foreach (var attr in attrName)
                        {
                            tempNode.Attributes.Remove(attr);
                        }

                    }
                }

                var htmlString = newDocument.DocumentNode.OuterHtml;

                return htmlString;
            }

            catch (Exception ex)
            {

            }

            return null;
        }

        public static string ReplaceFirstOccurrence(string source, string find, string replace)
        {
            try
            {
                if (replace == null)
                    replace = String.Empty;
                int place = source.IndexOf(find);
                if (place > 0)
                {
                    return source.Remove(place, find.Length).Insert(place, replace);
                }

                return source.Replace(find, replace);
            }
            catch(Exception ex)
            {
                return source;
            }
            
        }

        internal static string GetReplacedValue(string actualString, string pattern, string replaceValue)
        {
            try
            {
                actualString = actualString.Replace(pattern, replaceValue);
            }
            catch (Exception ex)
            {

            }

            return actualString;
        }

        internal static bool KSelectHandle(string rootAliasUrl, HtmlNode masternode, HtmlNode widget, dynamic business)
        {
            try
            {
                var htmlString = widget.OuterHtml; bool hasValueChanged = true;
                if (hasValueChanged)
                {
                    object result = null; var isShow = false;
                    if (widget.Attributes["k-show"] != null)
                    {
                        result = Parser.Execute(widget.Attributes["k-show"].Value.Replace("{", "").Replace("}", ""));
                        isShow = true;
                    }
                    else if (widget.Attributes["k-hide"] != null)
                    {
                        result = Parser.Execute(widget.Attributes["k-hide"].Value.Replace("{", "").Replace("}", ""));
                    }

                    if (result.ToString()?.ToLower() != "true")
                    {
                        return true;
                    }
                    else
                    {
                        if (isShow)
                        {
                            widget.Attributes["k-show"].Value = result.ToString();
                        }
                        else
                        {
                            widget.Attributes["k-hide"].Value = result.ToString();
                        }

                        return false;
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return false;
        }
        public static object GetDeepPropertyValue(object instance, string path)
        {
            var pp = path.Split('.');
            Type t = instance.GetType();
            foreach (var prop in pp)
            {
                PropertyInfo propInfo = t.GetProperty(prop);
                if (propInfo != null)
                {
                    instance = propInfo.GetValue(instance, null);
                    t = propInfo.PropertyType;
                }
                else throw new ArgumentException("Properties path is not correct");
            }
            return instance;

        }

        private static dynamic GetOfferDetailsObject(dynamic offersArray, string index)
        {
            try
            {
                Offer[] finalupdateArray = offersArray.ToObject<Offer[]>();
                var updateElement = finalupdateArray.ToList().Where(s => s.index.Equals(index)).FirstOrDefault();
                if (updateElement != null)
                {

                    var update = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(updateElement)); ;
                    dynamic updateValue = update;

                    return updateValue;
                }

            }
            catch (Exception ex)
            {
            }

            return null;
        }

        private static dynamic GetCustomPagesDetailsObject(dynamic offersArray, string index)
        {
            try
            {
                CustomPagesModel[] finalupdateArray = offersArray.ToObject<CustomPagesModel[]>();
                var updateElement = finalupdateArray.ToList().Where(s => s.id.Equals(index)).FirstOrDefault();
                if (updateElement != null)
                {

                    var update = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(updateElement)); ;
                    dynamic updateValue = update;

                    return updateValue;
                }

            }
            catch (Exception ex)
            {
            }

            return null;
        }

        private static dynamic GetProductDetailsObject(dynamic offersArray, string index)
        {
            try
            {
                Product[] finalupdateArray = offersArray.ToObject<Product[]>();
                var updateElement = finalupdateArray.ToList().Where(s => s.index.Equals(index)).FirstOrDefault();
                if (updateElement != null)
                {

                    var update = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(updateElement)); ;
                    dynamic updateValue = update;

                    return updateValue;
                }

            }
            catch (Exception ex)
            {
            }

            return null;
        }

        private static dynamic GetUpdateDetailsObject(dynamic updatesArray, string index)
        {
            try
            {

                Update[] finalupdateArray = updatesArray.ToObject<Update[]>();
                var updateElement = finalupdateArray.ToList().Where(s => s.index.Equals(index)).FirstOrDefault();
                if (updateElement != null)
                {

                    var update = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(updateElement)); ;
                    dynamic updateValue = update;

                    return updateValue;
                }

            }
            catch (Exception ex)
            {
            }

            return null;
        }

        private static dynamic GetDetailsObjectFromArray(dynamic arrayObject, string uniqueValue, string queryString)
        {
            try
            {
                dynamic array = new ExpandoObject();
                array = arrayObject;

                foreach (var elem in array)
                {
                    var indexValue = elem[queryString].ToString();
                    if (indexValue.Equals(uniqueValue))
                    {
                        return elem;
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return null;
        }

        private static dynamic GetWebActionDetailsObject(dynamic webactionsArray, string index)
        {
            try
            {
                var detailObject = ((IEnumerable<dynamic>)webactionsArray).Where(d => d._id.Value == index).FirstOrDefault();
                return detailObject;
            }
            catch (Exception ex)
            {
            }

            return null;
        }


        internal static async Task<string> ExpressionEvaluatorAsync(string expressionString, string rootAliasUrl, dynamic Business, string queryString, Models.Pagination viewDetails, string view, Dictionary<string, dynamic> webactionsApiDictionary, Dictionary<string, dynamic> searchApiDictionary, Dictionary<string, long> functionalLog, string themeid, bool isCacheEnabled = false, bool isDetailsView = false, bool isNFSite = false, string developerId = null)
        {
            try
            {
                var expression = expressionString;
                expression = expression.ToLower();
                var expressionArray = expression.Split('.');
                dynamic dataObject = null;

                #region WEB ACTION
                if (expressionArray != null && expressionArray.Length > 0 && expressionArray[0].ToLower().Equals("webactions"))
                {
                    var keyExists = functionalLog.ContainsKey("WEBACTIONS HANDLER"); long counter = 0;
                    if (keyExists)
                    {
                        counter = functionalLog["WEBACTIONS HANDLER"];
                    }
                    var count = 0;
                    expressionArray = expressionString.Split('.');
                    var webactionwidget = expressionArray[1].Split('[')[0];

                    if (webactionsApiDictionary != null && webactionsApiDictionary.Count() > 0)
                    {
                        if (webactionsApiDictionary.ContainsKey(webactionwidget.ToLower()))
                        {
                            dataObject = webactionsApiDictionary[webactionwidget.ToLower()];
                        }
                    }

                    if (dataObject == null)
                    {
                        dataObject = await ApiHelper.GetWebActionsDataAsync(developerId, webactionwidget, isNFSite ? Business.tag.Value : Business.websiteid.Value, themeid, isCacheEnabled);
                        webactionsApiDictionary.Add(webactionwidget.ToLower(), dataObject);
                    }


                    if (dataObject != null)
                    {
                        var extraObject = dataObject["Extra"];
                        //dataObject = dataObject["Data"];
                        if (expressionArray.Length > 1)
                            expressionArray[1] = expressionArray[1].Replace(webactionwidget, "Data");
                        foreach (var expressionValue in expressionArray)
                        {
                            if (count > 0)
                            {
                                var value = expressionValue.Trim('[', ']');
                                if (value.Contains("["))
                                {
                                    var dataElement = value.Split('[');
                                    dataObject = dataObject[dataElement[0]];
                                    var index = dataElement[1].Replace("]", "");
                                    int numValue;
                                    bool parsed = Int32.TryParse(index, out numValue);
                                    if (!parsed)
                                        numValue = 0;

                                    if (isDetailsView && !parsed)
                                    {
                                        dataObject = GetElementFromArray(dataObject, index, "_id", expression, viewDetails, isDetailsView, false, isNFSite);
                                    }
                                    else
                                    {
                                        dataObject = GetElementFromArray(dataObject, numValue.ToString(), "_id", expression, viewDetails, false, false, isNFSite);
                                    }


                                }
                                else
                                {
                                    if (value.Contains("length"))
                                    {
                                        dataObject = extraObject["TotalCount"];

                                        return dataObject.ToString();
                                    }

                                    else if (value.ToLower().Equals("replace"))
                                    {
                                        object tempDataObject = dataObject[value];
                                        var type = tempDataObject.GetType();
                                        if (type.Name.Equals("JValue"))
                                        {
                                            var tempString = value.Trim(')').Split('(')[1].Split(',');
                                            var pattern = tempString[0]; var valueString = tempString[1];
                                            dataObject = GetReplacedValue(dataObject, pattern, valueString);
                                        }
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

                        if (dataObject != null)
                        {
                            object tempDataObject = dataObject;
                            var type = tempDataObject.GetType();

                            if (type.Name.Equals("JArray"))
                            {
                                return dataObject.ToString();
                            }
                            else if (type.Name.Equals("Double"))
                            {
                                return "" + dataObject + "";
                            }
                            else if (type.Name.Equals("Int64"))
                            {
                                return "" + dataObject + "";
                            }
                            else if (type.Name.Equals("Int32"))
                            {
                                return "" + dataObject + "";
                            }
                            return "'" + WebUtility.HtmlEncode(dataObject.ToString()) + "'";

                        }
                        else
                        {
                            return "''";
                        }
                    }
                    else
                    {
                        return "''";
                    }


                }
                #endregion

                #region NAVIGATION LINKS
                else if (expression.Contains("currentpagenumber"))
                {
                    return viewDetails.currentpagenumber;
                }
                else if (expression.Contains("previous") || expression.Contains("next"))
                {
                    if (expression.Contains("previous"))
                    {
                        return "'" + WebUtility.HtmlEncode(viewDetails.prevpage.url) + "'";
                    }
                    else
                    {
                        return "'" + WebUtility.HtmlEncode(viewDetails.nextpage.url) + "'";
                    }
                }

                #endregion

                #region BUSINESS HANDLER
                if (expressionArray != null && expressionArray.Length > 1)
                {
                    var count = 0;

                    foreach (var expressionValue in expressionArray)
                    {
                        var value = expressionValue.Trim().Trim('[').Trim(']');
                        if (value.ToLower().Contains("length"))
                        {
                            return GetObjectSize(dataObject).ToString();
                        }
                        else if (value.ToLower().Contains("substr"))
                        {
                            var index = value.IndexOf('(');
                            return dataObject + "." + value.Replace(value.Substring(0, index), value.Substring(0, index).ToLower());
                        }
                        else
                        {
                            if (count != 0 && !value.ToLower().Contains("["))
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
                            else if (value.ToLower().Contains("["))
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

                                // change for details view
                                dataObject = GetElementFromArray(dataObject, detailValue, queryString, expression, viewDetails, isDetailsView, isDetailViewObject, isNFSite);

                            }
                            else
                            {
                                if (Business[value] != null)
                                {
                                    dataObject = Business[value];
                                }
                                else
                                {
                                    dataObject = Business;
                                }
                            }
                        }

                        count++;
                    }

                    if (dataObject != null)
                    {
                        object tempDataObject = dataObject;
                        var type = tempDataObject.GetType();

                        if (type.Name.Equals("JArray"))
                        {
                            return dataObject.ToString();
                        }
                        else if (type.Name.Equals("Double"))
                        {
                            return "" + dataObject + "";
                        }
                        else if (type.Name.Equals("Int64"))
                        {
                            return "" + dataObject + "";
                        }
                        else if (type.Name.Equals("Int32"))
                        {
                            return "" + dataObject + "";
                        }

                        return "'" + WebUtility.HtmlEncode(dataObject.ToString()) + "'";

                    }
                    else
                    {
                        return "''";
                    }
                }

                #endregion

            }
            catch (Exception ex)
            {
                var x = ex.ToString();
                var y = x;
                var z = y;
            }
            return "''";

        }
        private static bool DynamicFieldExists(dynamic obj, int field)
        {
            bool retval = false;
            try
            {
                dynamic finalObject = null;
                finalObject = obj;
                var temp = finalObject[field];
                retval = true;
            }
            catch (RuntimeBinderException) { }
            catch (Exception ex)
            {

            }
            return retval;
        }
        private static dynamic GetValueFromArray(dynamic obj, int field)
        {
            try
            {
                var temp = obj[field];
                return temp;
            }
            catch (Exception ex)
            {

            }

            return null;
        }
        internal static object GetElementFromArray(dynamic arrrayObject, string elementIndex, string queryString, string expression, Models.Pagination viewDetails, bool isDetailsView = false, bool isDetailsViewObject = false,bool isNFSite=false)
        {
            try
            {
                if (isNFSite && isDetailsView && isDetailsViewObject && (expression.Contains(".products") || expression.Contains(".updates") || expression.Contains(".offers") || expression.Contains("custompages")) && viewDetails.currentpagenumber.Equals(elementIndex.ToString()))
                {
                    if (expression.Contains("products"))
                    {
                        return GetProductDetailsObject(arrrayObject, elementIndex);
                    }
                    else if (expression.Contains("updates"))
                    {
                        return GetUpdateDetailsObject(arrrayObject, elementIndex);
                    }
                    else if (expression.Contains("offers"))
                    {
                        return GetOfferDetailsObject(arrrayObject, elementIndex);
                    }
                    else if (expression.Contains("custompages"))
                    {
                        return GetCustomPagesDetailsObject(arrrayObject, elementIndex);
                    }
                }
                else if (isDetailsView && expression.ToLower().Contains("webactions") && (expression.Contains("[i]") || expression.Contains("[k_obj_ind]")))
                {
                    return GetWebActionDetailsObject(arrrayObject, viewDetails.currentpagenumber);
                }

                object arrayObject = arrrayObject;
                if (arrayObject != null)
                {
                    try
                    {
                        try
                        {
                            return ((dynamic)arrayObject)["_" + elementIndex];
                        }
                        catch (RuntimeBinderException)
                        {
                            var valueExist = DynamicFieldExists(arrayObject, Convert.ToInt32(elementIndex));
                            if (valueExist)
                            {
                                return GetValueFromArray(arrayObject, Convert.ToInt32(elementIndex));
                            }
                        }
                    }
                    catch (Exception ex1)
                    {
                        try
                        {
                            return GetDetailsObjectFromArray(arrrayObject, elementIndex, queryString);
                        }
                        catch (Exception ex2) { throw ex2; }
                        throw ex1;
                    }
                }

                //if (isDetailsView && isDetailsViewObject && (expression.Contains(".products") || expression.Contains(".updates") || expression.Contains(".offers") || expression.Contains("custompages")) && viewDetails.currentpagenumber.Equals(elementIndex.ToString()))
                //{
                //    if (expression.Contains("products"))
                //    {
                //        return GetProductDetailsObject(arrrayObject, elementIndex);
                //    }
                //    else if (expression.Contains("updates"))
                //    {
                //        return GetUpdateDetailsObject(arrrayObject, elementIndex);
                //    }
                //    else if (expression.Contains("offers"))
                //    {
                //        return GetOfferDetailsObject(arrrayObject, elementIndex);
                //    }
                //    else if (expression.Contains("custompages"))
                //    {
                //        return GetCustomPagesDetailsObject(arrrayObject, elementIndex);
                //    }
                //    if (!String.IsNullOrEmpty(queryString))
                //    {
                //        return GetDetailsObjectFromArray(arrrayObject, elementIndex, queryString);
                //    }
                //}
                //else if (isDetailsView && expression.ToLower().Contains("webactions") && expression.Contains("[i]"))
                //{
                //    return GetWebActionDetailsObject(arrrayObject, viewDetails.currentpagenumber);
                //}
                //object arrayObject = arrrayObject;
                //if (arrayObject != null)
                //{
                //    var valueExist = DynamicFieldExists(arrayObject, Convert.ToInt32(elementIndex));
                //    if (valueExist)
                //    {
                //        return GetValueFromArray(arrayObject, Convert.ToInt32(elementIndex));
                //    }
                //}
            }
            catch (Exception ex)
            {
                var x = ex;
                var y = x;
                var z = y;
            }
            return null;
        }
        internal static long GetObjectSize(dynamic obj)
        {
            try
            {
                if (obj != null)
                {
                    dynamic objectValue = null;
                    objectValue = obj;
                    var tempObj = obj;
                    var tempObjType = tempObj.GetType().Name;
                    if (tempObjType.Equals("String"))
                    {
                        return obj.ToString().Length;
                    }
                    var objLength = 0;
                    try
                    {
                        objLength = obj._total;
                    }
                    catch (RuntimeBinderException)
                    {
                        objLength = objectValue.Count;
                    }
                    return objLength;
                }
            }
            catch (Exception ex)
            {

            }

            return 0;
        }

    }
}
