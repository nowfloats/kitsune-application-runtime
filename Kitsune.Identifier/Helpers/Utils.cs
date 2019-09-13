using Kitsune.Identifier.DataHandlers.Mongo;
using Kitsune.Identifier.Models;
using Kitsune.Models.Project;
using KitsuneLayoutManager.Helper.MongoConnector;
using KitsuneLayoutManager.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Kitsune.Identifier.Helpers.AWSHelper;

namespace Kitsune.Identifier.Helpers
{
	public static class Utils
	{
		internal static HashSet<string> BotList = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
		{
			"DatabaseDriverMysql","alphabot","semrushbot","mauibot","googlebot","googlebot-mobile","googlebot-image","googlebot-news","googlebot-video","adsbot-google","mediapartners","apis-google","bingbot","slurp","java","wget","curl","commons-httpclient","python-urllib","libwww","httpunit","nutch","go-http-client","phpcrawl","msnbot","jyxobot","fast-webcrawler","fast enterprise crawler","biglotron","teoma","convera","seekbot","gigabot","gigablast","exabot","ia_archiver","gingercrawler","webmon ","httrack","grub.org","usinenouvellecrawler","antibot","netresearchserver","speedy","fluffy","bibnum.bnf","findlink","msrbot","panscient","yacybot","aisearchbot","ioi","ips-agent","tagoobot","mj12bot","woriobot","yanga","buzzbot","mlbot","yandexbot","yandex.com","purebot","linguee bot","cyberpatrol","voilabot","baiduspider","citeseerxbot","spbot","twengabot","postrank","turnitinbot","scribdbot","page2rss","sitebot","linkdex","adidxbot","blekkobot","ezooms","dotbot","mail.ru_bot","discobot","heritrix","findthatfile","europarchive.org","nerdbynature.bot","sistrix crawler","ahrefsbot","aboundex","domaincrawler","wbsearchbot","summify","ccbot","edisterbot","seznambot","ec2linkfinder","gslfbot","aihitbot","intelium_bot","facebookexternalhit","yeti","retrevopageanalyzer","lb-spider","sogou","lssbot","careerbot","wotbox","wocbot","ichiro","duckduckbot","wordchampbot","pingdom","docomo","catchbot","lssrocketcrawler","drupact","webcompanycrawler","acoonbot","openindexspider","gnam gnam spider","web-archive-net.com.bot","backlinkcrawler","coccoc","integromedb","content crawler spider","toplistbot","seokicks-robot","it2media-domain-crawler","ip-web-crawler.com","siteexplorer.info","elisabot","proximic","changedetection","blexbot","arabot","wesee:search","niki-bot","crystalsemanticsbot","rogerbot","360spider","psbot","interfaxscanbot","cc metadata scaper","g00g1e.net","grapeshotcrawler","urlappendbot","brainobot","fr-crawler","binlar","simplecrawler","twitterbot","cxensebot","smtbot","bnf.fr_bot","a6-indexer","admantx","facebot","orangebot","memorybot","advbot","megaindex","semanticscholarbot","ltx71","nerdybot","xovibot","bubing","qwantify","archive.org_bot","applebot","tweetmemebot","crawler4j","findxbot","yoozbot","lipperhey","y!j-asr","domain re-animator bot","addthis","screaming frog seo spider","metauri","scrapy","livelap[bb]ot","openhosebot","capsulechecker","collection@infegy.com","istellabot","deusu","betabot","cliqzbot","mojeekbot","netestate ne crawler","safesearch microdata crawler","gluten free crawler","sonic","sysomos","trove","deadlinkchecker","slack-imgproxy","embedly","rankactivelinkbot","iskanie","safednsbot","skypeuripreview","veoozbot","slackbot","redditbot","datagnionbot","google-adwords-instant","adbeat_bot","whatsapp","contxbot","pinterest","electricmonk","garlikcrawler","bingpreview","vebidoobot","femtosearchbot","yahoo link preview","metajobbot","domainstatsbot","mindupbot","daum","jugendschutzprogramm-crawler","xenu link sleuth","pcore-http","moatbot","kosmiobot","pingdom","phantomjs","gowikibot","piplbot","discordbot","telegrambot","infopath.2","jetslide","newsharecounts","james bot","barkrowler","tineye-bot","socialrankiobot","trendictionbot","ocarinabot","epicbot","primalbot","duckduckgo-favicons-bot","gnowitnewsbot","leikibot","linkarchiver","yak","paperlibot","digg deeper","dcrawl","snacktory","anderspinkbot","fyrebot","everyonesocialbot","mediatoolkitbot","luminator-robots","extlinksbot","surveybot","ning","okhttp","nuzzel","omgili","pocketparser","yisouspider","um-ln","toutiaospider","muckrack","jamie\'s spider","ahc","netcraftsurveyagent","laserlikebot","apache-httpclient","appengine-google","jetty","upflow","thinklab","traackr.com","twurly","mastodon","http_get","dnyzbot","botify","007ac9 crawler","behloolbot","brandverity","check_http","bdcbot","zumbot","ezid","icc-crawler","archivebot","lcc", "k-sitemap"
		};
		//public static void PrintLineNumber()
		//{
		//	int lineNumber = (new System.Diagnostics.StackFrame(0, true)).GetFileLineNumber();
		//	Console.WriteLine("LINE: " + lineNumber);
		//}

		public static void PrintLineNumber(int lineNumber)
		{
			Console.WriteLine("LINE: " + lineNumber);
		}
		internal static bool IsBot(string userAgent)
		{
			try
			{
				return (BotList.Contains(userAgent));
			}
			catch { }

			return false;
		}

		public static byte[] ReadFully(this Stream input)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				input.CopyTo(ms);
				return ms.ToArray();
			}
		}

		private static string EmailAPI { get { return "https://api2.withfloats.com/Internal/v1/PushEmailToQueue/4C627432590419E9CF79252291B6A3AE25D7E2FF13347C6ACD1587CC93FC322"; } }
		private static string ClientId { get { return "4C627432590419E9CF79252291B6A3AE25D7E2FF13347C6ACD1587CC93FC322"; } }

		public static bool SendContactFormEmail(string domain, Dictionary<string, string> pair, string url)
		{
			try
			{
				//get the user mail id
				var userEmail = MongoHelper.GetCustomerEmailAsync(domain);

				//create the mail object
				string data = String.Empty;
				foreach (var key in pair)
				{
					data = data + key + ":" + pair + "<br/><br/>";
				}
				String subject = "New inquiry via website";
				String emailBody = "Hi, <br/> You have a received the following inquiry:<br/>" + data;

				var requestObj = new KitsuneConvertMailRequest
				{
					ClientId = ClientId,
					EmailBody = emailBody,
					Subject = subject,
					To = new List<string> { }
				};

				//send customer enquiry details to the user
				APIHelper.SendEmail(requestObj);

				return true;

			}
			catch (Exception ex)
			{
				// EventLogger.Write(ex, "Error in Submitform",requestUrl:url);
			}
			return false;
		}

		internal static async Task<KitsuneDomainDetails> GetDomainDetailsAsync(string domain, KitsuneRequestUrlType kitsuneRequestUrlType = KitsuneRequestUrlType.PRODUCTION)
		{
			if (String.IsNullOrEmpty(domain) || domain.Trim().Length == 0)
				return null;

			try
			{
				if (kitsuneRequestUrlType == KitsuneRequestUrlType.PRODUCTION)
					domain = domain.Trim().ToUpper();

				var domainDetails = await MongoHelper.GetCustomerDetailsFromDomainAsync(domain, kitsuneRequestUrlType);
				if (domainDetails == null)
					throw new Exception($"'{domain}' Domain not found");

				return domainDetails;
			}
			catch (Exception ex)
			{
				return null;
			}
		}

		public static bool IsStaticFile(string filename)
		{
			try
			{
				filename = filename.ToLower();
				if (filename.EndsWith(".js") || filename.EndsWith(".css") || filename.EndsWith(".jpeg") || filename.EndsWith(".jpg") || filename.EndsWith(".png") || filename.EndsWith(".svg")
					|| filename.EndsWith(".ico") || filename.EndsWith(".ttf") || filename.EndsWith(".otf") || filename.EndsWith(".woff"))
				{
					return true;
				}
			}
			catch { }
			return false;
		}

		public static Match GetRegexValue(string input, string regex)
		{
			try
			{
				if (!String.IsNullOrEmpty(regex))
				{
					var regexstring = regex;
					var inputstring = input.Replace(" ", "");
					return Regex.Match(inputstring, regexstring, RegexOptions.IgnoreCase);
				}
			}
			catch (Exception ex)
			{

			}

			return null;
		}

		internal static async Task<ResourceDetails> GetResourceDetailsAsync(string projectid, string encodedUrlPath, string originalUrlAbsolutePath, KitsuneRequestUrlType kitsuneRequestUrlType = KitsuneRequestUrlType.PRODUCTION, bool isDefaultView = false)
		{
			var kitsuneResourceDetails = new KitsuneResource();
			try
			{
				if (!Utils.IsStaticFile(encodedUrlPath))
				{
					//CALL THE ROUTING API TO FETCH THE DETAILS
					var result = APIHelper.GetRoutingObject(projectid, encodedUrlPath, kitsuneRequestUrlType, createIfNotExists: true);

					if (result != null)
					{
						if (!string.IsNullOrEmpty(result.RedirectPath))
						{
							//Redirect
							return new ResourceDetails { IsRedirect = true, RedirectPath = result.RedirectPath };
						}

						if (!String.IsNullOrEmpty(result.ResourceId))
						{
							//Get Resource Details
							var resourceDetails = await MongoHelper.GetProjectResourceAsync(projectid, result.ResourceId, kitsuneRequestUrlType);
							return new ResourceDetails { OptimizedPath = resourceDetails.OptimizedPath, UrlPattern = resourceDetails.UrlPattern, isStatic = resourceDetails.IsStatic, SourcePath = resourceDetails.SourcePath, PageType = resourceDetails.PageType, UrlPatternRegex = resourceDetails.UrlPatternRegex };
						}
						else if (result.StatusCode > 400 && result.StatusCode < 500)
						{
							return new ResourceDetails { StatusCode = result.StatusCode };
						}
					}
				}

				kitsuneResourceDetails = await MongoHelper.GetProjectResourceDetailsAsync(projectid, originalUrlAbsolutePath, isDefaultView, kitsuneRequestUrlType);
				if (kitsuneResourceDetails != null)
				{
					return new ResourceDetails
					{
						OptimizedPath = kitsuneResourceDetails.OptimizedPath,
						UrlPattern = kitsuneResourceDetails.UrlPattern,
						isStatic = kitsuneResourceDetails.IsStatic,
						SourcePath = kitsuneResourceDetails.SourcePath,
						PageType = kitsuneResourceDetails.PageType,
						UrlPatternRegex = kitsuneResourceDetails.UrlPatternRegex
					};
				}
			}
			catch (Exception ex)
			{
				try
				{
					kitsuneResourceDetails = await MongoHelper.GetProjectResourceDetailsAsync(projectid, originalUrlAbsolutePath, isDefaultView, kitsuneRequestUrlType);
					if (kitsuneResourceDetails != null)
					{
						return new ResourceDetails
						{
							OptimizedPath = kitsuneResourceDetails.OptimizedPath,
							UrlPattern = kitsuneResourceDetails.UrlPattern,
							isStatic = kitsuneResourceDetails.IsStatic,
							SourcePath = kitsuneResourceDetails.SourcePath,
							PageType = kitsuneResourceDetails.PageType,
							UrlPatternRegex = kitsuneResourceDetails.UrlPatternRegex
						};
					}
				}
				catch { }
			}
			return null;
		}

	}
}
