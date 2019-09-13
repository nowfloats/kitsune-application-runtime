using System.Collections.Generic;
using HtmlAgilityPack;
using Kitsune.Language.Models;

namespace KitsuneLayoutManager.Helper.TagProcessors
{
    class KPayAmountProcessor : TagProcessor
    {
        public KPayAmountProcessor()
        {
            TagProcessorIdentifier = "KPayAmountProcessor";
        }

        public override void Process(ref HtmlNode node, HtmlAttribute dynamicAttribute, Dictionary<string, AliasReference> classNameAlias, Dictionary<int, string> classNameAliasdepth, int depth, string websiteId, ExpressionEvaluator evaluator, KEntity entity, dynamic websiteData, Models.Pagination viewDetails, string queryString, Dictionary<string, long> functionLog, bool isDetailsView = false, bool isNFSite = false, string developerId = null)
        {
            List<string> amountList = new List<string>();
            string amount = Helper.TrimDelimiters(dynamicAttribute.Value);
            amount = evaluator.EvaluateExpression(amount, entity, viewDetails, classNameAlias, websiteData, websiteData?._system?.kresult, queryString, out bool hasData, functionLog, isDetailsView, isNFSite, developerId)?.ToString();
            dynamicAttribute.Value = amount;
            amountList.Add(amount);
            var checkSumAPIResponse = ApiHelper.GetKPayEncodedCheckSum(websiteId, amountList);
            amountList = checkSumAPIResponse.amounts;
            List<string> checkSumList = checkSumAPIResponse.checksums;
            node.SetAttributeValue("k-pay-checksum", checkSumList[0]);
        }
    }
}
