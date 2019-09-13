using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KitsuneLayoutManager.Models
{
    internal class ThemePerformanceModel
    {
        public int TotalActions { get; set; }
        public double TotalTimeSpent { get; set; }
        public double BounceRate { get; set; }
        public string ThemeId { get; set; }
    }
}
