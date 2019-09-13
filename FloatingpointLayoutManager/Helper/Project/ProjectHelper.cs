using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace KitsuneLayoutManager.Helper.Project
{
    public class ProjectHelper
    {
        public static string GetHtmlStringFromUrl(string urlString)
        {
            try
            {
                var myHttpWebRequest = (HttpWebRequest)WebRequest.Create(urlString);
                myHttpWebRequest.UserAgent = "Crawler";
                var response = string.Empty;
                using (Stream s = myHttpWebRequest.GetResponse().GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(s))
                    {
                        var jsonData = sr.ReadToEnd();
                        response += jsonData.ToString();
                    }
                }

                return response;
            }
            catch (Exception ex)
            {

            }

            return null;
        }
    }
}
