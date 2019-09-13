using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kitsune.Models;
using Kitsune.Models.Theme;
using KitsuneLayoutManager.Helper.MongoConnector;

namespace KitsuneLayoutManager.Helper.Theme_Picker
{
    public class ThemePicker
    {

        internal static async Task<string> GetHtmlForFPCodeAsync(KLMAuditLogModel auditLog, ulong fpCode, string fpTag, string fpId, string visitorCity, string visitorCountry, string view = "HOME", string category = null)
        {
            try
            {
                return await GetBestFitThemeCodeWithOutSparkAsync(auditLog, fpTag, fpId, fpCode, view, category, visitorCity, visitorCountry);
            }
            catch (Exception ex)
            {

            }

            return null;
        }

        internal static async Task<string> GetBestFitThemeCodeWithOutSparkAsync(KLMAuditLogModel auditLog, string fpTag, string fpId, ulong fpCode, string view, string category, string visitorCity, string visitorCountry)
        {
            try
            {
                var themes = await MongoHelper.GetAllThemesAsync(category);
                if (themes != null && themes.Any())
                {
                    var finalTheme = themes.Find(x => x.ThemeId == GetBestTheme(themes, fpCode, fpId, visitorCity, visitorCountry, category));
                    var htmlString = MongoHelper.GetHtmlForViewFromTheme(finalTheme.ThemeId, view);
                    auditLog.themeId = finalTheme.ThemeId;
                    auditLog.themeCode = finalTheme.ThemeCode;

                    ThreadPool.QueueUserWorkItem(delegate (object state)
                    {
                        try
                        {
                            MongoHelper.UpdateThemeDetails(auditLog._id, finalTheme.ThemeId, finalTheme.ThemeCode);
                        }
                        catch { }
                    });

                    return htmlString;
                }
            }
            catch (Exception ex)
            {
                MongoHelper.saveErrorLog(fpTag, fpCode.ToString(), null, String.Format("ThemePicker.GetBestFitThemeCodeWithOutSpark :: {0}", ex.ToString()));
            }
            return null;
        }

        class ThemeCode
        {
            public string ThemeId { get; set; }
            public ulong ThemeOriginalCode { get; set; }
            public ulong ThemeAndCode { get; set; }
            public int OriginalOnes { get; set; }
            public int AndOnes { get; set; }
        }


        public static string GetBestTheme(List<ProductionThemeModel> Themes, ulong FpCode, string fpId, string visitorCity, string visitorCountry, string categoryId)
        {
            List<ThemeCode> ThemeList = new List<ThemeCode>();

            //Creates List of ThemeCode which contains "and" operation of themeCode and fpCode.It also store the number of one in themeCode and "and"operated code.
            foreach (var Theme in Themes)
            {
                var AndCode = FpCode & Theme.ThemeCode;
                var Ones = NumberOfOneInBinaryFormat(Theme.ThemeCode);
                ThemeList.Add(new ThemeCode
                {
                    ThemeId = Theme.ThemeId,
                    ThemeOriginalCode = Theme.ThemeCode,
                    ThemeAndCode = AndCode,
                    OriginalOnes = Ones,
                    AndOnes = NumberOfOneInBinaryFormat(AndCode)
                });
            }

            //select the themes with maximum number of one in the "and" operated code
            List<ThemeCode> ThemeWithMaxAndOnes = new List<ThemeCode>();
            int max = ThemeList.Max(x => x.AndOnes);
            ThemeWithMaxAndOnes = ThemeList.FindAll(x => x.AndOnes == max);

            //select the themes from the ThemeWithMaxAndOnes which have mininum number of one in the original code 
            List<ThemeCode> ThemeWithMinOnes = new List<ThemeCode>();
            int min = ThemeWithMaxAndOnes.Min(x => x.OriginalOnes);
            ThemeWithMinOnes = ThemeWithMaxAndOnes.FindAll(x => x.OriginalOnes == min);

            //select any random theme from the ThemeWithMinOnes
            //var final = ThemeWithMinOnes.PickRandom();
            //return final.ThemeId;

            if (ThemeWithMinOnes.Count == 1)
                return ThemeWithMinOnes.First().ThemeId;

            #region Theme Selection Logic based on historic performance
            var final = MongoHelper.GetThemePerformanceStats(ThemeWithMinOnes.Select(x => x.ThemeId), fpId, visitorCity, categoryId, visitorCountry);
            if (final == null || final.Count <= 0)
                return ThemeWithMinOnes.PickRandom().ThemeId;
            else
                return final.OrderByDescending(x => x.TotalActions).OrderByDescending(x => x.TotalTimeSpent).OrderBy(x => x.BounceRate).First().ThemeId;
            #endregion
        }

        //internal static string FindBestPerformingTheme(string fpId, string city, IEnumerable<string> themeIds, string country, string categoryId)
        //{
        //    var perfThemes = 
        //    if()
        //}

        internal static int NumberOfOneInBinaryFormat(ulong Code)
        {
            int Count = 0;
            while (Code != 0)
            {
                Count = Code % 2 == 0 ? Count : Count + 1;
                Code = Code / 2;
            }
            return Count;
        }


    }

    public static class EnumerableExtension
    {
        public static T PickRandom<T>(this IEnumerable<T> source)
        {
            return source.PickRandom(1).Single();
        }

        public static IEnumerable<T> PickRandom<T>(this IEnumerable<T> source, int count)
        {
            return source.Shuffle().Take(count);
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            return source.OrderBy(x => Guid.NewGuid());
        }
    }

}
