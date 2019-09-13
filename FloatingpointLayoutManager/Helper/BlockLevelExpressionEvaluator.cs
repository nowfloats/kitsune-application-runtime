using AntlrLibrary;
using AntlrLibrary.Model;
using Kitsune.Language.Models;
using Kitsune.Server.Model.Kitsune;
using Kitsune.SyntaxParser;
using Kitsune.SyntaxParser.Models;
using KitsuneLayoutManager.Models;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KitsuneLayoutManager.Helper
{
    public class BlockLevelExpressionEvaluator
    {
        private ConcurrentDictionary<string, object> evaluatedExpressions;
        private ConcurrentDictionary<string, dynamic> webactionsApiDictionary;
        private ConcurrentDictionary<string, dynamic> searchApiDictionary;
        private readonly KEntity entity;
        private readonly Models.Pagination viewDetails;
        private readonly Dictionary<string, long> functionLog;
        private readonly string websiteId;
        private readonly string schemaId;
        private List<TOKENTYPE> skipTokenType;
        private readonly string baseClassName;
        private readonly string tag;
        private readonly bool enableSrcSet;
        private readonly string developerId;
        private object updateFunctionLogLock = new object();
        private object updateExpressionsLock = new object();
        private string kPayCheckSumFunction = "getchecksum";
        private string kIMGSrcSet = "getsrcset";
        private string rootAliasUri;
        private dynamic websiteData;
        private string[] BlackListKImgFileExtension = { "svg", "gif" };
        private string[] BlackListKImgDomain = { "firebasestorage.googleapis.com", "akamai.net", "cloudfront.net", "azureedge.net", "maps.googleapis.com", "maps.cit.api.here.com", "image.maps.cit.api.here.com", "dev.virtualearth.net" };
        private readonly Dictionary<string, string> collectionKIDMap;

        public BlockLevelExpressionEvaluator(KEntity entity, Models.Pagination viewDetails, Dictionary<string, long> functionLog, string websiteId, string schemaId, dynamic websiteData, string rootAliasUri, string fptag, bool enableSrcSet, string developerId)
        {
            this.entity = entity;
            this.viewDetails = viewDetails;
            this.functionLog = functionLog;
            evaluatedExpressions = new ConcurrentDictionary<string, object>();
            collectionKIDMap = new Dictionary<string, string>();
            this.websiteId = websiteId;
            this.schemaId = schemaId;
            this.websiteData = websiteData;
            this.rootAliasUri = rootAliasUri;
            this.tag = fptag;
            this.enableSrcSet = enableSrcSet;
            this.developerId = developerId;

            webactionsApiDictionary = new ConcurrentDictionary<string, dynamic>();
            searchApiDictionary = new ConcurrentDictionary<string, dynamic>();
            skipTokenType = new List<TOKENTYPE>();
            skipTokenType.Add(TOKENTYPE.Delimiter);
            skipTokenType.Add(TOKENTYPE.Ternary);
            skipTokenType.Add(TOKENTYPE.Arithmatic);
            baseClassName = entity?.Classes?.Where(c => c.ClassType == KClassType.BaseClass)?.FirstOrDefault().Name;
        }

        #region evaluate expression
        public async Task<dynamic> EvaluateExpressionAsync(string value, Dictionary<string, AliasReference> classNameAlias)
        {
            if (value.StartsWith(kPayCheckSumFunction))
            {
                dynamic res = await GetKPayCheckSumAsync(value, classNameAlias);
                return res;
            }
            else if (value.StartsWith(kIMGSrcSet))
            {
                dynamic res = await GetSrcSetAsync(value, classNameAlias);
                return res;
            }
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            try
            {
                if (value.IndexOf("length") > 0)
                {
                    value = value.Replace(".length()", ".length").Replace(".length", ".length()");
                }
                value = Helper.TrimDelimiters(value);
                Node tree = LexerGenerator.Parse(value);
                Token result = null;
                if (tree != null)
                {
                    await ReplaceExpressionsInTreeAsync(tree, classNameAlias);
                    result = ParseTress.Parse(tree);
                }
                return result?.Value ?? "";
            }
            catch (Exception ex) { throw; }
            finally
            {
                stopwatch.Stop();

                lock (updateFunctionLogLock)
                {
                    Helper.UpdateFunctionLog(functionLog, String.Format(Constant.EVALUATE_EXPRESSION, value) + ":", stopwatch.ElapsedMilliseconds);
                }
            }
        }

        private async Task<string> GetSrcSetAsync(string value, Dictionary<string, AliasReference> classNameAlias)
        {
            string srcSetValue = "";
            if (!enableSrcSet)
            {
                return srcSetValue;
            }
            int functionNameIndex = kIMGSrcSet.Length + 1;
            string source = value.Substring(functionNameIndex, value.Length - functionNameIndex - 1);
            MatchCollection expressionMatches = Kitsune.Helper.Constants.WidgetRegulerExpression.Matches(source);
            if (expressionMatches != null && expressionMatches.Count > 0)
            {
                foreach (Match match in expressionMatches)
                {
                    dynamic resultObj = await EvaluateExpressionAsync(match.Value, classNameAlias);
                    string result = resultObj.ToString();
                    source = source.Replace(match.Value, result);
                }
            }
            if (!string.IsNullOrEmpty(source)
                && !source.StartsWith("/")
                && !source.StartsWith(".")
                && !source.StartsWith("data:")
                && source.ToLower().IndexOf("k-img") < 0)
            {
                source = source.Replace(" ", "%20");
                string ext = source.Split('?')[0].Split('.').Last().ToLower();
                string domain = source.Replace("http://", "").Replace("https://", "").Split('/')[0].ToLower();
                if (!BlackListKImgFileExtension.Contains(ext)
                    && !BlackListKImgDomain.Contains(domain)
                    && !domain.Contains("cdn")
                    && !domain.Contains("akamai")
                    && !domain.Contains("cloudflare")
                    && !source.StartsWith(rootAliasUri, StringComparison.InvariantCultureIgnoreCase))
                {
                    foreach (int resolution in KLM_Constants.IMAGE_RESOLUTIONS)
                    {
                        srcSetValue += String.Format(KLM_Constants.K_IMG_FORMAT_STRING, resolution, source);
                    }

                }
            }
            return srcSetValue;
        }

        private async Task<dynamic> GetKPayCheckSumAsync(string value, Dictionary<string, AliasReference> classNameAlias)
        {
            int functionNameIndex = kPayCheckSumFunction.Length + 1;
            string amount = Helper.TrimDelimiters(value).Substring(functionNameIndex, value.Length - functionNameIndex - 1);
            dynamic amtObj = await EvaluateExpressionAsync(amount, classNameAlias);
            amount = amtObj.ToString();
            List<string> amountList = new List<string>();
            amountList.Add(amount);
            var checkSumAPIResponse = await ApiHelper.GetKPayEncodedCheckSumAsync(websiteId, amountList);
            amountList = checkSumAPIResponse.amounts;
            List<string> checkSumList = checkSumAPIResponse.checksums;
            return checkSumList[0];
        }

        private async Task ReplaceExpressionsInTreeAsync(Node tree, Dictionary<string, AliasReference> classNameAlias)
        {
            Node prevNode = null;
            bool isLength = false;
            bool isFind = false;
            bool isBusinessReference = true;
            Dictionary<string, object> whereCondition = null;
            try
            {
                Queue<Node> processQueue = new Queue<Node>();
                processQueue.Enqueue(tree);
                while (processQueue.Count > 0)
                {
                    Node node = processQueue.Dequeue();
                    prevNode = null;
                    isLength = false;
                    isFind = false;
                    isBusinessReference = true;
                    whereCondition = null;
                    if (node.Token.Type == TOKENTYPE.Expression && node.Token.Value == ACTIONS.PostfixUnaryWithArgEval && node.Children[1].Token.Value.ToLower() == "length")
                    {
                        isLength = true;
                        prevNode = node;
                        node = node.Children[0];
                    }
                    if (node.Token.Type == TOKENTYPE.Expression && node.Token.Value == ACTIONS.PostfixUnaryWithArgEval && node.Children[1].Token.Value.ToLower() == "find")
                    {
                        isFind = true;
                        prevNode = node;
                        //for(int i = 3; i < node.Children.Count; i++)
                        //{
                        //    if (node.Children[i].Token.Type != null && !skipTokenType.Contains((TOKENTYPE)node.Children[i].Token.Type))
                        //    {
                        //        ReplaceExpressionsInTree(node.Children[i], classNameAlias);
                        //    }
                        //}
                        //TODO:Extend to fully support find function
                        whereCondition = new Dictionary<string, object>();
                        string keyValue = node.Children[3].Children[2].Children[0].Token.Value.Trim('\'');
                        if (long.TryParse(keyValue, out long keyValueLong) && keyValue == keyValueLong.ToString())
                        {
                            whereCondition.Add(node.Children[3].Children[0].Children[0].Token.Value, keyValueLong);
                        }
                        else
                        {
                            whereCondition.Add(node.Children[3].Children[0].Children[0].Token.Value, keyValue);
                        }
                        node = node.Children[0];
                    }
                    if (node.Token.Type == TOKENTYPE.Expression && node.Token.Value == ACTIONS.ViewProperty)
                    {
                        var s = Evaluator.OperandEval(node.Children[1].Token);
                        if (s.Type == TOKENTYPE.Object)
                        {
                            string value = s.Value.ToLower();
                            if (value.Contains("currentpagenumber"))
                            {
                                node.Token.Type = TOKENTYPE.Expression;
                                node.Token.Value = ACTIONS.OperandEval;
                                node.Children[0].Token.Type = TOKENTYPE.String;
                                node.Children[0].Token.Value = viewDetails.currentpagenumber;
                            }
                            else if (value.Contains("previouspage"))
                            {
                                node.Token.Type = TOKENTYPE.Expression;
                                node.Token.Value = ACTIONS.OperandEval;
                                node.Children[0].Token.Type = TOKENTYPE.String;
                                node.Children[0].Token.Value = viewDetails.prevpage.url;
                            }
                            else if (value.Contains("nextpage"))
                            {
                                node.Token.Type = TOKENTYPE.Expression;
                                node.Token.Value = ACTIONS.OperandEval;
                                node.Children[0].Token.Type = TOKENTYPE.String;
                                node.Children[0].Token.Value = viewDetails.nextpage.url;
                            }
                        }
                    }
                    else if (node.Token.Type == TOKENTYPE.Expression && node.Token.Value == ACTIONS.OperandEval)
                    {
                        isBusinessReference = false;
                        var s = Evaluator.OperandEval(node.Children[0].Token);
                        if (s.Type == TOKENTYPE.Object)
                        {
                            string value = s.Value;
                            if (value.ToLower().Contains("rootaliasurl"))
                            {
                                node.Children[0].Token.Value = "'" + this.rootAliasUri + "'";
                                node.Children[0].Token.Type = TOKENTYPE.String;
                                continue;
                            }
                            int kobjectindex = 0;
                            string fullExpression = GetFullExpressionFromAliasEvaluation(value, classNameAlias, out bool isIterator, out kobjectindex);
                            if (!isIterator)
                            {
                                fullExpression = await ReplaceIteratorsAsync(fullExpression, classNameAlias);
                            }
                            string searchExpression = fullExpression.ToLower();
                            if (isLength)
                            {
                                searchExpression += ".length()";
                            }
                            // if(kobject != null)
                            //{
                            //  whereCondition = new Dictionary<string, object>();
                            //isFind = true;
                            //whereCondition.Add(kobject.objectId, kobject.objectValue);
                            //}
                            if (isIterator)
                            {
                                node.Children[0].Token.Value = fullExpression;
                                node.Children[0].Token.Type = TOKENTYPE.Long;
                            }
                            else if (evaluatedExpressions.ContainsKey(searchExpression))
                            {
                                dynamic evaluatedVal = evaluatedExpressions[searchExpression];
                                if (evaluatedVal == null)
                                {
                                    node.Children[0].Token.Value = "";
                                    node.Children[0].Token.Type = TOKENTYPE.NoData;
                                }
                                else
                                {
                                    if (isLength)
                                    {
                                        node = prevNode;
                                        node.Children.RemoveRange(1, node.Children.Count - 1);
                                        node.Token.Type = TOKENTYPE.Expression;
                                        node.Token.Value = ACTIONS.OperandEval;
                                    }
                                    node.Children[0].Token.Value = evaluatedVal;
                                    if (node.Children[0].Token.Value.GetType() == typeof(JArray))
                                    {
                                        node.Children[0].Token.Type = TOKENTYPE.Array;
                                    }
                                }
                            }
                            else
                            {
                                dynamic evaluationResult = null;
                                string baseReference = fullExpression.Split('.')[0].Split('[')[0].ToLower();
                                try
                                {
                                    if (classNameAlias.ContainsKey(fullExpression) && classNameAlias[fullExpression].aliasType == AliasType.absoluteAlias)
                                    {
                                        evaluationResult = classNameAlias[fullExpression].referenceObject;
                                    }
                                    else if (baseClassName == baseReference)
                                    {
                                        isBusinessReference = true;
                                        fullExpression = fullExpression.ToLower();
                                        //functions are extracted from objects and are required for per expression evaluation.
                                        if (isLength)
                                        {
                                            fullExpression += ".length()";
                                        }
                                        evaluationResult = GetBusinessDataFromAPIAsync(fullExpression, isFind ? whereCondition : null, kobjectindex).Result;
                                        /*if (evaluationResult.TryGetValue("Data", out dynamic result))
                                        {
                                            if (result != null)
                                            {
                                                evaluationResult = GetResultValue(result);
                                            }
                                            else
                                            {
                                                evaluationResult = null;
                                            }
                                        }
                                        */
                                        if ((isLength || isFind) && evaluationResult != null)
                                        {
                                            node = prevNode;
                                            node.Children.RemoveRange(1, node.Children.Count - 1);
                                            node.Token.Type = TOKENTYPE.Expression;
                                            node.Token.Value = ACTIONS.OperandEval;
                                        }
                                        if (evaluationResult == null)
                                        {
                                            evaluationResult = GetDefaultProperty(entity, fullExpression);
                                        }
                                    }
                                    else if (baseReference == "webactions")
                                    {
                                        evaluationResult = await GetDataFromWebActionAsync(fullExpression, whereCondition);
                                        if (isFind && evaluationResult != null)
                                        {
                                            node = prevNode;
                                            node.Children.RemoveRange(1, node.Children.Count - 1);
                                            node.Token.Type = TOKENTYPE.Expression;
                                            node.Token.Value = ACTIONS.OperandEval;
                                        }
                                    }
                                    else if (baseReference == "_system")
                                    {
                                        string[] expParts = fullExpression.Split('.');
                                        if (expParts[1].Equals("components", StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            //components
                                            evaluationResult = GetData(fullExpression, websiteData);
                                        }
                                        else
                                        {
                                            //custom variables
                                            evaluationResult = classNameAlias[expParts[expParts.Length - 1]].referenceObject;
                                        }
                                    }
                                    else if (classNameAlias.ContainsKey(baseReference))
                                    {
                                        evaluationResult = GetData(fullExpression, classNameAlias[baseReference].referenceObject);
                                    }
                                }
                                catch (Exception ex)
                                { }
                                if (evaluationResult != null)
                                {
                                    node.Children[0].Token.Value = evaluationResult;
                                    if (evaluationResult.GetType() == typeof(JValue))
                                    {
                                        node.Children[0].Token.Value = "'" + evaluationResult.Value + "'";
                                    }
                                    else if (evaluationResult.GetType() == typeof(JArray))
                                    {
                                        node.Children[0].Token.Type = TOKENTYPE.Array;
                                    }
                                    else if (evaluationResult.GetType() == typeof(DateTime))
                                    {
                                        node.Children[0].Token.Value = "'" + evaluationResult.ToString("s") + "'";
                                    }
                                    else if (evaluationResult.GetType() == typeof(string) && evaluationResult != "")
                                    {
                                        node.Children[0].Token.Value = "'" + evaluationResult + "'";
                                    }
                                    if (isBusinessReference && !isFind)
                                    {
                                        lock (updateExpressionsLock)
                                        {
                                            if (!evaluatedExpressions.ContainsKey(fullExpression))
                                            {
                                                evaluatedExpressions.TryAdd(fullExpression, node.Children[0].Token.Value);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    node.Children[0].Token.Type = TOKENTYPE.NoData;
                                    node.Children[0].Token.Value = "";
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!isFind)
                        {
                            foreach (Node childNode in node.Children)
                            {
                                if (childNode != null && childNode.Token.Type != null && !skipTokenType.Contains((TOKENTYPE)childNode.Token.Type))
                                {
                                    processQueue.Enqueue(childNode);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { throw; }
        }

        private async Task<string> ReplaceIteratorsAsync(string value, Dictionary<string, AliasReference> classNameAlias)
        {
            int indexOffset = 0;
            int startIndex = 0;
            int endIndex = 0;
            string iterator = "";
            string evaluatedIterator = "";
            string result = value;
            startIndex = result.IndexOf('[', indexOffset);
            if (startIndex < 0)
            {
                return value;
            }
            endIndex = result.IndexOf(']', startIndex);
            while (startIndex > 0 && endIndex > 0)
            {
                iterator = result.Substring(startIndex + 1, endIndex - startIndex - 1);
                if (classNameAlias.ContainsKey(iterator) && classNameAlias[iterator].referenceObject == null)
                {
                    evaluatedIterator = classNameAlias[iterator].iteration.ToString();
                }
                else if (int.TryParse(iterator, out int parseval) && parseval.ToString() == iterator)
                {
                    evaluatedIterator = iterator;
                }
                else
                {
                    dynamic evaluatedObj = await EvaluateExpressionAsync(iterator, classNameAlias);
                    evaluatedIterator = evaluatedObj.ToString();
                }
                if (iterator != "" && iterator != evaluatedIterator)
                {
                    result = result.Replace("[" + iterator + "]", "[" + evaluatedIterator + "]");
                    indexOffset = endIndex + evaluatedIterator.Length - iterator.Length;
                }
                else
                {
                    indexOffset = endIndex;
                }
                startIndex = result.IndexOf('[', indexOffset);
                if (startIndex > 0)
                {
                    endIndex = result.IndexOf(']', startIndex);
                }
            }
            return result;
        }

        private dynamic GetDefaultProperty(KEntity entity, string fullExpression)
        {
            KProperty property = GetProperty(fullExpression, entity);
            switch (property.Type)
            {
                case PropertyType.str:
                    return "";
                case PropertyType.number:
                    return 0;
                case PropertyType.boolean:
                    return false;
                case PropertyType.kstring:
                    return "";
            }
            return null;
        }

        private string GetFullExpressionFromAliasEvaluation(string value, Dictionary<string, AliasReference> classNameAlias, out bool isIterator, out int kobjectindex)
        {
            try
            {
                isIterator = false;
                kobjectindex = 0;
                do
                {
                    foreach (string key in classNameAlias.Keys)
                    {
                        AliasReference alias = classNameAlias[key];
                        if (alias.referenceObject == null)
                        {
                            value = value.Replace("[" + key + "]", "[" + alias.iteration.ToString() + "]");
                        }
                    }
                    string[] expressionSplit = value.Split('.');
                    if (!expressionSplit[0].StartsWith("kresult"))
                    {
                        string baseExpression = expressionSplit[0]?.ToLower();
                        if (classNameAlias.ContainsKey(baseExpression))
                        {
                            AliasReference aliasReference = classNameAlias[baseExpression];
                            if (aliasReference.kobjectIndex > 0)
                                kobjectindex = aliasReference.kobjectIndex;
                            if (classNameAlias.ContainsKey(baseExpression) && classNameAlias[baseExpression].aliasType == AliasType.absoluteAlias)
                            {
                                return value;
                            }
                            if (aliasReference.referenceObject == null)
                            {
                                expressionSplit[0] = aliasReference.iteration.ToString();
                                isIterator = true;
                            }
                            else
                            {
                                expressionSplit[0] = aliasReference.referenceObject
                                    + (aliasReference.iteration == -1 ? "" : "[" + aliasReference.iteration + "]");
                            }
                        }
                        //Handle K-Object aliasing to arrays.
                        else if (classNameAlias.ContainsKey(baseExpression.Split('[')[0]))
                        {
                            string[] expSplit = expressionSplit[0]?.Split('[');
                            AliasReference aliasReference = classNameAlias[expSplit[0]?.ToLower()];
                            if (aliasReference.referenceObject != null && aliasReference.iteration == -1)
                            {
                                expSplit[0] = aliasReference.referenceObject;
                                expressionSplit[0] = string.Join("[", expSplit);
                            }
                        }
                    }
                    value = string.Join(".", expressionSplit);
                } while (classNameAlias.ContainsKey(value.Split('.')[0].ToLower()) && !value.Split('.')[0].StartsWith("kresult"));
                return value;
            }
            catch (Exception ex) { throw; }
        }
        #endregion

        #region get data from source
      
        private async Task<dynamic> GetDataFromWebActionAsync(string fullExpression, Dictionary<string, object> whereCondition = null)
        {
            var expressionArray = fullExpression.ToLower().Split('.');
            expressionArray[0] = "WebActions";
            dynamic dataObject = null;
            #region WEB ACTION
            if (expressionArray != null && expressionArray.Length > 0 && expressionArray[0].ToLower().Equals("webactions"))
            {
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
                    dataObject = await ApiHelper.GetWebActionsDataAsync(developerId, webactionwidget, tag, null, false);
                    ChangePropertiesToLowerCase(dataObject);
                    webactionsApiDictionary.TryAdd(webactionwidget.ToLower(), dataObject);
                }

                dynamic result = null;

                if (dataObject != null)
                {
                    var extraObject = dataObject["extra"];
                    if (expressionArray.Length > 1)
                        expressionArray[1] = expressionArray[1].Replace(webactionwidget, "data");
                    for (int i = 1; i < expressionArray.Length; i++)
                    {
                        var expressionValue = expressionArray[i];
                        var value = expressionValue.Trim('[', ']');
                        if (value.Contains("["))
                        {
                            var dataElement = value.Split('[');
                            dataObject = dataObject[dataElement[0]];
                            var index = dataElement[1].Replace("]", "");
                            int numValue;
                            bool parsed = Int32.TryParse(index, out numValue);
                            if (!parsed)
                            {
                                numValue = 0;
                            }

                            //if (isDetailsView && !parsed)
                            //{
                            //    dataObject = GetElementFromArray(dataObject, index, "_id", fullExpression, viewDetails, isDetailsView, false, isNFSite);
                            //}
                            //else
                            //{
                            //dataObject = GetElementFromArray(dataObject, numValue.ToString(), "_id", fullExpression, viewDetails, false, false, isNFSite);
                            dataObject = GetElementFromArray(dataObject, numValue.ToString(), "_id", fullExpression, viewDetails, false, false, false);
                            //}
                        }
                        else
                        {
                            if (value.Contains("length"))
                            {
                                dataObject = extraObject["totalcount"];

                                result = dataObject;
                                break;
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

                    if (dataObject != null)
                    {
                        decimal value = 0;
                        if (decimal.TryParse(dataObject.ToString(), out value))
                            result = value;
                        else
                            result = dataObject;
                    }
                }
                if (whereCondition != null && result != null)
                {
                    try
                    {
                        foreach (KeyValuePair<string, object> condition in whereCondition)
                        {
                            JArray resultArray = new JArray();
                            foreach (dynamic res in result)
                            {
                                if (res[condition.Key] == condition.Value)
                                {
                                    resultArray.Add(res);
                                    break;
                                }
                            }
                            result = resultArray;
                        }
                    }
                    catch { }
                }
                return result;
            }
            #endregion
            return null;
        }

        private async Task<dynamic> GetBusinessDataFromAPIAsync(string fullExpression, Dictionary<string, object> whereCondition = null, int kobjectindex = 0)
        {
            List<PropertyPathSegment> propertyList = Helper.ExtractPropertiesFromPath(fullExpression, entity);
            if (whereCondition != null)
            {
                propertyList.Last().Filter = whereCondition;
            }
            //return ApiHelper.GetBusinessDataFromPropertyList(propertyList, schemaId, websiteId);
            var result = await MongoConnector.MongoHelper.GetWebsiteDataByPropertyPathAsync(propertyList, schemaId, websiteId, entity, collectionKIDMap, kobjectindex);
            //return result;
            return JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(result.Data));
            //if (result != null)
            //    return JsonConvert.DeserializeObject<dynamic>(result);
            //return null;
        }

        public string PrefillObjectKIDs(string fullExpression, string objectReference, string objectKey, int kobjectIndex)
        {
            List<PropertyPathSegment> propertyList = Helper.ExtractPropertiesFromPath(fullExpression, entity);
            var filter = new Dictionary<string, object>();
            filter.Add("websiteid", websiteId);
            double filterValue = 0;
            if (double.TryParse(objectKey, out filterValue))
            {
                filter.Add(objectReference, filterValue);
            }
            else
            {
                filter.Add(objectReference, objectKey);
            }
            propertyList[propertyList.Count - 1].Filter = filter;
            var parentId = MongoConnector.MongoHelper.PrefillKIDsAsync(propertyList, schemaId, websiteId, entity, 0, 1, collectionKIDMap, true, kobjectIndex).Result;
            return parentId;
        }
        public async Task PrefillKIDsAsync(string fullExpression, int startIndex, int endIndex)
        {
            List<PropertyPathSegment> propertyList = Helper.ExtractPropertiesFromPath(fullExpression, entity);
            await MongoConnector.MongoHelper.PrefillKIDsAsync(propertyList, schemaId, websiteId, entity, startIndex, endIndex, collectionKIDMap);
        }
        //Change all property name to lower to support case insensitive 
        public void ChangePropertiesToLowerCase(JToken jsonObject)
        {
            if (jsonObject.Type == JTokenType.Object)
            {
                foreach (var property in ((JObject)jsonObject).Properties().ToList())
                {
                    if (property.Value.Type == JTokenType.Object)// replace property names in child object
                        ChangePropertiesToLowerCase((JObject)property.Value);
                    else if (property.Value.Type == JTokenType.Array)
                    {
                        ChangePropertiesToLowerCase((JArray)property.Value);
                    }
                    property.Replace(new JProperty(property.Name.ToLower(), property.Value));// properties are read-only, so we have to replace them
                }
            }
            else if (jsonObject.Type == JTokenType.Array)
            {
                foreach (JToken property in (JArray)jsonObject)
                {
                    if (property.Type == JTokenType.Object)// replace property names in child object
                        ChangePropertiesToLowerCase((JObject)property);
                    else if (property.Type == JTokenType.Array)
                    {
                        ChangePropertiesToLowerCase((JArray)property);
                    }
                }

            }
            else if (jsonObject.Type == JTokenType.Property)
            {
                JProperty property = (JProperty)jsonObject;
                property.Replace(new JProperty(property.Name.ToLower(), property.Value));
            }
        }

        private dynamic GetData(string fullExpression, dynamic dataObject)
        {
            try
            {
                var expressionArray = fullExpression.Split('.');
                var count = 0;

                foreach (var expressionValue in expressionArray)
                {
                    var value = expressionValue.Trim('[', ']').ToLower();

                    if (value.Contains("["))
                    {
                        var dataElement = value.Split('[');
                        dataObject = dataObject[dataElement[0]];
                        var index = dataElement[1].Replace("]", "");
                        int numValue;
                        bool parsed = Int32.TryParse(index, out numValue);
                        if (!parsed)
                        {
                            numValue = 0;
                        }
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
                            object tempDataObject;
                            //To support json string value in k-script response, ideally there wont be any case but for nowfloats search api its coming.
                            if (dataObject.GetType().Name != "JObject")
                            {
                                dataObject = JsonConvert.DeserializeObject(dataObject.ToString());
                                ChangePropertiesToLowerCase(dataObject);
                            }

                            tempDataObject = dataObject[value];

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

                    count++;
                }
                return dataObject;
            }
            catch (Exception ex) { }
            return null;
        }
        #endregion

        #region data helper functions
        private static dynamic GetResultValue(dynamic result)
        {
            dynamic evaluationResult;
            if (result.GetType() == typeof(Dictionary<string, object>))
            {
                JObject jobject = new JObject();
                foreach (string key in result.Keys)
                {
                    if (result.TryGetValue(key, out dynamic keyValue))
                    {
                        jobject.Add(key, GetResultValue(keyValue));
                    }
                }
                evaluationResult = jobject;
            }
            else if (result.GetType() == typeof(Object[]))
            {
                dynamic arr = (dynamic)result;
                JArray arrResult = new JArray();
                foreach (dynamic arrValue in arr)
                {
                    arrResult.Add(GetResultValue(arrValue));
                }
                evaluationResult = arrResult;
            }
            else
            {
                evaluationResult = result;
            }

            return evaluationResult;
        }

        public object GetElementFromArray(dynamic arrayObject, string elementIndex)
        {
            try
            {
                if (arrayObject != null)
                {
                    return GetValueFromArray(arrayObject, elementIndex);
                }
            }
            catch { }
            return null;
        }

        private string GetReplacedValue(string actualString, string pattern, string replaceValue)
        {
            try
            {
                actualString = actualString.Replace(pattern, replaceValue);
            }
            catch { }
            return actualString;
        }

        private object GetElementFromArray(dynamic arrrayObject, string elementIndex, string queryString, string expression, Models.Pagination viewDetails, bool isDetailsView, bool isDetailsViewObject, bool isNFSite)
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

                try
                {
                    if (arrrayObject != null)
                    {
                        return GetValueFromArray(arrrayObject, elementIndex);
                    }
                }
                catch
                {
                    try
                    {
                        return GetDetailsObjectFromArray(arrrayObject, elementIndex, queryString);
                    }
                    catch (Exception ex2) { throw ex2; }
                }
            }
            catch { }
            return null;
        }

        private bool DynamicFieldExists(dynamic obj, int field)
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
                EventLogger.Write(ex, "Kitsune Helper :: DynamicFieldExists ", null);
            }
            return retval;
        }

        private dynamic GetValueFromArray(dynamic obj, string field)
        {
            try
            {
                if (int.TryParse(field, out int index) && field == index.ToString())
                {
                    return obj[index];
                }
            }
            catch
            { }
            return obj[field];
        }

        private dynamic GetOfferDetailsObject(dynamic offersArray, string index)
        {
            try
            {
                Offer[] finalupdateArray = offersArray.ToObject<Offer[]>();
                var updateElement = finalupdateArray.ToList().Where(s => s.index.Equals(index)).FirstOrDefault();
                if (updateElement != null)
                {
                    dynamic update = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(updateElement)); ;
                    return update;
                }
            }
            catch { }
            return null;
        }

        private dynamic GetCustomPagesDetailsObject(dynamic offersArray, string index)
        {
            try
            {
                CustomPagesModel[] finalupdateArray = offersArray.ToObject<CustomPagesModel[]>();
                var updateElement = finalupdateArray.ToList().Where(s => s.id.Equals(index)).FirstOrDefault();
                if (updateElement != null)
                {
                    dynamic update = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(updateElement)); ;
                    return update;
                }
            }
            catch { }
            return null;
        }

        private dynamic GetProductDetailsObject(dynamic offersArray, string index)
        {
            try
            {
                Product[] finalupdateArray = offersArray.ToObject<Product[]>();
                var updateElement = finalupdateArray.ToList().Where(s => s.index.Equals(index)).FirstOrDefault();
                if (updateElement != null)
                {
                    dynamic update = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(updateElement)); ;
                    return update;
                }
            }
            catch { }
            return null;
        }

        private dynamic GetUpdateDetailsObject(dynamic updatesArray, string index)
        {
            try
            {
                Update[] finalupdateArray = updatesArray.ToObject<Update[]>();
                var updateElement = finalupdateArray.ToList().Where(s => s.index.Equals(index)).FirstOrDefault();
                if (updateElement != null)
                {

                    dynamic update = JsonConvert.DeserializeObject<dynamic>(JsonConvert.SerializeObject(updateElement)); ;
                    return update;
                }
            }
            catch { }
            return null;
        }

        private dynamic GetDetailsObjectFromArray(dynamic arrayObject, string uniqueValue, string queryString)
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
            catch { }
            return null;
        }

        private dynamic GetWebActionDetailsObject(dynamic webactionsArray, string index)
        {
            try
            {
                var detailObject = ((IEnumerable<dynamic>)webactionsArray).Where(d => d._id.Value == index).FirstOrDefault();
                return detailObject;
            }
            catch { }
            return null;
        }

        public long GetObjectSize(object obj)
        {
            try
            {
                if (obj != null)
                {
                    dynamic objectValue = null;
                    objectValue = obj;
                    var tempObj = obj;
                    if (tempObj.GetType() == typeof(string))
                    {
                        return obj.ToString().Length;
                    }
                    var objLength = objectValue.Count;
                    return objLength;
                }
            }
            catch (Exception ex)
            {

            }

            return 0;
        }

        internal KProperty GetProperty(string expression, KEntity entity)
        {
            KProperty kProperty = null;
            KClass kClass = null;
            string[] classHierarchyList = expression.Split('.');
            if (entity != null)
            {
                kClass = entity.Classes.Where(x => x.ClassType == KClassType.BaseClass && x.Name.ToLower() == classHierarchyList[0].ToLower()).FirstOrDefault();
                if (kClass == null)
                {
                    return null;
                }
                for (int i = 1; i < classHierarchyList.Length - 1; i++)
                {
                    string propName = classHierarchyList[i].Split('[')[0];
                    KProperty prop = kClass.PropertyList.Where(x => x.Name.ToLower() == propName.ToLower()).FirstOrDefault();
                    if (prop == null)
                    {
                        return null;
                    }
                    kClass = entity.Classes.Where(x => x.Name.ToLower() == prop.DataType.Name.ToLower()).FirstOrDefault();
                    if (kClass == null)
                    {
                        return null;
                    }
                }
                string finalPropName = classHierarchyList[classHierarchyList.Length - 1].Split('[')[0].ToLower();
                kProperty = kClass.PropertyList.Where(x => x.Name == finalPropName).FirstOrDefault();
            }
            return kProperty;
        }
        #endregion
    }
}
