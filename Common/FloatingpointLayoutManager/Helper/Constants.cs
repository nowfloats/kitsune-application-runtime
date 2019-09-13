﻿using KitsuneLayoutManager.Constant;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KitsuneLayoutManager.Helper
{
    public class Constant
    {
        #region DOMAINS
        
        public static string KitsunePaymentDomain { get { return KLMEnvironmentalConstants.KLMConfigurations.KitsunePaymentDomain; } }
        public static string KitsuneApiDomain { get { return KLMEnvironmentalConstants.KLMConfigurations.KitsuneApiDomain; } }//"https://api2.kitsune.tools"
        public static string KitsuneOldApiDomain { get { return KLMEnvironmentalConstants.KLMConfigurations.KitsuneOldApiDomain; } }//"https://api2.kitsune.tools";

        #endregion


        #region K-PAY

        //0:domain
        public static string KPayEncodedCheckSumApi { get { return "{0}/api/v1/encode"; } }

        #endregion

        #region LANGUAGE

        //0:domain ,1:entity id
        public static string LanguageEntityApi { get { return "{0}/Language/v1/{1}/{2}"; } }

        #endregion

        #region perf log constants
        public static string GETTING_ENTITY = "GETTING ENTITY";
        public static string GETTING_HTTP_HEADER_INFO = "GETTING HTTP HEADER INFO";
        public static string GETTING_FP_DETAILS = "GET FP DETAILS FROM CACHE";
        public static string VIEW_VALIDATIONS = "VIEW VALIDATIONS";
        public static string GET_HTML_FROM_URL = "GET HTML FROM URL";
        public static string LOG_TO_KINESIS = "LOG USER TO KINESIS";
        public static string EVALUATE_EXPRESSION = "Evaluate expression: {0}";
        public static string KSCRIPT_API_RESPONSE = "Kscript api response: {0}";
        public static string MINIFICATION = "Minification";
        #endregion

        #region Regex constants
        public static Regex WidgetRegulerExpression = new Regex(@"\[\[+(.*?)\]\]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        #endregion
    }
}
