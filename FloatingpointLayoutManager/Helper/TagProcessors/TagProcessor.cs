using HtmlAgilityPack;
using Kitsune.Language.Models;
using System.Collections.Generic;
using System.Diagnostics;

namespace KitsuneLayoutManager.Helper.TagProcessors
{
    public abstract class TagProcessor
    {
        public string TagProcessorIdentifier = "abstract";

        public void ProcessNode(ref HtmlNode node, HtmlAttribute dynamicAttribute, Dictionary<string, AliasReference> classNameAlias, Dictionary<int, string> classNameAliasdepth, int depth, string websiteId, ExpressionEvaluator evaluator, KEntity entity, dynamic websiteData, Models.Pagination viewDetails, string queryString, Dictionary<string, long> functionLog, bool isDetailsView = false, bool isNFSite = false, string developerId = null)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            Process(ref node, dynamicAttribute, classNameAlias, classNameAliasdepth, depth, websiteId, evaluator, entity, websiteData, viewDetails, queryString, functionLog, isDetailsView, isNFSite);
            stopwatch.Stop();
            Helper.UpdateFunctionLog(functionLog, TagProcessorIdentifier, stopwatch.ElapsedMilliseconds);
        }

        public abstract void Process(ref HtmlNode node, HtmlAttribute dynamicAttribute, Dictionary<string, AliasReference> classNameAlias, Dictionary<int, string> classNameAliasdepth, int depth, string websiteId, ExpressionEvaluator evaluator, KEntity entity, dynamic websiteData, Models.Pagination viewDetails, string queryString, Dictionary<string, long> functionLog, bool isDetailsView = false, bool isNFSite = false, string developerId = null);
    }
}
