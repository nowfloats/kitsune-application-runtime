using KitsuneLayoutManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kitsune.Identifier.Models
{
	public class IdentifierConfigurations
	{
		public KLMConfigurations KLMConfigurations { get; set; }
		public string KitsuneAPIDomain { get; set; }
		public string RIAAPIDomain { get; set; }
		public string KitsuneRoutingApiDomain { get; set; }

		public string KitsuneSubDomain { get; set; }
		public string KitsuneDemoSubDomain { get; set; }
		public string KitsunePreviewSubDomain { get; set; }
		public string RoutingCreateFunctionName { get; set; }
		public string RoutingMatcherFunctionName { get; set; }
		public string DefaultNFDomainRootAliasUri { get; set; }

		public MongoDBDetails KitsuneCoreMongoDetails { get; set; }
		public MongoDBDetails KitsuneSchemaMongoDetails { get; set; }
		public bool EnableMVC { get; set; }
		public CacheSettings DynamicResponse { get; set; }
		public CacheSettings StaticResponse { get; set; }
		public CacheSettings BotResponse { get; set; }
		public string ServerHeader { get; set; }
		public string XPoweredByHeader { get; set; }
		public FixedCacheDurationRange AllResponseFixedDurationCache { get; set; }
		public AwsCredentials RoutingLambdaCredentials { get; set; }
        public bool EnableCacheTagHeader { get; set; }
        public string CacheTagHeader { get; set; }
	}

	public class CacheSettings
	{
		public bool CacheEnabled { get; set; }
		public string CacheControlValue { get; set; }
		public int ExpiresInSecondsValue { get; set; }
	}

	public class FixedCacheDurationRange
	{
		public bool Enabled { get; set; }
		public int StartHour_IST { get; set; }
		public int EndHour_IST { get; set; }
	}

	public class AwsCredentials
	{
		public string AccessKey { get; set; }
		public string Secret { get; set; }
		public string Region { get; set; }
	}


	public class MongoDBDetails
	{
		public string ConnectionStringUrl { get; set; }
		public string DatabaseName { get; set; }
	}
}
