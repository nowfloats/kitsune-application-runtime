using KitsuneLayoutManager.Constant;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KitsuneLayoutManager.Models
{
	public class KLM_Constants
	{

		internal static readonly int[] IMAGE_RESOLUTIONS = { 100, 650, 850, 1130 };
		internal static readonly string K_IMG_FORMAT_STRING = "/k-img/{0}x/filters:no_upscale()/{1} {0}w,";

		#region MONGO DB SERVER DETAILS
		//Max query time out from settings
		public static int MongoQueryMaxtimeOut
		{
			get
			{
				if (KLMEnvironmentalConstants.KLMConfigurations.MongoDBConfigurations.MongoQueryMaxtimeOut <= 0)
				{
					return 2000; //Default
				}
				else
				{
					return KLMEnvironmentalConstants.KLMConfigurations.MongoDBConfigurations.MongoQueryMaxtimeOut;
				}
			}
		}
		//Kitsune
		public static string KitsuneMongoServerUrl { get { return KLMEnvironmentalConstants.KLMConfigurations.MongoDBConfigurations.KitsuneCoreMongoServer.ServerUrl; } }
		public static string KitsuneDatabaseName { get { return KLMEnvironmentalConstants.KLMConfigurations.MongoDBConfigurations.KitsuneCoreMongoServer.DatabaseName; } }

		//Kitsune Schema
		public static string KitsuneSchemaMongoServerUrl { get { return KLMEnvironmentalConstants.KLMConfigurations.MongoDBConfigurations.KitsuneSchemaMongoServer.ServerUrl; } }
		public static string KitsuneSchemaDatabaseName { get { return KLMEnvironmentalConstants.KLMConfigurations.MongoDBConfigurations.KitsuneSchemaMongoServer.DatabaseName; } }

		#endregion

		#region MONGO DB COLLECTIONS

		public static string KitsuneWebsiteCollection { get { return KLMEnvironmentalConstants.KLMConfigurations.MongoDBConfigurations.CollectionDetails.KitsuneWebsiteCollectionName; } }
		public static string KitsuneWebsiteUserCollection { get { return KLMEnvironmentalConstants.KLMConfigurations.MongoDBConfigurations.CollectionDetails.KitsuneWebsiteUserCollectionName; } }
		public static string KitsuneDNSCollection { get { return KLMEnvironmentalConstants.KLMConfigurations.MongoDBConfigurations.CollectionDetails.KitsuneDNSCollectionName; } }
		public static string KitsuneProjectProductionCollection { get { return KLMEnvironmentalConstants.KLMConfigurations.MongoDBConfigurations.CollectionDetails.KitsuneProjectProductionCollectionName; } }
		public static string KitsuneResourcesProductionCollection { get { return KLMEnvironmentalConstants.KLMConfigurations.MongoDBConfigurations.CollectionDetails.KitsuneResourcesProductionCollectionName; } }
		public static string KitsuneProjectCollection { get { return KLMEnvironmentalConstants.KLMConfigurations.MongoDBConfigurations.CollectionDetails.KitsuneProjectCollectionName; } }
		public static string KitsuneResourcesCollection { get { return KLMEnvironmentalConstants.KLMConfigurations.MongoDBConfigurations.CollectionDetails.KitsuneResourcesCollectionName; } }

		#endregion

		public static string FPWebLogConnectionString { get { return KLMEnvironmentalConstants.KLMConfigurations.FPWebLogConnectionString; } }

		public static bool Base64Response { get { return KLMEnvironmentalConstants.KLMConfigurations.Base64Response; } }
		public static bool DisableGZipAndDisableBase64Response { get { return KLMEnvironmentalConstants.KLMConfigurations.DisableGZipAndDisableBase64Response; } }
		public static int CacheTimeout { get { return KLMEnvironmentalConstants.KLMConfigurations.CacheTimeout; } }
	}
}
