using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KitsuneLayoutManager.Models
{
    public class KPayCheckSumAPIResponse
    {
        public string pepper { get; set; }
        public List<string> amounts { get; set; }
        public List<string> checksums { get; set; }
    }
}
