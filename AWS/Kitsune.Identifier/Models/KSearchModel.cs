using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kitsune.Identifier.Models
{
    public class KSearchModel
    {
        public string FaviconUrl { get; set; }
        public List<SearchObject> SearchObjects { get; set; }
    }

    public class SearchObject
    {
        public string S3Url { get; set; }
        public List<string> Keywords { get; set; }
        public double Count { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }

    public class DynamicSearchObject
    {
        public string _kid { get; set; }
        public string Text { get; set; }
        public string Url { get; set; }
        public List<string> Keywords { get; set; }
    }

    public class Extra
    {
        public int CurrentIndex { get; set; }
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
    }

    public class KDynamicSearch
    {
        public List<DynamicSearchObject> Data { get; set; }
        public Extra Extra { get; set; }
    }
}
