using System;
using System.Collections.Generic;
using System.Linq;
using KitsuneLayoutManager.Models.Ria;
using KitsuneLayoutManager.Models;

namespace KitsuneLayoutManager.Helper.SqlConnector
{
    public static class KWebLogConnector
    {
        //private static MySqlConnection GetConnection()
        //{
        //    var conn = new MySqlConnection(KLM_Constants.FPWebLogConnectionString);
        //    conn.Open();
        //    return conn;
        //}

        #region RIA

        public static TopReferrersItem[] GetTopReferrers(string websiteId, DateTime startDate, DateTime endDate, int count = 10)
        {
            //using (var conn = GetConnection())
            //{
            //    var items = new List<TopReferrersItem>();
            //    using (MySqlCommand command = new MySqlCommand("call KitsuneWebLog.sp_TopReferrers(@websiteId, @start, @end, @count)", conn))
            //    {
            //        command.Parameters.AddWithValue("@websiteId", websiteId);
            //        command.Parameters.AddWithValue("@start", startDate.ToString("yyyy-MM-dd"));
            //        command.Parameters.AddWithValue("@end", endDate.ToString("yyyy-MM-dd"));
            //        command.Parameters.AddWithValue("@count", count);

            //        var reader = command.ExecuteReader();
            //        while (reader.Read())
            //        {
            //            items.Add(new TopReferrersItem
            //            {
            //                source_website = reader.GetString("referrer"),
            //                visits_count = reader.GetInt64("visits")
            //            });
            //        }
            //    }

            //    return items.ToArray();
            //}

            return null;
        }

        public static MostVisitedPagesItem[] GetMostVisitedPages(string websiteId, DateTime startDate, DateTime endDate, int count = 10)
        {
            //using (var conn = GetConnection())
            //{
            //    var items = new List<MostVisitedPagesItem>();

            //    using (MySqlCommand command = new MySqlCommand("call KitsuneWebLog.sp_MostVisitedPages(@websiteId, @start, @end, @count)", conn))
            //    {
            //        command.Parameters.AddWithValue("@websiteId", websiteId);
            //        command.Parameters.AddWithValue("@start", startDate.ToString("yyyy-MM-dd"));
            //        command.Parameters.AddWithValue("@end", endDate.ToString("yyyy-MM-dd"));
            //        command.Parameters.AddWithValue("@count", count);

            //        var reader = command.ExecuteReader();
            //        while (reader.Read())
            //        {
            //            items.Add(new MostVisitedPagesItem
            //            {
            //                page = reader.GetString("page"),
            //                visits_count = reader.GetInt64("visits")
            //            });
            //        }
            //    }

            //    return items.ToArray();
            //}
            return null;
        }

        public static VisitsVisitors GetVisitsVisitors(string websiteId, DateTime startDate, DateTime endDate)
        {
            //using (var conn = GetConnection())
            //{
            //    var items = new List<VisitsVisitors>();

            //    using (MySqlCommand command = new MySqlCommand("call KitsuneWebLog.sp_VisitsAndVisitorsForCustomer(@websiteId, @start, @end)", conn))
            //    {
            //        command.Parameters.AddWithValue("@websiteId", websiteId);
            //        command.Parameters.AddWithValue("@start", startDate.ToString("yyyy-MM-dd"));
            //        command.Parameters.AddWithValue("@end", endDate.ToString("yyyy-MM-dd"));

            //        var reader = command.ExecuteReader();
            //        while (reader.Read())
            //        {
            //            items.Add(new VisitsVisitors
            //            {
            //                Visitors = reader.GetInt64("visitors"),
            //                Visits = reader.GetInt64("visits")
            //            });
            //        }
            //    }

            //    return items.FirstOrDefault();
            //}
            return null;
        }

        #endregion
    }
}