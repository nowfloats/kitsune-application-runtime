using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KitsuneLayoutManager.Models
{
	public class KLMConfigurations
	{
		public CacheModel WebCache { get; set; }
		public CacheModel APICache { get; set; }
		public CacheModel ExpressionCache { get; set; }

		public MongoDBConfigurations MongoDBConfigurations { get; set; }

		public string KitsunePaymentDomain { get; set; }
		public string KitsuneApiDomain { get; set; }
		public string KitsuneOldApiDomain { get; set; }
		public string NowfloatsAPIDomain { get; set; }
		public string FPWebLogConnectionString { get; set; }
		public bool Base64Response { get; set; }
		public int CacheTimeout { get; set; }
		public bool DisableGZipAndDisableBase64Response { get; set; }
	}

	#region Mongo Models

	public class MongoDBConfigurations
	{
		public int MongoQueryMaxtimeOut { get; set; }
		public MongoConnectionConfig KitsuneCoreMongoServer { get; set; }
		public MongoConnectionConfig KitsuneSchemaMongoServer { get; set; }
		public MongoConnectionConfig NowfloatsMongoServer { get; set; }

		public MongoDBCollectionDetails CollectionDetails { get; set; }
	}

	public class MongoDBCollectionDetails
	{
		public string KitsuneWebsiteCollectionName { get; set; }
		public string KitsuneWebsiteUserCollectionName { get; set; }
		public string KitsuneDNSCollectionName { get; set; }
		public string KitsuneProjectProductionCollectionName { get; set; }
		public string KitsuneResourcesProductionCollectionName { get; set; }
		public string KitsuneProjectCollectionName { get; set; }
		public string KitsuneResourcesCollectionName { get; set; }
	}

	public class MongoConnectionConfig
	{
		public string ServerUrl { get; set; }
		public string DatabaseName { get; set; }
	}

	#endregion

	public class CacheModel
	{
		public string Url { get; set; }
		public bool IsEnabled { get; set; }
	}

}
