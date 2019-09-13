using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using Kitsune.Server.Model;
using KitsuneLayoutManager.Helper.Processor;
using Kitsune.Server.Model.Kitsune;
using Kitsune.Models;
using Newtonsoft.Json;
using System.Web;
using Kitsune.Server.Model;
using KitsuneLayoutManager.Helper.MongoConnector;
using System.Text.RegularExpressions;

namespace KitsuneLayoutManager.Helper
{
    public class DataHelper
    {
        private static List<string> DaysString = new List<string>() { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
        private static Regex HTTPRegex = new Regex(@"^(http:\/\/)|^(https:\/\/)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }
}
