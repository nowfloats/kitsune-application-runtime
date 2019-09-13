using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KitsuneLayoutManager.Models.Ria
{
	public class RiaArgsModel
	{
		[JsonProperty("perf_report_period")]
		public int PerfReportPeriod { get; set; }

		[JsonProperty("notif_type")]
		public string NotifType { get; set; }
	}

    public class VisitsVisitors
	{
		public long Visitors { get; set; }
		public long Visits { get; set; }
	}

	public enum SelectionWindow
	{
		Current, Prev
	}

	public class DateRange
	{
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
	}
}
