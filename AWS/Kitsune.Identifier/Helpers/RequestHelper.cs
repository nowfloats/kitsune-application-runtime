using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kitsune.Identifier.Helpers
{
    public class RequestHelper
    {
        private static List<string> ValidViewParameters = new List<string>()
        {
            "bizfloat",
            "offer",
            "imagegallery",
            "mapview",
            "campaign",
            "updates-",
            "unsubscribe",
            "allproducts",
            "product",
            "search",
            "productsearch",
            "latest-offers",
            "pages",
            "captcha",
            "emailResponse",

            "bizfloat2",
            "offer2",
            "product2",
            "sitemap.xml"
        };
        
        internal static bool IsCrawler(string userAgent)
        {
            try
            {
                if (!String.IsNullOrEmpty(userAgent))
                {
                    List<string> Crawlers3 = new List<string>()
                                                        {
                                                            "bot","crawler","spider","80legs","baidu","yahoo! slurp","ia_archiver","mediapartners-google",
                                                            "lwp-trivial","nederland.zoek","ahoy","anthill","appie","arale","araneo","ariadne",
                                                            "atn_worldwide","atomz","bjaaland","ukonline","calif","combine","cosmos","cusco",
                                                            "cyberspyder","digger","grabber","downloadexpress","ecollector","ebiness","esculapio",
                                                            "esther","felix ide","hamahakki","kit-fireball","fouineur","freecrawl","desertrealm",
                                                            "gcreep","golem","griffon","gromit","gulliver","gulper","whowhere","havindex","hotwired",
                                                            "htdig","ingrid","informant","inspectorwww","iron33","teoma","ask jeeves","jeeves",
                                                            "image.kapsi.net","kdd-explorer","label-grabber","larbin","linkidator","linkwalker",
                                                            "lockon","marvin","mattie","mediafox","merzscope","nec-meshexplorer","udmsearch","moget",
                                                            "motor","muncher","muninn","muscatferret","mwdsearch","sharp-info-agent","webmechanic",
                                                            "netscoop","newscan-online","objectssearch","orbsearch","packrat","pageboy","parasite",
                                                            "patric","pegasus","phpdig","piltdownman","pimptrain","plumtreewebaccessor","getterrobo-plus",
                                                            "raven","roadrunner","robbie","robocrawl","robofox","webbandit","scooter","search-au",
                                                            "searchprocess","senrigan","shagseeker","site valet","skymob","slurp","snooper","speedy",
                                                            "curl_image_client","suke","www.sygol.com","tach_bw","templeton","titin","topiclink","udmsearch",
                                                            "urlck","valkyrie libwww-perl","verticrawl","victoria","webscout","voyager","crawlpaper",
                                                            "webcatcher","t-h-u-n-d-e-r-s-t-o-n-e","webmoose","pagesinventory","webquest","webreaper",
                                                            "webwalker","winona","occam","robi","fdse","jobo","rhcs","gazz","dwcp","yeti","fido","wlm",
                                                            "wolp","wwwc","xget","legs","curl","webs","wget","sift","cmc"
                                                        };
                    userAgent = userAgent.ToLower();
                    return Crawlers3.Exists(x => userAgent.Contains(x));
                }
            }
            catch { }

            return false;
        }
    }
}
