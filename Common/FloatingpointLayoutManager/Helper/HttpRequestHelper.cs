using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace KitsuneLayoutManager.Helper
{
    public class HttpRequestHelper
    {
        internal static string GetIpAddress(HttpRequest Request)
        {
            try
            {
                #region IP ADDRESS

                var ip = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

                if (string.IsNullOrWhiteSpace(ip) || string.Equals(ip, "unknown", StringComparison.OrdinalIgnoreCase))
                {
                    ip = Request.ServerVariables["REMOTE_ADDR"];
                }

                #endregion

                return ip;
            }
            catch (Exception ex)
            {

            }

            return null;

        }

        public static bool IsMobileDevice(HttpRequest request)
        {
            try
            {

                //Checking if user agent contains the follwing strings.
                if (request.ServerVariables["HTTP_USER_AGENT"] != null)
                {
                    #region Checking for exception devices

                    if (request.ServerVariables["HTTP_USER_AGENT"].ToLower().Contains("ipad"))
                        return false;

                    if (request.ServerVariables["HTTP_USER_AGENT"].ToLower().Contains("android") && !request.ServerVariables["HTTP_USER_AGENT"].ToLower().Contains("mobile"))
                        return false;

                    if (request.ServerVariables["HTTP_USER_AGENT"].ToLower().Contains("tablet"))
                        return false;

                    #endregion

                    if (request.Browser.IsMobileDevice)
                    {
                        return true;
                    }

                    //Checking For WAP Profile Heaeder
                    if (request.ServerVariables["HTTP_X_WAP_PROFILE"] != null)
                    {
                        return true;
                    }

                    //Checking for devices aceepting WAP
                    if (request.ServerVariables["HTTP_ACCEPT"] != null &&
                        request.ServerVariables["HTTP_ACCEPT"].ToLower().Contains("wap"))
                    {
                        return true;
                    }

                    //Create a list of all mobile types
                    string[] mobiles =

                    {
                        "midp", "j2me", "avant", "docomo",
                        "novarra", "palmos", "palmsource",
                        "240x320", "opwv", "chtml",
                        "pda", "windows ce", "mmp/",
                        "blackberry", "mib/", "symbian",
                        "wireless", "nokia", "hand", "mobi",
                        "phone", "cdm", "up.b", "audio",
                        "SIE-", "SEC-", "samsung", "HTC",
                        "mot-", "mitsu", "sagem", "sony"
                        , "alcatel", "lg", "eric", "vx",
                        "NEC", "philips", "mmm", "xx",
                        "panasonic", "sharp", "wap", "sch",
                        "rover", "pocket", "benq", "java",
                        "pt", "pg", "vox", "amoi",
                        "bird", "compal", "kg", "voda",
                        "sany", "kdd", "dbt", "sendo",
                        "sgh", "gradi", "jb", "dddi",
                        "moto", "iphone"
                    };

                    //Loop through each item in the list created above 
                    //and check if the header contains that text
                    foreach (string s in mobiles)
                    {
                        if (request.ServerVariables["HTTP_USER_AGENT"].
                                                            ToLower().Contains(s.ToLower()))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static bool IsCrawler(string userAgent)
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
