using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Kitsune.Language.Models;
using Kitsune.Server.Model;
using System.Reflection;
using System.ComponentModel;
using static Kitsune.Language.Models.KEntity;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json.Linq;

namespace KitsuneLayoutManager.Helper.WidgetHandler
{

    public class WidgetHandler
    {


    }

    public static class Extensions
    {
        public static string ToEPOCTimeStamp(this DateTime datetime)
        {
            var time = (int)(datetime - new DateTime(1970, 1, 1)).TotalSeconds;
            return time.ToString();
        }
    }
}

