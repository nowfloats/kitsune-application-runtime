using System.Collections.Generic;
using Kitsune.AntlrLibrary;
using Kitsune.AntlrLibrary.Model;
using HtmlAgilityPack;
using Kitsune.Language.Models;

namespace KitsuneLayoutManager.Helper.TagProcessors
{
    public class KObjectProcessor : TagProcessor
    {
        public KObjectProcessor()
        {
            TagProcessorIdentifier = "KObjectProcessor";
        }

        /// <summary>
        /// Process nodes with k-object tags.
        /// </summary>
        public override void Process(ref HtmlNode node, HtmlAttribute dynamicAttribute, Dictionary<string, AliasReference> classNameAlias, Dictionary<int, string> classNameAliasdepth, int depth, string websiteId, ExpressionEvaluator evaluator, KEntity entity, dynamic websiteData, Models.Pagination viewDetails, string queryString, Dictionary<string, long> functionLog, bool isDetailsView = false, bool isNFSite = false, string developerId = null)
        {
            Node result = LexerGenerator.Parse(Helper.TrimDelimiters(dynamicAttribute.Value).Trim('[').Trim(']'));
            if (result?.Children?.Count == 3)
            {
                string referenceName = result.Children[0].Children[0].Token.Value.ToLower();
                string referenceObjectKey = result.Children[2].Children[0].Token.Value;
                AliasReference aliasReference = new AliasReference
                {
                    referenceObject = referenceObjectKey,
                    iteration = -1,
                    maxIteration = -1
                };
                if (!classNameAlias.ContainsKey(referenceName))
                {
                    classNameAlias.Add(referenceName, aliasReference);
                }
            }
        }
    }
}
