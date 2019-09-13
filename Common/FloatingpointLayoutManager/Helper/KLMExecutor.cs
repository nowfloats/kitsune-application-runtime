using HtmlAgilityPack;
using Kitsune.Language.Helper;
using Kitsune.Language.Models;
using KitsuneLayoutManager.Helper.TagProcessors;
using KitsuneLayoutManager.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace KitsuneLayoutManager.Helper
{
    public class KLMExecutor
    {
        private List<string> dynamicTagDescriptors;
        private ProcessorFactory processorFactory;
        private KitsuneRequestUrlType requestType;
        private string[] BlackListKImgFileExtension = { "svg", "gif" };
        private string[] BlackListKImgDomain = { "firebasestorage.googleapis.com", "akamai.net", "cloudfront.net", "azureedge.net", "maps.googleapis.com", "maps.cit.api.here.com", "image.maps.cit.api.here.com", "dev.virtualearth.net" };

        public KLMExecutor(KitsuneRequestUrlType requestType)
        {
            dynamicTagDescriptors = GetDynamicTagDescriptors(typeof(LanguageAttributes));
            processorFactory = ProcessorFactory.GetProcessorFactory();
            this.requestType = requestType;
        }

        public string Execute(string customerId, KEntity entity, dynamic fpDetails, Pagination viewDetails, string queryString, HtmlDocument document, Dictionary<string, long> functionLog, bool isDetailsView = false, bool isNFSite = false)
        {
            try
            {
                Dictionary<string, AliasReference> classNameAlias = new Dictionary<string, AliasReference>();
                Dictionary<int, string> classNameAliasDepth = new Dictionary<int, string>();
                int depth = 0;
                bool isIteration = false;
                bool isRepeat = false;
                ExpressionEvaluator evaluator = new ExpressionEvaluator();

                HtmlNode readNode = document.DocumentNode.FirstChild;
                HtmlDocument finalDocument = new HtmlDocument();
                if (readNode.OuterHtml.ToLower() == "<!doctype html>")
                {
                    finalDocument.LoadHtml(readNode.OuterHtml);
                }
                else
                {
                    finalDocument.LoadHtml(GetProcessedNode(readNode, classNameAlias, classNameAliasDepth, depth, isRepeat, evaluator, customerId, entity, fpDetails, viewDetails, queryString, functionLog, isDetailsView, isNFSite).OuterHtml);
                }
                HtmlNode writeNode = finalDocument.DocumentNode.FirstChild;

                while (readNode.NodeType != HtmlNodeType.Document)
                {
                    if (isIteration)
                    {
                        readNode = readNode.FirstChild;
                        writeNode.InsertAfter(GetProcessedNode(readNode, classNameAlias, classNameAliasDepth, depth, isRepeat, evaluator, customerId, entity, fpDetails, viewDetails, queryString, functionLog, isDetailsView, isNFSite), writeNode.LastChild);
                        writeNode = writeNode.LastChild;
                        depth++;
                        isIteration = false;
                        isRepeat = true;
                    }
                    else if (readNode.HasChildNodes && writeNode.NodeType != HtmlNodeType.Comment)
                    {
                        readNode = readNode.FirstChild;
                        writeNode.PrependChild(GetProcessedNode(readNode, classNameAlias, classNameAliasDepth, depth, isRepeat, evaluator, customerId, entity, fpDetails, viewDetails, queryString, functionLog, isDetailsView, isNFSite));
                        writeNode = writeNode.FirstChild;
                        depth++;
                    }
                    else if (readNode.NextSibling != null)
                    {
                        if (classNameAliasDepth.ContainsKey(depth))
                        {
                            string classAlias = classNameAliasDepth[depth];
                            if (classNameAlias.ContainsKey(classAlias))
                                classNameAlias.Remove(classAlias);
                            classNameAliasDepth.Remove(depth);
                        }
                        readNode = readNode.NextSibling;
                        writeNode.ParentNode.InsertAfter(GetProcessedNode(readNode, classNameAlias, classNameAliasDepth, depth, isRepeat, evaluator, customerId, entity, fpDetails, viewDetails, queryString, functionLog, isDetailsView, isNFSite), writeNode);
                        writeNode = writeNode.NextSibling;
                    }
                    else
                    {
                        while (!isIteration && readNode.NextSibling == null && readNode.NodeType != HtmlNodeType.Document)
                        {
                            depth--;
                            readNode = readNode.ParentNode;
                            writeNode = writeNode.ParentNode;
                            if (classNameAliasDepth.ContainsKey(depth))
                            {
                                string classAlias = classNameAliasDepth[depth];
                                AliasReference aliasReference = classNameAlias[classAlias];
                                if (classAlias != "kresult" && ++aliasReference.iteration < aliasReference.maxIteration)
                                {
                                    isIteration = true;
                                }
                                else
                                {
                                    if (classAlias == "kresult")
                                    {
                                        fpDetails["_system"]["kresult"] = null;
                                    }
                                    if (classNameAlias.ContainsKey(classAlias))
                                        classNameAlias.Remove(classAlias);

                                    //classNameAliasDepth.Remove(depth-1);
                                    classNameAliasDepth.Remove(depth);
                                }
                            }
                        }
                        if (!isIteration && readNode.NextSibling != null)
                        {
                            readNode = readNode.NextSibling;
                            writeNode.ParentNode.InsertAfter(GetProcessedNode(readNode, classNameAlias, classNameAliasDepth, depth, isRepeat, evaluator, customerId, entity, fpDetails, viewDetails, queryString, functionLog, isDetailsView, isNFSite), writeNode);
                            writeNode = writeNode.NextSibling;
                        }
                    }
                    if (isRepeat && classNameAliasDepth.Count == 0)
                    {
                        isRepeat = false;
                    }
                }

                HtmlNodeCollection kScriptNodes = finalDocument.DocumentNode.SelectNodes("//k-script");
                if (kScriptNodes != null && kScriptNodes.Count() > 0)
                {
                    foreach (HtmlNode kscriptNode in kScriptNodes)
                    {
                        if (kscriptNode.PreviousSibling != null)
                        {
                            HtmlNode sibling = kscriptNode.PreviousSibling;
                            foreach (HtmlNode childNode in kscriptNode.ChildNodes)
                            {
                                kscriptNode.ParentNode.InsertAfter(childNode, sibling);
                                sibling = childNode;
                            }
                        }
                        else
                        {
                            kscriptNode.ParentNode.PrependChildren(kscriptNode.ChildNodes);
                        }
                        kscriptNode.Remove();
                    }
                }

                string htmlString = finalDocument.DocumentNode.OuterHtml;
                if (!String.IsNullOrEmpty(htmlString))
                {
                    return System.Net.WebUtility.HtmlDecode(htmlString);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex) { throw; }
        }

        /// <summary>
        /// Verify if the current depth is under iteration.
        /// </summary>
        /// <param name="depth"></param>
        /// <param name="classNameAlias"></param>
        /// <param name="classNameAliasDepth"></param>
        /// <returns></returns>
        private bool HasIteration(int depth, Dictionary<string, AliasReference> classNameAlias, Dictionary<int, string> classNameAliasDepth)
        {
            try
            {
                bool hasIteration = false;
                if (classNameAliasDepth[depth] != null)
                {
                    string className = classNameAliasDepth[depth];
                    AliasReference aliasReference = classNameAlias[className];
                    if (aliasReference.iteration != -1)
                        hasIteration = true;
                }
                return hasIteration;
            }
            catch (Exception ex) { throw; }
        }

        /// <summary>
        /// Process the node for k-tags and k-expressions and return the processed node.
        /// </summary>
        /// <param name="readNode"></param>
        /// <param name="classNameAlias"></param>
        /// <param name="classNameAliasDepth"></param>
        /// <param name="depth"></param>
        /// <param name="isRepeat"></param>
        /// <param name="evaluator"></param>
        /// <returns></returns>
        private HtmlNode GetProcessedNode(HtmlNode readNode, Dictionary<string, AliasReference> classNameAlias, Dictionary<int, string> classNameAliasDepth, int depth, bool isRepeat, ExpressionEvaluator evaluator, string customerId, KEntity entity, dynamic websiteData, Models.Pagination viewDetails, string queryString, Dictionary<string, long> functionLog, bool isDetailsView, bool isNFSite)
        {
            try
            {
                HtmlNode returnNode = readNode.CloneNode(false);
                if (readNode.NodeType == HtmlNodeType.Comment)
                {
                    return returnNode;
                }
                else if (readNode.NodeType == HtmlNodeType.Text)
                {
                    returnNode = HtmlTextNode.CreateNode(ReplaceKLanguage(readNode.OuterHtml, classNameAlias, evaluator, entity, viewDetails, websiteData, websiteData?._system?.kresult, queryString, functionLog, isDetailsView, isNFSite)) ?? returnNode;
                    return returnNode;
                }
                else if (readNode.NodeType == HtmlNodeType.Element)
                {
                    if (isRepeat && (false || readNode.Attributes.Aggregate(false, (acc, x) => acc || x.Name.ToLower().Equals("k-norepeat"))))
                    {
                        returnNode = HtmlCommentNode.CreateNode("<!-- skip -->");
                    }

                    if (returnNode.Attributes.Count() > 0)
                    {
                        foreach (HtmlAttribute attribute in returnNode.Attributes.ToList())
                        {
                            if (returnNode.NodeType == HtmlNodeType.Comment)
                            {
                                break;
                            }
                            if (!String.IsNullOrEmpty(attribute.Name) && dynamicTagDescriptors.Contains(attribute.Name.ToLower()))
                            {
                                TagProcessor processor = processorFactory.GetProcessor(attribute.Name);
                                processor.ProcessNode(ref returnNode, attribute, classNameAlias, classNameAliasDepth, depth, customerId, evaluator, entity, websiteData, viewDetails, queryString, functionLog, isDetailsView, isNFSite);
                                if (!(attribute.Name.ToLower().Equals(LanguageAttributes.KPayAmount.GetDescription()) || attribute.Name.ToLower().Equals(LanguageAttributes.KPayPurpose.GetDescription())))
                                {
                                    returnNode.Attributes[attribute.Name.ToLower()]?.Remove();
                                }
                            }
                            else if (!attribute.Name.Equals("input", StringComparison.InvariantCultureIgnoreCase) && !attribute.Name.Equals("headers", StringComparison.InvariantCultureIgnoreCase) && Kitsune.Helper.Constants.WidgetRegulerExpression.IsMatch(attribute.Value))
                            {
                                attribute.Value = ReplaceKLanguage(attribute.Value, classNameAlias, evaluator, entity, viewDetails, websiteData, websiteData?._system?.kresult, queryString, functionLog, isDetailsView, isNFSite);
                                //attribute.Value = evaluator.EvaluateExpression(attribute.Value, entity, viewDetails, classNameAlias, websiteData, websiteData?._system?.kresult, queryString, isDetailsView, isNFSite);
                                if (returnNode.Name?.ToLower() == "img" && attribute.Name?.ToLower() == "src")
                                {
                                    attribute.Value = attribute.Value?.Trim();
                                }
                            }
                        }

                        if (requestType == KitsuneRequestUrlType.PRODUCTION && returnNode.Name?.ToLower() == "img")
                        {
                            string source = returnNode.Attributes.Where(x => x.Name.ToLower() == "src")?.FirstOrDefault()?.Value;
                            if (!string.IsNullOrEmpty(source) && !source.StartsWith("/") && !source.StartsWith(".") && !source.StartsWith("data:") && source.ToLower().IndexOf("k-img") < 0)
                            {
                                source = source.Replace(" ", "%20");
                                string ext = source.Split('?')[0].Split('.').Last().ToLower();
                                string domain = source.Replace("http://", "").Replace("https://", "").Split('/')[0].ToLower();
                                if (!BlackListKImgFileExtension.Contains(ext) && !BlackListKImgDomain.Contains(domain) && !domain.Contains("cdn") && !domain.Contains("akamai") && !domain.Contains("cloudflare"))
                                {
                                    string rootUrl = websiteData?.rootaliasurl?.url?.Value;
                                    if (!source.StartsWith(rootUrl, StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        string srcSetValue = "";
                                        foreach (int resolution in KLM_Constants.IMAGE_RESOLUTIONS)
                                        {
                                            srcSetValue += String.Format(KLM_Constants.K_IMG_FORMAT_STRING, resolution, source);
                                        }
                                        if (srcSetValue != null)
                                        {
                                            returnNode.Attributes.Add("srcset", srcSetValue);
                                            //returnNode.Attributes.Remove("src");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    //if (returnNode.Name?.ToLower() == "img")
                    //{
                    //    string source = returnNode.Attributes.Where(x => x.Name.ToLower() == "src").FirstOrDefault().Value;
                    //    if (!string.IsNullOrEmpty(source))
                    //    {
                    //        string[] srcParts = source.Split('?')[0].Split('.');
                    //        if (!srcParts[srcParts.Length - 1].Equals("svg", StringComparison.InvariantCultureIgnoreCase))
                    //        {
                    //            string rootUrl = websiteData?.rootaliasurl?.url?.Value;
                    //            string path = websiteData?._system?.request?.urlpath?.Value;
                    //            string srcSetValue = GetSrcSetValue(rootUrl, path, source);
                    //            if (srcSetValue != null)
                    //            {
                    //                returnNode.Attributes.Add("srcset", srcSetValue);
                    //                //returnNode.Attributes.Remove("src");
                    //            }
                    //        }
                    //    }
                    //}

                    if (returnNode.Name?.ToLower() == LanguageAttributes.KScript.GetDescription()?.ToLower())
                    {
                        TagProcessor processor = processorFactory.GetProcessor(returnNode.Name);
                        processor.ProcessNode(ref returnNode, null, classNameAlias, classNameAliasDepth, depth, customerId, evaluator, entity, websiteData, viewDetails, queryString, functionLog, isDetailsView, isNFSite);
                    }
                }
                return returnNode;
            }
            catch (Exception ex) { throw; }
        }

        /// <summary>
        /// Get descriptors of enum values.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private List<string> GetDynamicTagDescriptors(Type type)
        {
            try
            {
                var descs = new List<string>();
                var names = Enum.GetNames(type);
                foreach (var name in names)
                {
                    var field = type.GetField(name);
                    var fds = field.GetCustomAttributes(typeof(DescriptionAttribute), true);
                    foreach (DescriptionAttribute fd in fds)
                    {
                        descs.Add(fd.Description.ToLower());
                    }
                }
                return descs;
            }
            catch (Exception ex) { throw; }
        }

        private string GetReplacedValue(string actualString, string pattern, dynamic replaceValue)
        {
            try
            {
                string replaceString = replaceValue.ToString();
                if (replaceValue.GetType() == typeof(string))
                {
                    Helper.PostProcessReplacementString(ref replaceString);
                }
                actualString = actualString.Replace(pattern, replaceString);
            }
            catch (Exception e)
            {
                actualString = actualString.Replace(pattern, "");
            }
            return actualString;
        }

        /// <summary>
        /// Replace k-expressions in the input html with evaluated values and return final html.
        /// </summary>
        /// <param name="html"></param>
        /// <param name="classNameAlias"></param>
        /// <param name="evaluator"></param>
        /// <returns></returns>
        private string ReplaceKLanguage(string html, Dictionary<string, AliasReference> classNameAlias, ExpressionEvaluator evaluator, KEntity entity, Models.Pagination viewDetails, dynamic businessData, dynamic kresult, string queryString, Dictionary<string, long> functionLog, bool isDetailsView, bool isNFSite)
        {
            string outputHtml = html;
            try
            {

                var matches = Kitsune.Helper.Constants.WidgetRegulerExpression.Matches(html);
                foreach (Match match in matches)
                {
                    dynamic replaceValue = evaluator.EvaluateExpression(match.Value, entity, viewDetails, classNameAlias, businessData, kresult, queryString, out bool hasData, functionLog, isDetailsView, isNFSite);
                    outputHtml = GetReplacedValue(outputHtml, match.Value, replaceValue);
                }
                if (string.IsNullOrEmpty(outputHtml))
                {
                    outputHtml = " ";
                }
            }
            catch (Exception ex) { }
            return System.Net.WebUtility.HtmlEncode(outputHtml);
        }
    }
}
