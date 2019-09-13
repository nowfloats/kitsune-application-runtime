using AntlrLibrary;
using AntlrLibrary.Model;
using Kitsune.Language.Models;
using Kitsune.Server.Model.Kitsune;
using Kitsune.SyntaxParser;
using Kitsune.SyntaxParser.Models;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;

namespace KitsuneLayoutManager.Helper
{
    public class ExpressionEvaluator
    {
        private Dictionary<string, object> evaluatedExpressions;
        private Dictionary<string, dynamic> webactionsApiDictionary = new Dictionary<string, dynamic>();
        private Dictionary<string, dynamic> searchApiDictionary = new Dictionary<string, dynamic>();

        /// <summary>
        /// Initialize resources required for expression evaluation.
        /// </summary>
        public ExpressionEvaluator()
        {
            evaluatedExpressions = new Dictionary<string, object>();
        }

        #region evaluate expression
        /// <summary>
        /// Handle expression evaluation.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="classNameAlias"></param>
        /// <returns></returns>
        public dynamic EvaluateExpression(string value, KEntity entity, Models.Pagination viewDetails, Dictionary<string, AliasReference> classNameAlias, dynamic businessData, dynamic kresult, string queryString, out bool hasData, Dictionary<string, long> functionLog, bool isDetailsView = false, bool isNFSite = false, string developerId= null)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            try
            {
                value = Helper.TrimDelimiters(value);
                Node tree = LexerGenerator.Parse(value);
                Token result = null;
                hasData = false;
                if (tree != null)
                {
                    string baseClassName = entity.Classes.Where(c => c.ClassType == KClassType.BaseClass).FirstOrDefault().Name;
                    ReplaceExpressionsInTreeAsync(tree, baseClassName, viewDetails, classNameAlias, businessData, entity, kresult, queryString, isDetailsView, isNFSite, developerId);
                    result = ParseTress.Parse(tree);
                    if (result?.Type == null)
                    {
                        hasData = false;
                    }
                    else
                    {
                        hasData = result.Type != TOKENTYPE.NoData;
                    }
                }
                return result?.Value ?? "";
            }
            catch (Exception ex) { throw; }
            finally
            {
                stopwatch.Stop();
                Helper.UpdateFunctionLog(functionLog, String.Format(Constant.EVALUATE_EXPRESSION, value), stopwatch.ElapsedMilliseconds);
            }
        }

        public dynamic EvaluateExpression(Node tree, KEntity entity, Models.Pagination viewDetails, Dictionary<string, AliasReference> classNameAlias, dynamic businessData, dynamic kresult, string queryString, out bool hasData, Dictionary<string, long> functionLog, bool isDetailsView = false, bool isNFSite = false, string developerId = null)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            try
            {
                string baseClassName = entity.Classes.Where(c => c.ClassType == KClassType.BaseClass).FirstOrDefault().Name;
                ReplaceExpressionsInTreeAsync(tree, baseClassName, viewDetails, classNameAlias, businessData, entity, kresult, queryString, isDetailsView, isNFSite, developerId);
                Token result = ParseTress.Parse(tree);
                if (result?.Type == null)
                {
                    hasData = false;
                }
                else
                {
                    hasData = result.Type != TOKENTYPE.NoData;
                }
                return result?.Value ?? "";
            }
            catch (Exception ex) { throw; }
            finally
            {
                stopwatch.Stop();
                Helper.UpdateFunctionLog(functionLog, String.Format(Constant.EVALUATE_EXPRESSION, "Tree"), stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Replace expression data with actual data.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="entity"></param>
        /// <param name="classNameAlias"></param>
        /// <param name="businessData"></param>
        /// <param name="kresult"></param>
        /// <param name="webApiData"></param>
        private async System.Threading.Tasks.Task ReplaceExpressionsInTreeAsync(Node tree, string baseClassName, Models.Pagination viewDetails, Dictionary<string, AliasReference> classNameAlias, dynamic businessData, KEntity entity, dynamic kresult, string queryString, bool isDetailsView, bool isNFSite, string developerId)
        {
            try
            {
                Queue<Node> processQueue = new Queue<Node>();
                processQueue.Enqueue(tree);
                while (processQueue.Count > 0)
                {
                    Node node = processQueue.Dequeue();
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
                        var s = Evaluator.OperandEval(node.Children[0].Token);
                        if (s.Type == TOKENTYPE.Object)
                        {
                            string value = s.Value;

                            bool hasIterator = value.IndexOf("[") > 0;
                            string fullExpression = GetFullExpressionFromAliasEvaluation(value, classNameAlias, out bool isIterator, isDetailsView);
                            if (isIterator)
                            {
                                node.Children[0].Token.Value = long.Parse(fullExpression);
                            }
                            else if (evaluatedExpressions.ContainsKey(fullExpression.ToLower()))
                            {
                                node.Children[0].Token.Value = evaluatedExpressions[fullExpression.ToLower()];
                                if (node.Children[0].Token.Value.GetType() == typeof(JArray))
                                {
                                    node.Children[0].Token.Type = TOKENTYPE.Array;
                                }
                            }
                            else
                            {
                                dynamic evaluationResult = null;
                                string baseReference = fullExpression.Split('.')[0].Split('[')[0].ToLower();
                                try
                                {
                                    if (baseReference == "kresult")
                                    {
                                        evaluationResult = GetDataFromKResult(fullExpression, kresult, classNameAlias);
                                    }
                                    else if (baseClassName == baseReference)
                                    {
                                        fullExpression = fullExpression.ToLower();
                                        evaluationResult = GetDataFromBusiness(fullExpression, queryString, viewDetails, businessData, isDetailsView, isNFSite, classNameAlias);
                                        if (evaluationResult == null)
                                        {
                                            evaluationResult = GetDefaultProperty(entity, fullExpression);
                                        }
                                        if (!hasIterator)
                                        {
                                            if (evaluationResult.GetType() == typeof(DateTime))
                                            {
                                                evaluatedExpressions.Add(fullExpression, "'" + evaluationResult.ToString("s") + "'");
                                            }
                                            else if (evaluationResult.GetType() == typeof(string) && evaluationResult != "")
                                            {
                                                evaluatedExpressions.Add(fullExpression, "'" + evaluationResult + "'");
                                            }
                                            else
                                            {
                                                evaluatedExpressions.Add(fullExpression, evaluationResult);
                                            }
                                        }
                                    }
                                    else if (baseReference == "webactions")
                                    {
                                        evaluationResult = await GetDataFromWebActionAsync(fullExpression, viewDetails, businessData, isDetailsView, isNFSite, classNameAlias, developerId).Result;
                                    }
                                    else if (baseReference == "_system")
                                    {
                                        string[] expParts = fullExpression.Split('.');
                                        if (expParts[1].Equals("components", StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            //components
                                            evaluationResult = GetDataFromBusiness(fullExpression, queryString, viewDetails, businessData._system, isDetailsView, isNFSite, classNameAlias);
                                        }
                                        else
                                        {
                                            //custom variables
                                            evaluationResult = classNameAlias[expParts[expParts.Length - 1]].referenceObject;
                                        }
                                    }
                                    else if (classNameAlias.ContainsKey(fullExpression))
                                    {
                                        evaluationResult = classNameAlias[fullExpression].referenceObject;
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
                        foreach (Node childNode in node.Children)
                        {
                            if (childNode != null)
                            {
                                processQueue.Enqueue(childNode);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { throw; }
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

        /// <summary>
        /// Evaluate full expression after alias mapping.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="classNameAlias"></param>
        /// <returns></returns>
        private string GetFullExpressionFromAliasEvaluation(string value, Dictionary<string, AliasReference> classNameAlias, out bool isIterator, bool isDetailsView = false)
        {
            try
            {
                isIterator = false;
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
                        if (classNameAlias.ContainsKey(expressionSplit[0]?.ToLower()))
                        {
                            AliasReference aliasReference = classNameAlias[expressionSplit[0]?.ToLower()];
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
                        else if (classNameAlias.ContainsKey(expressionSplit[0]?.Split('[')[0]?.ToLower()))
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
                } while (classNameAlias.ContainsKey(value.Split('.')[0].ToLower()) && value.Split('.')[0] != "kresult");
                return value;
            }
            catch (Exception ex) { throw; }
        }
        #endregion

        #region get data from source
      
        private async System.Threading.Tasks.Task<dynamic> GetDataFromWebActionAsync(string fullExpression, Models.Pagination viewDetails, dynamic businessData, bool isDetailsView, bool isNFSite, Dictionary<string, AliasReference> classNameAlias, string developerId)
        {
            var expressionArray = fullExpression.Split('.');
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
                    dataObject = await ApiHelper.GetWebActionsDataAsync(developerId, webactionwidget, isNFSite ? businessData.tag.Value : businessData.websiteid.Value, null, false);
                    webactionsApiDictionary.Add(webactionwidget.ToLower(), dataObject);
                }

                if (dataObject != null)
                {
                    var extraObject = dataObject["Extra"];
                    if (expressionArray.Length > 1)
                        expressionArray[1] = expressionArray[1].Replace(webactionwidget, "Data");
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
                                if (classNameAlias.ContainsKey(index))
                                {
                                    numValue = classNameAlias[index].iteration;
                                }
                            }

                            if (isDetailsView && !parsed)
                            {
                                dataObject = GetElementFromArray(dataObject, index, "_id", fullExpression, viewDetails, isDetailsView, false, isNFSite);
                            }
                            else
                            {
                                dataObject = GetElementFromArray(dataObject, numValue.ToString(), "_id", fullExpression, viewDetails, false, false, isNFSite);
                            }
                        }
                        else
                        {
                            if (value.Contains("length"))
                            {
                                dataObject = extraObject["TotalCount"];

                                return dataObject;
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
                            return value;
                        else
                            return dataObject;
                    }
                }
            }
            #endregion
            return null;
        }

        private dynamic GetDataFromBusiness(string fullExpression, string queryString, Models.Pagination viewDetails, dynamic businessData, bool isDetailsView, bool isNFSite, Dictionary<string, AliasReference> classNameAlias)
        {
            dynamic dataObject = null;
            var expressionArray = fullExpression.Split('.');
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
                        if (count > 0 && !value.ToLower().Contains("["))
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
                        else if (count > 0 && value.ToLower().Contains("["))
                        {
                            var dataElement = value.ToLower().Split('[');
                            dataObject = dataObject[dataElement[0]];
                            var index = dataElement[1].Replace("]", "");
                            int numValue; var detailValue = string.Empty;
                            bool parsed = Int32.TryParse(index, out numValue);
                            bool isDetailViewObject = false;
                            if (!parsed)
                            {
                                if (classNameAlias.ContainsKey(index))
                                {
                                    detailValue = classNameAlias[index].iteration.ToString();
                                }
                                else if (isDetailsView)
                                {
                                    detailValue = viewDetails.currentpagenumber;
                                    isDetailViewObject = true;
                                }
                            }
                            else
                            {
                                detailValue = numValue.ToString();
                            }

                            // change for details view
                            dataObject = GetElementFromArray(dataObject, detailValue, queryString, fullExpression, viewDetails, isDetailsView, isDetailViewObject, isNFSite);

                        }
                        else
                        {
                            if (businessData[value] != null)
                            {
                                dataObject = businessData[value];
                            }
                            else
                            {
                                dataObject = businessData;
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
                        return dataObject;
                    }
                    else if (type.Name.Equals("Double"))
                    {
                        return dataObject;
                    }

                    decimal value = 0;
                    if (decimal.TryParse(dataObject.ToString(), out value) && dataObject.ToString() == value.ToString())
                        return value;
                    else
                        return dataObject;

                }
            }
            return null;
            #endregion
        }

        private dynamic GetDataFromKResult(string fullExpression, dynamic kresult, Dictionary<string, AliasReference> classNameAlias, bool isArray = false)
        {
            try
            {
                var expressionArray = fullExpression.Split('.');
                var dataObject = kresult; var count = 0;
                foreach (var expressionValue in expressionArray)
                {
                    var value = expressionValue.Trim('[', ']');

                    if (count == 0 && isArray)
                    {
                        var dataElement = value.Split('[');
                        var index = dataElement[1].Replace("]", "");
                        int numValue;
                        bool parsed = Int32.TryParse(index, out numValue);
                        if (!parsed)
                        {
                            numValue = 0;
                            if (classNameAlias.ContainsKey(index))
                            {
                                numValue = classNameAlias[index].iteration;
                            }
                        }
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
                            {
                                numValue = 0;
                                if (classNameAlias.ContainsKey(index))
                                {
                                    numValue = classNameAlias[index].iteration;
                                }
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

                //decimal decimalvalue = 0;
                //if (decimal.TryParse(dataObject.ToString(), out decimalvalue))
                //    return decimalvalue;
                //else
                return dataObject;
            }
            catch { }
            return null;
        }
        #endregion

        #region data helper functions
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

        /// <summary>
        /// GetElement from array
        /// </summary>
        /// <param name="arrrayObject"></param>
        /// <param name="elementIndex"></param>
        /// <param name="queryString"></param>
        /// <param name="expression"></param>
        /// <param name="viewDetails"></param>
        /// <param name="isDetailsView"></param>
        /// <param name="isDetailsViewObject"></param>
        /// <returns></returns>
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
