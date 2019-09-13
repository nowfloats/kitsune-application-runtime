using Kitsune.Models.Project;
using KitsuneLayoutManager.Helper.MongoConnector;
using System;
using System.Linq;
using KitsuneLayoutManager.Models.Ria;
using KitsuneLayoutManager.Helper.SqlConnector;
using System.Threading.Tasks;

namespace KitsuneLayoutManager.Helper
{
	public static class RiaHelper
	{
		public static dynamic GetRIAAppData(ProductionKitsuneProject project, string websiteId, RiaArgsModel riaArgs, string rootaliasuri = null)
		{
			try
			{
				var website = MongoHelper.GetWebsiteDetailsById(websiteId);

				var dateRange = GetDateRange(riaArgs.PerfReportPeriod, SelectionWindow.Current);
				var dateRangePrev = GetDateRange(riaArgs.PerfReportPeriod, SelectionWindow.Prev);

				var ria = new RiaSchema()
				{
					website_performance = new PeriodicPerformanceStats
					{
						period = riaArgs.PerfReportPeriod,
						current_window = new PerformanceStats
						{
							date_range = new DateRangeDisplay
							{
								start_date = dateRange.StartDate.ToString("dd-MMM-yyyy"),
								end_date = dateRange.EndDate.ToString("dd-MMM-yyyy")
							}
						},
						previous_window = new PerformanceStats
						{
							date_range = new DateRangeDisplay
							{
								start_date = dateRangePrev.StartDate.ToString("dd-MMM-yyyy"),
								end_date = dateRangePrev.EndDate.ToString("dd-MMM-yyyy")
							},
						},
					},
					website_domain = website.WebsiteUrl?.ToLower(),
					project_id = website.ProjectId,
					notification_type = riaArgs.NotifType,
					recipient_email = website.WebsiteOwner.Contact.Email,
					website_user_id = website.WebsiteOwner.UserId,
					unsubscribe_url = $"{rootaliasuri}/k-unsubscribe/5ab5190ba35c3b04e9817cb5?websiteUserId={website.WebsiteOwner.UserId}",
					rootaliasurl = new rootaliasurl() { url = rootaliasuri }
				};

				Parallel.Invoke(
				() =>
				{
					var visitsVisitors = KWebLogConnector.GetVisitsVisitors(websiteId, dateRange.StartDate, dateRange.EndDate);
					ria.website_performance.current_window.visits_count = visitsVisitors.Visits;
					ria.website_performance.current_window.visitors_count = visitsVisitors.Visitors;
				},
				() =>
				{
					var visitsVisitorsPrev = KWebLogConnector.GetVisitsVisitors(websiteId, dateRangePrev.StartDate, dateRangePrev.EndDate);
					ria.website_performance.previous_window.visits_count = visitsVisitorsPrev.Visits;
					ria.website_performance.previous_window.visitors_count = visitsVisitorsPrev.Visitors;
				},
				() =>
				{
					ria.website_performance.current_window.top_referrers = KWebLogConnector.GetTopReferrers(websiteId, dateRange.StartDate, dateRange.EndDate, 5);
					ria.website_performance.current_window.total_visits_from_top_referrers = ria.website_performance.current_window.top_referrers.Sum(x => x.visits_count);
				},
				() =>
				{
					ria.website_performance.previous_window.top_referrers = KWebLogConnector.GetTopReferrers(websiteId, dateRangePrev.StartDate, dateRangePrev.EndDate, 5);
					ria.website_performance.previous_window.total_visits_from_top_referrers = ria.website_performance.previous_window.top_referrers.Sum(x => x.visits_count);
				},
				() =>
				{
					ria.website_performance.current_window.most_visited_pages = KWebLogConnector.GetMostVisitedPages(websiteId, dateRange.StartDate, dateRange.EndDate, 5);
					ria.website_performance.current_window.total_visits_from_most_visited_pages = ria.website_performance.current_window.most_visited_pages.Sum(x => x.visits_count);
				},
				() =>
				{
					ria.website_performance.previous_window.most_visited_pages = KWebLogConnector.GetMostVisitedPages(websiteId, dateRangePrev.StartDate, dateRangePrev.EndDate, 5);
					ria.website_performance.previous_window.total_visits_from_most_visited_pages = ria.website_performance.previous_window.most_visited_pages.Sum(x => x.visits_count);
				});


				return ria;
			}
			catch (Exception ex)
			{
				ConsoleLogger.Write(ex.ToString());
				//TODO: Log Ex
			}
			return null;
		}

		private static DateRange GetDateRange(int period, SelectionWindow selectionWindow)
		{
			switch (selectionWindow)
			{
				case SelectionWindow.Prev:
					{
						var startDate = DateTime.Today.Subtract(TimeSpan.FromDays(period * 2)).Subtract(TimeSpan.FromDays(1));
						var endDate = DateTime.Today.Subtract(TimeSpan.FromDays(period)).Subtract(TimeSpan.FromDays(1));
						return new DateRange
						{
							StartDate = startDate,
							EndDate = endDate
						};
					}
				case SelectionWindow.Current:
					{
						var startDate = DateTime.Today.Subtract(TimeSpan.FromDays(period)).Subtract(TimeSpan.FromDays(1));
						var endDate = DateTime.Today.Subtract(TimeSpan.FromDays(1));
						return new DateRange
						{
							StartDate = startDate,
							EndDate = endDate
						};
					}
				default:
					return null;
			}
		}
	}
}
