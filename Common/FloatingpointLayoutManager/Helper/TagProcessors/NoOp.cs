using System.Collections.Generic;
using HtmlAgilityPack;
using Kitsune.Language.Models;

namespace KitsuneLayoutManager.Helper.TagProcessors
{
    public class NoOp : TagProcessor
    {
        public NoOp()
        {
            TagProcessorIdentifier = "NoOp";
        }

        public override void Process(ref HtmlNode node, HtmlAttribute dynamicAttribute, Dictionary<string, AliasReference> classNameAlias, Dictionary<int, string> classNameAliasdepth, int depth, string websiteId, ExpressionEvaluator evaluator, KEntity entity, dynamic websiteData, Models.Pagination viewDetails, string queryString, Dictionary<string, long> functionLog, bool isDetailsView = false, bool isNFSite = false, string developerId = null)
        {
        }
    }
}
