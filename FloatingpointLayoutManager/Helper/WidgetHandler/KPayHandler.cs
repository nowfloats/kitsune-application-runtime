using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KitsuneLayoutManager.Helper.WidgetHandler
{
    public class KPayHandler
    {
        public static string ProcessKPay(string htmlString,string websiteId)
        {
            if (htmlString == null)
                throw new ArgumentNullException(nameof(htmlString));
            if (string.IsNullOrEmpty(websiteId))
                throw new ArgumentNullException(nameof(websiteId));

            try
            {
                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(htmlString);
                ProcessKPay(htmlDocument.DocumentNode, websiteId);
                return htmlDocument.DocumentNode.OuterHtml;
            }
            catch(Exception ex)
            {
                EventLogger.Write(ex, $"KLM exception occured while Processing k-pay for WebsiteId:{websiteId}");
                return htmlString;
            }
        }

        public static void ProcessKPay(HtmlNode htmlNode,string websiteId)
        {
            if (htmlNode == null)
                throw new ArgumentNullException(nameof(htmlNode));
            if (string.IsNullOrEmpty(websiteId))
                throw new ArgumentNullException(nameof(websiteId));

            try
            {
                //Select List of all k-pay tags
                var nodes = htmlNode.SelectNodes("//*[@k-pay-amount]");

                //Couldn't found any k-pay tag
                if (nodes == null)
                    return;

                //Get all k-pay-amount
                List<string> amountList = new List<string>();
                foreach(var node in nodes)
                {
                    var kPayAmount = node.GetAttributeValue("k-pay-amount","0");
                    amountList.Add(kPayAmount);
                }

                //Get all k-pay-checksum
                var checkSumAPIResponse = ApiHelper.GetKPayEncodedCheckSum(websiteId, amountList);
                amountList = checkSumAPIResponse.amounts;
                List<string> checkSumList = checkSumAPIResponse.checksums;
                
                //Set k-pay-checksum
                for(int i=0;i<nodes.Count;i++)
                {
                    var kPayAmount = nodes[i].GetAttributeValue("k-pay-amount", "0");
                    if(kPayAmount.Equals(amountList[i],StringComparison.InvariantCultureIgnoreCase))
                    {
                        nodes[i].SetAttributeValue("k-pay-checksum", checkSumList[i]);
                    }
                    else
                    {
                        //LOG
                    }
                }
            }
            catch(Exception ex)
            {
                EventLogger.Write(ex, $"KLM exception occured while Processing k-pay for WebsiteId:{websiteId}");
            }
        }
    }
}
