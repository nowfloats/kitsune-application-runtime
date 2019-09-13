using System.Collections.Generic;
using AntlrLibrary;
using AntlrLibrary.Model;
using HtmlAgilityPack;
using Kitsune.Language.Models;
using Newtonsoft.Json;

namespace KitsuneLayoutManager.Helper.TagProcessors
{
    /// <summary>
    /// Process nodes with k-repeat tags.
    /// </summary>
    public class KRepeatProcessor : TagProcessor
    {
        public KRepeatProcessor()
        {
            TagProcessorIdentifier = "KRepeatProcessor";
        }

        public override void Process(ref HtmlNode node, HtmlAttribute dynamicAttribute, Dictionary<string, AliasReference> classNameAlias, Dictionary<int, string> classNameAliasdepth, int depth, string websiteId, ExpressionEvaluator evaluator, KEntity entity, dynamic websiteData, Models.Pagination viewDetails, string queryString, Dictionary<string, long> functionLog, bool isDetailsView = false, bool isNFSite = false, string developerId = null)
        {
            Node result = LexerGenerator.Parse(Helper.TrimDelimiters(dynamicAttribute.Value));
            if (result.Token.Value == ACTIONS.Loop)
            {
                string referenceObject = result.Children[0].Children[0].Token.Value;
                string referenceName = result.Children[2].Children[0].Token.Value?.ToLower();
                var refObj = evaluator.EvaluateExpression(referenceObject, entity, viewDetails, classNameAlias, websiteData, websiteData?._system?.kresult, queryString, out bool hasData, functionLog, isDetailsView, isNFSite);
                if (refObj.GetType() == typeof(string) && refObj == "")
                {
                    node = HtmlCommentNode.CreateNode("<!-- skip -->");
                    return;
                }
                string offsetObj = evaluator.EvaluateExpression(result.Children[6], entity, viewDetails, classNameAlias, websiteData, websiteData?._system?.kresult, queryString, out hasData, functionLog, isDetailsView, isNFSite, developerId).ToString();
                int.TryParse(offsetObj, out int offset);
                int iteration = 0;
                int maxIteration = 0;
                if (dynamicAttribute.Value.IndexOf("offset") > 0)
                {
                    if (int.TryParse(viewDetails.currentpagenumber, out int currentPage) && currentPage > 0)
                    {
                        iteration = offset * (currentPage - 1);
                    }
                }
                else
                {
                    string iterationObj = evaluator.EvaluateExpression(result.Children[4], entity, viewDetails, classNameAlias, websiteData, websiteData?._system?.kresult, queryString, out hasData, functionLog, isDetailsView, isNFSite, developerId).ToString();
                    int.TryParse(iterationObj, out iteration);
                }
                maxIteration = iteration + offset;
                int objSize = (int)evaluator.GetObjectSize(refObj);
                maxIteration = (maxIteration < objSize) ? maxIteration : objSize;
                if (iteration > maxIteration)
                {
                    node = HtmlCommentNode.CreateNode("<!-- skip -->");
                    return;
                }
                else if (objSize == maxIteration && dynamicAttribute.Value.IndexOf("offset") > 0)
                {
                    viewDetails.nextpage.url = "#";
                    viewDetails.pagesize = maxIteration - iteration + 1;
                }

                AliasReference aliasReference = new AliasReference
                {
                    referenceObject = null,
                    iteration = iteration,
                    maxIteration = maxIteration
                };
                if (!classNameAlias.ContainsKey(referenceName))
                {
                    classNameAlias.Add(referenceName, aliasReference);
                    classNameAliasdepth.Add(depth, referenceName);
                }
            }
            else if (result.Token.Value == ACTIONS.InLoop)
            {
                string referenceName = result.Children[0].Children[0].Token.Value?.ToLower();
                string referenceObject = result.Children[2].Children[0].Token.Value;
                var obj = evaluator.EvaluateExpression(referenceObject, entity, viewDetails, classNameAlias, websiteData, websiteData?._system?.kresult, queryString, out bool hasData, functionLog, isDetailsView, isNFSite);
                if (obj.GetType() == typeof(string) && obj == "")
                {
                    node = HtmlCommentNode.CreateNode("<!-- skip -->");
                    return;
                }
                AliasReference aliasReference = new AliasReference();
                aliasReference.referenceObject = referenceObject;
                aliasReference.iteration = 0;
                aliasReference.maxIteration = (int)evaluator.GetObjectSize(obj);

                if (!classNameAlias.ContainsKey(referenceName))
                {
                    classNameAlias.Add(referenceName, aliasReference);
                    classNameAliasdepth.Add(depth, referenceName);
                }
            }
        }
    }
}
