using AntlrLibrary;
using AntlrLibrary.Model;
using Kitsune.Language.Models;
using Kitsune.Models;
using Kitsune.Models.KLM;
using Kitsune.Models.Nodes;
using KitsuneLayoutManager.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KitsuneLayoutManager.Helper
{
    public class BlockLevelKLMExecutor
    {
        private readonly KEntity entity;
        private readonly BlockLevelExpressionEvaluator evaluator;
        private readonly Dictionary<string, long> functionLog;
        private KitsunePage page;
        private readonly string rootAliasUri;
        private readonly string url;
        private readonly string urlPattern;
        private readonly string urlPatternRegex;
        private readonly Pagination viewDetail;
        private readonly string developerId;
        private bool CacheableResult = true;
        private readonly string websiteId;
        private readonly string baseClassName;
        private const int _kobjectStartIndex = 10000;

        private object expressionEvaluationThreadCountLock = new object();

        public BlockLevelKLMExecutor(KEntity entity, Dictionary<string, long> functionLog, KitsunePage page, Pagination viewDetail, string rootAliasUri, string websiteId, string schemaId, string url, string urlPattern, string urlPatternRegex, dynamic websiteData, string fptag, bool enableSrcSet = true, string developerId = null)
        {
            this.entity = entity;
            this.functionLog = functionLog;
            this.url = url ?? "";
            if (urlPattern != null)
            {
                this.urlPattern = urlPattern.Substring(urlPattern.IndexOf("/") + 1);
            }
            else
            {
                this.urlPattern = urlPattern ?? "";
            }
            this.urlPatternRegex = urlPatternRegex?.Replace("a-zA-Z0-9", @"a-zA-Z0-9\(\)_\[\]") ?? "";
            this.page = page;
            this.rootAliasUri = rootAliasUri ?? "";
            this.viewDetail = viewDetail;
            this.developerId = developerId;
            this.websiteId = websiteId;
            baseClassName = entity?.Classes?.Where(c => c.ClassType == KClassType.BaseClass)?.FirstOrDefault().Name;

            evaluator = new BlockLevelExpressionEvaluator(entity, viewDetail, functionLog, websiteId, schemaId, websiteData, rootAliasUri, fptag, enableSrcSet, developerId);
        }

        public KLMResponseModel Execute()
        {
            Dictionary<string, AliasReference> classNameAlias = new Dictionary<string, AliasReference>();
            int currentPage = -1;
            if (!string.IsNullOrEmpty(urlPatternRegex))
            {
                Regex urlRegex = new Regex(urlPatternRegex.ToLower());
                string matchUrl = url.ToLower().Replace("http://", "").Replace("https://", "").Replace(rootAliasUri.ToLower().Replace("http://", "").Replace("https://", ""), "").TrimStart('/');
                Match matches = urlRegex.Match(matchUrl.ToLower());
                Match kdlMatches = urlRegex.Match(urlPattern.Replace("[[", "").Replace("]]", "").Replace("'", "").Replace("+", "").Replace(" ", "").ToLower());
                for (int i = 1; i < kdlMatches.Groups.Count; i++)
                {
                    var collectionIdentifiers = new List<string>();
                    if (page.CollectionIdentifier != null)
                        collectionIdentifiers = page.CollectionIdentifier.Split(',').Select(x => x.Trim()).ToList();

                    if (kdlMatches.Groups[i].Value.ToLower().Contains("currentpagenumber"))
                    {
                        if (int.TryParse(matches.Groups[i].Value, out int icurrentPage) && icurrentPage.ToString() == matches.Groups[i].Value)
                        {
                            currentPage = icurrentPage;
                        }
                    }
                    else if (page.Offset == null && page.CollectionIdentifier != null && kdlMatches.Groups[i].Value.ToLower().Contains(collectionIdentifiers[collectionIdentifiers.Count - 1]))
                    {
                        if (i == kdlMatches.Groups.Count - 1)
                        {
                            string objectKey = matches.Groups[i].Value;
                            if (string.IsNullOrEmpty(objectKey))
                            {
                                continue;
                            }
                            string objectReference = kdlMatches.Groups[i].Value;
                            if (objectReference.Contains("urlencode"))
                            {
                                objectKey = System.Web.HttpUtility.UrlPathEncode(objectKey).Replace("-", " ");
                                objectReference = objectReference.Replace(".urlencode()", "");
                            }
                            if (objectReference != null)
                            {
                                Node tree = LexerGenerator.Parse(objectReference);
                                string[] objectReferenceParts;
                                if (tree.Token.Type == TOKENTYPE.Expression && tree.Token.Value == ACTIONS.OperandEval)
                                {
                                    objectReferenceParts = tree?.Children[0]?.Token?.Value?.Split('.');
                                }
                                else
                                {
                                    objectReferenceParts = tree?.Children[0]?.Children[0]?.Token?.Value?.Split('.');
                                }
                                objectReference = objectReferenceParts[objectReferenceParts.Count() - 1];



                                if (page.Collection.ToLower().StartsWith("webactions."))
                                {
                                    objectReferenceParts = objectReferenceParts.Take(objectReferenceParts.Count() - 1).ToArray();
                                    string objectCollection = string.Join(".", objectReferenceParts);
                                    string query = page.Collection + ".find(" + objectReference + " == '" + objectKey + "')";
                                    dynamic result = evaluator.EvaluateExpressionAsync(query, classNameAlias).GetAwaiter().GetResult();
                                    if (result != null && result.ToString() != "")
                                    {
                                        JObject tempResult = new JObject();
                                        tempResult.Add(collectionIdentifiers[0], result[0]);
                                        result = tempResult;
                                    }
                                    if (classNameAlias.ContainsKey(collectionIdentifiers[0]))
                                    {
                                        classNameAlias.Remove(collectionIdentifiers[0]);
                                    }
                                    classNameAlias.Add(collectionIdentifiers[0], new AliasReference()
                                    {
                                        iteration = -1,
                                        maxIteration = -1,
                                        referenceObject = result,
                                        aliasType = AliasType.absoluteAlias
                                    });
                                }
                                else
                                {

                                    //Updatet he k-object reference to avoid getting entire object : Chirag
                                    //set kobject index for each objects starting from 10000
                                    var _kobjectIndex = (collectionIdentifiers.Count - 1) + _kobjectStartIndex;


                                    var parentId = evaluator.PrefillObjectKIDs(page.Collection, objectReference, objectKey, _kobjectIndex);
                                    if(!string.IsNullOrEmpty(parentId))
                                    {
                                        classNameAlias.Add(collectionIdentifiers[collectionIdentifiers.Count - 1], new AliasReference()
                                        {
                                            iteration = -1,
                                            maxIteration = -1,
                                            referenceObject = page.Collection + $"[k_obj_{_kobjectIndex}]",
                                            aliasType = AliasType.referenceAlias,
                                            kobjectIndex = page.Collection.Split('.').Length
                                        });
                                        classNameAlias.Add($"k_obj_{_kobjectIndex}", new AliasReference()
                                        {
                                            iteration = -1,
                                            maxIteration = -1,
                                            referenceObject = _kobjectIndex.ToString(),
                                            aliasType = AliasType.absoluteAlias,
                                        });

                                        if (collectionIdentifiers.Count > 1)
                                        {
                                            for (var j = collectionIdentifiers.Count - 2; j >= 0; j--)
                                            {
                                                _kobjectIndex = j + _kobjectStartIndex;

                                                page.Collection = page.Collection.Substring(0, page.Collection.LastIndexOf('.'));

                                                parentId = evaluator.PrefillObjectKIDs(page.Collection, "_kid", parentId, _kobjectIndex);

                                                classNameAlias.Add(collectionIdentifiers[j], new AliasReference()
                                                {
                                                    iteration = -1,
                                                    maxIteration = -1,
                                                    referenceObject = page.Collection + $"[k_obj_{_kobjectIndex}]",
                                                    aliasType = AliasType.referenceAlias,
                                                    kobjectIndex = page.Collection.Split('.').Length
                                                });
                                                classNameAlias.Add($"k_obj_{_kobjectIndex}", new AliasReference()
                                                {
                                                    iteration = -1,
                                                    maxIteration = -1,
                                                    referenceObject = (j + _kobjectIndex).ToString(),
                                                    aliasType = AliasType.absoluteAlias,
                                                });
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //TODO : Check if we need to send the NotFound or empty
                                        //KLMResponseModel notresponse = new KLMResponseModel();
                                        //notresponse.HtmlCode = "Not Found";
                                        //notresponse.CacheableResult = false;
                                        
                                        //return notresponse;
                                    }
                                   

                                }

                            }
                        }
                    }
                    else
                    {
                        string variableString = kdlMatches.Groups[i].Value.Replace("[[", "").Replace("]]", "");
                        if (variableString.Replace(".urlencode()", "").Split('.').Count() == 1)
                        {
                            string variableName = variableString.Split('.')[0].ToLower();
                            string variableValue = matches.Groups[i].Value;
                            if (variableString.ToLower().Contains("urlencode"))
                            {
                                variableValue = System.Web.HttpUtility.UrlPathEncode(variableValue).Replace("-", " ");
                            }
                            classNameAlias.Add(variableName, new AliasReference()
                            {
                                iteration = -1,
                                maxIteration = -1,
                                referenceObject = variableValue,
                                aliasType = AliasType.absoluteAlias,
                            });
                        }
                    }
                }
            }
            if (currentPage == -1 && int.TryParse(viewDetail.currentpagenumber, out int cpgn) && viewDetail.currentpagenumber == cpgn.ToString())
            {
                currentPage = cpgn;
            }
            if (!string.IsNullOrEmpty(page.Offset))
            {
                Dictionary<string, AliasReference> tempClassNameAlias = new Dictionary<string, AliasReference>();
                int.TryParse(evaluator.EvaluateExpressionAsync(page.Collection + ".length()", tempClassNameAlias).GetAwaiter().GetResult().ToString(), out int total);
                int.TryParse(evaluator.EvaluateExpressionAsync(page.Offset, tempClassNameAlias).GetAwaiter().GetResult().ToString(), out int offset);
                page.Offset = offset.ToString();
                if (offset * (currentPage) > total)
                {
                    viewDetail.nextpage.url = "#";
                }
            }
            if (currentPage == -1)
            {
                int.TryParse(viewDetail.currentpagenumber, out currentPage);
            }
            string htmlCode = ProcessNodesAsync(page.Nodes, classNameAlias, false).Result;
            KLMResponseModel response = new KLMResponseModel();
            response.HtmlCode = htmlCode;
            response.CacheableResult = CacheableResult;
            return response;
        }

        private async Task<string> ProcessNodesAsync(List<INode> nodes, Dictionary<string, AliasReference> classNameAlias, bool isRepeat)
        {
            string[] resultStrings = new string[nodes.Count];

            for (int i = 0; i < nodes.Count; i++)
            {
                resultStrings[i] = await ExecuteNodeAsync(nodes[i], classNameAlias, isRepeat);
            }
            return string.Join(String.Empty, resultStrings);
        }

        private async Task<string> ExecuteNodeAsync(INode node, Dictionary<string, AliasReference> classNameAlias, bool isRepeat)
        {
            string result = "";
            Task<string> resultTask;
            switch (node.NodeType)
            {
                case NodeType.textNode:
                    return ExecuteTextNode((TextNode)node, classNameAlias, isRepeat);
                case NodeType.expressionNode:
                    resultTask = ExecuteExpressionNodeAsync((ExpressionNode)node, classNameAlias, isRepeat);
                    break;
                case NodeType.kShowNode:
                    resultTask = ExecuteKShowNodeAsync((KShowNode)node, classNameAlias, isRepeat);
                    break;
                case NodeType.kHideNode:
                    resultTask = ExecuteKHideNodeAsync((KHideNode)node, classNameAlias, isRepeat);
                    break;
                case NodeType.kNoRepeatNode:
                    resultTask = ExecuteKNoRepeatNodeAsync((KNoRepeatNode)node, classNameAlias, isRepeat);
                    break;
                case NodeType.kScriptNode:
                    resultTask = ExecuteKScriptNodeAsync((KScriptNode)node, classNameAlias, isRepeat);
                    break;
                case NodeType.kRepeatNode:
                    resultTask = ExecuteRepeatNodeAsync((KRepeatNode)node, classNameAlias, isRepeat);
                    break;
                default:
                    return "";
            }
            result = await resultTask;
            return result;
        }

        private string ExecuteTextNode(TextNode node, Dictionary<string, AliasReference> classNameAlias, bool isRepeat)
        {
            return node.Text;
        }

        private async Task<string> ExecuteExpressionNodeAsync(ExpressionNode node, Dictionary<string, AliasReference> classNameAlias, bool isRepeat)
        {
            try
            {
                dynamic res = await evaluator.EvaluateExpressionAsync(node.Expression, classNameAlias);
                return res.ToString();
            }
            catch (Exception e)
            {
                ConsoleLogger.Write($"ERROR: ExecuteExpressionNodeAsync: \t {e.ToString()}, source:" + page.SourcePath);
                CacheableResult = false;
            }
            return null;
        }

        private async Task<string> ExecuteKShowNodeAsync(KShowNode node, Dictionary<string, AliasReference> classNameAlias, bool isRepeat)
        {
            try
            {
                dynamic result = await evaluator.EvaluateExpressionAsync(node.Expression, classNameAlias);
                if (result.GetType() == typeof(bool) && result)
                {
                    return await ProcessNodesAsync(node.Children, classNameAlias, isRepeat);
                }
            }
            catch (Exception e)
            {
                ConsoleLogger.Write($"ERROR: ExecuteKShowNodeAsync: \t {e.ToString()}, source:" + page.SourcePath);
                CacheableResult = false;
            }
            return "";
        }

        private async Task<string> ExecuteKHideNodeAsync(KHideNode node, Dictionary<string, AliasReference> classNameAlias, bool isRepeat)
        {
            try
            {
                dynamic result = await evaluator.EvaluateExpressionAsync(node.Expression, classNameAlias);
                if (result.GetType() == typeof(bool) && !result)
                {
                    return await ProcessNodesAsync(node.Children, classNameAlias, isRepeat);
                }
            }
            catch (Exception e)
            {
                CacheableResult = false;
            }
            return "";
        }

        private async Task<string> ExecuteKNoRepeatNodeAsync(KNoRepeatNode node, Dictionary<string, AliasReference> classNameAlias, bool isRepeat)
        {
            if (!isRepeat)
            {
                return await ProcessNodesAsync(node.Children, classNameAlias, isRepeat);
            }
            return "";
        }

        private async Task<string> ExecuteKScriptNodeAsync(KScriptNode node, Dictionary<string, AliasReference> classNameAlias, bool isRepeat)
        {
            try
            {
                StringBuilder resultString = new StringBuilder("");
                string apiUrl = string.Copy(node.API);
                string input = "";
                if (!string.IsNullOrEmpty(node.Input))
                {
                    input = string.Copy(node.Input);
                }
                string headers = "";
                if (!string.IsNullOrEmpty(node.Headers))
                {
                    headers = string.Copy(node.Headers);
                }
                bool cacheEnabled = true;
                if (!string.IsNullOrEmpty(node.IsCacheEnabledString))
                {
                    string cacheEnabledStr = await evaluator.EvaluateExpressionAsync(node.IsCacheEnabledString, classNameAlias);
                    bool.TryParse(cacheEnabledStr, out cacheEnabled);
                }
                if (!cacheEnabled)
                {
                    CacheableResult = false;
                }
                if (!string.IsNullOrEmpty(apiUrl))
                {
                    if (!string.IsNullOrEmpty(input))
                    {
                        var inputMatches = Constant.WidgetRegulerExpression.Matches(input);
                        Dictionary<string, string> matchDict = new Dictionary<string, string>();

                        //Handle the (,) withing the expression business.name.replace('hello',''). it was spliting by comma so used unique pattern
                        for (var i = 0; i < inputMatches.Count; i++)
                        {
                            matchDict.Add($"##__{i}__##", inputMatches[i].Value);
                            input = input.Replace(inputMatches[i].Value, $"##__{i}__##");
                        }

                        var inputArray = input.Split(',');

                        int count = 0;
                        foreach (var inputVal in inputArray)
                        {
                            var elem = inputVal;
                            foreach (var placHolder in matchDict)
                            {
                                elem = elem.Replace(placHolder.Key, placHolder.Value);
                            }
                            if (elem.Contains("]") || elem.Contains("["))
                            {
                                dynamic outputObject = await evaluator.EvaluateExpressionAsync(elem, classNameAlias);
                                var output = outputObject.ToString();
                                apiUrl = apiUrl.Replace("{" + count + "}", output.Trim('\''));
                            }
                            else
                            {
                                apiUrl = apiUrl.Replace("{" + count + "}", elem);
                            }
                            count++;
                        }
                    }

                    var headerDict = new Dictionary<string, string>();
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
                    //var result = GetResponseFromKScript(apiUrl, headerDict, cacheEnabled, functionLog);
                    var result = await ApiHelper.GetResponseFromKScriptAsync(apiUrl, headerDict, cacheEnabled, rootAliasUri, functionLog, websiteId);
                    if (result != null)
                    {
                        evaluator.ChangePropertiesToLowerCase(result);
                        classNameAlias.Remove("kresult");
                        classNameAlias.Add("kresult", new AliasReference { iteration = -1, maxIteration = -1, referenceObject = result });
                    }
                    string res = await ProcessNodesAsync(node.Children, classNameAlias, isRepeat);
                    resultString.Append(res);
                    classNameAlias.Remove("kresult");
                }
                return resultString.ToString();

            }
            catch (Exception e)
            {
                CacheableResult = false;
                ConsoleLogger.Write($"ERROR: ExecuteKScriptNodeAsync: \t {e.ToString()}, source:" + page.SourcePath);
            }
            return "";
        }

        private async Task<string> ExecuteRepeatNodeAsync(KRepeatNode node, Dictionary<string, AliasReference> classNameAlias, bool isRepeat)
        {
            try
            {
                int startIndex = 0;
                int offset = 0;
                decimal tmpIndex = 0;
                if (node.StartIndex.IndexOf("offset") > 0)
                {
                    if (string.IsNullOrEmpty(page.Offset))
                    {
                        node.StartIndex = "0";
                    }
                    else
                    {
                        int.TryParse(viewDetail.currentpagenumber, out int pageNumber);
                        dynamic offsetObj = await evaluator.EvaluateExpressionAsync(page.Offset, classNameAlias);
                        int.TryParse(offsetObj.ToString(), out offset);
                        startIndex = offset * (pageNumber - 1);
                    }
                }
                else
                {
                    dynamic startIndexObj = await evaluator.EvaluateExpressionAsync(node.StartIndex, classNameAlias);
                    //int.TryParse(startIndexObj.ToString(), out startIndex);
                    startIndex = decimal.TryParse(startIndexObj.ToString(), out tmpIndex) ? (int)tmpIndex : 0;

                }
                dynamic endIndexObj = await evaluator.EvaluateExpressionAsync(node.EndIndex, classNameAlias);
                string endIndexString = endIndexObj.ToString();
                if (endIndexString == "")
                {
                    return "";
                }
                int endIndex = decimal.TryParse(endIndexString, out tmpIndex) ? (int)tmpIndex : 0;
                //int.TryParse(endIndexString, out int endIndex);
                endIndex += startIndex;
                dynamic totalObj = await evaluator.EvaluateExpressionAsync(node.Collection + ".length()", classNameAlias);
                int.TryParse(totalObj.ToString(), out int total);
                if (endIndex > total)
                {
                    endIndex = total;
                }
                string baseReference = node.Collection.Split('.')[0].Split('[')[0].ToLower();
                if (baseClassName == baseReference)
                {
                    await evaluator.PrefillKIDsAsync(node.Collection.Trim(), startIndex, endIndex);
                }

                StringBuilder resultString = new StringBuilder();
                for (int i = startIndex; i < endIndex; i++)
                {
                    Dictionary<string, AliasReference> newClassNameAlias = new Dictionary<string, AliasReference>();
                    foreach (var alias in classNameAlias)
                    {
                        newClassNameAlias.Add(alias.Key, alias.Value.Clone());
                    }
                    if (string.IsNullOrEmpty(node.CollectionAlias))
                    {
                        newClassNameAlias.Add(node.Iterator.Trim(), new AliasReference { iteration = i, maxIteration = endIndex, referenceObject = null });
                    }
                    else
                    {
                        newClassNameAlias.Add(node.CollectionAlias.Trim(), new AliasReference { iteration = -1, maxIteration = -1, referenceObject = node.Collection.Trim() + "[" + i + "]" });
                    }
                    resultString.Append(await ProcessNodesAsync(node.Children, newClassNameAlias, isRepeat || (i != startIndex)));
                }
                return resultString.ToString();
            }
            catch (Exception e)
            {
                CacheableResult = false;
                ConsoleLogger.Write($"ERROR: ExecuteRepeatNodeAsync: \t {e.ToString()}, source:" + page.SourcePath);
            }
            return "";
        }
    }
}
