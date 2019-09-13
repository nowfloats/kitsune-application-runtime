using System.Collections.Generic;
using HtmlAgilityPack;
using Kitsune.Language.Models;

namespace KitsuneLayoutManager.Helper.TagProcessors
{
    public class KHideProcessor : TagProcessor
    {
        public KHideProcessor()
        {
            TagProcessorIdentifier = "KHideProcessor";
        }

        public override void Process(ref HtmlNode node, HtmlAttribute dynamicAttribute, Dictionary<string, AliasReference> classNameAlias, Dictionary<int, string> classNameAliasdepth, int depth, string websiteId, ExpressionEvaluator evaluator, KEntity entity, dynamic websiteData, Models.Pagination viewDetails, string queryString, Dictionary<string, long> functionLog, bool isDetailsView = false, bool isNFSite = false, string developerId = null)
        {
            if (Kitsune.Helper.Constants.WidgetRegulerExpression.IsMatch(dynamicAttribute.Value))
            {
                string attributeValue = evaluator.EvaluateExpression(dynamicAttribute.Value, entity, viewDetails, classNameAlias, websiteData, websiteData?._system?.kresult, queryString, out bool hasData, functionLog, isDetailsView, isNFSite, developerId).ToString();
                if (!hasData || (System.Boolean.TryParse(attributeValue, out bool attVal) && attVal))
                {
                    node = HtmlCommentNode.CreateNode("<!-- skip -->");
                }
            }
        }
    }
}
