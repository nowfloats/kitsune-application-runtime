using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Kitsune.Helper;
using Kitsune.Language.Models;
using System.Web;

namespace KitsuneLayoutManager.Helper.TagProcessors
{
    public class KDLProcessor : TagProcessor
    {
        public KDLProcessor()
        {
            TagProcessorIdentifier = "KDLProcessor";
        }

        /// <summary>
        /// Process nodes with KDL tag.
        /// </summary>
        public override void Process(ref HtmlNode node, HtmlAttribute dynamicAttribute, Dictionary<string, AliasReference> classNameAlias, Dictionary<int, string> classNameAliasdepth, int depth, string websiteId, ExpressionEvaluator evaluator, KEntity entity, dynamic websiteData, Models.Pagination viewDetails, string queryString, Dictionary<string, long> functionLog, bool isDetailsView = false, bool isNFSite = false, string developerId = null)
        {
            var kdlValue = dynamicAttribute.Value;
            string rootUrlString = string.Format("{0}.rootaliasurl.url", entity?.EntityName?.ToLower());
            if (kdlValue.IndexOf(rootUrlString) < 0)
            {
                kdlValue = String.Format("[[{0}]]{1}{2}", rootUrlString, ( kdlValue.StartsWith("/")? "" : "/" ), kdlValue);
            }
            var attributeValue = HtmlHelper.GetExpressionFromElement(kdlValue, 0);
            if (attributeValue != null && attributeValue.Any())
            {
                List<string> customVariables = new List<string>();
                List<string> objects = null;
                string dlObjects = "";
                string[] baseObj;

                #region Parse and get all runtime variables
                foreach (var attr in attributeValue)
                {
                    baseObj = null;
                    try
                    {
                        objects = Kitsune.SyntaxParser.Parser.GetObjects(Helper.TrimDelimiters(attr.Value));

                        foreach (var obj in objects)
                        {
                            baseObj = Helper.TrimDelimiters(obj).Split('.');
                            if (baseObj.Length == 1)
                            {
                                var baseClass = entity.Classes.FirstOrDefault(x => x.ClassType == KClassType.BaseClass && x.Name?.ToLower() == baseObj[0].ToLower());
                                if (baseClass == null)
                                {
                                    if (!classNameAlias.ContainsKey(baseObj[0].ToLower()))
                                    {
                                        customVariables.Add(baseObj[0]);
                                    }
                                }
                            }
                            else if ((obj.ToLower().IndexOf("k_obj_ind") > 0 && attr.Value == attributeValue[attributeValue.Count - 1].Value) || classNameAlias.ContainsKey(baseObj[0].ToLower()))
                            {
                                dlObjects = GetdlObject(baseObj);
                            }
                        }
                    }
                    catch { }
                }
                #endregion

                if (customVariables != null && customVariables.Any())
                {
                    string value = string.Empty;
                    var dataPath = string.Empty;
                    var custRegex = string.Empty;

                    //Initialize custom variables form the url segments
                    foreach (var customVariable in customVariables)
                    {
                        try
                        {
                            var dynamicVariable = customVariable.Trim();
                            if (!String.IsNullOrEmpty(dynamicVariable))
                            {
                                dynamicVariable = customVariable.ToLower();
                                var splitTemp = kdlValue.ToLower().Split('/');
                                bool isEncoded = false;
                                for (int i = 0; i < splitTemp.Length; i++)
                                {
                                    if (splitTemp[i].Contains(dynamicVariable))
                                    {
                                        custRegex = KitsuneCommonUtils.GenerateUrlPatternRegex(splitTemp[i], "");
                                        dataPath = $"urlsegments[{i}]";
                                        if (splitTemp[i].ToLower().Contains("urlencode()"))
                                        {
                                            isEncoded = true;
                                        }
                                        //dataPath = $"_system.request.urlsegments[{i}]";
                                        break;
                                    }
                                }

                                value = string.Empty;
                                if (!string.IsNullOrEmpty(dataPath))
                                {
                                    var httpRequestObject = websiteData["_system"]["request"];
                                    value = ((string)httpRequestObject.SelectToken(dataPath))?.Trim('/');

                                    if (!String.IsNullOrEmpty(value) && !String.IsNullOrEmpty(custRegex))
                                        value = new Regex(custRegex).Match(value)?.Groups[1]?.Value;
                                    if (isEncoded)
                                    {
                                        value = HttpUtility.UrlPathEncode(value).Replace("-", " ");
                                    }
                                }
                                AliasReference aliasReference = new AliasReference();
                                aliasReference.iteration = -1;
                                aliasReference.maxIteration = -1;
                                aliasReference.referenceObject = value;
                                if (!classNameAlias.ContainsKey(dynamicVariable))
                                {
                                    classNameAlias.Add(dynamicVariable, aliasReference);
                                }
                            }
                        }
                        catch { }
                    }
                }

                if (dlObjects != "")
                {
                    string referenceObjectKey;
                    string baseObject = dlObjects.Split('.')[0];
                    if (classNameAlias.ContainsKey(baseObject))
                    {
                        referenceObjectKey = classNameAlias[baseObject].referenceObject;
                    }
                    else
                    {
                        referenceObjectKey = dlObjects.Split(new string[] { "[k_obj_ind]" }, StringSplitOptions.None)[0];
                    }
                    dynamic referenceObject = evaluator.EvaluateExpression(referenceObjectKey, entity, viewDetails, classNameAlias, websiteData, websiteData?._system?.kresult, queryString, out bool hasData, functionLog, isDetailsView, isNFSite, developerId);
                    long objSize = evaluator.GetObjectSize(referenceObject);
                    if (referenceObject.GetType() != typeof(string) && objSize > 0)
                    {
                        //Defaults to first object for nfsite preview.
                        int objIndex = 0;
                        for (int index = 0; index < objSize; index++)
                        {
                            dynamic element = evaluator.GetElementFromArray(referenceObject, index.ToString());
                            if (element?[queryString]?.Value?.ToString()?.ToLower() == viewDetails.currentpagenumber)
                            {
                                objIndex = index;
                                break;
                            }
                        }

                        //Required for new scenarios where custom objects are not replaced by compiler
                        if (classNameAlias.ContainsKey(baseObject))
                        {
                            referenceObjectKey += "[" + objIndex + "]";
                            classNameAlias.Remove(baseObject);
                            classNameAlias.Add(baseObject, new AliasReference
                            {
                                referenceObject = referenceObjectKey,
                                iteration = -1,
                                maxIteration = -1
                            });
                        }
                        //For backward compatibility using replacement
                        else
                        {
                            AliasReference k_obj_ind_alias = new AliasReference
                            {
                                referenceObject = null,
                                iteration = objIndex,
                                maxIteration = objIndex
                            };
                            classNameAlias.Add("k_obj_ind", k_obj_ind_alias);
                        }
                    }
                }
            }
        }

        private static string GetdlObject(string[] baseObj)
        {
            string dlObjects;
            List<string> objStringList = baseObj.ToList();
            objStringList.RemoveAt(baseObj.Length - 1);
            baseObj = objStringList.ToArray<string>();
            dlObjects = String.Join(".", baseObj).ToLower();
            return dlObjects;
        }
    }
}
