using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace KitsuneLayoutManager.Helper.CacheHandler
{
	public class RedisCacheService
	{
		private IDatabase Cache;
		private ConnectionMultiplexer ConnectionMultiplexer;
		private static TimeSpan _timeout = TimeSpan.FromMilliseconds(1000);


		internal RedisCacheService(string host, int port)
		{
			if (!String.IsNullOrEmpty(host))
			{
				InitiateConnection(host);
			}
		}

		public bool IsConnectionActive()
		{
			if (Cache != null && ConnectionMultiplexer != null)
				return ConnectionMultiplexer.IsConnected;

			return false;
		}

		void InitiateConnection(string host = null)
		{
			try
			{
				if (Cache == null || !ConnectionMultiplexer.IsConnected)
				{
					if (!String.IsNullOrEmpty(host))
					{
						ConnectionMultiplexer = ConnectionMultiplexer.Connect(host);
						Cache = ConnectionMultiplexer.GetDatabase();
					}
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public bool Exists(string key)
		{
			try
			{
				if (!ConnectionMultiplexer.IsConnected || Cache == null)
					InitiateConnection();

				if (!String.IsNullOrEmpty(key))
				{
					key = key.ToUpper();

					return Cache.KeyExists(key);
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
			return false;
		}

		public async Task<bool> Exists(string hashSetKey, string objectKey)
		{
			try
			{
				if (!ConnectionMultiplexer.IsConnected || Cache == null)
					InitiateConnection();

				if (!String.IsNullOrEmpty(hashSetKey) && !String.IsNullOrEmpty(objectKey))
				{
					hashSetKey = hashSetKey.ToUpper();
					objectKey = objectKey.ToUpper();

					return await Cache.HashExistsAsync(hashSetKey, objectKey);
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
			return false;
		}

		public async Task<string> Get(string key)
		{
			try
			{
				if (!ConnectionMultiplexer.IsConnected || Cache == null)
					InitiateConnection();

				if (!String.IsNullOrEmpty(key))
				{
					key = key.ToUpper();
					return await Cache.StringGetAsync(key);
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
			return null;
		}

		public string Get(string hashSetKey, string objectKey)
		{
			try
			{
				if (!ConnectionMultiplexer.IsConnected || Cache == null)
					InitiateConnection();

				if (!String.IsNullOrEmpty(hashSetKey) && !String.IsNullOrEmpty(objectKey))
				{
					hashSetKey = hashSetKey.ToUpper();
					objectKey = objectKey.ToUpper();

					return Cache.HashGet(hashSetKey, objectKey);
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
			return null;
		}

		public dynamic GetRedisValue(string hashSetKey, string objectKey)
		{
			try
			{
				if (!ConnectionMultiplexer.IsConnected || Cache == null)
					InitiateConnection();

				if (!String.IsNullOrEmpty(hashSetKey) && !String.IsNullOrEmpty(objectKey))
				{
					hashSetKey = hashSetKey.ToUpper();
					objectKey = objectKey.ToUpper();

					return Cache.HashGet(hashSetKey, objectKey);
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
			return null;
		}

		public async Task Save(string key, string value, long ttl = -1)
		{
			try
			{
				if (!ConnectionMultiplexer.IsConnected || Cache == null)
					InitiateConnection();

				if (!String.IsNullOrEmpty(key) && !String.IsNullOrEmpty(value))
				{
					key = key.ToUpper();

					var task = await Cache.StringSetAsync(key, value);

					if (ttl > -1)
					{
						DateTime expireTime = DateTime.Now;
						await Cache.KeyExpireAsync(key, expireTime.AddMinutes(ttl));
					}
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public async Task Save(string hashSetKey, string objectKey, string value, long ttl = -1)
		{
			try
			{
				if (!ConnectionMultiplexer.IsConnected || Cache == null)
					InitiateConnection();

				if (!String.IsNullOrEmpty(hashSetKey) && !String.IsNullOrEmpty(objectKey) && !String.IsNullOrEmpty(value))
				{
					hashSetKey = hashSetKey.ToUpper();
					objectKey = objectKey.ToUpper();

					var task = await Cache.HashSetAsync(hashSetKey, objectKey, value);

					if (ttl > -1)
					{
						DateTime expireTime = DateTime.Now;
						await Cache.KeyExpireAsync(objectKey, expireTime.AddMinutes(ttl));
					}
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public async Task Save(string hashSetKey, string objectKey, RedisValue value, long ttl = -1)
		{
			try
			{
				if (!ConnectionMultiplexer.IsConnected || Cache == null)
					InitiateConnection();

				if (!String.IsNullOrEmpty(hashSetKey) && !String.IsNullOrEmpty(objectKey) &&
					!String.IsNullOrEmpty(value))
				{
					hashSetKey = hashSetKey.ToUpper();
					objectKey = objectKey.ToUpper();

					var task = await Cache.HashSetAsync(hashSetKey, objectKey, value);

					// Populate my result object with the necessary values
					if (ttl > -1)
					{
						DateTime expireTime = DateTime.Now;
						await Cache.KeyExpireAsync(objectKey, expireTime.AddMinutes(ttl));
					}
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public void RemoveAllKeysInHash(string hashSetKey)
		{
			try
			{
				if (!ConnectionMultiplexer.IsConnected || Cache == null)
					InitiateConnection();

				if (!String.IsNullOrEmpty(hashSetKey))
				{
					hashSetKey = hashSetKey.ToUpper();
					Cache.KeyDelete(hashSetKey);
					
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public void Remove(string key)
		{
			try
			{
				if (!ConnectionMultiplexer.IsConnected || Cache == null)
					InitiateConnection();

				if (!String.IsNullOrEmpty(key))
				{
					key = key.ToUpper();
					Cache.KeyDeleteAsync(key);
				}
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public void RemovePattern(string patternstring)
		{
			try
			{
				if (!string.IsNullOrEmpty(patternstring))
				{
					if (!ConnectionMultiplexer.IsConnected || Cache == null)
						InitiateConnection();

					var endpoints = ConnectionMultiplexer.GetEndPoints();
					Parallel.ForEach(endpoints, endpoint =>
					{
						var server = ConnectionMultiplexer.GetServer(endpoint);

						Parallel.ForEach(server.Keys(pattern: patternstring + "*"), key =>
						{
							Cache.KeyDelete(key);
						});
					});
				}
			}
			catch { }
		}

		public void Clear()
		{
			try
			{
				if (!ConnectionMultiplexer.IsConnected || Cache == null)
					InitiateConnection();

				var endpoints = ConnectionMultiplexer.GetEndPoints();
				foreach (var endpoint in endpoints)
				{
					var server = ConnectionMultiplexer.GetServer(endpoint);

					Parallel.ForEach(server.Keys(), key =>
					{
						ConsoleLogger.Write(key);
						Cache.KeyDelete(key);
					});
				}
				//if (!server.IsSlave)
				//{
				//    server.FlushAllDatabasesAsync();
				//}
				//server.FlushAllDatabasesAsync();                
			}
			catch (Exception ex)
			{
				if (Cache == null)
					InitiateConnection();

				throw ex;
				//Cache.KeyDeleteAsync("*");
			}
			//Cache.KeyDelete("*");
		}

		public void Dispose()
		{
			if (ConnectionMultiplexer.IsConnected)
				ConnectionMultiplexer.Dispose();
		}
	}
}