using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KitsuneLayoutManager.Models.Ria
{
	/// <summary>
	/// Properties in RiaSchema must follow snake_case naming convention since they are exposed to kitsune developers. 
	/// </summary>
	public class RiaSchema
	{
		public string website_domain { get; set; }
		public PeriodicPerformanceStats website_performance { get; set; }

		public string recipient_email { get; set; }
		public string project_id { get; set; }
		public string notification_type { get; set; }
		public string website_user_id { get; set; }
		public string unsubscribe_url { get; set; }

		public rootaliasurl rootaliasurl { get; set; }
	}

	public class PerformanceStats
	{
		public DateRangeDisplay date_range { get; set; }
		public long visits_count { get; set; }
		public long visitors_count { get; set; }
		public TopReferrersItem[] top_referrers { get; set; }
		public long total_visits_from_top_referrers { get; set; }
		public MostVisitedPagesItem[] most_visited_pages { get; set; }
		public long total_visits_from_most_visited_pages { get; set; }
	}

	public class PeriodicPerformanceStats
	{
		public int period { get; set; }
		public PerformanceStats current_window { get; set; }
		public PerformanceStats previous_window { get; set; }
	}

	public class rootaliasurl
	{
		public string url { get; set; }
	}

	public class TopReferrersItem
	{
		public string source_website { get; set; }
		public long visits_count { get; set; }
	}

	public class MostVisitedPagesItem
	{
		public string page { get; set; }
		public long visits_count { get; set; }
	}

	public class DateRangeDisplay
	{
		public string start_date { get; set; }
		public string end_date { get; set; }
	}
}
