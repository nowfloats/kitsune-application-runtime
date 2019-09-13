using Kitsune.Language.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace KitsuneLayoutManager.Helper.Entity
{
    public class EntityHelper
    {
        private const string KitsuneClientId = "4C627432590419E9CF79252291B6A3AE25D7E2FF13347C6ACD1587C47C6ACDD";

        internal static KEntity GetEntityFromAPI(string entityId, bool isApiCacheEnabled)
        {
            try
            {
                var url = String.Format(Constant.LanguageEntityApi, Constant.KitsuneApiDomain, entityId, KitsuneClientId);

                var request = (HttpWebRequest)WebRequest.Create(new Uri(url));
                request.Method = "GET";
                request.ContentType = "application/json";
                WebResponse wbResponse = request.GetResponse();
                var sr = new StreamReader(wbResponse.GetResponseStream());
                var outputString = sr.ReadToEnd();

                if (!String.IsNullOrEmpty(outputString))
                {
                    var entity = JsonConvert.DeserializeObject<KEntity>(outputString);

                    if (isApiCacheEnabled)
                    {
                        CacheHelper.SaveEntityInfo(entityId, entity);
                    }

                    return entity;
                }
            }
            catch (Exception ex)
            {
                EventLogger.Write(ex, "KLM: Failed at GetEntityFromAPI:" + entityId, null);
            }

            return null;
        }
    }
}
