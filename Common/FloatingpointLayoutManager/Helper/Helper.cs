using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kitsune.Server.Model;
using System.Net;
using System.IO;
using Kitsune.Server.Model.Kitsune;
using Kitsune.Language.Models;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace KitsuneLayoutManager.Helper
{
    public class Helper
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

        public static string GetQueryStringForDL(string dynamicLink)
        {
            try
            {
                if (!String.IsNullOrEmpty(dynamicLink))
                {
                    var paramArray = dynamicLink.Split('/');
                    if (paramArray.Count() > 0)
                    {
                        var paramList = paramArray.ToList();

                        if (!String.IsNullOrEmpty(paramList.Find(x => x.Contains("_kid"))))
                        {
                            return "_kid";
                        }
                        else if (!String.IsNullOrEmpty(paramList.Find(x => (String.Compare(x, "[[id]]", true) == 0))))
                        {
                            return "id";
                        }
                        else if (!String.IsNullOrEmpty(paramList.Find(x => (x.Contains("_id")))))
                        {
                            return "_id";
                        }
                        else if (!String.IsNullOrEmpty(paramList.Find(x => (x.Contains(".id")))))
                        {
                            return "id";
                        }
                        else if (!String.IsNullOrEmpty(paramList.Find(x => x.Contains("index"))))
                        {
                            return "index";
                        }
                        else if (paramArray[paramArray.Length -1].Split('.').Length > 1)
                        {
                            return paramArray[paramArray.Length - 1].Split('.')[1];
                        }
                    }
                }
            }
            catch(Exception ex)
            {

            }

            return null;
        }

        public static string TrimDelimiters(string delimitedExpression)
        {
            string expression = delimitedExpression.Replace("[[", "").Replace("]]", "");
            return expression;
        }

        public static void UpdateFunctionLog(Dictionary<string, long> functionLog, string name, long elapsedMilliSeconds)
        {
            if (functionLog == null)
            {
                return;
            }
            long millis = 0;
            if (functionLog.ContainsKey(name))
            {
                millis = functionLog[name];
                functionLog.Remove(name);
            }
            millis += elapsedMilliSeconds;
            functionLog.Add(name, millis);
        }

        internal static void PostProcessReplacementString(ref string replaceString)
        {
            replaceString = replaceString.Replace("\r\n", "<br/>").Replace("\n", "<br/>");
        }

        public static List<PropertyPathSegment> ExtractPropertiesFromPath(string propertyPath, KEntity entity)
        {
            List<PropertyPathSegment> kProperties = new List<PropertyPathSegment>();

            var objectPathArray = propertyPath.ToLower().Split('.');
            var obClass = new KClass();
            var obProperty = new KProperty();
            var dataTypeClasses = new string[] { "str", "date", "number", "boolean", "kstring" };
            var currentProperty = string.Empty;
            var arrayRegex = new System.Text.RegularExpressions.Regex(@".*\[(\d+)\]", System.Text.RegularExpressions.RegexOptions.Compiled);
            var functionRegex = new System.Text.RegularExpressions.Regex(@"(\w+)\((.*)\)", System.Text.RegularExpressions.RegexOptions.Compiled);
            int? arrayIndex = 0;
            int tempIndex = 0;

            System.Text.RegularExpressions.Match arrayMatch = null;
            System.Text.RegularExpressions.Match functionMatch = null;
            for (var i = 0; i<objectPathArray.Length; i++)
            {
                currentProperty = objectPathArray[i];
                arrayMatch = arrayRegex.Match(currentProperty);
                arrayIndex = null;
                if (arrayMatch != null && arrayMatch.Success)
                {
                    if (int.TryParse(arrayMatch.Groups[1].Value, out tempIndex))
                    {
                        arrayIndex = tempIndex;
                    }
                    currentProperty = currentProperty.Substring(0, currentProperty.IndexOf('['));
                }

                if (i == 0)
                {
                    obClass = entity.Classes.FirstOrDefault(x => x.ClassType == KClassType.BaseClass && x.Name.ToLower() == currentProperty);
                    if (obClass != null)
                        kProperties.Add(new PropertyPathSegment { PropertyDataType = obClass.Name.ToLower(), PropertyName = currentProperty, Type = PropertyType.obj });
                }
                else
                {
                    obProperty = obClass.PropertyList.FirstOrDefault(x => x.Name.ToLower() == currentProperty);
                    if (obProperty != null)
                    {
                        if ((obProperty.Type == PropertyType.array && !dataTypeClasses.Contains(obProperty.DataType?.Name?.ToLower())) || obProperty.Type == PropertyType.obj || obProperty.Type == PropertyType.kstring || obProperty.Type == PropertyType.phonenumber)
                        {
                            kProperties.Add(new PropertyPathSegment
                            {
                                PropertyName = obProperty.Name.ToLower(),
                                PropertyDataType = obProperty.DataType.Name.ToLower(),
                                Index = arrayIndex,
                                Type = obProperty.Type
                            });

                            obClass = entity.Classes.FirstOrDefault(x => x.Name?.ToLower() == obProperty.DataType?.Name?.ToLower());
                        }
                        else
                        {
                            kProperties.Add(new PropertyPathSegment
                            {
                                PropertyName = obProperty.Name.ToLower(),
                                PropertyDataType = obProperty.DataType.Name.ToLower(),
                                Index = arrayIndex,
                                Type = obProperty.Type
                            });
                        }
                    }
                    else
                    {
                        functionMatch = functionRegex.Match(currentProperty);
                        if (functionMatch.Success)
                        {
                            kProperties.Add(new PropertyPathSegment
                            {
                                PropertyName = functionMatch.Groups[1].Value,
                                PropertyDataType = "function",
                                Type = PropertyType.function
                            });
                        }
                    }

                }
            }
            return kProperties;
        }

        public static bool IsStaticFile(string filename)
        {
            try
            {
                filename = filename.ToLower();
                if (filename.EndsWith(".js") || filename.EndsWith(".css") || filename.EndsWith(".jpeg") || filename.EndsWith(".jpg") || filename.EndsWith(".png") || filename.EndsWith(".svg")
                    || filename.EndsWith(".ico") || filename.EndsWith(".ttf") || filename.EndsWith(".otf") || filename.EndsWith(".woff"))
                {
                    return true;
                }
            }
            catch { }
            return false;
        }

        public static Match GetRegexValue(string input, string regex)
        {
            try
            {
                if (!String.IsNullOrEmpty(regex))
                {
                    var regexstring = regex;
                    var inputstring = input.Replace(" ", "");
                    return Regex.Match(inputstring, regexstring, RegexOptions.IgnoreCase);
                }
            }
            catch (Exception ex)
            {

            }

            return null;
        }

    }
}
