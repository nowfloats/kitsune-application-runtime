using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.Configuration;
using Kitsune.Language.Models;
using Newtonsoft.Json.Serialization;
using KitsuneLayoutManager.Helper.CacheHandler;
using KitsuneLayoutManager.Models;
using System.Security.Cryptography;
using Murmur;
using KitsuneLayoutManager.Helper.MongoConnector;
using KitsuneLayoutManager.Constant;
using System.Threading.Tasks;
using MessagePack;
using MongoDB.Bson;

namespace KitsuneLayoutManager.Helper
{
	public class CacheHelper
	{
		//public static string CacheUrl = ConfigurationManager.AppSettings["KLM-Web-Cache"];
		private static string KLMApiCacheUrl = KLMEnvironmentalConstants.KLMConfigurations.APICache.Url;
		private static string KLMWebCacheUrl = KLMEnvironmentalConstants.KLMConfigurations.WebCache.Url;
		private static string KLMExpressionCacheUrl = KLMEnvironmentalConstants.KLMConfigurations.ExpressionCache.Url;

		public static bool isApiCacheEnabled = KLMEnvironmentalConstants.KLMConfigurations.APICache.IsEnabled;
		public static bool isWebCacheEnabled = KLMEnvironmentalConstants.KLMConfigurations.WebCache.IsEnabled;
		public static bool isExpressionCacheEnabled = KLMEnvironmentalConstants.KLMConfigurations.ExpressionCache.IsEnabled;

		private static RedisCacheService ApiCacheService, WebCacheService, ExpressionCacheService;
		private static TimeSpan _cacheTimeout = TimeSpan.FromMilliseconds(KLMEnvironmentalConstants.KLMConfigurations.CacheTimeout);

		static CacheHelper()
		{
			InitCacheConnection();

			InitPropertyCacheConnection();
		}

		private static void InitCacheConnection()
		{
			try
			{
				if (isApiCacheEnabled && (ApiCacheService == null || !ApiCacheService.IsConnectionActive()))
					ApiCacheService = new RedisCacheService(KLMApiCacheUrl, 6379);

				if (isWebCacheEnabled && (WebCacheService == null || !WebCacheService.IsConnectionActive()))
					WebCacheService = new RedisCacheService(KLMWebCacheUrl, 6379);
			}
			catch (Exception ex)
			{
				ConsoleLogger.Write($"ERROR: CACHE InitCacheConnection - {ex.ToString()}");
			}
		}

		private static void InitPropertyCacheConnection()
		{
			try
			{
				if (isExpressionCacheEnabled &&
					(ExpressionCacheService == null || !ExpressionCacheService.IsConnectionActive()))
					ExpressionCacheService = new RedisCacheService(KLMExpressionCacheUrl, 6379);
			}
			catch (Exception ex)
			{
				ConsoleLogger.Write($"ERROR: CACHE InitPropertyCacheConnection - {ex.ToString()}");
			}
		}

		internal static Dictionary<string, object> GetKeyValuePairsFromLanguage<T>(T language)
		{
			try
			{
				var reSerialize = JsonConvert.SerializeObject(language);
				return JsonConvert.DeserializeObject<Dictionary<string, object>>(reSerialize);
			}
			catch (Exception ex)
			{
				ConsoleLogger.Write($"ERROR: CACHE GetKeyValuePairsFromLanguage - {ex.ToString()}");
			}
			return null;
		}

		internal static T GetBusinessDetailsFromCache<T>(string fpTag, bool isCacheEnabled, string domainName, bool isNFSite, T businessClass, string schema = null, string developerId = null, string customerId = null, bool isEnterprise = false, string rootAliasUri = null)
		{
			try
			{
				fpTag = fpTag.ToUpper();

				if (isCacheEnabled && isApiCacheEnabled)
				{
					string cacheServiceResponse = string.Empty;
					try
					{
						try
						{
							var dnsHost = string.Empty;
							try
							{
								dnsHost = new Uri(rootAliasUri).DnsSafeHost?.ToUpper();
							}
							catch (Exception ex)
							{
								throw ex;
							}

							var cacheKey = HashUrl.GetHashAsString(fpTag);
							var cacheHashKey = HashUrl.GetHashAsString(dnsHost);

							var task = Task.Run(() => ApiCacheService.Get(GetMetaInfoCacheKey(cacheHashKey, cacheKey)));
							if (task.Wait(_cacheTimeout))
							{
								cacheServiceResponse = task.Result;
							}
							else
							{
								ConsoleLogger.Write($"ERROR: CACHE GetExpression - Redis TimeOut");
							}
						}
						catch (Exception ex)
						{
							ConsoleLogger.Write($"ERROR: Unable to GetBusinessDetailsFromCache() - Redis TimeOut");
						}

						if (!String.IsNullOrEmpty(cacheServiceResponse))
						{
							return JsonConvert.DeserializeObject<T>(JsonConvert.DeserializeObject(cacheServiceResponse).ToString());
						}
					}
					catch (Exception ex) { throw ex; }
				}

				var temp = ApiHelper.GetBusinessDataFromNewAPI(fpTag, schema, customerId, developerId);

				if (isCacheEnabled && isApiCacheEnabled)
				{
					try
					{
						UpdateBusinessDetailsCache(fpTag, temp, rootAliasUri);
					}
					catch (Exception ex)
					{
						throw ex;
					}
				}

				return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(temp));

			}
			catch (Exception ex)
			{
				ConsoleLogger.Write($"ERROR: CACHE GetBusinessDetailsFromCache - {ex.ToString()}");
			}

			return JsonConvert.DeserializeObject<T>(null);
		}

		internal static void UpdateBusinessDetailsCache<T>(string key, T fpCacheModel, string rootAliasUri)
		{
			try
			{
				if (!String.IsNullOrEmpty(key) && isApiCacheEnabled)
				{
					key = key.ToUpper();

					var cacheObject = JsonConvert.SerializeObject(fpCacheModel);

					var dnsHost = string.Empty;
					try
					{
						dnsHost = new Uri(rootAliasUri).DnsSafeHost?.ToUpper();
					}
					catch { }

					var cacheHashKey = HashUrl.GetHashAsString(dnsHost);
					var cacheKey = HashUrl.GetHashAsString(key);

					var task = ApiCacheService.Save(cacheHashKey, cacheKey, cacheObject);
					if (!task.Wait(_cacheTimeout))
					{
						ConsoleLogger.Write($"ERROR: CACHE UpdateBusinessDetailsCache - Redis TimeOut");
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLogger.Write($"ERROR: Unable to UpdateBusinessDetailsCache({key}) exception: {ex.ToString()}");
			}
		}

		internal static T GetBusinessDetailsWithMetaInfoFromCache<T>(string fpTag, string websiteId, bool isCacheEnabled, string schema, string projectId, string filePath, string currentPageNumber, string developerId, string rootAliasUri)
		{
			try
			{
				var serializerSettings = new JsonSerializerSettings();
				serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

				string dataObject = string.Empty;
				if (isCacheEnabled && isApiCacheEnabled)
				{
					try
					{
						var task = Task.Run(() => ApiCacheService.Get(GetBusinessDetailsWithMetaInfoCacheKey(fpTag, filePath)));
						if (task.Wait(_cacheTimeout))
						{
							var redisValue = task.Result;
							if (redisValue != null)
								dataObject = JsonConvert.SerializeObject(redisValue, serializerSettings);
						}
						else
						{
							ConsoleLogger.Write($"ERROR: CACHE GetBusinessDetailsWithMetaInfoFromCache - Redis TimeOut");
						}

					}
					catch (Exception ex)
					{
						ConsoleLogger.Write($"ERROR: Unable to GetBusinessDetailsWithMetaInfoFromCache() - Redis TimeOut");
					}
				}

				if (String.IsNullOrEmpty(dataObject))
				{
					dataObject = JsonConvert.SerializeObject(ApiHelper.GetBusinessDataWithMetaInfo(projectId, websiteId, schema, filePath, currentPageNumber, developerId), serializerSettings);

					if (isCacheEnabled && isApiCacheEnabled)
					{
						try
						{
							UpdateBusinessDetailsCache(GetBusinessDetailsWithMetaInfoCacheKey(fpTag, filePath), dataObject, rootAliasUri);
						}
						catch (Exception ex)
						{
							ConsoleLogger.Write($"ERROR: Unable to GetBusinessDetailsWithMetaInfoFromCache() - UpdateBusinessDetailsCache");
						}
					}
				}

				return JsonConvert.DeserializeObject<T>(dataObject);

			}
			catch (Exception ex)
			{
				ConsoleLogger.Write($"ERROR: Unable to GetBusinessDetailsWithMetaInfoFromCache({fpTag}) exception: {ex.ToString()}");
			}

			return JsonConvert.DeserializeObject<T>(null);
		}

		internal static T GetMetaInfoFromCache<T>(string fpTag, bool isCacheEnabled, string projectId, string sourcePath, string rootAliasUri, string developerId)
		{
			try
			{
				var serializerSettings = new JsonSerializerSettings();
				serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

				string dataObject = string.Empty;
				if (isCacheEnabled && isApiCacheEnabled)
				{
					try
					{
						var task = Task.Run(() => ApiCacheService.Get(GetMetaInfoCacheKey(fpTag, sourcePath)));
						if (task.Wait(_cacheTimeout))
						{
							var redisValue = task.Result;
							if (redisValue != null)
								dataObject = JsonConvert.SerializeObject(redisValue, serializerSettings);
						}
						else
						{
							ConsoleLogger.Write($"ERROR: CACHE GetMetaInfoFromCache - Redis TimeOut");
						}
					}
					catch (Exception ex)
					{
						ConsoleLogger.Write($"ERROR: Unable to GetMetaInfoFromCache() - Redis TimeOut");
					}
				}

				if (String.IsNullOrEmpty(dataObject))
				{
					dataObject = JsonConvert.SerializeObject(ApiHelper.GetMetaInfoFromAPI(projectId, sourcePath, developerId), serializerSettings);

					if (isCacheEnabled && isApiCacheEnabled)
					{
						try
						{
							UpdateBusinessDetailsCache(GetMetaInfoCacheKey(fpTag, sourcePath), dataObject, rootAliasUri);
						}
						catch { }
					}
				}

				return JsonConvert.DeserializeObject<T>(dataObject);

			}
			catch (Exception ex)
			{
				ConsoleLogger.Write($"ERROR: Unable to GetMetaInfoFromCache({fpTag}) exception: {ex.ToString()}");
			}

			return JsonConvert.DeserializeObject<T>(null);
		}

		private static string GetMetaInfoCacheKey(string fpTag, string sourcePath)
		{
			return "MI_" + fpTag + "_" + sourcePath;
		}

		private static string GetBusinessDetailsWithMetaInfoCacheKey(string fpTag, string filePath)
		{
			return fpTag + "_" + filePath;
		}

		internal static void UpdateThirdPartyAPI(string apiendpoint, string model, string rootAliasuri)
		{
			try
			{
				if (!String.IsNullOrEmpty(apiendpoint) && isApiCacheEnabled && !String.IsNullOrEmpty(rootAliasuri))
				{
					var dnsHost = string.Empty;
					try
					{
						dnsHost = new Uri(rootAliasuri).DnsSafeHost?.ToUpper();
					}
					catch { }

					apiendpoint = apiendpoint.ToUpper();
					var cacheKey = HashUrl.GetHashAsString(apiendpoint);
					var cacheHashKey = HashUrl.GetHashAsString(dnsHost);

					var task = ApiCacheService.Save(cacheHashKey, cacheKey, model);
					if (!task.Wait(_cacheTimeout))
					{
						ConsoleLogger.Write($"ERROR: CACHE UpdateThirdPartyAPI - Redis TimeOut");
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLogger.Write($"ERROR: Unable to UpdateThirdPartyAPI({apiendpoint}, {rootAliasuri}) exception: {ex.ToString()}");
			}
		}

		internal static string GetThirdPartyAPIResponseFromCache(string apiendpoint, bool isCacheEnabled, string rootAliasuri)
		{
			try
			{
				if (isCacheEnabled && isApiCacheEnabled)
				{
					var dnsHost = string.Empty;
					try
					{
						dnsHost = new Uri(rootAliasuri).DnsSafeHost?.ToUpper();
					}
					catch { }

					apiendpoint = apiendpoint.ToUpper();
					var cacheKey = HashUrl.GetHashAsString(apiendpoint);
					var cacheHashKey = HashUrl.GetHashAsString(dnsHost);

					var task = Task.Run(() => ApiCacheService.Get(cacheHashKey, cacheKey));
					if (task.Wait(_cacheTimeout))
					{
						var response = task.Result;
						if (!String.IsNullOrEmpty(response))
						{
							return response;
						}
					}
					else
					{
						ConsoleLogger.Write($"ERROR: CACHE GetThirdPartyAPIResponseFromCache - Redis TimeOut");
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLogger.Write($"ERROR: Unable to GetThirdPartyAPIResponseFromCache({apiendpoint}, {rootAliasuri}) exception: {ex.ToString()}");
			}

			return null;
		}

		internal static async Task<KEntity> GetEntityInfoAsync(string key, bool isCacheEnabled)
		{
			try
			{
				if (isCacheEnabled && isApiCacheEnabled)
				{
					key = key.ToUpper();

					var task = Task.Run(() => ApiCacheService.Get(key));
					if (task.Wait(_cacheTimeout))
					{
						var response = await task;
						if (!String.IsNullOrEmpty(response))
						{
							return JsonConvert.DeserializeObject<KEntity>(JsonConvert.DeserializeObject(response).ToString());
						}
					}
					else
					{
						ConsoleLogger.Write($"ERROR: CACHE GetEntityInfoAsync - Redis TimeOut");
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLogger.Write($"ERROR: Unable to GetEntityInfoAsync() exception: {ex.ToString()}");
			}

			return null;
		}

		internal static void SaveEntityInfo(string key, KEntity entityModel)
		{
			try
			{
				if (!String.IsNullOrEmpty(key) && isApiCacheEnabled)
				{
					key = key.ToUpper();

					var cacheObject = JsonConvert.SerializeObject(entityModel);

					var task = ApiCacheService.Save(key, cacheObject);
					if (!task.Wait(_cacheTimeout))
					{
						ConsoleLogger.Write($"ERROR: CACHE SaveEntityInfo - Redis TimeOut");
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLogger.Write($"ERROR: CACHE SaveEntityInfo - {ex.ToString()}");
			}
		}

		#region IDENTIFIER HELPERS FOR WEB-CACHE WITH URL

		public static WebCacheFileModel GetCacheEntityFromUrl(string url, out string cacheKey)
		{
			cacheKey = null;
			try
			{
				if (WebCacheService == null)
					InitCacheConnection();

				if (isWebCacheEnabled && !String.IsNullOrEmpty(url) && WebCacheService != null)
				{
					var dnsHost = string.Empty;
					var pathWithQuery = string.Empty;
					try
					{
						var uri = new Uri(url);
						dnsHost = uri.DnsSafeHost?.ToUpper();
						pathWithQuery = uri?.PathAndQuery;
					}
					catch (Exception ex)
					{
						ConsoleLogger.Write($"ERROR: CACHE GetCacheEntityFromUrl->>dnsHost {url} - {ex.ToString()}");
					}

					var cacheHashKey = HashUrl.GetHashAsString(dnsHost);
					var objectHashKey = HashUrl.GetHashAsString(pathWithQuery);
					cacheKey = $"{dnsHost}-{pathWithQuery}";
					var task = Task.Run(() => WebCacheService.GetRedisValue(cacheHashKey, objectHashKey));
					if (task.Wait(_cacheTimeout))
						return MessagePackSerializer.Deserialize<WebCacheFileModel>(task.Result);
					else
						ConsoleLogger.Write($"ERROR: CACHE GetCacheEntityFromUrl - Redis TimeOut");
				}
			}
			catch (Exception ex)
			{
				ConsoleLogger.Write($"ERROR: CACHE GetCacheEntityFromUrl {url} - {ex.ToString()}");
			}
			return null;
		}

		public static async Task<string> SaveCacheEntity(string url, string key, WebCacheFileModel cacheEntity, long ttl = -1)
		{
			try
			{
				if (WebCacheService == null)
					InitCacheConnection();

				if (isWebCacheEnabled && WebCacheService != null)
				{
					var dnsHost = string.Empty;
					try
					{
						dnsHost = new Uri(url).DnsSafeHost?.ToUpper();
					}
					catch { }

					var cacheHashKey = HashUrl.GetHashAsString(dnsHost);
					var cacheKey = HashUrl.GetHashAsString(key);
					var cacheValue = JsonConvert.SerializeObject(cacheEntity);

					var task = WebCacheService.Save(cacheHashKey, cacheKey, cacheValue);
					if (task.Wait(_cacheTimeout))
						return $"{cacheHashKey}-{cacheKey}";
					else
						ConsoleLogger.Write($"ERROR: CACHE SaveCacheEntity(url, key, entity, ttl) - Redis TimeOut");
				}
			}
			catch (Exception ex)
			{
				ConsoleLogger.Write($"ERROR: CACHE SaveCacheEntity(url, key, entity, ttl) - {ex.ToString()}");
			}

			return null;
		}

		public static async Task<string> SaveCacheEntity(string url, WebCacheFileModel cacheEntity, long ttl = -1)
		{
			try
			{
				if (WebCacheService == null)
					InitCacheConnection();

				if (isWebCacheEnabled && WebCacheService != null)
				{
					var dnsHost = string.Empty;
					var pathWithQuery = string.Empty;
					try
					{
						var uri = new Uri(url);
						dnsHost = uri.DnsSafeHost?.ToUpper();
						pathWithQuery = uri?.PathAndQuery;
					}
					catch { }

					var cacheHashKey = HashUrl.GetHashAsString(dnsHost);
					var cacheKey = HashUrl.GetHashAsString(pathWithQuery);
					var cacheValue = MessagePackSerializer.Serialize<WebCacheFileModel>(cacheEntity);

					var task = WebCacheService.Save(cacheHashKey, cacheKey, cacheValue);
					if (task.Wait(_cacheTimeout))
						return $"{dnsHost}-{pathWithQuery}";
					else
						ConsoleLogger.Write($"ERROR: CACHE SaveCacheEntity(url, entity, ttl) - Redis TimeOut");
				}
			}
			catch (Exception ex)
			{
				ConsoleLogger.Write($"ERROR: CACHE SaveCacheEntity(url, entity, ttl) - {ex.ToString()}");
			}

			return null;
		}

		public static void DeleteCacheEntityWithUrl(string url)
		{
			if (WebCacheService == null)
				InitCacheConnection();

			if (isWebCacheEnabled && WebCacheService != null)
			{
				var dnsHost = string.Empty;
				try
				{
					dnsHost = new Uri(url).DnsSafeHost?.ToUpper();
				}
				catch { }

				var cacheHashKey = HashUrl.GetHashAsString(dnsHost);

				var task = Task.Run(() => WebCacheService.RemoveAllKeysInHash(cacheHashKey));
				if (!task.Wait(_cacheTimeout))
					ConsoleLogger.Write($"ERROR: CACHE DeleteCacheEntityWithUrl - Redis TimeOut");
			}
		}

		public static void DeleteCacheEntity(string host)
		{
			try
			{
				if (WebCacheService == null)
					InitCacheConnection();

				if (WebCacheService != null)
				{
					var cacheHashKey = HashUrl.GetHashAsString(host.ToUpper());

					var task = Task.Run(() => WebCacheService.RemoveAllKeysInHash(cacheHashKey));
					if (!task.Wait(_cacheTimeout))
						ConsoleLogger.Write($"ERROR: CACHE DeleteCacheEntity - Redis TimeOut");
				}
			}
			catch (Exception ex)
			{
				ConsoleLogger.Write($"ERROR: CACHE DeleteCacheEntity - {ex.ToString()}");
			}
		}

		public class HashUrl
		{
			public static string GetHashAsString(string key)
			{
				try
				{
					if (String.IsNullOrEmpty(key))
						throw new Exception("Incorrect key");

					HashAlgorithm murmur128 = MurmurHash.Create128(0, true, AlgorithmPreference.X86);
					var hash = murmur128.ComputeHash(Encoding.ASCII.GetBytes(key));

					if (hash.Length < 16)
						throw new Exception("Hash Length must be more than 16 byte");

					var builder = new StringBuilder(16);
					for (int i = 0; i < hash.Length; i++)
						builder.Append(hash[i].ToString("x2"));

					return builder.ToString().ToUpper();
				}
				catch { throw; }
			}
		}

		#endregion

		#region EXPRESSION CACHE
		public static string SaveExpression(string key, dynamic value)
		{
			try
			{
				if (ExpressionCacheService == null || !ExpressionCacheService.IsConnectionActive())
					InitPropertyCacheConnection();

				if (isExpressionCacheEnabled)
				{
					var cacheKey = HashUrl.GetHashAsString(key);
					var cacheValue = JsonConvert.SerializeObject(value);

					var task = ExpressionCacheService.Save(cacheKey, cacheValue, 60 * 12);
					if (task.Wait(_cacheTimeout))
						return $"{cacheKey}";
					else
						ConsoleLogger.Write($"ERROR: CACHE SaveExpression - Redis TimeOut");
				}
			}
			catch (Exception ex)
			{
				ConsoleLogger.Write($"ERROR: CACHE SaveExpression - {ex.ToString()}");
			}

			return null;
		}

		public static List<BsonDocument> GetExpression(string key)
		{
			try
			{
				if (ExpressionCacheService == null || !ExpressionCacheService.IsConnectionActive())
					InitPropertyCacheConnection();

				if (isExpressionCacheEnabled)
				{
					var objectHashKey = HashUrl.GetHashAsString(key);

					var task = ExpressionCacheService.Get(objectHashKey);
					if (task.Wait(_cacheTimeout))
						return JsonConvert.DeserializeObject<List<BsonDocument>>(task.Result);
					else
					{
						ConsoleLogger.Write($"ERROR: CACHE GetExpression - Redis TimeOut");
					}
				}
			}
			catch (Exception ex)
			{
				ConsoleLogger.Write($"ERROR: CACHE GetExpression - {ex.ToString()}");
			}
			return null;
		}
		#endregion
	}
}