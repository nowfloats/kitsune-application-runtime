using Kitsune.Models;
using Kitsune.Server.Model.Kitsune;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KitsuneLayoutManager.Models
{
    public class PaginationModel
    {
        public string currentPageNumber { get; set; }
        public string nextPageLink { get; set; }
        public string prevPageLink { get; set; }
    }

    public class PageModel
    {
        public string currentpagenumber;
        public string nextpage;
        public string prevpage;
        public string url;
    }

    public class Pagination
    {
        public string currentpagenumber;
        public string searchtext;
        public string totalpagescount;
        public int pagesize;
        public Kitsune.Server.Model.Kitsune.Link nextpage;
        public Kitsune.Server.Model.Kitsune.Link prevpage;
    }

    #region WithFloats CustomWidget Object

    public class FloatingPointProductCustomLocalWidget
    {
        public string key;
        public string html;

        //SCRIPT or UI Widget
        public string type;
    }

    #endregion
}
